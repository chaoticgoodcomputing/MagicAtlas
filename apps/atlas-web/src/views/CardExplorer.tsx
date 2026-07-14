// 06 · Card Explorer — the card page.
//
// A chip row picks the current card; three columns show, left→right:
//   • cards that EMIT what this card consumes (they feed its consume side),
//   • the card itself (image + highlighted oracle text),
//   • cards that CONSUME what this card emits (they drain its emit side).
// Clicking a highlighted oracle clause focuses one side of the query — the
// center dims the opposite clause (via OracleText's `focus`) and the matching
// candidate column dims with it.

import { useState } from "react";
import { EXPLORER_CARDS, cardImage, famHue, type Side } from "../data/mock";
import { useCardNeighbours, useOracle } from "../data/atlas";
import { TierChip, FamilyDot, SectionHead } from "../components/primitives";
import { OracleText } from "../components/OracleText";
import type { Candidate } from "../data/mock";

type Focus = Side | "both";

function CandidateRow({ c }: { c: Candidate }) {
  return (
    <div className="list-row">
      <span
        style={{
          flex: 1,
          minWidth: 0,
          overflow: "hidden",
          textOverflow: "ellipsis",
          whiteSpace: "nowrap",
        }}
      >
        {c.card}
      </span>
      {c.via && (
        <span
          className="ws-mono"
          title="satisfied via supergroup match"
          style={{ fontSize: 9, color: famHue(c.port), whiteSpace: "nowrap" }}
        >
          ⊃ via {c.port}
        </span>
      )}
      <FamilyDot family={c.port} />
      <TierChip tier={c.tier} conf={c.conf} />
    </div>
  );
}

function CandidatePanel({
  title,
  fam,
  list,
  emptyHint,
  dim,
}: {
  title: string;
  fam: string | null | undefined;
  list: Candidate[];
  emptyHint: string;
  dim: boolean;
}) {
  return (
    <div
      className="panel"
      style={{ opacity: dim ? 0.32 : 1, transition: "opacity .18s" }}
    >
      <header
        className="section-head"
        style={{ marginBottom: "var(--space-3)", display: "flex", alignItems: "baseline", gap: 8 }}
      >
        <h3 style={{ fontSize: 14 }}>{title}</h3>
        {fam && (
          <span className="ws-mono" style={{ fontSize: 10.5, color: famHue(fam) }}>
            {fam} · {list.length}
          </span>
        )}
      </header>
      {list.length ? (
        list.map((c) => <CandidateRow key={c.card} c={c} />)
      ) : (
        <div style={{ color: "var(--atlas-muted)", fontSize: 12, padding: "10px 2px" }}>
          {emptyHint}
        </div>
      )}
    </div>
  );
}

export default function CardExplorer() {
  const [card, setCard] = useState<string>(EXPLORER_CARDS[0] ?? "Midnight Reaper");
  const [focus, setFocus] = useState<Focus>("both");

  const oracle = useOracle(card).data;
  const { card: pool, emitters, consumers } = useCardNeighbours(card).data;

  const inFam = pool?.in ?? null; // family this card consumes → left column feeds it
  const outFam = pool?.out ?? null; // family this card emits → right column drains it

  const onFocus = (side: Side) => setFocus((f) => (f === side ? "both" : side));

  return (
    <section>
      <SectionHead kicker="06 · Explore + Exploit" title="Card Explorer">
        Left: cards that emit what this line consumes. Right: cards that consume what it emits
        (⊃ = supergroup match). Click a clause to focus one side.
      </SectionHead>

      <div style={{ display: "flex", flexWrap: "wrap", gap: 8, marginBottom: 16 }}>
        {EXPLORER_CARDS.map((name) => {
          const on = name === card;
          return (
            <button
              key={name}
              type="button"
              className={`btn ${on ? "btn-primary" : "btn-secondary"}`}
              onClick={() => {
                setCard(name);
                setFocus("both");
              }}
            >
              {name}
            </button>
          );
        })}
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
              src={cardImage(card)}
              alt={card}
              style={{
                width: "100%",
                aspectRatio: "488 / 680",
                borderRadius: 8,
                background: "#0f1120",
                objectFit: "cover",
              }}
            />
            <div>
              <h4 style={{ margin: 0 }}>{card}</h4>
              {oracle && (
                <div style={{ color: "var(--atlas-muted)", fontSize: 12, marginTop: 2 }}>
                  {oracle.type}
                </div>
              )}
            </div>
            {oracle ? (
              <OracleText oracle={oracle} focus={focus} onFocus={onFocus} />
            ) : (
              <div style={{ color: "var(--atlas-muted)", fontSize: 13 }}>
                oracle text unavailable
              </div>
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
  );
}
