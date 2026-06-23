# MAST judge — batch verdict

**Date:** 2026-06-23
**Batch:** dice structural unlocks (extra-combat driver, Class level-up, d20 results-table)
**Scope:** 11 judged targets — 3 gold fixtures, 6 AST/parser files, 3 projection decisions
**Result:** PASS

## Summary

- PASS: 11
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/BreathOfFury.json` — PASS. Oracle verbatim. `enchantRestriction` static + `triggered{DealsCombatDamageToPlayer, IsEnchanted}` -> `composite[sacrifice(It), attach(creature you control), conditional(precedingActionPerformed -> composite[untap(Each creature you control), additionalCombatPhase])]`. Faithful to "sacrifice it and attach … If you do, untap … additional combat phase." No unparsed.
- `libs/magic-ast/AST/Abilities/Condition.cs#PrecedingActionPerformedCondition` — PASS. Field-less marker for the mid-resolution "If you do" idiom; cites CR 101.3 (impossible parts ignored) and explicitly distinguishes itself from a CR 603.12 reflexive trigger (which would put a NEW delayed trigger on the stack). Sound and non-duplicative vs the other 8 `Condition` arms (count, keywordCostPaid, triggeringObjectCounter, quantityComparison, triggeringAbilityIsMana, castThisObject, objectHasSubtype, other).
- `libs/magic-ast/AST/Effects/Timing/AdditionalCombatPhaseEffect.cs` — PASS. Confirmed REUSED: a single pre-existing node (introduced for Combat Celebrant / Bumi, not re-added in the Breath of Fury commit), only one `[OracleEffect("additionalCombatPhase")]` definition exists. CR 500.8 ("effects can add phases … directly after the specified phase") matches the modeling.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/AFR/BarbarianClass.json` — PASS. `class` ability: `BaseAbilities`=the dice-advantage `replacement` (CR 716.3 top section, always active), `Levels`=[L2 `{1}{R}` whenever-you-roll buff, L3 `{2}{R}` haste grant] (CR 716.2). The L2 composite carries BOTH `modifyPT +2/+0` and `gainAbility(menace)`. The card's oracle has no separate Level 1, so the fixture's scope is complete. No unparsed.
- `libs/magic-ast/AST/Abilities/ClassAbility.cs` — PASS. `BaseAbilities` (CR 716.3 always-active top section) + ordered `Levels{Level, Cost, Abilities}` (CR 716.2 level bar = activated + static; CR 107.16). Activation-cost doc paraphrase aligns with CR 602.1a; reminder dropped per CR 207.2. All citations resolve and match.
- `libs/magic-ast/Parsing/Parsers/TriggeredAbilityParser.cs#TryParseGetsPTAndGainsKeyword` — PASS. The in-scope fix delegates the "gets +N/+M and gains <kw> until end of turn" composite to the existing spell composite rules, emitting the flat `[modifyPT, gainAbility]` list rather than letting the single-rule path silently drop the menace grant. Menace modeled as `evasion` + `CanBeBlockedBy{creature}` + `MinimumBlockers:2` per CR 702.111b. Matches gold; non-regressing.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/EarthCultElemental.json` — PASS. `Siege Monster — When this enters, roll a d20.` -> `[rollDie{20}, rollResultsTable{rows}]`. All three rows faithful: 1–9 EachPlayer sac permanent; 10–19 EachOpponent sac permanent; 20 EachOpponent sac permanent Count:2. The single-value "20" row encodes Min==Max==20. No dropped row, no unparsed/IUnparsed. CR 706.2/706.3.
- `libs/magic-ast/AST/Effects/Dice/RollResultsTableEffect.cs` — PASS. `Rows:[{MinResult,MaxResult,Effects}]`, ranges inclusive/closed on both ends, single-value row = Min==Max, composes after `rollDie`. CR 706.3 results-table text matches; row outcomes drawn from the ordinary effect vocabulary.
- `discriminator-projection:rollResultsTable` — PASS. Semantic projection present and sensible: `PortWalkProjection` entry + a dedicated `PortGraph` case (PortGraph.cs:532) recursing every row's effects as GATED ports (Amber floor, CR 706.3 result-gated fan-out). Earth-Cult Elemental's d20 ETB demonstrably projects `emit:rolldice` — the novel-combo PoC reconstructs Emiel/Eldrazi + Drake + Earth-Cult through it. A flow rule clearly wants the row payoffs; correctly NOT parked coarse.
- `discriminator-projection:precedingActionPerformed` — PASS. Projects transparently through the `conditional` effect case (PortGraph.cs:516): the `Then` branch is recursed and its ports marked `Gated`, so a loop through the "if you do" gate floors to Amber. Sound; nothing to park coarse.
- `discriminator-projection:class` — PASS. `class` is an ability-kind container discriminator, OUT of the projection ratchet's scope by design — `PortWalkExhaustivenessTests` scans only `effectType`/`costType`/`triggerEvent`/`restriction`. It follows the established container pattern (Saga/Modal/LevelUp also do not recurse their nested bodies in the interaction layer). This is the pre-existing behaviour for every container kind, not a newly-parked insensible coarse choice — see process note.

## Glossary gaps

(none — Class, menace, roll a die, results table, additional combat phase are all covered by CR subsections 716/702.111/706/500.8)

## Process notes

- **Discriminator baseline:** the regen commit (`7e64525d`) added EXACTLY the three new discriminators — `ConditionKind:precedingActionPerformed`, `OracleAbility:class`, `OracleEffect:rollResultsTable` (lines 18/35/224 of `libs/magic-ast/schema/discriminator-baseline.json`). `additionalCombatPhase` was already in the baseline (line 70) — confirming it is reused, not new.
- **ClauseSplitter dual-cluster hand-resolution:** structurally sound. The Class cluster (`ClassBaseAbilities`/`ClassLevels`) and the results-table cluster (`ResultsTableRowClause`) hang off independent nullable fields on `OracleClause` alongside the pre-existing modal/level-up clusters; they do not collide. All three gold fixtures contain zero `unparsed` nodes.
- **CR citation cross-reference:** every cited rule resolves in `rules-structure.json` and matches its claim — 101.3, 603.12, 500.8, 702.111(a/b/c), 716/716.2/716.3, 107.16, 207.2, 602.1a, 614.1, 706.1/706.1a, 706.2, 706.3. No absent or contradictory citation.
- **Container-kind body projection (known limitation, NOT a fidelity defect):** no ability-kind container (Saga, Modal, LevelUp, Class) recurses its nested bodies in `PortGraph.ProjectAbility`, which reads only the top-level `Effects[]`. A `class` ability whose content lives in `BaseAbilities`/`Levels[].Abilities` therefore projects no interaction ports today. This is the established pattern for ALL container kinds and is out of the projection ratchet's scope; Barbarian Class is not in the interaction corpus, so nothing regressed. Surfaced here only as background — if container-kind body recursion becomes wanted (e.g. a Class level bar that grants a dice/sac engine), it would be an interaction-layer enhancement, not a fidelity fix to this batch.
