// Shared card-profile panels — the "deep dive" sections (ports, combos, anchor,
// rulings, and the price/rank meta figure) rendered below the Card Explorer's
// explore/exploit columns. Extracted so the Explorer *is* the card page.

import {
  type CardPort, type CardCombo, type CardAnchor, type CardRuling,
} from "../data/atlas";
import { FamilyDot, TierChip, PortFidelity, SectionHead } from "./primitives";
import { CardLink } from "./CardLink";

const panelStyle = { marginBottom: "var(--space-4)" } as const;

// ── Ports ────────────────────────────────────────────────────────────────────
export function PortsPanel({ ports }: { ports: CardPort[] }) {
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title="Ports">
        What this card consumes and emits, one row per port — with its conditionality (is the mechanism
        conditional, and how) and, separately, its provenance (parsed, inferred, or catalogued).
      </SectionHead>
      {ports.length ? (
        ports.map((p, i) => (
          <div className="list-row" key={`${p.side}:${p.family}:${p.label}:${i}`}>
            <FamilyDot family={p.family} />
            <span style={{ minWidth: 92 }}>{p.family}</span>
            <span className="ws-mono" style={{ fontSize: 11, color: "var(--atlas-muted)", minWidth: 64 }}>
              {p.side}
            </span>
            <span
              className="ws-mono"
              style={{ flex: 1, minWidth: 0, fontSize: 11, color: "var(--atlas-muted)", overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}
              title={p.label}
            >
              {p.label}
            </span>
            <PortFidelity conditionality={p.conditionality} provenance={p.provenance} confidence={p.confidence} />
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
export function CombosPanel({ name, combos }: { name: string; combos: CardCombo[] }) {
  if (!combos.length) return null;
  return (
    <div className="panel" style={panelStyle}>
      <SectionHead title={`Combos · ${combos.length}`}>
        Reconstructed named combos this card appears in, most popular first.
      </SectionHead>
      {combos.map((c) => (
        <div key={c.comboId} className="list-row" style={{ alignItems: "baseline", gap: 10, flexWrap: "wrap" }}>
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
          <span className="ws-mono" style={{ fontSize: 10.5, color: "var(--atlas-muted)" }}>{c.familyRing}</span>
          <TierChip tier={c.tier} />
          <span className="ws-mono" style={{ fontSize: 11, color: "var(--atlas-muted)", fontVariantNumeric: "tabular-nums" }} title="popularity (decks seen)">
            {c.popularity.toLocaleString()}
          </span>
        </div>
      ))}
    </div>
  );
}

// ── Anchor ───────────────────────────────────────────────────────────────────
export function AnchorPanel({ anchor }: { anchor: CardAnchor }) {
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
                  <span className="ws-mono" style={{ fontSize: 9.5, color: "var(--atlas-muted)", marginLeft: 6 }}>(unparsed)</span>
                )}
              </span>
              <span className="ws-mono" style={{ fontSize: 11, color: "var(--atlas-muted)", fontVariantNumeric: "tabular-nums" }}>
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

/** A price / rank meta figure: a big tabular-aligned value over a small caps
 *  label, so USD and EDHREC read as two clearly separated columns. */
export function MetaStat({ value, label }: { value: string; label: string }) {
  return (
    <div style={{ minWidth: 72 }}>
      <div style={{ fontSize: 20, fontWeight: 600, lineHeight: 1.1, color: "var(--color-text)", fontVariantNumeric: "tabular-nums" }}>
        {value}
      </div>
      <div style={{ fontSize: 10, marginTop: 3, color: "var(--atlas-muted)", textTransform: "uppercase", letterSpacing: "0.06em" }}>
        {label}
      </div>
    </div>
  );
}

// ── Rulings ──────────────────────────────────────────────────────────────────
export function RulingsPanel({ rulings }: { rulings: CardRuling[] }) {
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
            <span className="ws-mono" style={{ fontSize: 11, color: "var(--color-accent)" }}>{r.source}</span>
          </div>
          <p style={{ margin: 0, fontSize: 13, lineHeight: 1.5, color: "var(--color-text)" }}>{r.comment}</p>
        </div>
      ))}
    </div>
  );
}
