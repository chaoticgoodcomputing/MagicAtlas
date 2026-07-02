# MAST judge — batch verdict (delta)

**Date:** 2026-07-02
**Scope:** 1 fixture (regenerated gold on branch `mast-tdd/2026-07-02-eternal-skylord`)
**Task:** eternal-skylord — static ability granting a keyword to a filtered subset of your creatures ("Zombie tokens you control have flying.")
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WAR/EternalSkylord.json` — PASS.
  Target line is the second ability: `Kind: static` + `gainAbility` effect whose
  `Target` is `Each` object matching `Subtypes:[Zombie], IsToken:true, Controller:You`
  — a faithful, describe-not-execute rendering of "Zombie tokens you control have
  flying" (CR 604.1 static-ability statement, CR 604.2 continuous effect, CR 111.1
  token). The `GainedAbility` is the canonical flying shape: a static ability with
  `KeywordSource: Flying` and an `evasion` effect whose `CanBeBlockedBy` is a creature
  with keyword Flying or Reach — matching CR 702.9a ("Flying is an evasion ability")
  and 702.9b ("can't be blocked except by creatures with flying and/or reach"), and
  identical to `StaticRuleHelpers.MapKeywordToStaticAbility("flying")` used across the
  existing fixture corpus. No timing is baked into the effect (the ability is a bare
  static, no trigger). Characteristics are structured keyword objects, not free text.
  Oracle text verified verbatim against oracle-cards.json.

## Cross-checks

- (a) Structure: correct node/discriminator (`static` + `gainAbility` + `Each` filtered
  target), faithful to the card, describe-not-execute, no baked-in timing. PASS.
- (b) No new free-text/unparsed residual: `Characteristics` entries are typed
  `keyword` objects; no `Kind: unparsed` / `EffectType: unparsed` anywhere; the
  `Reminder` text is verbatim-by-design (exempt). Amass sibling is structured
  (`amass` effect, `Count` literal 2, `ArmySubtype: Zombies`). PASS.
- (c) No regression: new fixture; both oracle abilities captured (amass entry-trigger
  + flying static), none dropped/added/inverted, out-of-axis nodes intact. PASS.
- (d) Citations: CR 604.1, 604.2, 111.1 all exist in rules-structure.json and match the
  modeling; CR 702.9b confirms the flying-as-evasion expansion. PASS.

## Projection decision (initiative 03)

No new AST discriminator introduced. The branch adds only a parser rule
(`SubtypeTokensHaveKeywordRule`); the underlying discriminators (`gainAbility`,
`evasion` EffectType, `IsToken` filter field) are all pre-existing and already
projected. No new PortGraph case or coarse-projection entry is required. N/A.

## Glossary gaps

(none)

## Process notes

The `SubtypeTokensHaveKeywordRule` parser guards against the "Creature/Creatures"
card-type word (deferring to `BareKeywordGrantRule` Arm 2) and anchors its pattern —
out of scope for rules-accuracy but consistent with the fixture's `Subtypes`-only
filter. No concern.
