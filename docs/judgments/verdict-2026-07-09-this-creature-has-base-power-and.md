# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** this-creature-has-base-power-and
**Branch:** mast/this-creature-has-base-power-and (baseSha 6b4b1d17)
**Scope:** 5 targets (1 fixture, 1 AST node, 1 parser rule, 1 schema entry, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SNC/BrokersInitiate.json` — PASS. Input.OracleText `{4}{G/U}: This creature has base power and toughness 5/5 until end of turn.` is byte-identical to oracle-cards.json (mana {W}, type Cat Citizen, 0/4, colors [W], CI [G,U,W] all verified). The single activated ability is fully structured: mana cost {4}{G/U} (generic 4 + G/U hybrid), one `setBasePT` effect (Subject Self, Power/Toughness literal 5, Duration untilTime Turn/End). No unparsed/unstructured node, no lossy drop or merge. Faithful per CR 208.1 ("power and toughness can be ... set to particular values by effects") and CR 611.1.
- `libs/magic-ast/AST/Effects/Modification/SetBasePTEffect.cs` — PASS. A `ContinuousEffect` that SETS base power and base toughness to two independently-stated `Quantity` values (so unequal boxes and X/X are expressible). Fields Subject/Power/Toughness are rules-grounded; Duration inherited from the base. Doc-comment cites CR 611.1, 208.1, 613.4, 604.3, 613.7 — all present in rules-structure.json and consistent with the modeling. Correctly distinguished from `modifyPT` (adds +X/+Y), `definePT` (CDA single value, CR 604.3), and `becomesCreature` (type/color change).
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/SetBasePTEffectRule.cs` — PASS. Priority 900 activated-effect rule; regex fully anchored `^...until end of turn$` on the trimmed sentence, so it cannot claim compound/target-scoped sibling clauses as substrings. Emits exactly one `SetBasePTEffect`. Cited CR correct. (Parser mechanics themselves are NUnit's scope; only citation/soundness judged here.)
- `libs/magic-ast/schema/ast-schema.json#SetBasePTEffect` — PASS. Auto-generated discriminator entry (`setBasePT`, IsUnparsed false, fields Power/Subject/Toughness). Duration is not listed, matching the convention of sibling ContinuousEffects (setCardTypes, modifyPT, becomesCreature all omit the inherited Duration). SchemaHash bump is the expected regeneration side-effect.
- `libs/mast-interaction/known-coarse-projections.json#setBasePT` — PASS (projection decision). New discriminator `setBasePT` is parked as a justified coarse `effectType` fallback. Sensible: no flow arm reads P/T at all — its closest sibling `definePT` is likewise a coarse entry, and `modifyPT`/`switchPT` carry only inert stable labels (no arm consumes them). The combat-damage arm keys off the static card profile's power, not P/T-mutation ports, so P/T-setting is genuinely inert for interaction recall. The ratchet-required presence is satisfied and the coarse choice is not something a flow rule would clearly want.

## Glossary gaps

None. "Base power and toughness" is covered by CR 208.1; power/toughness are standard glossary terms.

## Process notes

- The coarse-projection reason string calls `setBasePT` a "sibling of the coarse definePT / modifyPT". Strictly, `modifyPT` is not in the coarse file — it is a semantic PortWalkProjection label (`modify:pt`) that is inert-by-design. The substance holds (no flow arm reads any P/T effect), so this is a wording nuance, not a FAIL.
- Fixture Output color-identity attribute is reordered to [W,U,G] vs input [G,U,W]; color identity is a set, so this is canonicalization, not a fidelity loss.

ALL PASS
