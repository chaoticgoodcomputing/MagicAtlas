# MAST judge — batch verdict (spin-engine)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-spin-engine
**Base:** cb048c63ea6ae85ef069e0d47244ec68945a5415
**Scope:** 3 targets (1 fixture, 1 AST node field, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/AER/SpinEngine.json` — PASS. Oracle text confirmed verbatim against oracle-cards.json ("{R}: Target creature can't block this creature this turn."). Modeled as one `activated` ability: cost = mana `{R}`; effect = `cantBlock` with `Target` = creature filter (`CardTypes:["creature"]`, from "target creature"), `Blocks` = `Self` (from "this creature", the activating creature), `Duration` = `untilTime` Turn/End (from "this turn"). Timing lives in a separate Duration node — not baked into the effect discriminator (CR 509.1 blocker-side restriction / CR 602.1 activated-ability shape). `IsManaAbility:false` correct.
- `libs/magic-ast/AST/Effects/Combat/CantBlockEffect.cs#Blocks` — PASS. Purely additive optional `Blocks` (`ObjectReference`) naming the specific attacker the restricted creature can't block; null-default = blanket restriction, so existing golds (Copper Carapace) are unaffected. Cited CR 602.1 and CR 509.1 both exist verbatim in rules-structure.json and match the modeling; the doc-comment frames it as the negative dual of `MustBlockEffect.Blocks` (Tangle Angler).
- `mast-tdd/2026-07-02-spin-engine#projection` — PASS. No new effect/cost/trigger discriminator: `Blocks` refines the pre-existing `cantBlock` effect, which already carries a `known-coarse-projections.json` baseline entry ("no flow rule consumes it yet"). Coarse is sensible — a can't-block combat restriction yields no consumable/producible resource edge that a flow rule would want; siblings `cantAttack` / `mustBlock` are coarse for the same reason. Branch touches no projection file; ratchet presence already satisfied.

## Regression check

- New fixture (no base version to regress); single activated ability faithful to the card — no dropped/added/inverted ability.
- Out-of-axis nodes unchanged: `CantBlockEffect.cs` diff has no non-comment deletions (purely additive); schema change is the `Blocks` field entry + `SchemaHash` bump. Attributes (manaCost, colors, colorIdentity, creatureStats) preserved.
- No `"Kind":"unparsed"` / `"EffectType":"unparsed"` anywhere; no free-text residual on the target axis.

## Glossary gaps

(none)

## Process notes

The new parser rule class `TargetCreatureCantBlockThisCreatureThisTurnEffectRule` has a verbose name but emits the plain `cantBlock` discriminator with a composite Duration — timing is not baked into the AST discriminator, so the descriptive shape is correct. Parser correctness itself is NUnit's job, out of judge scope.

**ALL PASS**
