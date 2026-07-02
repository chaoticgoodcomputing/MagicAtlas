# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** military-intelligence
**Branch:** mast-tdd/2026-07-02-military-intelligence
**Base:** 4618d17338a822bf84df3140d62dfe77a15a617c
**Scope:** 1 fixture (JOU/MilitaryIntelligence.json) + 1 supporting parser rule; 1 projection check
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JOU/MilitaryIntelligence.json` — PASS.
  Oracle text verified against oracle-cards.json: "Whenever you attack with two or more
  creatures, draw a card." Modeled as a single `triggered` ability:
  `Trigger{Timing:Whenever, Event:Attacks, MinimumCount:2, Filter{CardTypes:[creature], Controller:You}}`
  + `Effects:[drawCards, Count literal 1, Player You]`.
  - (a) Correct discriminator/structure: attacker-count-gated attack-declaration trigger. The
    "two or more" threshold is carried by the typed `MinimumCount:2` field (pre-existing on
    TriggerCondition), not free text. "you attack with ... creatures" → creature filter +
    Controller:You. Draw defaults to `Player:You`. Describe-not-execute; no baked-in timing —
    Timing/Event/Effect are separate composable nodes and the effect (`drawCards`) names only the
    action. Consistent with the sibling `AttackWithAndAnotherConditionRule` (Event.Attacks +
    Controller:You) pattern.
  - (b) No new free-text/unparsed residual: no `unparsed`, no `Characteristics`, no rules-bearing
    `*Text`/`*Description`. The only `Raw` fields are verbatim-by-design (manaCost `{1}{U}`,
    TypeLine "Enchantment").
  - (c) No regression: new file, single ability matching the single-sentence oracle; siblings
    preserved (only the fixture + one new parser rule touched — no other AST/enum/fixture files
    changed). Out-of-axis nodes correct: Attributes {manaCost MV 2, colors U, colorIdentity U},
    TypeLine Enchantment.
  - (d) CR citations cross-referenced in rules-structure.json and matching the modeling:
    508.3c ("Whenever [a player] attacks with [a creature]" — attack-with framing),
    508.3d ("Whenever [a player] attacks" — one-or-more/count framing),
    508.1a (active player chooses creatures they control — justifies Controller:You). All present.

- `mast-tdd/2026-07-02-military-intelligence#projection` — PASS.
  Projection decision N/A: the branch adds no new discriminator (effect/cost type, trigger event,
  or restriction). It reuses `TriggerEvent.Attacks` and `TriggerCondition.MinimumCount`, both
  present at the base sha. No new `PortGraph` case / `PortWalkProjection` entry or
  `known-coarse-projections.json` entry is required, and the exhaustiveness ratchet does not fire.

## Glossary gaps

(none)

## Process notes

The diff is exactly two added files: the gold fixture and a new parser rule
(`AttackWithNumberOrMoreCreaturesConditionRule.cs`). No AST node/enum, projection, or sibling
fixture was modified, so there is no regression surface beyond the new fixture itself.

**ALL PASS**
