// ─────────────────────────────────────────────────────────────────────────────
// Concept 03 · Deck Lens — the exploiter hero ("killer feature").
//
// Paste a decklist → a directional port-coverage profile (with super/subgroup
// double-counts drawn as hollow stacked segments), the complete rings the deck
// already makes, and the near-miss closers one card away.
//
// Ported from the concept canvas's `drawCoverage` / `drawRings` / `drawDeck`:
// the coverage chart is emitted as JSX SVG (plain Math for the diverging scale,
// no d3), everything else is HTML. All data comes through `useDeckAnalysis`.
// ─────────────────────────────────────────────────────────────────────────────

import { useEffect, useRef, useState, type CSSProperties, type ReactNode } from "react";
import {
  FAM, TIER, famHue,
  type CoverRow, type CoverSide, type NearMiss, type Ring, type Tier,
} from "../data/mock";
import { sampleDeck, useDeckAnalysis, type DeckState } from "../data/atlas";
import { FamilyDot, SectionHead, SegControl, TierChip } from "../components/primitives";

// ── Coverage-chart geometry (data-independent) ───────────────────────────────
const ROW_H = 34;
const PAD_T = 30;
const LABEL_W = 110;
const TH = 13; // bar height
const BAR_W = 150; // max bar width per side
const PIVOT = LABEL_W + BAR_W; // dashed vertical axis (260)
const TOTAL_W = LABEL_W + 2 * BAR_W + 58; // 468
const HL = 10; // arrowhead length
const H = TH / 2;

const tierColor = (t: Tier): string => TIER[t].color;
const hollowFill = (color: string): string => `color-mix(in srgb, ${color} 9%, transparent)`;
const tot = (s: CoverSide): number => s.own + (s.subs ?? []).reduce((a, x) => a + x[1], 0);

interface Seg { len: number; hollow: boolean; sf?: string; }

interface BarProps { x0: number; x1: number; yc: number; color: string; fillOp: number; hollow: boolean; }
function Bar({ x0, x1, yc, color, fillOp, hollow }: BarProps): ReactNode {
  if (x1 - x0 < 0.5) return null;
  return hollow ? (
    <rect
      x={x0} y={yc - H} width={x1 - x0} height={TH} rx={2.5}
      fill={hollowFill(color)} stroke={color} strokeWidth={1.2} strokeDasharray="3 2"
    />
  ) : (
    <rect x={x0} y={yc - H} width={x1 - x0} height={TH} rx={2.5} fill={color} opacity={fillOp} />
  );
}

interface HeadProps { xBase: number; xTip: number; yc: number; color: string; fillOp: number; hollow: boolean; }
function Head({ xBase, xTip, yc, color, fillOp, hollow }: HeadProps): ReactNode {
  const d = `M${xBase},${yc - H - 3.5} L${xTip},${yc} L${xBase},${yc + H + 3.5} Z`;
  return hollow ? (
    <path d={d} fill={hollowFill(color)} stroke={color} strokeWidth={1.2} />
  ) : (
    <path d={d} fill={color} opacity={fillOp} />
  );
}

