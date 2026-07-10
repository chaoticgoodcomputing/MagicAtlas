# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** kalastria-highborn-you-may-pay-b
**Branch:** mast/kalastria-highborn-you-may-pay-b (base b1c7f83)
**Family:** you-may-pay-b-if-you-do-target-p — "you may pay {B}. If you do, target player loses 2 life and you gain 2 life." (Kalastria Highborn, WWK)
**Scope:** 4 files (1 fixture, 2 parser rules, 1 shared whitelist) + 1 projection verdict
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WWK/KalastriaHighborn.json` — PASS. Eventual-truth gold: no `unparsed`/`UnstructuredEffect`, no lossy drop. `Input.OracleText` byte-identical to oracle-cards.json (mana `{B}{B}`, `Creature — Vampire Shaman`, 2/2, colors/CI `[B]` all match). Trigger `{creature, Vampire, You}` correctly subsumes "this creature" because Kalastria Highborn is itself a Vampire, so the "this creature or another Vampire you control" disjunction collapses without loss (CR 205.3m, CR 700.4). Effect chain `optional(conditionalPay {B})` with `ifYouDo = composite[loseLife(target player, 2), gainLife(you, 2)]` faithfully models CR 118.12.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/MayPayTargetPlayerDrainRule.cs` — PASS. Composes pre-existing `OptionalEffect`/`ConditionalPayEffect` and delegates the consequent to the existing `TargetPlayerLoseAndYouGainLifeRule`. Cited CR 118.12 (the "[player] may [do]. If [that player] does, [effect]" cost-on-resolution rule) and CR 603.1 (triggered-ability structure) both exist in rules-structure.json and match the modeling.
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AnotherSubtypeDiesConditionRule.cs` — PASS. Emits existing `TriggerEvent.Dies` with a `{creature, Vampire, You}` filter; capitalised-subtype guard keeps generic "creature" text falling through to `DiesConditionRule`. Cited CR 700.4 ("dies" = put into a graveyard from the battlefield), CR 205.3m (Vampire is an enumerated creature type), CR 603.1 all verified present and consistent.
- `tests/magic-ast-tests/Fixtures/whitelist-freetext.json` — PASS. Narrow, card-scoped entry (`WWK/KalastriaHighborn`, sink `Instructions`, tag `irreducible`). Not rules-load-bearing: the `{B}` cost is structurally carried by `ConditionalPayEffect.Cost` and the optionality by `OptionalEffect`, so the residual "you may pay {B}" string is a redundant adjunct, consistent with the established Deathgreeter/Emiel precedent. Sound, non-widening generalization.
- `mast/kalastria-highborn-you-may-pay-b#projection` — PASS. No new discriminator (`newAstNode=false`): `ConditionalPayEffect`, `OptionalEffect`, `CompositeEffect`, `LoseLifeEffect`, `GainLifeEffect`, and `TriggerEvent.Dies` all pre-exist on base b1c7f83. The two new files are parser rules that only compose existing nodes, so the PortWalk projection ratchet carries no obligation for this branch and no projection entry is expected or missing.

## Glossary gaps

None. "dies" is in glossary.json (→ CR 700.4); "Vampire" is enumerated under CR 205.3m.

## Process notes

The trigger-filter collapse "this creature or another Vampire you control" → `{creature, Vampire, You}` is only lossless because the source card is itself a Vampire; the parser rule's doc-comment states this assumption. Correct for Kalastria Highborn. A future non-Vampire card bearing "this creature or another <Subtype> you control" would not be safe to collapse this way — worth a note if that shape recurs, but not a defect here.

## Verdict

ALL PASS
