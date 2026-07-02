# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (regenerated gold) on unmerged branch `mast-tdd/2026-07-02-howling-banshee`
**Task:** howling-banshee — ETB symmetric life-loss trigger ("When this creature enters, each player loses 3 life")
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/WTH/HowlingBanshee.json` — PASS. Oracle text matches oracle-cards.json verbatim ("Flying\nWhen this creature enters, each player loses 3 life."). The in-axis line is the target ETB trigger: modeled as a composite `Trigger{Timing:When, Event:Enters, Filter:{CardTypes:[creature], IsSelf:true}}` (CR 603.6, enters-the-battlefield / zone-change trigger) PLUS a plain `loseLife` effect (`Amount` literal 3, `Player:{Kind:EachPlayer}`) — timing and effect kept as separate composable nodes, describe-not-execute, no baked-in timing. Life-loss modeled per CR 119.3 (cited verbatim in the rule doc-comment). "Each player" correctly maps to `ObjectReferenceKind.EachPlayer` (symmetric: controller + all opponents). Flying sibling preserved and structured as an `evasion` static (CanBeBlockedBy creatures with Flying/Reach, CR 702.9b) — out-of-axis but faithful. No `unparsed` Kind/EffectType and no rules-bearing free-text residual anywhere in the abilities. New fixture (all-additions), so no dropped/added/inverted ability.
- `mast-tdd/2026-07-02-howling-banshee#projection` — PASS. The branch adds a parser rule (`EachPlayerLosesLifeRule`) but introduces NO new discriminator: it reuses the pre-existing `LoseLifeEffect` and `ObjectReferenceKind.EachPlayer` (both present at base `c9b1439a`). No new PortGraph case is warranted; PortWalk projection decision is N/A and the exhaustiveness ratchet has nothing new to enforce.

## Citation cross-reference

- **CR 119.3** — "If an effect causes a player to gain life or lose life, that player's life total is adjusted accordingly." Present in rules-structure.json; quoted verbatim in the rule doc-comment; matches the loseLife modeling.
- **CR 603.6** — zone-change / enters-the-battlefield triggers. Present; matches the "When this creature enters" trigger.
- **CR 702.9** — Flying. Present; matches the evasion static on the sibling ability.

## Glossary gaps

(none)

## Process notes

The new `EachPlayerLosesLifeRule` mirrors the established `ThatPlayerLosesLifeRule` pattern, wiring existing nodes rather than adding a discriminator — hence the clean projection verdict.

**ALL PASS**
