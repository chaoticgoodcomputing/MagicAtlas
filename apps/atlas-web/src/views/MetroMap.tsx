// ─────────────────────────────────────────────────────────────────────────────
// Concept 01 · Metro Map — the Explorer hero.
//
// The 17-family resource graph rendered as a transit map: stations are families
// at their metro coordinates, lines are the directional emit→consume EDGES drawn
// as uniform-weight right-angle (cardinal) tracks. Ported from the concept
// canvas's `drawMetro` / `metroLegend`, but emitted as declarative JSX SVG with
// React state + SVG pointer events for the drag interactions (no d3, no
// imperative DOM). Data flows in through the `useFamilyGraph` seam.
// ─────────────────────────────────────────────────────────────────────────────

import { useCallback, useMemo, useRef, useState, type PointerEvent as ReactPointerEvent } from "react";
import { famHue, TIER, SPARSE_EDGES, edgeKey, type Tier } from "../data/mock";
import { useFamilyGraph } from "../data/atlas";
import { SegControl, SectionHead, FamilyDot, TierChip } from "../components/primitives";
import {
  orthoPts, orthoPath, endAngle, pointBackFromEnd, pointAt, arrowHeadPath, rotate,
} from "../lib/ortho";

// ── Local constants (visual only — all corpus data comes from the foundation) ──
const INK = "#0a0b12"; // --atlas-ink
const LW = 3.4; // line width
const R_RANGE: [number, number] = [5, 13]; // station radius range (sqrt scale)

type MetroState = "empty" | "loading" | "sparse" | "dense";

const STATES: { value: MetroState; label: string }[] = [
  { value: "empty", label: "empty" },
  { value: "loading", label: "loading" },
  { value: "sparse", label: "sparse" },
  { value: "dense", label: "dense" },
];

interface XY { x: number; y: number }

/** Drag session held in a ref so pointer-move math never chases stale state. */
type DragSeed =
  | { kind: "station"; key: string }
  | { kind: "edge"; key: string; horiz: boolean };
type Drag = DragSeed & { lastX: number; lastY: number };

