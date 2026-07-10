# MAST judge — batch verdict

**Date:** 2026-07-09
**Branch:** mast/target-creature-opponent-cant-block
**Base:** aaec9d3b
**Scope:** 2 changed files (1 fixture, 1 parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/JOU/SmeltWardMinotaur.json` — PASS. Input.OracleText byte-identical to Scryfall oracle-cards.json ("Whenever you cast an instant or sorcery spell, target creature an opponent controls can't block this turn."); ManaCost {2}{R}, type line, 2/3, colors [R] all match. Trigger → `SpellCast` + Filter `{CardTypes:[spell,instant,sorcery], Controller:You}`, matching the established convention (Mirari, Young Pyromancer, Bill Potts, Thousand-Year Storm). Effect → `cantBlock` with `Target{Kind:Target, Filter:{CardTypes:[creature], Controller:Opponent}}` ("target creature an opponent controls") and `Duration` = untilTime Turn/End ("this turn", matching Falter). No unparsed nodes, no UnstructuredEffect, no OtherX, no free-text characteristics, no lossy drop/merge. CR 603.2 (triggered abilities) + CR 509.1/.1b (can't-block restriction) both present in rules-structure.json and consistent.

- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/TargetControllerCantBlockThisTurnTriggeredRule.cs` — PASS. New parser rule (no new AST node); maps to the existing `CantBlockEffect`. Doc-comment cites CR 603.2 and CR 509.1 — both exist and match the modeling (509.1b is the specific "effects that say a creature can't block" restriction subrule; parent cite is fine). Anchored regex (`^…$`) at Priority=70 correctly avoids stealing the unqualified surface owned by the sibling `CantBlockThisTurnTriggeredRule`. Controller clause maps to valid `ControllerFilter` values (Opponent/You/DefendingPlayer), each with matching enum semantics. Sound generalization of the controller-scoped surface.

- `mast/target-creature-opponent-cant-block#projection` — PASS. Introduces no new discriminator: reuses the existing `cantBlock` effect type, `SpellCast` trigger event, and `ControllerFilter.Opponent`. The projection ratchet is already satisfied; the pre-existing `known-coarse-projections.json` entry for `cantBlock` ("baseline coarse fallback — no flow rule consumes it yet") is sensible, since a combat-block restriction is genuinely inert for the resource/interaction flow graph.

## Glossary gaps

(none — "block", "declare blockers" covered; card names are verbatim-by-design)

## Process notes

Verified Input.OracleText against tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json — byte-identical. Duration serialization ({DurationType:untilTime, Until:{Part:Turn, Edge:End}}) matches existing golds. Aside (not a FAIL, not in this diff): the pre-existing `CantBlockEffect` doc-comment and sibling rule cite 509.1c (requirements) where 509.1b (restrictions) is the tighter subrule; subrule-letter imprecision is explicitly out of FAIL scope.

ALL PASS
