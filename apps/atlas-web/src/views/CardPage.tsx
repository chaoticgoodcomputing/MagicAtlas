// ─────────────────────────────────────────────────────────────────────────────
// Card page — a full profile for one card, addressed by name (#/card/<name>).
//
// Everything here is live: the header/imagery/oracle/ports come from
// useCardProfile, the highlighted oracle from useOracle, and the combo / anchor
// / ruling panels from their sibling hooks in data/atlas. Card names throughout
// (combo parts, co-stars) are <CardLink>s, so the page is a hub you can walk.
// ─────────────────────────────────────────────────────────────────────────────

import {
  useCardProfile, useCardCombos, useCardAnchor, useCardRulings, useOracle,
  type CardPort, type CardCombo, type CardAnchor, type CardRuling, type CardProfile,
} from "../data/atlas";
import { cardImage } from "../data/mock";
import { FamilyDot, TierChip, ConfidenceMeter, SectionHead } from "../components/primitives";
import { OracleText } from "../components/OracleText";
import { CardLink } from "../components/CardLink";
import { ManaCost, SymbolsProvider } from "../ManaCost";
import type { ViewKey } from "../App";

const panelStyle = { marginBottom: "var(--space-4)" } as const;

// ── Ports ────────────────────────────────────────────────────────────────────
function PortsPanel({ ports }: { ports: CardPort[] }) {
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title="Ports">
        What this card consumes and emits, one row per parsed port, with its fidelity tier.
      </SectionHead>
      {ports.length ? (
        ports.map((p, i) => (
          <div className="list-row" key={`${p.side}:${p.family}:${p.label}:${i}`}>
            <FamilyDot family={p.family} />
            <span style={{ minWidth: 92 }}>{p.family}</span>
            <span
              className="ws-mono"
              style={{ fontSize: 11, color: "var(--atlas-muted)", minWidth: 64 }}
            >
              {p.side}
            </span>
            <span
              className="ws-mono"
              style={{ flex: 1, minWidth: 0, fontSize: 11, color: "var(--atlas-muted)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}
              title={p.label}
            >
              {p.label}
            </span>
            {p.tier === "Inferred" && p.confidence != null && (
              <ConfidenceMeter value={p.confidence} />
            )}
            <TierChip tier={p.tier} conf={p.confidence ?? undefined} />
          </div>
        ))
      ) : (
        <div style={{ color: "var(--atlas-muted)", fontSize: 13, padding: "8px 2px" }}>
          No parsed ports for this card yet.
        </div>
      )}
    </div>
  );
}

// ── Combos ───────────────────────────────────────────────────────────────────
function CombosPanel({ name, combos }: { name: string; combos: CardCombo[] }) {
  if (!combos.length) return null;
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title={`Combos · ${combos.length}`}>
        Reconstructed named combos this card appears in, most popular first.
      </SectionHead>
      {combos.map((c) => (
        <div
          key={c.comboId}
          className="list-row"
          style={{ alignItems: "baseline", gap: 10, flexWrap: "wrap" }}
        >
          <span style={{ flex: 1, minWidth: 180, fontSize: 13, lineHeight: 1.5 }}>
            {c.cards.map((part, i) => (
              <span key={part}>
                {i > 0 && <span style={{ color: "var(--atlas-muted)" }}> + </span>}
                {part === name
                  ? <strong style={{ color: "var(--color-text)" }}>{part}</strong>
                  : <CardLink name={part} />}
              </span>
            ))}
          </span>
          <span className="ws-mono" style={{ fontSize: 10.5, color: "var(--atlas-muted)" }}>
            {c.familyRing}
          </span>
          <TierChip tier={c.tier} />
          <span
            className="ws-mono"
            style={{ fontSize: 11, color: "var(--atlas-muted)", fontVariantNumeric: "tabular-nums" }}
            title="popularity (decks seen)"
          >
            {c.popularity.toLocaleString()}
          </span>
        </div>
      ))}
    </div>
  );
}

// ── Anchor ───────────────────────────────────────────────────────────────────
function AnchorPanel({ anchor }: { anchor: CardAnchor }) {
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title="Combo anchor">
        This card gates combos — removing it breaks them. Its most-blocked co-stars are below.
      </SectionHead>
      <div style={{ display: "flex", flexWrap: "wrap", gap: "var(--space-5)", marginBottom: 12 }}>
        <Stat value={anchor.blockedComboCount.toLocaleString()} label="combos blocked" />
        <Stat value={anchor.soleBlockerCount.toLocaleString()} label="sole blocker for" />
        <Stat value={anchor.maxComboPopularity.toLocaleString()} label="top combo popularity" />
      </div>
      {anchor.coStars.length > 0 && (
        <>
          <div style={{ fontSize: 12, color: "var(--atlas-muted)", marginBottom: 6 }}>Co-stars</div>
          {anchor.coStars.map((co) => (
            <div className="list-row" key={co.card}>
              <span style={{ flex: 1, minWidth: 0 }}>
                <CardLink name={co.card} />
                {co.alsoUnparsed && (
                  <span className="ws-mono" style={{ fontSize: 9.5, color: "var(--atlas-muted)", marginLeft: 6 }}>
                    (unparsed)
                  </span>
                )}
              </span>
              <span
                className="ws-mono"
                style={{ fontSize: 11, color: "var(--atlas-muted)", fontVariantNumeric: "tabular-nums" }}
              >
                {co.sharedCombos.toLocaleString()} shared
              </span>
            </div>
          ))}
        </>
      )}
    </div>
  );
}

