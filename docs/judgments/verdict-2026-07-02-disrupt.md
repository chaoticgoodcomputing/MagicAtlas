# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** disrupt
**Branch:** mast-tdd/2026-07-02-disrupt
**Base:** cb048c63
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NEM/Disrupt.json` — PASS. Oracle text confirmed verbatim against oracle-cards.json ("Counter target instant or sorcery spell unless its controller pays {1}. / Draw a card."). The tax/soft-counter target line is modeled as `PreventableEffect` (EffectType `preventable`) wrapping an `Inner` `counterSpell` with an `UnlessClause{Player: Controller, Cost: mana {1}}` — a describe-not-execute composite with no baked-in timing. "instant or sorcery spell" is a structured `CardTypes: ["spell","instant","sorcery"]` disjunction (not free text), matching the established sibling convention. Sibling ability "Draw a card" → `drawCards` count 1, Player You — preserved. Out-of-axis nodes (TypeLine, manaCost {U} MV1, colors/colorIdentity U) unchanged. No `unparsed` Kind/EffectType anywhere.
- `libs/magic-ast/Parsing/Parsers/Spell/Rules/CounterTypeOrTypeUnlessPaysRule.cs` — PASS. Doc-comment cites CR 701.6a, CR 701.6b, CR 118.1 and glossary "Counter" — all present verbatim in rules-structure.json / glossary.json and all consistent with the counter + pay-cost modeling. Rule hybridizes the two-type disjunction of `CounterTargetTypeOrSubtypeSpellRule` (pri 80) with the unless-pays wrapper of `CounterUnlessPaysRule` (pri 60) at pri 85; anchored regex + color-word guard.
- `mast-tdd/2026-07-02-disrupt#projection` — PASS. No new discriminator: `PreventableEffect`/`EffectWrap.Preventable`, `CounterSpellEffect`, and `UnlessClause` all pre-exist at the base SHA and are already used by the `CounterUnlessPaysRule` sibling. The diff touches only the new parser rule + fixture — no AST node, PortGraph case, PortWalkProjection entry, or known-coarse-projections.json entry. Nothing new to project, so no projection decision is required.

## Glossary gaps

(none — "Counter", "counter target ... spell", and pay-cost concepts all covered)

## Process notes

Oracle text cross-checked against `tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json` (exact match). CR citations 701.6a / 701.6b / 118.1 verified verbatim in `libs/mtg-rules/Data/_03_Primary/Datasets/rules-structure.json`. The pre-existing convention of listing `"spell"` alongside card types in `CardTypes` (a stack-object pseudo-type, not a CR 205 card type) is a family-wide sibling convention out of this task's axis; it is consistent and structured, not a per-item regression.

ALL PASS
