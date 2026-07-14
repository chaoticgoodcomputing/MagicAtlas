// Shared visual primitives for the concept views: tier chips, family dots,
// the confidence meter and a segmented control. All read the tier/family
// tokens from ../data/mock so the encoding stays in one place.

import type { CSSProperties, ReactNode } from "react";
import { FAM, TIERS, famHue, type Tier } from "../data/mock";

const tierName = (t: Tier) => TIERS.find((x) => x.key === t)?.name ?? t;

/** A tier badge. Where hue is free the tier owns hue; Inferred adds a
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
