// ─────────────────────────────────────────────────────────────────────────────
// 04 · Deck Synergy Web — the deck as a PORT graph (exploiter view).
//
// One node per PORTS row {card, in:consume | null, out:emit | null, pkg}. Nodes
// live in a columnar lattice: COLUMNS = supergroups (PORT_PKG, indexed by pkg),
// ROWS within a column = consume-subgroups keyed by subKey(p) = p.in ?? "§src"
// (the "source / no-consume" band sorts LAST). Every emit→consume wire is routed
// out the source column's RIGHT channel, DOWN to a staggered bottom bus, ACROSS,
// then UP the target column's LEFT channel — so no wire ever crosses behind a
// node. Ported from the concept canvas's imperative `drawSynergyWeb`; layout and
// drag-reorder are plain Math + React state + SVG pointer events (no d3).
// ─────────────────────────────────────────────────────────────────────────────

import { useEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import {
  PORTS,
  PORT_PKG,
  SYNERGY_SPARSE,
  ORACLE,
  famHue,
  subsumes,
  type Port,
} from "../data/mock";
import { SegControl, SectionHead } from "../components/primitives";
import { CardLink } from "../components/CardLink";
import {
  orthoPath,
  arrowHeadPath,
  rotate,
  endAngle,
  type Pt,
} from "../lib/ortho";

// ── layout constants ─────────────────────────────────────────────────────────
const VW = 1240;
const VH = 700;
const CHIP_W = 220;
const CHIP_H = 58;
const TOP = 122;
const ROW_GAP = 72;
const SUB_GAP = 20;
const SLOT_X = [160, 480, 800, 1080];
const GRID_BOTTOM = VH - 56; // 644

type SynState = "empty" | "loading" | "sparse" | "dense";

interface SynOrder {
  cols: number[];
  subs: Record<number, string[]>;
  ports: Record<string, number[]>;
}

interface Placed extends Port {
  idx: number;
  x: number;
  y: number;
  pk: number;
  sg: string;
}

interface Band {
  pk: number;
  sg: string;
  x: number;
  yLabel: number;
  yMid: number;
  count: number;
}

interface EdgeR {
  s: Placed;
  t: Placed;
  kind: "direct" | "super";
  fam: string;
}

type Drag =
  | { kind: "col"; pk: number; px: number; py: number }
  | { kind: "sub"; pk: number; sg: string; px: number; py: number }
  | { kind: "chip"; idx: number; pk: number; sg: string; px: number; py: number }
  | null;

// ── keys / helpers (pure) ────────────────────────────────────────────────────
const subKey = (p: Port): string => p.in ?? "§src";
const subLabel = (sg: string): string => (sg === "§src" ? "source (no input)" : "consume " + sg);
const subHue = (sg: string): string => (sg === "§src" ? "#5b5f70" : famHue(sg));
const collate = (s: string): string => (s === "§src" ? "￿" : s);

/** The natural (undragged) lattice order, derived from PORTS. */
function buildOrder(): SynOrder {
  const cols = [...new Set(PORTS.map((p) => p.pkg))];
  const subs: Record<number, string[]> = {};
  const ports: Record<string, number[]> = {};
  cols.forEach((pk) => {
    const ps = PORTS.map((p, i) => ({ p, i })).filter((o) => o.p.pkg === pk);
    const sl = [...new Set(ps.map((o) => subKey(o.p)))].sort((a, b) =>
      collate(a).localeCompare(collate(b)),
    );
    subs[pk] = sl;
    sl.forEach((sg) => {
      ports[pk + "|" + sg] = ps.filter((o) => subKey(o.p) === sg).map((o) => o.i);
    });
  });
  return { cols, subs, ports };
}

/** ORACLE lookup tolerant of PORTS short-names (e.g. "Chatterfang"). */
function resolveOracle(card: string): (typeof ORACLE)[string] | undefined {
  if (ORACLE[card]) return ORACLE[card];
  const key = Object.keys(ORACLE).find((k) => k.startsWith(card) || card.startsWith(k));
  return key ? ORACLE[key] : undefined;
}

const hlStyle = (fam: string): CSSProperties => ({
  background: `color-mix(in srgb, ${famHue(fam)} 22%, transparent)`,
  borderBottom: `1.5px solid ${famHue(fam)}`,
  borderRadius: 2,
  padding: "0 2px",
  color: "#eef",
});

/** The port's own oracle clause(s), highlighted — rendered inside the node. */
function portClause(p: Placed): ReactNode {
  const o = resolveOracle(p.card);
  if (o) {
    const c = p.in ? o.segs.find((z) => z.role === "consume" && z.fam === p.in) : undefined;
    const e = p.out ? o.segs.find((z) => z.role === "emit" && z.fam === p.out) : undefined;
    if (c || e) {
      return (
        <>
          {c && <span style={hlStyle(p.in as string)}>{c.t}</span>}
          {c && e && <span style={{ color: "#75798c" }}> ▸ </span>}
          {e && <span style={hlStyle(p.out as string)}>{e.t}</span>}
        </>
      );
    }
  }
  // No oracle span — fall back to the family labels, in mono.
  return (
    <span style={{ fontFamily: "var(--font-mono)", color: "#9aa0b4" }}>
      consume:{p.in ?? "—"} ▸ emit:{p.out ?? "—"}
    </span>
  );
}

/** A colored (emit/consume) or hollow (null) presence dot. */
function dot(fam: string | null): ReactNode {
  return fam ? (
    <span
      style={{ width: 7, height: 7, borderRadius: "50%", background: famHue(fam), display: "inline-block" }}
    />
  ) : (
    <span
      style={{
        width: 7,
        height: 7,
        borderRadius: "50%",
        border: "1px solid #5b5f70",
        boxSizing: "border-box",
        display: "inline-block",
      }}
    />
  );
}

const SEG_OPTS: { value: SynState; label: string }[] = [
  { value: "empty", label: "empty" },
  { value: "loading", label: "loading" },
  { value: "sparse", label: "sparse" },
  { value: "dense", label: "dense" },
];

export default function SynergyWeb() {
  const [state, setState] = useState<SynState>("dense");
  const [order, setOrder] = useState<SynOrder>(() => buildOrder());
  const [drag, setDrag] = useState<Drag>(null);
  const svgRef = useRef<SVGSVGElement | null>(null);

  // Reorder state is persistent across drags, but recomputed when the deck
  // state (sparse/dense) changes — a fresh lattice for a fresh port set.
  useEffect(() => {
    setOrder(buildOrder());
    setDrag(null);
  }, [state]);

  // viewBox coords from a pointer event.
  const svgCoords = (e: { clientX: number; clientY: number }): Pt => {
    const svg = svgRef.current;
    if (!svg) return [0, 0];
    const r = svg.getBoundingClientRect();
    return [((e.clientX - r.left) / r.width) * VW, ((e.clientY - r.top) / r.height) * VH];
  };

  // ── visible ports (sparse trims the deck) ──────────────────────────────────
  const visSet = new Set<number>();
  PORTS.forEach((p, i) => {
    if (state !== "sparse" || SYNERGY_SPARSE.has(p.card)) visSet.add(i);
  });

  // ── columns → compacted slots ──────────────────────────────────────────────
  const colSlot: Record<number, number> = {};
  let slot = 0;
  order.cols.forEach((pk) => {
    if (PORTS.some((p, i) => p.pkg === pk && visSet.has(i))) colSlot[pk] = slot++;
  });
  const nSlots = slot;
  const nearestSlot = (px: number): number => {
    let k = 0;
    let best = 1e9;
    for (let j = 0; j < nSlots; j++) {
      const d = Math.abs(SLOT_X[j] - px);
      if (d < best) {
        best = d;
        k = j;
      }
    }
    return k;
  };

  // ── place ports; record subgroup bands ─────────────────────────────────────
  const placed: Placed[] = [];
  const bands: Band[] = [];
  order.cols.forEach((pk) => {
    if (colSlot[pk] === undefined) return;
    const x = SLOT_X[colSlot[pk]];
    let y = TOP;
    (order.subs[pk] ?? []).forEach((sg) => {
      const list = (order.ports[pk + "|" + sg] ?? []).filter((i) => visSet.has(i));
      if (!list.length) return;
      bands.push({ pk, sg, x, yLabel: y - CHIP_H / 2 - 6, yMid: y, count: list.length });
      list.forEach((i) => {
        placed.push({ ...PORTS[i], idx: i, x, y, pk, sg });
        y += ROW_GAP;
      });
      y += SUB_GAP;
    });
  });

  // ── channels (midpoints between adjacent column edges; ±24 at the ends) ─────
  const colLeft = SLOT_X.map((x) => x - CHIP_W / 2);
  const colRight = SLOT_X.map((x) => x + CHIP_W / 2);
  const chL: number[] = [];
  const chR: number[] = [];
  for (let k = 0; k < nSlots; k++) {
    chL[k] = k === 0 ? colLeft[0] - 24 : (colRight[k - 1] + colLeft[k]) / 2;
    chR[k] = k === nSlots - 1 ? colRight[k] + 24 : (colRight[k] + colLeft[k + 1]) / 2;
  }

  // ── edges (DIRECT t.in===s.out, or SUPER subsumes(t.in,s.out)) ─────────────
  const deg: Record<number, number> = {};
  placed.forEach((p) => (deg[p.idx] = 0));
  const edges: EdgeR[] = [];
  placed.forEach((s) => {
    if (!s.out) return;
    const so = s.out;
    placed.forEach((t) => {
      const ti = t.in;
      if (s.idx === t.idx || !ti) return;
      const direct = ti === so;
      const sup = !direct && subsumes(ti, so);
      if (direct || sup) {
        edges.push({ s, t, kind: direct ? "direct" : "super", fam: so });
        deg[s.idx]++;
        deg[t.idx]++;
      }
    });
  });

  // ── drag commit / start / move ─────────────────────────────────────────────
  const startDrag = (e: React.PointerEvent, d: Drag) => {
    e.stopPropagation();
    svgRef.current?.setPointerCapture(e.pointerId);
    setDrag(d);
  };

  const onMove = (e: React.PointerEvent) => {
    if (!drag) return;
    const [px, py] = svgCoords(e);
    setDrag({ ...drag, px, py });
  };

  const onUp = (e: React.PointerEvent) => {
    if (!drag) return;
    const [px, py] = svgCoords(e);
    if (drag.kind === "col") {
      const k = nearestSlot(px);
      const vis = order.cols.filter((c) => colSlot[c] !== undefined);
      const hidden = order.cols.filter((c) => colSlot[c] === undefined);
      const arr = vis.filter((c) => c !== drag.pk);
      arr.splice(Math.min(k, arr.length), 0, drag.pk);
      setOrder({ ...order, cols: [...arr, ...hidden] });
    } else if (drag.kind === "sub") {
      const others = bands.filter((z) => z.pk === drag.pk && z.sg !== drag.sg);
      let ti = 0;
      others.forEach((z) => {
        if (z.yMid < py) ti++;
      });
      const rest = (order.subs[drag.pk] ?? []).filter((s) => s !== drag.sg);
      rest.splice(Math.min(ti, rest.length), 0, drag.sg);
      setOrder({ ...order, subs: { ...order.subs, [drag.pk]: rest } });
    } else if (drag.kind === "chip") {
      const key = drag.pk + "|" + drag.sg;
      const sib = placed.filter((q) => q.pk === drag.pk && q.sg === drag.sg && q.idx !== drag.idx);
      let ti = 0;
      sib.forEach((q) => {
        if (q.y < py) ti++;
      });
      const rest = (order.ports[key] ?? []).filter((x) => x !== drag.idx);
      rest.splice(Math.min(ti, rest.length), 0, drag.idx);
      setOrder({ ...order, ports: { ...order.ports, [key]: rest } });
    }
    setDrag(null);
  };

  // ── drop-indicator geometry (from live drag) ───────────────────────────────
  const colBottom = (pk: number): number => {
    let b = -1e9;
    placed.forEach((p) => {
      if (p.pkg === pk) b = Math.max(b, p.y + CHIP_H / 2);
    });
    return b > -1e9 ? b : VH - 90;
  };
  const subInsertY = (pk: number, sg: string, py: number): number => {
    const others = bands.filter((z) => z.pk === pk && z.sg !== sg);
    if (!others.length) return TOP - 12;
    let ti = 0;
    others.forEach((z) => {
      if (z.yMid < py) ti++;
    });
    return ti >= others.length
      ? others[others.length - 1].yMid + ROW_GAP / 2
      : others[ti].yMid - ROW_GAP / 2;
  };
  const chipInsertY = (p: Placed, py: number): number => {
    const sib = placed.filter((q) => q.pk === p.pk && q.sg === p.sg && q.idx !== p.idx);
    if (!sib.length) return p.y;
    let ti = 0;
    sib.forEach((q) => {
      if (q.y < py) ti++;
    });
    return ti >= sib.length ? sib[sib.length - 1].y + ROW_GAP / 2 : sib[ti].y - ROW_GAP / 2;
  };

  const supN = edges.filter((e) => e.kind === "super").length;

  // ── background grid (shared by all states) ─────────────────────────────────
  const grid: ReactNode[] = [];
  for (let gx = 0; gx <= VW; gx += 42)
    grid.push(<line key={`gx${gx}`} x1={gx} y1={0} x2={gx} y2={VH} stroke="#161826" strokeWidth={1} />);
  for (let gy = 0; gy <= VH; gy += 42)
    grid.push(<line key={`gy${gy}`} x1={0} y1={gy} x2={VW} y2={gy} stroke="#161826" strokeWidth={1} />);

  return (
    <div className="view-grid">
      <SectionHead kicker="04 · Exploit" title="Deck Synergy Web">
        The deck as a port graph — one node per consume ▸ emit — grouped supergroup columns × subgroup
        rows, wired emit→consume through a collision-free bus. Drag columns, bands, or ports to reorder.
      </SectionHead>

      <SegControl options={SEG_OPTS} value={state} onChange={setState} />

      <div className="panel panel-svg">
        <svg
          ref={svgRef}
          viewBox={`0 0 ${VW} ${VH}`}
          width="100%"
          role="img"
          aria-label="deck synergy port graph"
          style={{ touchAction: "none", userSelect: "none" }}
          onPointerMove={onMove}
          onPointerUp={onUp}
          onPointerCancel={() => setDrag(null)}
        >
          {grid}

          {state === "empty" && (
            <text
              x={VW / 2}
              y={VH / 2}
              textAnchor="middle"
              fill="#75798c"
              fontSize={16}
              fontFamily="var(--font-mono)"
            >
              No deck loaded
            </text>
          )}

          {state === "loading" &&
            Array.from({ length: 18 }, (_, i) => {
              const a = (i / 18) * Math.PI * 2;
              return (
                <circle
                  key={i}
                  cx={VW / 2 + Math.cos(a) * 200}
                  cy={VH / 2 + Math.sin(a) * 150}
                  r={7}
                  fill="#23252f"
                >
                  <animate
                    attributeName="opacity"
                    values="0.25;1;0.25"
                    dur="1.2s"
                    begin={`${i * 0.05}s`}
                    repeatCount="indefinite"
                  />
                </circle>
              );
            })}

          {(state === "sparse" || state === "dense") && (
            <>
              {/* ── edges: right channel → bottom bus → left channel ── */}
              {edges.map((e, i) => {
                const hue = famHue(e.fam);
                const sk = colSlot[e.s.pkg];
                const tk = colSlot[e.t.pkg];
                const sRight = colRight[sk];
                const tLeft = colLeft[tk];
                const sCh = chR[sk];
                const tCh = chL[tk];
                const busY = GRID_BOTTOM + (i % 8) * 6;
                const pts: Pt[] = [
                  [sRight, e.s.y],
                  [sCh, e.s.y],
                  [sCh, busY],
                  [tCh, busY],
                  [tCh, e.t.y],
                  [tLeft, e.t.y],
                ];
                const ang = endAngle(pts);
                const c2 = pts[pts.length - 2];
                const p1 = pts[pts.length - 1];
                const L = Math.hypot(p1[0] - c2[0], p1[1] - c2[1]) || 1;
                const tt = Math.max(0.2, (L - 9) / L);
                const ax = c2[0] + (p1[0] - c2[0]) * tt;
                const ay = c2[1] + (p1[1] - c2[1]) * tt;
                const key = `${e.s.idx}-${e.t.idx}-${e.kind}`;
                return (
                  <g key={key}>
                    <path
                      d={orthoPath(pts)}
                      fill="none"
                      stroke={hue}
                      strokeWidth={e.kind === "super" ? 1.5 : 2}
                      strokeLinejoin="round"
                      strokeDasharray={e.kind === "super" ? "3 4" : undefined}
                      opacity={e.kind === "super" ? 0.5 : 0.9}
                    >
                      <title>
                        {`${e.s.card}  emit:${e.fam}  →  ${e.t.card}  consume:${e.t.in}` +
                          (e.kind === "super"
                            ? `   (${e.fam} ⊂ ${e.t.in} — counted via supergroup)`
                            : "")}
                      </title>
                    </path>
                    <circle cx={sRight} cy={e.s.y} r={2.4} fill={hue} opacity={0.9} />
                    <path
                      d={arrowHeadPath(4)}
                      transform={rotate(ax, ay, ang)}
                      fill={hue}
                      opacity={e.kind === "super" ? 0.65 : 0.95}
                    />
                    {e.kind === "super" && (
                      <text
                        x={(sCh + tCh) / 2}
                        y={busY - 3}
                        textAnchor="middle"
                        fontSize={10}
                        fill={famHue(e.t.in)}
                        fontFamily="var(--font-mono)"
                      >
                        ⊃
                      </text>
                    )}
                  </g>
                );
              })}

              {/* ── column headers (drag left/right to reorder supergroups) ── */}
              {order.cols.map((pk) => {
                if (colSlot[pk] === undefined) return null;
                const x = SLOT_X[colSlot[pk]];
                return (
                  <g
                    key={`hdr${pk}`}
                    style={{ cursor: "grab" }}
                    opacity={drag?.kind === "col" && drag.pk === pk ? 0.28 : 1}
                    onPointerDown={(e) => {
                      const [px, py] = svgCoords(e);
                      startDrag(e, { kind: "col", pk, px, py });
                    }}
                  >
                    <rect
                      x={x - CHIP_W / 2}
                      y={46}
                      width={CHIP_W}
                      height={40}
                      rx={6}
                      fill="#12131f"
                      stroke="#23252f"
                      strokeWidth={1}
                    />
                    <text
                      x={x}
                      y={60}
                      textAnchor="middle"
                      fontSize={8}
                      fontFamily="var(--font-mono)"
                      fill="#5b5f70"
                      letterSpacing="0.1em"
                    >
                      ⣿ SUPERGROUP
                    </text>
                    <text
                      x={x}
                      y={77}
                      textAnchor="middle"
                      fontSize={12}
                      fontFamily="var(--font-mono)"
                      fill="#cfd3e5"
                    >
                      {PORT_PKG[pk]}
                    </text>
                  </g>
                );
              })}

              {/* ── subgroup labels (drag up/down within a supergroup) ── */}
              {bands.map((b) => (
                <text
                  key={`band${b.pk}-${b.sg}`}
                  x={b.x - CHIP_W / 2 + 2}
                  y={b.yLabel}
                  fontSize={7.5}
                  fontFamily="var(--font-mono)"
                  fill={subHue(b.sg)}
                  letterSpacing="0.06em"
                  style={{ cursor: "grab" }}
                  opacity={drag?.kind === "sub" && drag.pk === b.pk && drag.sg === b.sg ? 0.3 : 1}
                  onPointerDown={(e) => {
                    const [px, py] = svgCoords(e);
                    startDrag(e, { kind: "sub", pk: b.pk, sg: b.sg, px, py });
                  }}
                >
                  {"⣿ SUB · " + subLabel(b.sg).toUpperCase()}
                </text>
              ))}

              {/* ── port chips (drag to reorder within a band) ── */}
              {placed.map((p) => {
                const x0 = p.x - CHIP_W / 2;
                const y0 = p.y - CHIP_H / 2;
                const dragging = drag?.kind === "chip" && drag.idx === p.idx;
                const dy = dragging ? (drag as Extract<Drag, { kind: "chip" }>).py - p.y : 0;
                return (
                  <g
                    key={`chip${p.idx}`}
                    transform={dy ? `translate(0,${dy})` : undefined}
                    opacity={dragging ? 0.9 : 1}
                    style={{ cursor: "grab" }}
                    onPointerDown={(e) => {
                      const [px, py] = svgCoords(e);
                      startDrag(e, { kind: "chip", idx: p.idx, pk: p.pk, sg: p.sg, px, py });
                    }}
                  >
                    <rect
                      x={x0}
                      y={y0}
                      width={CHIP_W}
                      height={CHIP_H}
                      rx={7}
                      fill="#141522"
                      stroke={dragging ? "#9184d9" : "#2c2f3d"}
                      strokeWidth={dragging ? 1.8 : 1.2}
                    >
                      <title>
                        {`${p.card} · consume ${p.in ?? "—"} → emit ${p.out ?? "—"} · ${deg[p.idx]} links · drag to reorder`}
                      </title>
                    </rect>
                    <foreignObject
                      x={x0}
                      y={y0}
                      width={CHIP_W}
                      height={CHIP_H}
                      style={{ pointerEvents: "none" }}
                    >
                      <div
                        style={{
                          height: "100%",
                          boxSizing: "border-box",
                          padding: "5px 9px",
                          fontFamily: "Inter, system-ui, sans-serif",
                          overflow: "hidden",
                        }}
                      >
                        <div
                          style={{
                            display: "flex",
                            alignItems: "center",
                            justifyContent: "space-between",
                            gap: 6,
                            marginBottom: 2,
                          }}
                        >
                          <span
                            style={{
                              fontSize: 10,
                              fontWeight: 600,
                              color: "#e9e9ed",
                              whiteSpace: "nowrap",
                              overflow: "hidden",
                              textOverflow: "ellipsis",
                            }}
                          >
                            <CardLink name={p.card} />
                          </span>
                          <span style={{ display: "flex", alignItems: "center", gap: 3, flex: "none" }}>
                            {dot(p.in)}
                            <span style={{ color: "#75798c", fontSize: 9 }}>▸</span>
                            {dot(p.out)}
                          </span>
                        </div>
                        <div
                          style={{
                            fontSize: 9.5,
                            lineHeight: 1.5,
                            color: "#cfd3e5",
                            display: "-webkit-box",
                            WebkitLineClamp: 2,
                            WebkitBoxOrient: "vertical",
                            overflow: "hidden",
                          }}
                        >
                          {portClause(p)}
                        </div>
                      </div>
                    </foreignObject>
                  </g>
                );
              })}

              {/* ── ghost + drop-indicator overlay ── */}
              {drag && (
                <g pointerEvents="none">
                  {drag.kind === "col" &&
                    (() => {
                      const cb = colBottom(drag.pk);
                      const ix = SLOT_X[nearestSlot(drag.px)] - CHIP_W / 2 - 11;
                      return (
                        <>
                          <rect
                            x={drag.px - CHIP_W / 2}
                            y={46}
                            width={CHIP_W}
                            height={cb - 46 + 8}
                            rx={8}
                            fill="color-mix(in srgb, #9184d9 15%, transparent)"
                            stroke="#9184d9"
                            strokeWidth={1.5}
                            strokeDasharray="5 3"
                          />
                          <line
                            x1={ix}
                            y1={42}
                            x2={ix}
                            y2={cb + 8}
                            stroke="#b5abfc"
                            strokeWidth={3}
                            strokeLinecap="round"
                          />
                          <text
                            x={ix}
                            y={36}
                            textAnchor="middle"
                            fontSize={9}
                            fontFamily="var(--font-mono)"
                            fill="#b5abfc"
                          >
                            {"▸ slot " + (nearestSlot(drag.px) + 1)}
                          </text>
                        </>
                      );
                    })()}

                  {drag.kind === "sub" &&
                    (() => {
                      const band = bands.find((z) => z.pk === drag.pk && z.sg === drag.sg);
                      const bx = band ? band.x : SLOT_X[colSlot[drag.pk] ?? 0];
                      const iy = subInsertY(drag.pk, drag.sg, drag.py);
                      return (
                        <line
                          x1={bx - CHIP_W / 2 - 6}
                          y1={iy}
                          x2={bx + CHIP_W / 2 + 6}
                          y2={iy}
                          stroke="#b5abfc"
                          strokeWidth={3}
                          strokeLinecap="round"
                        />
                      );
                    })()}

                  {drag.kind === "chip" &&
                    (() => {
                      const p = placed.find((q) => q.idx === drag.idx);
                      if (!p) return null;
                      const iy = chipInsertY(p, drag.py);
                      return (
                        <line
                          x1={p.x - CHIP_W / 2}
                          y1={iy}
                          x2={p.x + CHIP_W / 2}
                          y2={iy}
                          stroke="#b5abfc"
                          strokeWidth={3}
                          strokeLinecap="round"
                        />
                      );
                    })()}
                </g>
              )}

              {/* footnote: super-edge count */}
              <text
                x={16}
                y={VH - 12}
                fontSize={10}
                fontFamily="var(--font-mono)"
                fill="#5b5f70"
              >
                {`${placed.length} ports · ${edges.length} wires · ${supN} via supergroup`}
              </text>
            </>
          )}
        </svg>
      </div>
    </div>
  );
}
