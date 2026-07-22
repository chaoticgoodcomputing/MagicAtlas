// The shared visual language, on one page: the uncertainty tiers (concept 00),
// oracle port highlighting (concept 05), and the token/component starter
// (concept 07). This is the reference the other views are built against.

import { ORACLE, TIER, TIERS } from "../data/mock";
import { useFamilyGraph } from "../data/atlas";
import { ConfidenceMeter, FamilyChip, SectionHead, TierChip, PortFidelity } from "../components/primitives";
import { OracleText } from "../components/OracleText";

const SHOWCASE = ["Midnight Reaper", "Grave Pact", "Ashnod's Altar", "Zulaport Cutthroat"];
const CONF = [
  { card: "Mikaeus, the Unhallowed", conf: 0.87 },
  { card: "Warren Soultrader", conf: 0.61 },
  { card: "Carrion Feeder", conf: 0.42 },
];

/** A short SVG stroke sample carrying a tier's texture channel. */
function TierLine({ tierKey }: { tierKey: keyof typeof TIER }) {
  const t = TIER[tierKey];
  return (
    <svg width="100%" height="16" viewBox="0 0 200 16" preserveAspectRatio="none" aria-hidden>
      <line
        x1="4" y1="8" x2="196" y2="8"
        stroke={t.color} strokeWidth={3} strokeOpacity={t.op}
        strokeDasharray={t.dash ?? undefined} strokeLinecap="round"
      />
    </svg>
  );
}

