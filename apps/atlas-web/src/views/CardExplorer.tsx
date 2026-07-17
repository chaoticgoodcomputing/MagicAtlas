// 06 · Card Explorer — the card-specific page.
//
// This IS the card page: addressed by #/card/<name>, so every <CardLink> across
// the app lands here scoped to that card. Layout, left→right:
//   • cards that EMIT what this card consumes (feed its consume side),
//   • the card itself (image + meta + highlighted oracle text),
//   • cards that CONSUME what this card emits (drain its emit side),
// then the deep-dive profile below (ports, combos, anchor, rulings). Clicking a
// highlighted oracle clause focuses one side; the search box or any CardLink
// navigates to that card's page.

import { useState, useEffect, type ReactNode } from "react";
import { cardImage, famHue } from "../data/mock";
import {
  useCardNeighbours, useCardProfile, useOracle,
  useCardCombos, useCardAnchor, useCardRulings,
  type CardPort,
} from "../data/atlas";
import { TierChip, FamilyDot, SectionHead } from "../components/primitives";
import { CardLink } from "../components/CardLink";
import { CardSearch } from "../components/CardSearch";
import { PortsPanel, CombosPanel, AnchorPanel, RulingsPanel, MetaStat } from "../components/CardProfile";
import { ManaCost, SymbolsProvider } from "../ManaCost";
import type { Candidate } from "../data/mock";

const DEFAULT_CARD = "Ashnod's Altar";
const goToCard = (name: string) => { window.location.hash = `/card/${encodeURIComponent(name)}`; };

