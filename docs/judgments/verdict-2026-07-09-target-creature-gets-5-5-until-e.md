# MAST judge — batch verdict

**Date:** 2026-07-09
**Batch:** target-creature-gets-5-5-until-e (Ob Nixilis's Cruelty)
**Branch:** mast/target-creature-gets-5-5-until-e
**Scope:** 3 files (1 fixture, 1 new static rule, 1 shared AST edit) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 4
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ZNR/ObNixilissCruelty.json` — PASS. `Input.OracleText` is byte-identical to Scryfall's oracle-cards.json ("Target creature gets -5/-5 until end of turn. If that creature would die this turn, exile it instead."). Sentence 1 -> `modifyPT` (Target creature, -5/-5 literal, `untilTime` Turn/End). Sentence 2 -> `replacement` on `death` event, `OriginalEventOccurs:false`, `Replacement: exile` — a faithful "exile instead of dying" replacement (CR 614.6). Both anaphora ("that creature", trailing "it") -> `Kind:It`. Fully structured: no IUnparsed, no UnstructuredEffect, no `unparsed` Kind/EffectType, no lossy drop/merge. Mirrors the already-landed Incendiary Flow (KLD) death-replacement family.
- `libs/magic-ast/Parsing/Parsers/Static/Rules/ModifyPTThenExileInsteadReplacementRule.cs` — PASS. Emits exactly the gold shape (modifyPT `SpellAbility` + linked death-replacement `StaticAbility`). Cited CR 614.1 (replacement effects), 614.6 (replaced event never happens; modified event occurs), 700.4 ("dies" = put into graveyard from battlefield) all exist in rules-structure.json and match the modeling. "That creature"/"it" -> `ObjectReferenceKind.It`, the documented convention shared with `UntapThatCreatureRule`/`ThreatenRule`.
- `libs/magic-ast/AST/Effects/Replacement/DeathEvent.cs#DyingObject` — PASS. Sound shared generalization: the new optional `DyingObject` `ObjectReference` pins ONE already-identified object by anaphoric reference, a distinct axis from the inherited filter-scoped `AffectedObjects` (Incendiary Flow's "a creature dealt damage this way"). A filter cannot express "this specific target"; a linked reference (ADR 0004 reference-not-resolution) is the right tool. Null-omitted for filter-scoped death events, so pre-existing fixtures are unaffected. CR 700.4 dies-citation on the type is correct.
- `mast/target-creature-gets-5-5-until-e#projection` — PASS. No new discriminator (worker reports `newAstNode=false`). The emitted `modifyPT`, `replacement`, `death` event, and `exile` effect types already have PortWalk/PortGraph projections (`replacement` = intercept port + inner-effect emit per CR 614; `modifyPT` = `modify:pt` emit). `DyingObject` is a reference refinement, not a projection-relevant discriminator, so the exhaustiveness ratchet requires no new `PortGraph`/`PortWalkProjection` entry and none was added — a sensible outcome, not an insensible coarse parking.

## Glossary gaps

(none — "dies" and "replacement effect" are both present in glossary.json with correct CR cites.)

## Process notes

- The "this turn" temporal bound on the second sentence is not encoded as a `Duration` on the `ReplacementEffect` (there is no such field on the node). This exactly matches the already-landed sibling Incendiary Flow fixture; it is a family-wide convention, not a regression introduced by this branch, and there is no existing structured field to carry it, so it is not a free-text shortcut. Surfaced for awareness only.
- Modeling a resolving instant's created replacement as `Kind:static` follows the established Incendiary Flow precedent; structural critique of that choice belongs to the engine-lens audit, not this rules verdict.

**Verdict: ALL PASS — PROCEED.**
