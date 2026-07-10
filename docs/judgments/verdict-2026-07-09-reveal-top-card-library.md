# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** reveal-top-card-library
**Branch:** mast/reveal-top-card-library
**Scope:** 5 targets (1 fixture, 1 AST effect node, 1 parser rule, 1 schema edit, 1 projection decision)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NMS/CallOfTheWild.json` — PASS. `Input.OracleText` is byte-identical to the real Nemesis card (verified against oracle-cards.json). Gold models the `{2}{G}{G}` activated ability as a single coupled `revealTopCardMatchToBattlefieldElseGraveyard` effect (`Player {Kind: You}`, `Filter {CardTypes: ["creature"]}`), `IsManaAbility: false`. No `unparsed`, no `unstructured`, no `OtherX`, no lossy drop/merge; the only `Raw`/`RawText` fields are the verbatim-by-design type-line/oracle/mana-cost carriers. Semantics faithful: reveal top card, creature → battlefield, else → graveyard.
- `libs/magic-ast/AST/Effects/CardFlow/RevealTopCardMatchToBattlefieldElseGraveyardEffect.cs` — PASS. Doc-comment cites CR 701.20 (Reveal — 701.20a "To reveal a card, show that card to all players for a brief time"), CR 400.7 ("An object that moves from one zone to another becomes a new object…"), CR 404.1 ("A player's graveyard is their discard pile."). All three exist in rules-structure.json and their text matches the modeling. The "it"-back-reference coupling justification is sound; the filter is a structured `ObjectFilter`, not free text; minimal-fields (no destination enum) follows the sibling reveal-top precedent — a granularity choice, structural critique is out of judge scope.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/RevealTopCardMatchToBattlefieldElseGraveyardEffectRule.cs` — PASS. Anchored (`^…$`) single-effect rule at Priority 96; cited CR rules exist and match; emits structured `CardTypes`/`Subtypes` + `Player`. Parser execution correctness is NUnit's scope, not the judge's; citations and emitted shape are sound.
- `libs/magic-ast/schema/ast-schema.json` (RevealTopCardMatchToBattlefieldElseGraveyardEffect entry) — PASS. Sound generalization: registers the new effect type with `Fields: ["Filter","Player"]`, `IsUnparsed: false`; `SchemaHash` regenerated consistently.
- `libs/mast-interaction/known-coarse-projections.json` (revealTopCardMatchToBattlefieldElseGraveyard entry) — PASS (projection decision, initiative 03). A justified `known-coarse-projections.json` entry. The four sibling keys it cites as precedent (`topLookPutOntoBattlefield`, `putFromHandOntoBattlefield`, `revealTopMayPutMatchingRestToGraveyard`, `revealTopPutMatchingToHand`) all genuinely exist in the file and are already coarse, so the "same coarse precedent" claim is truthful. No flow rule consumes library-cheat-into-play look yet — the coarse choice is sensible and consistent, not something a flow rule clearly wants parked as coarse.

## Glossary gaps

None. "Reveal" is covered by CR 701.20 / glossary; card introduces no term absent from the rules data.

## Process notes

Cross-referenced all three cited CR rules (701.20, 400.7, 404.1) directly against rules-structure.json — all present and textually consistent. Oracle text verified byte-for-byte against tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json. Sibling-precedent claims in the coarse-projections reason string were confirmed present in the committed file rather than taken on faith. Shared-file edits (ast-schema.json, known-coarse-projections.json) are additive, sound generalizations.

**ALL PASS**
