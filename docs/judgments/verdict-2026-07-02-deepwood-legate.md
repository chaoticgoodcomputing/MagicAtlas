# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** deepwood-legate (branch `mast-tdd/2026-07-02-deepwood-legate`)
**Scope:** 1 fixture (new) + 1 projection decision — `tests/magic-ast-tests/Fixtures/HandParsedCards/WTH/DeepwoodLegate.json`
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WTH/DeepwoodLegate.json` — PASS. Target line "If an opponent controls a Forest and you control a Swamp, you may cast this spell without paying its mana cost." is modeled as `Kind: static` with `Condition: AllCondition{ CountCondition(Subtypes:[Forest], Controller:Opponent, >=1), CountCondition(Subtypes:[Swamp], Controller:You, >=1) }` + `Effects: [castWithoutPaying{Target: Self}]`. Faithful to CR 118.9's canonical alternative-cost phrasing ("You may cast [this object] without paying its mana cost"); the AND gate is honestly structured (not two independent statics that would read as OR); the two controller axes are split correctly; Forest/Swamp go on `Subtypes` per CR 205.3i, not baked as `CardTypes:["land"]`; describe-not-execute (no pre-evaluation, no baked-in timing). No free-text or unparsed residual anywhere. New card, no regression: the `{B}: +1/+1 until end of turn` sibling (activated, mana cost {B}, `modifyPT` untilTime Turn/End, `IsManaAbility:false`) and all Attributes (mana {3}{B} MV4, colors B, colorIdentity B, creatureStats 1/1) are faithful. Oracle text matches oracle-cards.json exactly. Citations CR 118.9 / 118.9b / 118.9c / 601.3 / 205.3i all exist in rules-structure.json and match the modeling.

- `mast-tdd/2026-07-02-deepwood-legate#projection` — PASS. The branch's only new discriminator is `AllCondition` (`ConditionKind("all")`). ConditionKind is **not** a PortWalk-projected axis — `PortWalkExhaustivenessTests` enumerates only `effectType`, `costType`, `triggerEvent`, and `restriction` — so no `PortGraph`/`PortWalkProjection` entry or `known-coarse-projections.json` entry is required for it. The effect it dispatches (`castWithoutPaying`) is pre-existing (not introduced here) and is already registered coarse with a plausible reason ("baseline coarse fallback — no flow rule consumes it yet"); Deepwood Legate's board-gated free-cast is a niche conditional discount, not something a flow rule would clearly demand, so leaving it coarse is sensible.

## Glossary gaps

(none — Forest, Swamp, alternative cost are standard covered terms)

## Process notes

Fixture is a net-new file (absent at merge base `ea78f976`); "no regression" is judged against internal consistency + the card's full ability set, both of which hold. `AllCondition` is a genuinely new AST condition node but composes existing `Condition` primitives (per its doc-comment, ADR 0007) rather than introducing a free-text residual, and it lives on the condition axis (outside interaction-layer projection), so it triggers no projection obligation.
