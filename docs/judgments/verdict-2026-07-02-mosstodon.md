# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** mosstodon (branch `mast-tdd/2026-07-02-mosstodon`, base `90209551`)
**Scope:** 1 fixture (Mosstodon.json) + 1 parser rule (TargetCreatureWithPowerFilterGainsKeywordEffectRule.cs, not a new AST node); projection decision reviewed
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/Mosstodon.json` — PASS. Oracle text verified verbatim against oracle-cards.json (`{1}: Target creature with power 5 or greater gains trample until end of turn.`). The power-filtered target is structured correctly: `Target` reference + `ObjectFilter { CardTypes:["creature"], PowerComparison:{Operator:GreaterThanOrEqual, Value:5} }` — faithful to "creature with power 5 or greater", right node, no free text. Trample is granted as a `static` ability (`KeywordSource:"Trample"` + `keywordAbility` effect), matching CR 702.19a which defines trample as a static ability. Timing is a separate composite `Duration:{untilTime → Turn/End}`, not baked into the effect discriminator (plain `gainAbility`). GainedAbility block matches the established grant-keyword convention (cf. VinesOfTheRecluse). Siblings/attributes (manaCost, colors, colorIdentity, creatureStats) all present and correct. No unparsed/free-text residual on any axis. Cited CR 602.1 (activated ability cost:effect shape), CR 611.1 (continuous effect), and CR 702.19a (trample static ability) all exist in rules-structure.json and match the modeling.
- `mast-tdd/2026-07-02-mosstodon#projection` — PASS. No new discriminator introduced. The branch adds a parser rule + fixture only; all AST nodes/fields it emits (`GainAbilityEffect`, `ObjectFilter.PowerComparison`, `ComparisonOperator.GreaterThanOrEqual`, `KeywordAbilityEffect`, `UntilTimeDuration`) pre-exist. The PortWalk projection ratchet (initiative 03) requires a decision only for a new effect/cost type, trigger event, or restriction; none is added here, so no `PortGraph` case or `known-coarse-projections.json` entry is expected.

## Glossary gaps

(none — "trample" is a standard keyword; CR 702.19)

## Process notes

- The new parser rule's priority ordering vs. the generic `GainAbilityEffectRule` (995) and its regex anchoring are parser-correctness concerns, out of judge scope (covered by NUnit greenness).
- Delta check (a)-(d) all hold: target structured correctly, no new free-text/unparsed residual, no regression (new file, single faithful ability, out-of-axis nodes intact), cited CR rules exist and match.

**PROCEED** — FAIL count is 0.