// ── The diverging coverage chart (SVG) ───────────────────────────────────────
function CoverageChart({ rows }: { rows: CoverRow[] }): ReactNode {
  const maxV = Math.max(24, ...rows.map((r) => Math.max(tot(r[1]), tot(r[2]))));
  const scale = (v: number): number => (v / maxV) * BAR_W;
  const totalH = PAD_T + rows.length * ROW_H + 6;

  // Segments of a stack, own-first (nearest the pivot), subs as hollow tail.
  const segsOf = (stack: CoverSide): Seg[] => [
    { len: scale(stack.own), hollow: false },
    ...(stack.subs ?? []).map(([sf, c]): Seg => ({ len: scale(c), hollow: true, sf })),
  ];

  // EMIT — points right, tip AT the pivot; own nearest pivot, hollow subs stack left.
  const emitNodes = (emit: CoverSide, yc: number, hue: string): ReactNode[] => {
    const es = segsOf(emit);
    const nodes: ReactNode[] = [
      <Head key="head" xBase={PIVOT - HL} xTip={PIVOT} yc={yc} color={hue} fillOp={0.9} hollow={false} />,
    ];
    let x = PIVOT;
    es.forEach((s, k) => {
      const right = k === 0 ? PIVOT - HL : x;
      const left = k === 0 ? PIVOT - s.len : x - s.len;
      nodes.push(<Bar key={`b${k}`} x0={left} x1={right} yc={yc} color={hue} fillOp={0.9} hollow={s.hollow} />);
      x = k === 0 ? PIVOT - s.len : x - s.len;
      if (s.hollow && s.sf) {
        nodes.push(
          <text
            key={`s${k}`} x={(left + right) / 2} y={yc - H - 2} textAnchor="middle"
            fontSize={6.5} fontFamily="var(--font-mono)" fill={hue}
          >{`⊃${s.sf.slice(0, 3)}`}</text>,
        );
      }
    });
    const totX = PIVOT - scale(tot(emit)) - 5;
    nodes.push(
      <text key="tot" x={totX} y={yc + 3.5} textAnchor="end" fontSize={9} fontFamily="var(--font-mono)" fill="#9397ab">
        {tot(emit)}
      </text>,
    );
    if (emit.note) {
      nodes.push(
        <text
          key="note" x={totX} y={yc + 12} textAnchor="end" fontSize={7}
          fontFamily="var(--font-mono)" fill={famHue(emit.note)}
        >{`also ∈ ${emit.note}`}</text>,
      );
    }
    return nodes;
  };

  // CONSUME — points right, tip at the OUTER end; own nearest pivot, hollow subs stack right.
  const consumeNodes = (consume: CoverSide, yc: number, hue: string): ReactNode[] => {
    const cs = segsOf(consume);
    const nodes: ReactNode[] = [];
    let cx = PIVOT;
    cs.forEach((s, k) => {
      const last = k === cs.length - 1;
      const x0 = cx;
      const x1 = cx + s.len;
      const bodyEnd = last ? x1 - HL : x1;
      nodes.push(<Bar key={`b${k}`} x0={x0} x1={bodyEnd} yc={yc} color={hue} fillOp={0.34} hollow={s.hollow} />);
      if (last) {
        nodes.push(<Head key={`h${k}`} xBase={bodyEnd} xTip={x1} yc={yc} color={hue} fillOp={0.34} hollow={s.hollow} />);
      }
      if (s.hollow && s.sf) {
        nodes.push(
          <text
            key={`s${k}`} x={(x0 + x1) / 2} y={yc - H - 2} textAnchor="middle"
            fontSize={6.5} fontFamily="var(--font-mono)" fill={hue}
          >{`⊃${s.sf.slice(0, 3)}`}</text>,
        );
      }
      cx = x1;
    });
    nodes.push(
      <text
        key="tot" x={PIVOT + scale(tot(consume)) + 6} y={yc + 3.5}
        fontSize={9} fontFamily="var(--font-mono)" fill="#9397ab"
      >{tot(consume)}</text>,
    );
    return nodes;
  };

  return (
    <svg viewBox={`0 0 ${TOTAL_W} ${totalH}`} width="100%" role="img" aria-label="Directional port coverage">
      <text x={PIVOT - 6} y={16} textAnchor="end" fontSize={9} fontFamily="var(--font-mono)" fill="var(--atlas-muted)">
        EMIT →
      </text>
      <text x={PIVOT + 6} y={16} textAnchor="start" fontSize={9} fontFamily="var(--font-mono)" fill="var(--atlas-muted)">
        → CONSUME
      </text>
      <line
        x1={PIVOT} y1={PAD_T - 6} x2={PIVOT} y2={PAD_T + rows.length * ROW_H - 12}
        stroke="#3a3d52" strokeWidth={1} strokeDasharray="2 3"
      />
      {rows.map((row, i) => {
        const [fam, emit, consume] = row;
        const y = PAD_T + i * ROW_H + 2;
        const yc = y + TH / 2;
        const hue = famHue(fam);
        return (
          <g key={fam}>
            <foreignObject x={2} y={yc - 9} width={LABEL_W - 6} height={18}>
              <div
                style={{
                  display: "flex", alignItems: "center", gap: 6,
                  fontFamily: "var(--font-mono)", fontSize: 11, color: "var(--color-text)",
                }}
              >
                <FamilyDot family={fam} size={8} />
                {FAM[fam]?.name ?? fam}
              </div>
            </foreignObject>
            {emitNodes(emit, yc, hue)}
            {consumeNodes(consume, yc, hue)}
          </g>
        );
      })}
    </svg>
  );
}

// ── The view ─────────────────────────────────────────────────────────────────
const cardStyle: CSSProperties = {
  background: "var(--atlas-panel)", border: "1px solid var(--atlas-border)",
  borderRadius: 8, padding: "11px 13px", marginBottom: 8,
};

