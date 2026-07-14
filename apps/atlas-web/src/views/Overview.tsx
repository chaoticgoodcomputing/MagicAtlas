// Concept cover — the headline the whole atlas hangs off: what got parsed, the
// four fidelity tiers as a sticky legend, and the realized archetypes.

import { useArchetypes, useHeadlineStats, useTiers } from "../data/atlas";
import { FamilyDot, TierChip } from "../components/primitives";
import type { ViewKey } from "../App";

export default function Overview({ onNavigate }: { onNavigate?: (v: ViewKey) => void }) {
  const { data: stats } = useHeadlineStats();
  const { data: tiers } = useTiers();
  const { data: archetypes } = useArchetypes();

  return (
    <div className="view-grid">
      <header style={{ maxWidth: "68ch" }}>
        <div className="kicker" style={{ color: "var(--color-accent)", letterSpacing: "0.14em", textTransform: "uppercase", fontSize: 11, marginBottom: 8 }}>
          Interaction-structure explorer
        </div>
        <h1 style={{ fontSize: 46, marginBottom: 12 }}>Magic Atlas</h1>
        <p style={{ color: "var(--atlas-muted-2)", fontSize: 17, lineHeight: 1.5 }}>
          Every card is a set of <strong style={{ color: "var(--color-text)" }}>ports</strong> — what it
          consumes and what it emits, in one of seventeen resource families. The atlas is the graph those
          ports make: the families that feed each other, the combos that close into rings, and the decks
          that ride them. Every edge carries a fidelity tier, so you always know whether a mechanism was
          verified, reconstructed, inferred, or merely catalogued.
        </p>
      </header>

      <div className="stat-row">
        {stats.map((s) => (
          <div key={s.label} className="stat-tile">
            <div className="value">{s.value}</div>
            <div className="label">{s.label}</div>
          </div>
        ))}
      </div>

      {/* Sticky tier legend — the vocabulary the rest of the site speaks in. */}
      <div
        className="panel"
        style={{ position: "sticky", top: 60, zIndex: 10, display: "flex", flexWrap: "wrap", gap: "var(--space-6)", alignItems: "center" }}
      >
        <strong style={{ fontSize: 13, color: "var(--atlas-muted)" }}>Fidelity</strong>
        {tiers.map((t) => (
          <div key={t.key} style={{ display: "flex", alignItems: "center", gap: 8 }}>
            <TierChip tier={t.key} conf={t.key === "Inferred" ? 0.87 : undefined} />
            <span style={{ fontSize: 12, color: "var(--atlas-muted)" }}>{t.vol}</span>
          </div>
        ))}
      </div>

      <section>
        <div className="section-head">
          <div className="kicker">Realized archetypes</div>
          <h3>51 / 3,286 family signatures with a real deck behind them</h3>
          <p>The recurring family triples the combo corpus actually produces, ranked by how many combos realize them.</p>
        </div>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(280px, 1fr))", gap: "var(--space-3)" }}>
          {archetypes.map((a) => (
            <button
              key={a.sig}
              type="button"
              className="panel"
              onClick={() => onNavigate?.("metro")}
              style={{ textAlign: "left", cursor: "pointer", display: "flex", flexDirection: "column", gap: 8, padding: "var(--space-4)", background: "var(--atlas-panel)" }}
            >
              <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                <FamilyDot family={a.fam} />
                <strong style={{ fontFamily: "var(--font-mono)", fontSize: 13 }}>{a.sig}</strong>
              </div>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 8 }}>
                <span style={{ color: "var(--atlas-muted)", fontSize: 12 }}>{a.combos.toLocaleString()} combos</span>
                <TierChip tier={a.tier} />
              </div>
            </button>
          ))}
        </div>
      </section>
    </div>
  );
}