function CandidateRow({ c }: { c: Candidate }) {
  // The connecting port-pair, oriented emit→consume: e.g. Deadeye's `blink`
  // feeds a neighbour's `etb`. Equal families = a direct same-resource match
  // (shown once); differing = the flow hop (shown as `emit → consume`, with `↝`
  // when it rides a combo-adjacent family bridge rather than a shared resource).
  const from = c.linkEmit ?? c.port;
  const to = c.linkConsume ?? c.port;
  const cross = from !== to;
  return (
    <div className="list-row">
      <span style={{ flex: 1, minWidth: 0, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
        <CardLink name={c.card} />
      </span>
      <span
        className="ws-mono"
        title={cross
          ? `${from} feeds ${to}${c.via ? " — via a combo-adjacent family bridge" : ""}`
          : `direct ${from} match`}
        style={{ fontSize: 9.5, whiteSpace: "nowrap", display: "inline-flex", alignItems: "center", gap: 3 }}
      >
        <span style={{ color: famHue(from) }}>{from}</span>
        {cross && (
          <>
            <span style={{ color: "var(--atlas-muted)" }}>{c.via ? "↝" : "→"}</span>
            <span style={{ color: famHue(to) }}>{to}</span>
          </>
        )}
      </span>
      <FamilyDot family={c.port} />
      <TierChip tier={c.tier} conf={c.conf} />
    </div>
  );
}

function CandidatePanel({
  title, fams, list, emptyHint, dim,
}: { title: string; fams: string[]; list: Candidate[]; emptyHint: string; dim: boolean }) {
  return (
    <div className="panel" style={{ opacity: dim ? 0.32 : 1, transition: "opacity .18s" }}>
      <header className="section-head" style={{ marginBottom: "var(--space-3)", display: "flex", alignItems: "baseline", gap: 8, flexWrap: "wrap" }}>
        <h3 style={{ fontSize: 14 }}>{title}</h3>
        {fams.length > 0 && (
          <span className="ws-mono" style={{ fontSize: 10.5 }}>
            {fams.map((f, i) => (
              <span key={f}>
                {i > 0 && <span style={{ color: "var(--atlas-muted)" }}> · </span>}
                <span style={{ color: famHue(f) }}>{f}</span>
              </span>
            ))}
            <span style={{ color: "var(--atlas-muted)" }}> · {list.length}</span>
          </span>
        )}
      </header>
      {list.length ? (
        // Cap the visible height so a long candidate list (a hub family can have
        // 100+) scrolls in place instead of burying the profile panels below.
        <div style={{ maxHeight: "62vh", overflowY: "auto", margin: "0 -4px", padding: "0 4px" }}>
          {list.map((c) => <CandidateRow key={c.card} c={c} />)}
        </div>
      ) : (
        <div style={{ color: "var(--atlas-muted)", fontSize: 12, padding: "10px 2px" }}>{emptyHint}</div>
      )}
    </div>
  );
}

// ── Line-oriented oracle (v2) ────────────────────────────────────────────────
// Every port carries its oracle line + the [start,end] spans of the clause that
// projects it (ADR-0003 provenance). So the oracle text renders line-by-line:
// each line tints its ports' spans and lists the ports it emits/consumes, and
// selecting a line scopes the explore/exploit columns to THAT clause's ports —
// "what does the blink clause feed" rather than "what does the whole card feed".

/** Group a card's ports by the oracle line they project from. */
function portsByLine(ports: CardPort[]): Map<number, CardPort[]> {
  const m = new Map<number, CardPort[]>();
  for (const p of ports) {
    const arr = m.get(p.lineIndex);
    if (arr) arr.push(p);
    else m.set(p.lineIndex, [p]);
  }
  return m;
}

/** Merge overlapping [start,end) line-relative ranges. */
function mergeRanges(rs: [number, number][]): [number, number][] {
  const out: [number, number][] = [];
  for (const [s, e] of [...rs].sort((a, b) => a[0] - b[0])) {
    const last = out[out.length - 1];
    if (last && s <= last[1]) last[1] = Math.max(last[1], e);
    else out.push([s, e]);
  }
  return out;
}

/** Render `text` with the given line-relative ranges wrapped in a tinted mark. */
function highlightText(text: string, ranges: [number, number][]): ReactNode {
  if (!ranges.length) return text;
  const out: ReactNode[] = [];
  let cur = 0;
  mergeRanges(ranges).forEach(([s, e], k) => {
    if (s > cur) out.push(text.slice(cur, s));
    out.push(
      <mark key={k} style={{ background: "color-mix(in srgb, var(--atlas-accent, #6c8cff) 26%, transparent)", color: "inherit", borderRadius: 2, padding: "0 1px" }}>
        {text.slice(s, e)}
      </mark>,
    );
    cur = e;
  });
  if (cur < text.length) out.push(text.slice(cur));
  return out;
}

const sideGlyph = (side: CardPort["side"]) => (side === "emit" ? "▸" : side === "intercept" ? "⇄" : "◂");

function PortChip({ p }: { p: CardPort }) {
  const hue = famHue(p.family);
  return (
    <span
      className="ws-mono"
      title={`${p.side} · ${p.label} · ${p.tier}`}
      style={{
        fontSize: 9.5, display: "inline-flex", alignItems: "center", gap: 3, padding: "1px 5px",
        borderRadius: 4, whiteSpace: "nowrap",
        background: `color-mix(in srgb, ${hue} 13%, transparent)`,
        border: `1px solid color-mix(in srgb, ${hue} 30%, transparent)`,
      }}
    >
      <span style={{ color: "var(--atlas-muted)" }}>{sideGlyph(p.side)}</span>
      <span style={{ color: hue }}>{p.family}</span>
    </span>
  );
}

/** The oracle text as selectable, port-annotated lines. */
function OracleLines({ oracleText, ports, selected, onSelect }: {
  oracleText: string; ports: CardPort[];
  selected: number | null; onSelect: (line: number | null) => void;
}) {
  const lines = oracleText.split("\n");
  const byLine = portsByLine(ports);
  let off = 0;
  const starts = lines.map((l) => { const s = off; off += l.length + 1; return s; });
  return (
    <div className="oracle-text" style={{ display: "flex", flexDirection: "column", gap: 3 }}>
      {lines.map((text, i) => {
        const lps = byLine.get(i) ?? [];
        const start = starts[i];
        const end = start + text.length;
        const ranges = lps
          .flatMap((p) => p.spans ?? [])
          .map(([s, e]) => [Math.max(s, start) - start, Math.min(e, end) - start] as [number, number])
          .filter(([s, e]) => e > s);
        const sel = selected === i;
        const clickable = lps.length > 0;
        return (
          <div
            key={i}
            onClick={clickable ? () => onSelect(sel ? null : i) : undefined}
            title={clickable ? "focus this clause's connections" : undefined}
            style={{
              cursor: clickable ? "pointer" : "default",
              borderLeft: `2px solid ${sel ? "var(--atlas-accent, #6c8cff)" : "transparent"}`,
              background: sel ? "color-mix(in srgb, var(--atlas-accent, #6c8cff) 9%, transparent)" : "transparent",
              padding: "3px 7px", borderRadius: 4,
              opacity: selected != null && !sel ? 0.45 : 1, transition: "opacity .15s",
            }}
          >
            <p style={{ margin: 0, fontSize: 13, lineHeight: 1.45 }}>{highlightText(text, ranges)}</p>
            {lps.length > 0 && (
              <div style={{ display: "flex", flexWrap: "wrap", gap: 4, marginTop: 4 }}>
                {lps.map((p) => <PortChip key={p.label} p={p} />)}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}

export default function CardExplorer({ card: routeCard }: { card?: string }) {
  const card = routeCard ?? DEFAULT_CARD;
  // v2: the selected oracle line scopes the columns to that clause's ports.
  const [selectedLine, setSelectedLine] = useState<number | null>(null);
  useEffect(() => setSelectedLine(null), [card]);

  const oracle = useOracle(card).data;
  const profile = useCardProfile(card).data;
  // v2: when a line is selected, the columns reflect only THAT clause's ports.
  const allPorts = profile?.ports ?? [];
  const activePorts = selectedLine != null ? allPorts.filter((p) => p.lineIndex === selectedLine) : allPorts;
  const { emitters, consumers, inFams, outFams } = useCardNeighbours(activePorts, card).data;
  const combos = useCardCombos(card).data;
  const anchor = useCardAnchor(card).data;
  const rulings = useCardRulings(profile?.oracleId).data;


  return (
    <SymbolsProvider>
      <section>
        <SectionHead kicker="06 · Explore + Exploit" title="Card Explorer">
          Left: cards that feed this card's consume side. Right: cards its emit side feeds. Matches are
          direct (same resource) or flow-bridged along the combo graph (↝ = via an adjacent resource,
          e.g. token↝sacrifice). Click a clause to focus one side; click any card to open it here.
        </SectionHead>

        <div style={{ display: "flex", alignItems: "center", gap: 14, flexWrap: "wrap", marginBottom: 16 }}>
          <CardSearch onSelect={goToCard} />
          <span style={{ fontSize: 12, color: "var(--atlas-muted)" }}>
            Viewing <strong style={{ color: "var(--color-text)" }}>{card}</strong>
          </span>
        </div>

        <div className="expl-grid">
          <CandidatePanel
            title="Feeds this card"
            fams={inFams}
            list={emitters}
            emptyHint={inFams.length === 0 ? "This card consumes no tracked resource" : "Nothing emits what this card consumes"}
            dim={false}
          />

          <div className="panel">
            <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>
              <img
                src={profile?.imageUriNormal ?? cardImage(card)}
                alt={card}
                style={{ width: "100%", aspectRatio: "488 / 680", borderRadius: 8, background: "#0f1120", objectFit: "cover" }}
              />
              <div>
                <h4 style={{ margin: 0, display: "flex", alignItems: "center", gap: 10, flexWrap: "wrap" }}>
                  {card}
                  {profile?.manaCost && <span style={{ fontSize: 15 }}><ManaCost value={profile.manaCost} /></span>}
                </h4>
                {(profile?.typeLine ?? oracle?.type) && (
                  <div style={{ color: "var(--atlas-muted)", fontSize: 12, marginTop: 2 }}>
                    {profile?.typeLine ?? oracle?.type}
                  </div>
                )}
              </div>
              {(profile?.priceUsd != null || profile?.edhrecRank != null) && (
                <div style={{ display: "flex", flexWrap: "wrap", columnGap: 24, rowGap: 8 }}>
                  {profile?.priceUsd != null && <MetaStat value={`$${Number(profile.priceUsd).toFixed(2)}`} label="USD" />}
                  {profile?.edhrecRank != null && <MetaStat value={`#${profile.edhrecRank.toLocaleString()}`} label="EDHREC rank" />}
                </div>
              )}
              {profile?.oracleText ? (
                <OracleLines
                  oracleText={profile.oracleText}
                  ports={allPorts}
                  selected={selectedLine}
                  onSelect={setSelectedLine}
                />
              ) : (
                <div style={{ color: "var(--atlas-muted)", fontSize: 13 }}>oracle text unavailable</div>
              )}
              {profile?.scryfallUri && (
                <a href={profile.scryfallUri} target="_blank" rel="noreferrer" style={{ fontSize: 12 }}>View on Scryfall →</a>
              )}
              <div className="ws-mono" style={{ fontSize: 10.5, color: "var(--atlas-muted)" }}>
                {selectedLine != null
                  ? "columns scoped to the selected clause — click it again to clear"
                  : "click an oracle clause to scope the columns to just that clause's ports"}
              </div>
            </div>
          </div>

          <CandidatePanel
            title="This card feeds"
            fams={outFams}
            list={consumers}
            emptyHint={outFams.length === 0 ? "This card emits no tracked resource" : "Nothing consumes what this card emits"}
            dim={false}
          />
        </div>

        {/* Deep-dive profile below the explore/exploit columns */}
        <div style={{ marginTop: "var(--space-6)" }}>
          <PortsPanel ports={profile?.ports ?? []} />
          <CombosPanel name={card} combos={combos} />
          {anchor && <AnchorPanel anchor={anchor} />}
          <RulingsPanel rulings={rulings} />
        </div>

        <style>{`
          .expl-grid {
            display: grid;
            grid-template-columns: 320px 1fr 320px;
            gap: var(--space-4);
            align-items: start;
          }
          @media (max-width: 900px) {
            .expl-grid { grid-template-columns: 1fr; }
          }
        `}</style>
      </section>
    </SymbolsProvider>
  );
}
