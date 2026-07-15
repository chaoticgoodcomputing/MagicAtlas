// ─────────────────────────────────────────────────────────────────────────────
// Concept 02 · Station Focus — Explorer drill-in.
//
// Drill into ONE resource family: its one-hop neighbourhood laid out as a small
// transit station, its top cards, and every line running through it. Clicking a
// neighbour station travels there (re-focuses on that family).
//
// Ported from the concept canvas's `drawStationFocus`; geometry is plain Math,
// SVG is emitted as JSX (no d3), and all data comes through `useStation`.
// ─────────────────────────────────────────────────────────────────────────────

import { useState } from "react";
import { FAMILY_KEYS, FAM, famHue, TIER, type Edge } from "../data/mock";
import { useStation } from "../data/atlas";
import { FamilyChip, TierChip, FamilyDot, SectionHead } from "../components/primitives";
import { CardLink } from "../components/CardLink";
import { arrowHeadPath, rotate, pointAt, type Pt } from "../lib/ortho";

const CX = 300;
const CY = 230;
const LW = 3.2;

interface Neighbour {
  edge: Edge;
  fam: string;
  dir: "out" | "in";
}

export default function StationFocus() {
  const [focused, setFocused] = useState<string>("sacrifice");
  const { data } = useStation(focused);
  const { family, neighbours, topCards } = data;

  const focusHue = family?.hue ?? famHue(focused);
  const n = neighbours.length || 1;

  // Rail: lines through this station, ranked by realized-combo volume.
  const lines: Neighbour[] = [...neighbours].sort((a, b) => b.edge.combos - a.edge.combos);

  return (
    <div className="view-grid">
      <SectionHead kicker="02 · Explorer" title="Station Focus">
        One family&apos;s anchored one-hop neighbourhood, its top cards and the lines through it.
        Click a neighbour to travel.
      </SectionHead>

      <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 4 }}>
        {FAMILY_KEYS.map((k) => (
          <FamilyChip key={k} family={k} active={k === focused} onClick={() => setFocused(k)} />
        ))}
      </div>

      <div className="two-col">
        <div className="panel panel-svg">
          <svg viewBox="0 0 600 460" width="100%" role="img" aria-label={`${focused} station`}>
            {neighbours.map((nb, i) => {
              const ang = -Math.PI / 2 + (i / n) * Math.PI * 2;
              const nx = CX + Math.cos(ang) * 180;
              const ny = CY + Math.sin(ang) * 150;
              const nHue = famHue(nb.fam);
              const T = TIER[nb.edge.tier];

              // Flow runs source → sink; the line paints in the source's hue.
              const from: Pt = nb.dir === "out" ? [CX, CY] : [nx, ny];
              const to: Pt = nb.dir === "out" ? [nx, ny] : [CX, CY];
              const lineHue = nb.dir === "out" ? focusHue : nHue;

              const head = pointAt([from, to], 0.62);
              const key = `${nb.edge.from}>${nb.edge.to}-${nb.dir}-${i}`;

              return (
                <g key={key}>
                  {/* casing under the coloured line for contrast */}
                  <line
                    x1={from[0]} y1={from[1]} x2={to[0]} y2={to[1]}
                    stroke="var(--atlas-svg)" strokeWidth={LW + 3} strokeLinecap="round"
                  />
                  <line
                    x1={from[0]} y1={from[1]} x2={to[0]} y2={to[1]}
                    stroke={lineHue} strokeWidth={LW} strokeLinecap="round"
                    opacity={T.op} strokeDasharray={T.dash ?? undefined}
                  />
                  <path
                    d={arrowHeadPath(4.5)}
                    transform={rotate(head.x, head.y, head.ang)}
                    fill={lineHue} opacity={T.op}
                  />
                  <g style={{ cursor: "pointer" }} onClick={() => setFocused(nb.fam)}>
                    <circle cx={nx} cy={ny} r={14} fill={nHue} stroke="var(--atlas-svg)" strokeWidth={2} />
                    <text
                      x={nx} y={ny - 20} textAnchor="middle" fontSize={11} fill="#cfd3e5"
                      stroke="var(--atlas-svg)" strokeWidth={3} paintOrder="stroke"
                    >
                      {FAM[nb.fam]?.name ?? nb.fam}
                    </text>
                  </g>
                </g>
              );
            })}

            {/* center station */}
            <circle cx={CX} cy={CY} r={34} fill={focusHue} opacity={0.1} />
            <circle cx={CX} cy={CY} r={18} fill={focusHue} />
            <text
              x={CX} y={CY + 40} textAnchor="middle" fontSize={14} fontWeight={600} fill="#e9e9ed"
              stroke="var(--atlas-svg)" strokeWidth={4} paintOrder="stroke"
            >
              {family?.name ?? focused}
            </text>
          </svg>
        </div>

        <div className="rail">
          <div className="panel">
            <h5>
              Top cards · <span style={{ color: focusHue }}>{family?.name ?? focused}</span>
            </h5>
            {topCards.map((c) => (
              <div className="list-row" key={c}>
                <FamilyDot family={focused} />
                <span style={{ flex: 1 }}><CardLink name={c} /></span>
              </div>
            ))}
          </div>

          <div className="panel">
            <h5>Lines through {family?.name ?? focused}</h5>
            {lines.map((nb, i) => {
              const label = nb.dir === "out"
                ? `${focused} → ${nb.fam}`
                : `${nb.fam} → ${focused}`;
              return (
                <div className="list-row" key={`${nb.edge.from}>${nb.edge.to}-${i}`}>
                  <span style={{ flex: 1 }}>{label}</span>
                  <TierChip tier={nb.edge.tier} />
                  <span style={{ color: "var(--atlas-muted)", fontVariantNumeric: "tabular-nums" }}>
                    {nb.edge.combos.toLocaleString()}
                  </span>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
}
