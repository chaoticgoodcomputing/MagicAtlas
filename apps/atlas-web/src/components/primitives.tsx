// Shared visual primitives for the concept views: tier chips, family dots,
// the confidence meter and a segmented control. All read the tier/family
// tokens from ../data/mock so the encoding stays in one place.

import type { CSSProperties, ReactNode } from "react";
import { FAM, TIERS, famHue, PORT_UNCONDITIONAL, type Tier, type PortProvenance } from "../data/mock";

const tierName = (t: Tier) => TIERS.find((x) => x.key === t)?.name ?? t;

/** A COMBO / EDGE certainty badge (ADR 0004 #43: no longer used for ports — see
 *  `PortFidelity`). Where hue is free the tier owns hue; Inferred adds a
 *  confidence read-out, Declared is a dotted outline. */
export function TierChip({ tier, conf }: { tier: Tier; conf?: number }) {
  return (
    <span className={`tier-chip tier-${tier}`}>
      <span className="tier-dot" />
      {tierName(tier)}
      {tier === "Inferred" && conf != null && <> · {conf.toFixed(2)}</>}
    </span>
  );
}

// ── Port fidelity, as two independent facts (ADR 0004 #43) ────────────────────
// The retired four-valued port tier conflated "is the mechanism conditional?"
// with "where did the port come from?". These render the two dimensions
// separately, so a port that is BOTH conditional AND inferred shows both — the
// conflation the old single chip could not express.

/** Dimension 1 — the conditionality phrase (PROVISIONAL copy from the pipeline).
 *  `PORT_UNCONDITIONAL` reads as a quiet, neutral chip; any gate reads amber. */
export function ConditionChip({ conditionality }: { conditionality: string }) {
  if (!conditionality) return null; // backfill port: no parsed mechanism to describe
  const unconditional = conditionality === PORT_UNCONDITIONAL;
  const hue = unconditional ? "#3fbf7f" : "#E0A53C";
  return (
    <span
      className="ws-mono"
      title={unconditional ? "fires unconditionally" : `conditional — ${conditionality}`}
      style={{
        fontSize: 10, padding: "1px 6px", borderRadius: 4, whiteSpace: "nowrap",
        color: hue,
        background: `color-mix(in srgb, ${hue} 13%, transparent)`,
        border: `1px solid color-mix(in srgb, ${hue} 34%, transparent)`,
      }}
    >
      {conditionality}
    </span>
  );
}

/** Dimension 2 — the provenance marker. Parsed renders nothing (the default);
 *  Inferred adds its confidence; Declared is a dotted "catalogued" badge. */
export function ProvenanceMark({
  provenance, confidence,
}: { provenance: PortProvenance; confidence?: number | null }) {
  if (provenance === "") return null; // parsed — the default, no marker
  const inferred = provenance === "Inferred";
  const hue = inferred ? "#9184d9" : "#7f8399";
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 4 }}>
      <span
        className="ws-mono"
        title={inferred ? "inferred from similar cards" : "catalogued, not yet parsed"}
        style={{
          fontSize: 10, padding: "1px 6px", borderRadius: 4, whiteSpace: "nowrap", color: hue,
          border: `1px ${inferred ? "dashed" : "dotted"} color-mix(in srgb, ${hue} 60%, transparent)`,
        }}
      >
        {inferred ? "inferred" : "declared"}
        {inferred && confidence != null && <> · {confidence.toFixed(2)}</>}
      </span>
      {inferred && confidence != null && <ConfidenceMeter value={confidence} />}
    </span>
  );
}

/** Both port fidelity facts, side by side — the ADR 0004 #43 conflation undone. */
export function PortFidelity({
  conditionality, provenance, confidence,
}: { conditionality: string; provenance: PortProvenance; confidence?: number | null }) {
  return (
    <span style={{ display: "inline-flex", alignItems: "center", gap: 6 }}>
      <ConditionChip conditionality={conditionality} />
      <ProvenanceMark provenance={provenance} confidence={confidence} />
    </span>
  );
}

export function FamilyDot({ family, size = 10 }: { family: string | null | undefined; size?: number }) {
  return <span className="fam-dot" style={{ width: size, height: size, background: famHue(family) }} />;
}

/** A selectable family pill; when active it lights in its own hue. */
export function FamilyChip({
  family, active, onClick,
}: { family: string; active?: boolean; onClick?: () => void }) {
  const hue = FAM[family]?.hue ?? "#75798c";
  const style: CSSProperties = active
    ? { color: "#fff", borderColor: hue, boxShadow: `inset 0 0 0 1px ${hue}, 0 0 10px ${hue}44` }
    : {};
  return (
    <button type="button" className={`fam-chip${active ? " active" : ""}`} style={style} onClick={onClick}>
      <FamilyDot family={family} size={8} />
      {family}
    </button>
  );
}

/** Inferred's 0–1 confidence as a hatched fill-meter (never a bare number). */
export function ConfidenceMeter({ value }: { value: number }) {
  const inset = Math.max(0, Math.min(1, 1 - value));
  return (
    <span className="conf-meter" title={`confidence ${value.toFixed(2)}`}>
      <span style={{ inset: `0 ${(inset * 100).toFixed(0)}% 0 0` }} />
    </span>
  );
}

export interface SegOption<T extends string> { value: T; label: ReactNode; }

export function SegControl<T extends string>({
  options, value, onChange,
}: { options: SegOption<T>[]; value: T; onChange: (v: T) => void }) {
  return (
    <div className="seg" role="tablist">
      {options.map((o) => (
        <button
          key={o.value}
          type="button"
          role="tab"
          aria-selected={o.value === value}
          className={`seg-opt${o.value === value ? " active" : ""}`}
          onClick={() => onChange(o.value)}
        >
          {o.label}
        </button>
      ))}
    </div>
  );
}

export function SectionHead({
  kicker, title, children,
}: { kicker?: string; title: string; children?: ReactNode }) {
  return (
    <header className="section-head">
      {kicker && <div className="kicker">{kicker}</div>}
      <h3>{title}</h3>
      {children && <p>{children}</p>}
    </header>
  );
}
