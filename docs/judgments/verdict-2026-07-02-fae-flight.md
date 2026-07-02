# MAST judge — batch verdict (delta: fae-flight)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-fae-flight
**Base:** cb048c63ea6ae85ef069e0d47244ec68945a5415
**Scope:** 1 fixture (regenerated gold, new) + 1 parser rule
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## Target under judgment

Task fae-flight: Aura ETB grant — "When this Aura enters, enchanted creature gains hexproof until end of turn."

Oracle text verified verbatim against oracle-cards.json:
> Flash / Enchant creature / When this Aura enters, enchanted creature gains hexproof until end of turn. / Enchanted creature gets +1/+0 and has flying.

## PASS verdicts

- `tests/.../BLB/FaeFlight.json#etb-hexproof-trigger` — PASS.
  - (a) **Structure correct.** Timing and effect are a composite: a `Trigger{Timing:When, Event:Enters, Filter:{aura, IsSelf}}` node carries the "when this Aura enters," and a plain `gainAbility` effect carries the action. No `...OnEntry`-style baked-in timing.
  - Target is `Kind: EnchantedOrEquipped` — the correct reference for "enchanted creature" from an Aura (CR 303.4), and the same kind used by static Aura grant clauses. Correctly distinct from the anaphoric `It` used when a chosen/named object gains (cf. DiveDown).
  - GainedAbility is a structured static `Hexproof` keyword (`keywordAbility` effect), matching the DiveDown "gains hexproof until end of turn" convention exactly.
  - Duration is a distinct `untilTime` (Turn/End) node — describe-not-execute, no baked timing.
  - (b) **No free text / no unparsed.** Fully structured; the parser rule returns false on an unrecognized keyword (routing to fallback) rather than emitting prose.
  - (c) **No regression.** New fixture; all four oracle lines are represented (Flash → timingModification; Enchant creature → enchantRestriction; ETB trigger; static +1/+0 & flying composite). No dropped/added/inverted ability; siblings preserved; out-of-axis nodes structured.
  - (d) **Citations cross-referenced.** CR 603.1 (triggered ability = trigger + effect), 702.11a (hexproof is a static ability), 702.11b (hexproof text), 303.4 (Aura attachment), 611.1 (continuous effect with fixed duration), 514.2 ("until end of turn" ends in cleanup) — all present in rules-structure.json and consistent with the modeling.

- `tests/.../BLB/FaeFlight.json#projection` — PASS.
  - No new discriminator introduced. The branch adds a parser rule (`EnchantedCreatureGainsKeywordUntilEndOfTurnRule.cs`) plus the fixture; every AST node it emits (`gainAbility`, `keywordAbility`, `EnchantedOrEquipped`, `untilTime`) pre-exists on base. No PortWalk projection decision is required, and none is missing.

## FAIL verdicts

None.

## Glossary gaps

None. "Hexproof", "Aura", "Flash", "Enchant", "Flying" are all covered domain terms.

## Process notes

The static "gets +1/+0 and has flying" clause and the Flash/Enchant lines are outside task fae-flight's axis but are fully structured (no residuals), so they raise no concern. The parser's local hexproof-only keyword builder is a code-organization choice, out of judge scope.

ALL PASS
