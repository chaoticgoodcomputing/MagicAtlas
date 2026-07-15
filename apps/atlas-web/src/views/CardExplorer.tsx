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

import { useState } from "react";
import { cardImage, famHue, type Side } from "../data/mock";
import {
  useCardNeighbours, useCardProfile, useOracle, primaryPortFamily,
  useCardCombos, useCardAnchor, useCardRulings,
} from "../data/atlas";
import { TierChip, FamilyDot, SectionHead } from "../components/primitives";
import { OracleText } from "../components/OracleText";
import { CardLink } from "../components/CardLink";
import { CardSearch } from "../components/CardSearch";
import { PortsPanel, CombosPanel, AnchorPanel, RulingsPanel, MetaStat } from "../components/CardProfile";
import { ManaCost, SymbolsProvider } from "../ManaCost";
import type { Candidate } from "../data/mock";

type Focus = Side | "both";
const DEFAULT_CARD = "Ashnod's Altar";
const goToCard = (name: string) => { window.location.hash = `/card/${encodeURIComponent(name)}`; };

function CandidateRow({ c }: { c: Candidate }) {
  return (
    <div className="list-row">
      <span style={{ flex: 1, minWidth: 0, overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
        <CardLink name={c.card} />
      </span>
      {c.via && (
        <span className="ws-mono" title="satisfied via supergroup match" style={{ fontSize: 9, color: famHue(c.port), whiteSpace: "nowrap" }}>
          ⊃ via {c.port}
        </span>
      )}
      <FamilyDot family={c.port} />
      <TierChip tier={c.tier} conf={c.conf} />
    </div>
  );
}

function CandidatePanel({
  title, fam, list, emptyHint, dim,
}: { title: string; fam: string | null | undefined; list: Candidate[]; emptyHint: string; dim: boolean }) {
  return (
    <div className="panel" style={{ opacity: dim ? 0.32 : 1, transition: "opacity .18s" }}>
      <header className="section-head" style={{ marginBottom: "var(--space-3)", display: "flex", alignItems: "baseline", gap: 8 }}>
        <h3 style={{ fontSize: 14 }}>{title}</h3>
        {fam && <span className="ws-mono" style={{ fontSize: 10.5, color: famHue(fam) }}>{fam} · {list.length}</span>}
      </header>
      {list.length ? (
        list.map((c) => <CandidateRow key={c.card} c={c} />)
      ) : (
        <div style={{ color: "var(--atlas-muted)", fontSize: 12, padding: "10px 2px" }}>{emptyHint}</div>
      )}
    </div>
  );
}

export default function CardExplorer({ card: routeCard }: { card?: string }) {
  const card = routeCard ?? DEFAULT_CARD;
  const [focus, setFocus] = useState<Focus>("both");

  const oracle = useOracle(card).data;
  const profile = useCardProfile(card).data;
  const inFam = primaryPortFamily(profile?.ports ?? [], "consume"); // consumed → left column feeds it
  const outFam = primaryPortFamily(profile?.ports ?? [], "emit");   // emitted → right column drains it
  const { emitters, consumers } = useCardNeighbours(inFam, outFam).data;
  const combos = useCardCombos(card).data;
  const anchor = useCardAnchor(card).data;
  const rulings = useCardRulings(profile?.oracleId).data;

  const onFocus = (side: Side) => setFocus((f) => (f === side ? "both" : side));

  return (
    <SymbolsProvider>
      <section>
        <SectionHead kicker="06 · Explore + Exploit" title="Card Explorer">
          Left: cards that emit what this line consumes. Right: cards that consume what it emits
          (⊃ = supergroup match). Click a clause to focus one side; click any card to open it here.
        </SectionHead>

        <div style={{ display: "flex", alignItems: "center", gap: 14, flexWrap: "wrap", marginBottom: 16 }}>
          <CardSearch onSelect={goToCard} />
          <span style={{ fontSize: 12, color: "var(--atlas-muted)" }}>
            Viewing <strong style={{ color: "var(--color-text)" }}>{card}</strong>
          </span>
        </div>

        <div className="expl-grid">
          <CandidatePanel
            title="Emits what this line consumes"
            fam={inFam}
            list={emitters}
            emptyHint="No consume port on this card"
            dim={focus === "emit"}
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
              {oracle ? (
                <OracleText oracle={oracle} focus={focus} onFocus={onFocus} />
              ) : profile?.oracleText ? (
                <div className="oracle-text">
                  {profile.oracleText.split("\n").map((line, i) => (
                    <p key={i} style={{ margin: "0 0 0.4em" }}><ManaCost value={line} /></p>
                  ))}
                </div>
              ) : (
                <div style={{ color: "var(--atlas-muted)", fontSize: 13 }}>oracle text unavailable</div>
              )}
              {profile?.scryfallUri && (
                <a href={profile.scryfallUri} target="_blank" rel="noreferrer" style={{ fontSize: 12 }}>View on Scryfall →</a>
              )}
              <div className="ws-mono" style={{ fontSize: 10.5, color: "var(--atlas-muted)" }}>
                {focus === "both"
                  ? "click a highlighted clause to focus one side of the query"
                  : focus === "consume"
                    ? "showing what EMITS into this card's consume — click again to clear"
                    : "showing what CONSUMES this card's emit — click again to clear"}
              </div>
            </div>
          </div>

          <CandidatePanel
            title="Consumes what this line emits"
            fam={outFam}
            list={consumers}
            emptyHint="No emit port on this card"
            dim={focus === "consume"}
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
