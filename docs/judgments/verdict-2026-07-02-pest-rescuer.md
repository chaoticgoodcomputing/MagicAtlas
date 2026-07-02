# MAST judge — batch verdict (delta: pest-rescuer)

**Date:** 2026-07-02
**Scope:** 1 fixture (`PestRescuer.json`), regenerated on branch `mast-tdd/2026-07-02-pest-rescuer`
**Task axis:** life-gain replacement effect — "If you would gain life, you gain that much life plus N instead."
**Result:** PASS

## Summary

- PASS: 1
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/PestRescuer.json#static-replacement` — PASS. Target line models the life-gain augmentation as a static `ReplacementEffect` (`EventType: lifeChange`, `ChangeType: gain`, `Controller: You`, `OriginalEventOccurs: false`, `Modifier: {Type: plus, Amount: 1}`). Correct discriminator, faithful to "you gain that much life plus 1 instead", describe-not-execute, no baked-in timing. CR 614.1a ("Effects that use the word 'instead' are replacement effects") grounds the static-replacement (not triggered) classification; CR 119.3 grounds the life-gain event.

## Cross-checks

- **Oracle text**: fixture `Input.OracleText` and `Oracle.RawText` match `oracle-cards.json` verbatim (incl. the embedded token ability and the plus-1 augmentation clause).
- **CR citations** (all present in `rules-structure.json`, all consistent with the modeling):
  - CR 614.1 — replacement effects "watch for a particular event ... replace that event with a different event". ✓
  - CR 614.1a — "Effects that use the word 'instead' are replacement effects." ✓ (key: static, not triggered)
  - CR 119.3 — life gain/loss adjusts the life total. ✓
  - CR 111.1 — token marker (token-creation sibling). ✓
  - CR 700.4 — "dies" = put into graveyard from battlefield (token's dies-trigger). ✓
  - CR 701.7a — create tokens (token-creation sibling). ✓
- **Node types pre-exist**: `ReplacementEffect`, `LifeChangeEvent`, `ReplacementModifier` already in `libs/magic-ast/AST/Effects/Replacement/`; the new rule mirrors `MillDoublingReplacementRule` / `NoncombatDamageDoublingReplacementRule`. No new discriminator node type introduced, so the `ReplacementEffect` PortWalk projection is inherited — no projection files touched, nothing insensible parked as coarse.
- **No regression**: new fixture; both abilities present and faithful — (1) the upkeep triggered ability creating the 1/1 B/G Pest token, whose quoted "When this token dies, you gain 1 life" is itself fully structured (`TriggeredAbility` Dies/IsSelf + `GainLifeEffect`), and (2) the static replacement. Attributes (manaCost, colors, colorIdentity, creatureStats) and TypeLine intact.

## Process notes

- The token-creation trigger carries an `InterveningIf` free-text `{ConditionType: "other", Text: "you don't control a Pest creature token"}`, whitelisted this batch as `sink: OtherCondition, tag: debt, reason: PB-7 structured-condition buckets`. This residual is on the **intervening-if / structured-condition axis** (PB-7), NOT the life-gain replacement axis this task owns — per the delta-judge carve-out ("a residual on a DIFFERENT axis that another task owns is NOT a fail"), it does not block PASS.
- Minor (non-blocking): `ReplacementModifier.Type` doc-comment lists example values `plusOne`/`plusX`; this rule uses `Type: "plus"` + a parameterized `Amount`. That is a cleaner, generalizing shape (avoids one string per N) and remains structured/faithful — an internal-convention note, not a rules-accuracy fault. Out of judge scope (code review / engine-lens).

## Glossary gaps

(none)