export default function DeckLens() {
  const [state, setState] = useState<DeckState>("full");
  const [text, setText] = useState<string>(sampleDeck("full"));
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => () => { if (timer.current) clearTimeout(timer.current); }, []);

  const analyze = (): void => {
    setState("loading");
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(() => setState("full"), 900);
  };

  const clear = (): void => {
    if (timer.current) clearTimeout(timer.current);
    setState("empty");
    setText("");
  };

  const pick = (v: "sparse" | "full"): void => {
    setState(v);
    setText(sampleDeck(v));
  };

  // Hook is called unconditionally; the fallback arg is inert unless resolved.
  const analysisState: "sparse" | "full" = state === "sparse" ? "sparse" : "full";
  const { data } = useDeckAnalysis(analysisState);
  const resolved = state === "sparse" || state === "full";

  return (
    <div className="view-grid">
      <SectionHead kicker="03 · Exploit" title="Deck Lens">
        Paste a decklist → port-coverage profile (directional, with super/subgroup double-counts),
        the complete rings it already makes, and the near-miss closers one card away.
      </SectionHead>

      {/* input */}
      <div className="panel">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginBottom: 8 }}>
          <h5 style={{ margin: 0, color: "var(--atlas-muted)" }}>Decklist</h5>
          <SegControl<"sparse" | "full">
            options={[{ value: "sparse", label: "Sparse" }, { value: "full", label: "Dense" }]}
            value={analysisState}
            onChange={pick}
          />
        </div>
        <textarea
          className="input"
          value={text}
          spellCheck={false}
          rows={12}
          onChange={(e) => setText(e.target.value)}
          aria-label="Decklist"
        />
        <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
          <button type="button" className="btn btn-primary" onClick={analyze}>Analyze</button>
          <button type="button" className="btn btn-secondary" onClick={clear}>Clear</button>
        </div>
      </div>

      {/* results */}
      {state === "empty" && (
        <div className="panel">
          <div className="empty-state">Paste a decklist to begin</div>
        </div>
      )}

      {state === "loading" && (
        <div className="panel">
          <div className="loading-state">
            <span className="ws-spin" />
            Resolving ports · matching rings…
          </div>
        </div>
      )}

      {resolved && (
        <>
          {/* 1 · Port coverage */}
          <div className="panel">
            <h5 style={{ marginTop: 0, color: "var(--atlas-muted)" }}>Port coverage</h5>
            <div className="panel-svg" style={{ padding: 12 }}>
              <CoverageChart rows={data.coverage} />
            </div>
          </div>

          {/* 2 · Complete rings */}
          <div className="panel">
            <h5 style={{ marginTop: 0, color: "var(--atlas-muted)" }}>Complete rings · {data.rings.length}</h5>
            {data.rings.map((r: Ring) => {
              const color = tierColor(r.tier);
              return (
                <div key={r.cards} style={{ ...cardStyle, borderLeft: `3px solid ${color}` }}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginBottom: 6 }}>
                    <TierChip tier={r.tier} conf={r.conf} />
                    <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--atlas-muted)" }}>
                      · seen in {r.pop.toLocaleString()} combos
                    </span>
                  </div>
                  <div style={{ fontSize: 12.5, color: "var(--color-text)", lineHeight: 1.35, marginBottom: 4 }}>
                    {r.cards}
                  </div>
                  <div style={{ fontFamily: "var(--font-mono)", fontSize: 11, color }}>{r.ring}</div>
                </div>
              );
            })}
          </div>

          {/* 3 · One card away (near-miss) */}
          <div className="panel">
            <h5 style={{ marginTop: 0, color: "var(--atlas-muted)" }}>One card away · {data.nearMiss.length}</h5>
            {data.nearMiss.map((nm: NearMiss) => {
              const color = tierColor(nm.resultTier);
              return (
                <div key={nm.missing} style={cardStyle}>
                  <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginBottom: 7 }}>
                    <span style={{ fontSize: 12, color: "var(--color-text)" }}>
                      Missing: <em style={{ fontStyle: "normal", color: "var(--color-accent)" }}>{nm.missing}</em>
                    </span>
                    <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
                      <TierChip tier={nm.resultTier} />
                      <span style={{ fontSize: 10, color: "var(--atlas-muted)" }}>result tier</span>
                    </span>
                  </div>
                  <div style={{ fontFamily: "var(--font-mono)", fontSize: 11, color, marginBottom: 6 }}>{nm.ring}</div>
                  {nm.cands.map((cd) => (
                    <div className="list-row" key={cd.name}>
                      <span style={{ flex: 1, fontSize: 12 }}>{cd.name}</span>
                      <span style={{ fontSize: 10, color: "var(--atlas-muted)" }}>{cd.evidence}</span>
                      <span style={{ fontFamily: "var(--font-mono)", fontSize: 10, color: "var(--atlas-muted)" }}>{cd.price}</span>
                      <span className="tag tag-accent" style={{ marginLeft: 4 }}>{cd.score}</span>
                    </div>
                  ))}
                </div>
              );
            })}
          </div>
        </>
      )}
    </div>
  );
}
