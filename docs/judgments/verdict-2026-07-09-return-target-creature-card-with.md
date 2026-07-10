# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 AST/parser rule) + 1 projection check
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

_None._

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DriverOfTheDead.json` — PASS. `Input.OracleText` is byte-identical to the printed card ("When this creature dies, return target creature card with mana value 2 or less from your graveyard to the battlefield."). Gold models a `triggered` ability: `Trigger{Timing:When, Event:Dies, Filter{CardTypes:[creature], IsSelf:true}}` ("this creature dies") + `returnToBattlefield` on a `Target` `ObjectReference` filtered `CardTypes:[creature]`, `Zone:Graveyard`, `Controller:You`, `ManaValueComparison{LessThanOrEqual, 2}`, `Tapped:false`. Every oracle clause maps to a structured field — no `IUnparsed`, no `UnstructuredEffect`, no lossy drop/merge, no free text. CR 603.2 (trigger), 400.7/404.1 (zone move from graveyard), 115.1 (target).
- `libs/magic-ast/Parsing/Parsers/Triggered/Rules/ReturnTargetCardWithManaValueFromGraveyardToBattlefieldTriggeredRule.cs` — PASS. Sound new rule: it introduces no new discriminator, reusing the existing `ReturnToBattlefieldEffect` and lifting the printed mana-value bound onto the existing structured `ObjectFilter.ManaValueComparison` axis. Fully anchored (`^…$`); provably mutually exclusive with the plain sibling `ReturnTargetFromGraveyardToBattlefieldTriggeredRule` (sibling requires `card\s+from`; this one requires the interposed `card with mana value …` clause). All four cited rules (CR 400.7, 404.1, 115.1, 603.2) exist in `rules-structure.json` and their text matches the modeling.
- `projection:mast/return-target-creature-card-with` — PASS. No new effect/cost type, trigger event, or restriction discriminator is added; `ReturnToBattlefieldEffect`, `Comparison`, `ComparisonOperator`, and `ObjectFilter.ManaValueComparison` all pre-exist. Initiative-03 PortWalk projection decision is therefore not required.

## Glossary gaps

_None._ ("mana value", "graveyard", "target", triggered "dies" are all standard CR/glossary terms.)

## Process notes

- Branch touches exactly two files (both additions); no shared edits to judge (`shared=[]` holds).
- Verified reused types pre-exist on the base: `ReturnToBattlefieldEffect` (`libs/magic-ast/AST/Effects/ZoneChange/ReturnToBattlefieldEffect.cs`, with `Tapped`), and `ManaValueComparison`/`Comparison`/`ComparisonOperator` (`libs/magic-ast/AST/References/ObjectFilter.cs` + `ObjectFilterRelations.cs`).
- Parser default-to-`Equal` for a bare "mana value N" (no "or less/greater") is a parser-level detail outside the fixture; the fixture correctly uses `LessThanOrEqual 2` for "2 or less". Parser correctness is covered by NUnit, not this judge.

ALL PASS