function Stat({ value, label }: { value: string; label: string }) {
  return (
    <div>
      <div style={{ fontSize: 22, fontWeight: 600, color: "var(--color-text)" }}>{value}</div>
      <div style={{ fontSize: 11, color: "var(--atlas-muted)" }}>{label}</div>
    </div>
  );
}

// ── Rulings ──────────────────────────────────────────────────────────────────
function RulingsPanel({ rulings }: { rulings: CardRuling[] }) {
  if (!rulings.length) return null;
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title={`Rulings · ${rulings.length}`} />
      {rulings.map((r) => (
        <div key={r.id} style={{ padding: "8px 0", borderTop: "1px solid var(--atlas-border-soft)" }}>
          <div style={{ display: "flex", gap: 10, marginBottom: 4 }}>
            <span className="ws-mono" style={{ fontSize: 11, color: "var(--atlas-muted)" }}>
              {new Date(r.publishedAt).toLocaleDateString()}
            </span>
            <span className="ws-mono" style={{ fontSize: 11, color: "var(--color-accent)" }}>
              {r.source}
            </span>
          </div>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5, color: "var(--color-text)" }}>{r.comment}</p>
        </div>
      ))}
    </div>
  );
}

// ── Header ───────────────────────────────────────────────────────────────────
function Header({ profile }: { profile: CardProfile }) {
  const img = profile.imageUriNormal ?? cardImage(profile.name);
  return (
    <div style={{ display: "grid", gridTemplateColumns: "minmax(0, 260px) 1fr", gap: "var(--space-5)", alignItems: "start" }} className="card-header-grid">
      <img
        src={img}
        alt={profile.name}
        style={{ width: "100%", aspectRatio: "488 / 680", borderRadius: 10, background: "#0f1120", objectFit: "cover" }}
      />
      <div>
        <h1 style={{ fontSize: 30, margin: "0 0 6px", display: "flex", alignItems: "center", gap: 12, flexWrap: "wrap" }}>
          {profile.name}
          {profile.manaCost && (
            <span style={{ fontSize: 18 }}><ManaCost value={profile.manaCost} /></span>
          )}
        </h1>
        {profile.typeLine && (
          <div style={{ color: "var(--atlas-muted-2)", fontSize: 15, marginBottom: 12 }}>{profile.typeLine}</div>
        )}

        <div style={{ display: "flex", flexWrap: "wrap", gap: "var(--space-5)", marginBottom: 12 }}>
          {profile.priceUsd != null && <Stat value={`$${Number(profile.priceUsd).toFixed(2)}`} label="USD" />}
          {profile.edhrecRank != null && <Stat value={`#${profile.edhrecRank.toLocaleString()}`} label="EDHREC rank" />}
        </div>

        {profile.keywords.length > 0 && (
          <div style={{ display: "flex", flexWrap: "wrap", gap: 6, marginBottom: 12 }}>
            {profile.keywords.map((k) => (
              <span key={k} className="tag" style={{ fontSize: 11 }}>{k}</span>
            ))}
          </div>
        )}

        {profile.scryfallUri && (
          <a href={profile.scryfallUri} target="_blank" rel="noreferrer" style={{ fontSize: 13 }}>
            View on Scryfall →
          </a>
        )}
      </div>
    </div>
  );
}

// ── Page ─────────────────────────────────────────────────────────────────────
export default function CardPage({ name, onNavigate }: { name: string; onNavigate?: (v: ViewKey) => void }) {
  const { data: profile, loading } = useCardProfile(name);
  const { data: oracle } = useOracle(name);
  const { data: combos } = useCardCombos(name);
  const { data: anchor } = useCardAnchor(name);
  const { data: rulings } = useCardRulings(profile?.oracleId);

  if (loading && !profile) {
    return (
      <div className="view-grid">
        <div className="panel"><div className="loading-state"><span className="ws-spin" /> Loading card…</div></div>
      </div>
    );
  }

  if (!profile) {
    return (
      <div className="view-grid">
        <button type="button" className="btn btn-secondary" onClick={() => onNavigate?.("explorer")} style={{ alignSelf: "start" }}>
          ← Card Explorer
        </button>
        <div className="panel">
          <div className="empty-state">No card named “{name}” found.</div>
        </div>
      </div>
    );
  }

  return (
    <SymbolsProvider>
      <div className="view-grid">
        <button type="button" className="btn btn-ghost" onClick={() => onNavigate?.("explorer")} style={{ alignSelf: "start" }}>
          ← Card Explorer
        </button>

        <Header profile={profile} />

        {/* Oracle — highlighted where MAST has spans, plain otherwise. */}
        <div className="panel" style={panelStyle}>
          <SectionHead title="Oracle text" />
          {oracle ? (
            <OracleText oracle={oracle} />
          ) : profile.oracleText ? (
            <div className="oracle-text">
              {profile.oracleText.split("\n").map((line, i) => (
                <p key={i} style={{ margin: "0 0 0.5em" }}><ManaCost value={line} /></p>
              ))}
            </div>
          ) : (
            <div style={{ color: "var(--atlas-muted)", fontSize: 13 }}>No oracle text.</div>
          )}
        </div>

        <PortsPanel ports={profile.ports} />
        <CombosPanel name={profile.name} combos={combos} />
        {anchor && <AnchorPanel anchor={anchor} />}
        <RulingsPanel rulings={rulings} />
      </div>

      <style>{`
        @media (max-width: 720px) {
          .card-header-grid { grid-template-columns: 1fr !important; }
        }
      `}</style>
    </SymbolsProvider>
  );
}
