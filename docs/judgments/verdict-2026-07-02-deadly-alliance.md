# MAST judge — batch verdict (deadly-alliance)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-deadly-alliance
**Scope:** 3 files (1 fixture, 2 AST/parser nodes) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ZNC/DeadlyAlliance.json` — PASS. Oracle text byte-matches oracle-cards.json. Party cost reduction ("This spell costs {1} less to cast for each creature in your party.") → `static` ability, `costReduction` effect, `Amount` literal 1, `PerObject: {CardTypes:["creature"], Controller:"You", InParty:true}`. Describe-not-execute: the "up to one each, capped at four" counting is left to the engine; the AST records only the filter scope. No baked-in timing (cost reduction is a static ability applied on cast). Sibling `Destroy target creature or planeswalker` preserved as `spell`/`destroy` with `CardTypes:["creature","planeswalker"]` — byte-identical to the established Dreadbore idiom. Reminder text survives only in verbatim `RawText`/`Input.OracleText` (CR 207.2 exempt fields). No `unparsed`, no free-text `Characteristics`.
- `libs/magic-ast/AST/References/ObjectFilter.cs#InParty` — PASS. New boolean party-membership axis; doc-comment quotes CR 700.8 verbatim and parallels `IsHistoric` (CR 700.6). Correctly rejects encoding "Party" as a subtype (it is a CR 700-level game grouping, not a printed type).
- `libs/magic-ast/Parsing/Parsers/Static/Rules/PartyCostReductionForEachRule.cs` — PASS. Emits the pre-existing `CostReductionEffect` node; strips reminder text (CR 207.2) before the end-anchored regex. Cited CR 700.8 / 207.2 exist in rules-structure.json and match the modeling.
- Projection decision (`InParty` / `costReduction`) — PASS. `costReduction` is already registered coarse in `libs/mast-interaction/known-coarse-projections.json` ("baseline coarse fallback — no flow rule consumes it yet"). `InParty` is a filter refinement of that inert cost-reduction effect and rides its existing coarse projection; a mana-cost reducer is genuinely inert to the interaction/combo port graph, so coarse is sensible.

## Glossary gaps

(none — CR 700.8 "party" is present in rules-structure.json)

## Process notes

- Fixture is a NEW file (git index 00000000), not a regeneration of a prior gold, so there is no prior-gold regression surface; both abilities are present, correctly ordered, and out-of-axis attribute nodes (manaCost/colors/colorIdentity) are standard.
- CR citations cross-referenced against `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`: 700.8 (party — verbatim match), 700.6 (historic — matches parallel use), 207.2 (reminder text — matches).

**ALL PASS**