export default function DesignSystem() {
  const { data: fg } = useFamilyGraph();
  return (
    <div className="view-grid" style={{ gap: "var(--space-8)" }}>
      {/* 00 · Uncertainty language */}
      <section>
        <SectionHead kicker="00 · Foundations" title="Uncertainty language">
          A port's fidelity is two independent facts, not one tier (ADR 0004 #43). <strong>Conditionality</strong>
          — is the mechanism conditional, and how (it fires unconditionally, or needs to tap / needs a counter /
          is rate-limited). <strong>Provenance</strong>, separately — is the port parsed, inferred (with a 0–1
          confidence), or catalogued only. Because they are separate, a conditional <em>and</em> inferred port
          shows both — the conflation the old single chip could not express. Copy is PROVISIONAL, pending sign-off.
        </SectionHead>
        <div className="panel" style={{ marginBottom: "var(--space-4)", display: "flex", flexDirection: "column", gap: 10 }}>
          {[
            { label: "parsed · unconditional", conditionality: "fires unconditionally", provenance: "" as const, confidence: null },
            { label: "parsed · conditional", conditionality: "needs to tap · rate-limited", provenance: "" as const, confidence: null },
            { label: "inferred · conditional (both facts at once)", conditionality: "rate-limited", provenance: "Inferred" as const, confidence: 0.61 },
            { label: "catalogued only", conditionality: "", provenance: "Declared" as const, confidence: null },
          ].map((r) => (
            <div key={r.label} style={{ display: "grid", gridTemplateColumns: "260px 1fr", alignItems: "center", gap: "var(--space-4)" }}>
              <span style={{ fontSize: 12, color: "var(--atlas-muted)" }}>{r.label}</span>
              <PortFidelity conditionality={r.conditionality} provenance={r.provenance} confidence={r.confidence} />
            </div>
          ))}
        </div>
        <h5 style={{ color: "var(--atlas-muted)", margin: "0 0 var(--space-3)" }}>
          Combo &amp; edge certainty (a reconstructed cycle / resource line — distinct from port fidelity)
        </h5>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(240px, 1fr))", gap: "var(--space-3)" }}>
          {TIERS.map((t) => (
            <div key={t.key} className="panel" style={{ display: "flex", flexDirection: "column", gap: 10 }}>
              <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                <TierChip tier={t.key} conf={t.key === "Inferred" ? 0.87 : undefined} />
                <span style={{ fontSize: 11, color: "var(--atlas-muted)", fontFamily: "var(--font-mono)" }}>{t.textureLabel}</span>
              </div>
              <TierLine tierKey={t.key} />
              <p style={{ margin: 0, fontSize: 13, color: "var(--atlas-muted-2)" }}>{t.desc}</p>
              <div style={{ fontSize: 11, color: "var(--atlas-muted)" }}>volume · {t.vol}</div>
            </div>
          ))}
        </div>

        <div className="panel" style={{ marginTop: "var(--space-4)" }}>
          <h5 style={{ color: "var(--atlas-muted)", marginBottom: "var(--space-4)" }}>Inferred carries a 0–1 confidence</h5>
          <div style={{ display: "flex", flexDirection: "column", gap: "var(--space-4)" }}>
            {CONF.map((c) => (
              <div key={c.card} style={{ display: "grid", gridTemplateColumns: "220px 1fr 48px", alignItems: "center", gap: "var(--space-4)" }}>
                <span style={{ fontSize: 13 }}>{c.card}</span>
                <ConfidenceMeter value={c.conf} />
                <span style={{ fontSize: 12, fontFamily: "var(--font-mono)", color: "var(--color-accent-300)", textAlign: "right" }}>{c.conf.toFixed(2)}</span>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* 05 · Oracle port highlighting */}
      <section>
        <SectionHead kicker="05 · Shared primitive" title="Oracle port highlighting">
          A card's oracle text is the legend: every port clause underlined in its family hue and tagged
          ◂ consume / emit ▸. (Spans are hand-authored today — see the upstream plan: MAST must emit each
          port's source character range.)
        </SectionHead>
        <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(320px, 1fr))", gap: "var(--space-3)" }}>
          {SHOWCASE.map((name) => {
            const o = ORACLE[name];
            if (!o) return null;
            return (
              <div key={name} className="panel">
                <div style={{ display: "flex", alignItems: "baseline", justifyContent: "space-between", marginBottom: 8 }}>
                  <strong style={{ fontSize: 15 }}>{name}</strong>
                  <span style={{ fontSize: 11, color: "var(--atlas-muted)" }}>{o.type}</span>
                </div>
                <OracleText oracle={o} />
              </div>
            );
          })}
        </div>
      </section>

      {/* 07 · Design-system starter */}
      <section>
        <SectionHead kicker="07 · Tokens" title="Design system">
          The Nocturne dark ground, the four tier tokens, and the {fg.keys.length} resource-family hues that key
          every station, line, dot and underline in the atlas.
        </SectionHead>

        <div className="panel" style={{ marginBottom: "var(--space-4)" }}>
          <h5 style={{ color: "var(--atlas-muted)", marginBottom: "var(--space-4)" }}>{fg.keys.length} family hues</h5>
          <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(150px, 1fr))", gap: "var(--space-2)" }}>
            {fg.keys.map((k) => {
              const f = fg.families[k];
              return (
                <div key={k} style={{ display: "flex", alignItems: "center", gap: 8, padding: "6px 8px", borderRadius: 8, background: "var(--atlas-panel-2)" }}>
                  <span style={{ width: 16, height: 16, borderRadius: 5, background: f.hue, boxShadow: `0 0 10px ${f.hue}55`, flex: "none" }} />
                  <span style={{ display: "flex", flexDirection: "column", minWidth: 0 }}>
                    <span style={{ fontSize: 13 }}>{k}</span>
                    <span style={{ fontSize: 10, color: "var(--atlas-muted)", fontFamily: "var(--font-mono)" }}>{f.cards.toLocaleString()} · {f.labels}</span>
                  </span>
                </div>
              );
            })}
          </div>
        </div>

        <div className="panel">
          <h5 style={{ color: "var(--atlas-muted)", marginBottom: "var(--space-4)" }}>Core components</h5>
          <div style={{ display: "flex", flexWrap: "wrap", gap: "var(--space-6)", alignItems: "center" }}>
            <button className="btn btn-primary">Analyze deck</button>
            <button className="btn btn-secondary">Reset</button>
            <TierChip tier="Green" />
            <TierChip tier="Amber" />
            <TierChip tier="Inferred" conf={0.61} />
            <TierChip tier="Declared" />
            <FamilyChip family="death" active />
            <FamilyChip family="mana" />
            <div style={{ width: 120 }}><ConfidenceMeter value={0.72} /></div>
            <span className="tag tag-accent">deep-link ⌘</span>
          </div>
        </div>
      </section>
    </div>
  );
}
