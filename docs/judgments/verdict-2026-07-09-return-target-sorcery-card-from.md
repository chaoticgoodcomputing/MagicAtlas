# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** return-target-sorcery-card-from
**Branch:** tdd/return-target-sorcery-card-from
**Scope:** 2 files (1 fixture, 1 AST/parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/TSP/DejaVu.json` — PASS. Déjà Vu ("Return target sorcery card from your graveyard to your hand.") is modeled as a single `spell` ability with one `returnToHand` effect targeting an object filtered by `CardTypes:[sorcery]`, `Zone:Graveyard`, `Controller:You`. `Input.OracleText` is byte-identical to `oracle-cards.json`. No `unparsed`/`UnstructuredEffect`, no free text, no lossy drop or merge — the full sentence is captured. Grounded in CR 608.2 (one-shot resolution), CR 404.1 (graveyard = source), CR 402.1 (hand = destination), CR 115.1 (target).
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ReturnSorceryFromGraveyardSpellRule.cs` — PASS. New `ISpellRule` file that clones the already-vetted `ReturnInstantOrSorceryFromGraveyardSpellRule` for the bare single-type "sorcery" case, emitting the existing `ReturnToHandEffect` + `ObjectFilter`. Discriminator `returnToHand` matches CR concept; `Zone.Graveyard`/`ControllerFilter.You`/`CardTypes:[sorcery]` faithfully encode "your graveyard" + single-type filter. All four cited CR rules (608.2, 404.1, 402.1, 115.1) exist in `rules-structure.json` and their text matches the modeling.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/ReturnSorceryFromGraveyardSpellRule.cs#projection` — PASS. The branch introduces no new discriminator: `returnToHand`, the `sorcery` card type, and the `Zone`/`Controller` filter fields are all pre-existing. `returnToHand` already carries a `PortWalkProjection` entry that projects graveyard instant/sorcery recursion as a spell-recast arm — which sensibly covers this exact addition — so no new projection decision is required by the ratchet.

## Glossary gaps

None. "sorcery", "graveyard", "hand", and "target" are all standard CR terms.

## Process notes

- `newAstNode=false`, `shared=[]` — no `ObjectFilter`/`AbilityClassifier`/`TriggerCondition`/`RuleHelpers` edits; the only changes are the new isolated parser rule + its fixture. Nothing to review for shared-generalization soundness.
- Rule precedence vs the generic `ReturnFromGraveyardToHandRule` (Priority 70 vs default 50) is a parser-correctness matter (NUnit's job), out of scope for rules-accuracy; the doc-comment's mutual-exclusivity note is plausible.