export default function MetroMap() {
  const { data, loading, error } = useFamilyGraph();
  const { families, keys, edges: allEdges } = data;

  const [view, setView] = useState<MetroState>("dense");
  const [metroPos, setMetroPos] = useState<Record<string, XY>>(() => {
    const p: Record<string, XY> = {};
    for (const k of keys) p[k] = { x: families[k].x, y: families[k].y };
    return p;
  });
  const [edgeOff, setEdgeOff] = useState<Record<string, number>>({});
  const [hover, setHover] = useState<string | null>(null);

  const svgRef = useRef<SVGSVGElement | null>(null);
  const dragRef = useRef<Drag | null>(null);
  // Effective corridor offset per visible edge, refreshed each render so the
  // drag handler always shifts from the value currently on screen.
  const offMapRef = useRef<Record<string, number>>({});

  // Hook loading / error take precedence over the segmented control.
  const state: MetroState = error ? "empty" : loading ? "loading" : view;

  // ── sqrt radius scale (domain = corpus min/max cards, range = R_RANGE) ──────
  const [dMin, dMax] = useMemo<[number, number]>(() => {
    let lo = Infinity, hi = -Infinity;
    for (const k of keys) { const c = families[k].cards; if (c < lo) lo = c; if (c > hi) hi = c; }
    return [lo, hi];
  }, [families, keys]);

  const radius = useCallback((cards: number): number => {
    const [r0, r1] = R_RANGE;
    const span = Math.sqrt(dMax) - Math.sqrt(dMin);
    const t = span === 0 ? 0 : (Math.sqrt(cards) - Math.sqrt(dMin)) / span;
    const clamped = Math.max(0, Math.min(1, t));
    return r0 + (r1 - r0) * clamped;
  }, [dMin, dMax]);

  // ── which edges are visible in the current state ────────────────────────────
  const edges = useMemo(() => {
    if (state === "sparse") return allEdges.filter((e) => SPARSE_EDGES.has(edgeKey(e)));
    if (state === "dense") return allEdges;
    return [];
  }, [state, allEdges]);

  const usedFam = useMemo(() => {
    const s = new Set<string>();
    for (const e of edges) { s.add(e.from); s.add(e.to); }
    return s;
  }, [edges]);

  // ── pointer → viewBox conversion (client px → 0..1120 / 0..660 units) ────────
  const clientToVB = useCallback((clientX: number, clientY: number): XY => {
    const svg = svgRef.current;
    const ctm = svg?.getScreenCTM();
    if (!svg || !ctm) return { x: 0, y: 0 };
    const p = new DOMPoint(clientX, clientY).matrixTransform(ctm.inverse());
    return { x: p.x, y: p.y };
  }, []);

  const beginDrag = useCallback((ev: ReactPointerEvent, seed: DragSeed) => {
    ev.stopPropagation();
    const { x, y } = clientToVB(ev.clientX, ev.clientY);
    dragRef.current = { ...seed, lastX: x, lastY: y };
    svgRef.current?.setPointerCapture(ev.pointerId);
  }, [clientToVB]);

  const onPointerMove = useCallback((ev: ReactPointerEvent) => {
    const d = dragRef.current;
    if (!d) return;
    const { x, y } = clientToVB(ev.clientX, ev.clientY);
    const dx = x - d.lastX;
    const dy = y - d.lastY;
    d.lastX = x;
    d.lastY = y;
    if (d.kind === "station") {
      setMetroPos((prev) => ({ ...prev, [d.key]: { x: prev[d.key].x + dx, y: prev[d.key].y + dy } }));
    } else {
      const cur = offMapRef.current[d.key] ?? 0;
      setEdgeOff((prev) => ({ ...prev, [d.key]: cur + (d.horiz ? dx : dy) }));
    }
  }, [clientToVB]);

  const endDrag = useCallback((ev: ReactPointerEvent) => {
    if (dragRef.current) {
      dragRef.current = null;
      try { svgRef.current?.releasePointerCapture(ev.pointerId); } catch { /* capture already gone */ }
    }
  }, []);

  const tiers = Object.keys(TIER) as Tier[];

  return (
    <section>
      <SectionHead kicker="01 · Explorer" title="Metro Map">
        The {keys.length}-family resource graph as a transit map — directional emit→consume tracks,
        rules-vs-card origin, engine loops. Drag a station or a line to untangle.
      </SectionHead>

      <div style={{ display: "flex", justifyContent: "flex-end", marginBottom: 12 }}>
        <SegControl<MetroState> options={STATES} value={view} onChange={setView} />
      </div>

      <div className="panel">
        <svg
          ref={svgRef}
          className="panel-svg"
          viewBox="0 0 1120 660"
          width="100%"
          style={{ display: "block", touchAction: "none", userSelect: "none" }}
          onPointerMove={onPointerMove}
          onPointerUp={endDrag}
          onPointerCancel={endDrag}
        >
          {state === "empty" && (
            <text x={560} y={330} textAnchor="middle" fill="var(--atlas-muted)" fontSize={18}
              fontFamily="ui-monospace, Menlo, monospace">
              No parsed lines yet — run the flow engine.
            </text>
          )}

          {state === "loading" && keys.map((k, i) => {
            const f = families[k];
            return (
              <circle key={k} cx={f.x} cy={f.y} r={9} fill="#23252f"
                style={{ animation: "wsPulse 1.4s ease-in-out infinite", animationDelay: `${i * 0.05}s` }} />
            );
          })}

          {(state === "sparse" || state === "dense") && (
            <>
              {/* ── lines: dark casing + hue track + arrowhead + origin marker ── */}
              {edges.map((e, i) => {
                const a = metroPos[e.from];
                const b = metroPos[e.to];
                const hue = famHue(e.from);
                const T = TIER[e.tier];
                const key = edgeKey(e);
                const defaultOff = ((i * 29) % 5 - 2) * 11;
                const off = edgeOff[key] ?? defaultOff;
                offMapRef.current[key] = off;
                const horiz = Math.abs(b.x - a.x) >= Math.abs(b.y - a.y);
                const pts = orthoPts(a.x, a.y, b.x, b.y, off);
                const d = orthoPath(pts);
                const head = pointBackFromEnd(pts, 16);
                const ang = endAngle(pts);
                const mid = pointAt(pts, 0.5);
                const hot = hover === key;
                return (
                  <g key={key}>
                    <path d={d} fill="none" stroke={INK} strokeWidth={LW + 3.5}
                      strokeLinejoin="round" strokeLinecap="round" style={{ pointerEvents: "none" }} />
                    <path
                      d={d} fill="none" stroke={hue}
                      strokeWidth={hot ? LW + 2.2 : LW}
                      strokeOpacity={T.op}
                      strokeDasharray={T.dash ?? undefined}
                      strokeLinejoin="round" strokeLinecap="round"
                      style={{ cursor: "grab" }}
                      onPointerDown={(ev) => beginDrag(ev, { kind: "edge", key, horiz })}
                      onPointerEnter={() => setHover(key)}
                      onPointerLeave={() => setHover((h) => (h === key ? null : h))}
                    >
                      <title>{`${e.from} → ${e.to} · ${e.combos.toLocaleString()} combos · ${e.tier}`}</title>
                    </path>
                    <path transform={rotate(head[0], head[1], ang)} d={arrowHeadPath()}
                      fill={hue} opacity={T.op} style={{ pointerEvents: "none" }} />
                    {e.engine ? (
                      <>
                        <circle cx={mid.x} cy={mid.y} r={8.5} fill="#12131f" stroke={hue}
                          strokeWidth={1.5} opacity={T.op} style={{ pointerEvents: "none" }} />
                        <text x={mid.x} y={mid.y + 3.5} textAnchor="middle" fontSize={11}
                          fill={hue} opacity={T.op} style={{ pointerEvents: "none" }}>⟳</text>
                      </>
                    ) : e.origin === "card" ? (
                      <rect x={mid.x - 4.5} y={mid.y - 6} width={9} height={12} rx={1.5}
                        fill="#12131f" stroke={hue} strokeWidth={1.4} opacity={T.op}
                        style={{ pointerEvents: "none" }} />
                    ) : null}
                  </g>
                );
              })}

              {/* ── stations: casing + hue-stroked circle + stroked label ── */}
              {keys.map((k) => {
                const f = families[k];
                const p = metroPos[k];
                const on = usedFam.has(k);
                const r = radius(f.cards);
                return (
                  <g key={k} opacity={on ? 1 : 0.22} style={{ cursor: "grab" }}
                    onPointerDown={(ev) => beginDrag(ev, { kind: "station", key: k })}>
                    <circle cx={p.x} cy={p.y} r={r + 3} fill={INK} />
                    <circle cx={p.x} cy={p.y} r={r} fill="#12131f" stroke={f.hue} strokeWidth={2.5} />
                    <text
                      x={p.x} y={p.y - r - 6} textAnchor="middle"
                      fontSize={12.5} fontWeight={600} fill="#e9e9ed"
                      stroke={INK} strokeWidth={3} paintOrder="stroke"
                      style={{ pointerEvents: "none" }}
                    >
                      {f.name}
                    </text>
                    <title>{`${f.name} · ${f.cards.toLocaleString()} cards · ${f.labels} port labels · drag to move`}</title>
                  </g>
                );
              })}
            </>
          )}
        </svg>
      </div>

      {/* ── legend band (ported from metroLegend) ── */}
      <div style={{
        display: "flex", flexWrap: "wrap", alignItems: "center", gap: "10px 16px",
        marginTop: 14, fontSize: 11, color: "var(--atlas-muted-2)",
      }}>
        {keys.map((k) => (
          <span key={k} style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
            <FamilyDot family={k} size={9} />{families[k].name}
          </span>
        ))}

        <span aria-hidden style={{ width: 1, height: 14, background: "var(--atlas-border)", margin: "0 2px" }} />

        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <svg width={26} height={10} aria-hidden>
            <path d="M0,5 L18,5" stroke="currentColor" strokeWidth={2} />
            <path d="M15,1 L23,5 L15,9 Z" fill="currentColor" />
          </svg>
          → direction · emit→consume
        </span>
        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <svg width={16} height={12} aria-hidden>
            <rect x={3} y={0.5} width={9} height={11} rx={1.5} fill="#12131f" stroke="currentColor" strokeWidth={1.4} />
          </svg>
          ◆ card-origin
        </span>
        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <svg width={20} height={10} aria-hidden>
            <path d="M1,5 L19,5" stroke="currentColor" strokeWidth={2} />
          </svg>
          ▬ rules-origin
        </span>
        <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
          <svg width={16} height={16} aria-hidden viewBox="0 0 16 16">
            <circle cx={8} cy={8} r={7} fill="#12131f" stroke="currentColor" strokeWidth={1.2} />
            <text x={8} y={11.5} textAnchor="middle" fontSize={10} fill="currentColor">⟳</text>
          </svg>
          ⟳ engine loop
        </span>

        <span aria-hidden style={{ width: 1, height: 14, background: "var(--atlas-border)", margin: "0 2px" }} />

        {tiers.map((t) => <TierChip key={t} tier={t} />)}
      </div>
    </section>
  );
}
