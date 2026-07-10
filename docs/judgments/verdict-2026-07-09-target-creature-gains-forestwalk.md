# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 parser rule) + 1 projection decision
**Branch:** mast/target-creature-gains-forestwalk
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/NEM/IvyDancer.json` — PASS. Input.OracleText byte-identical to the real Ivy Dancer (verified vs oracle-cards.json). `{T}: Target creature gains forestwalk until end of turn.` is modeled as one `activated` ability with a `tap` cost and a `gainAbility` effect over a `Target` creature; the granted forestwalk is a `static` ability (`KeywordSource: Forestwalk`) whose body is an `evasion` effect with `DefendingPlayerControls` over the `Forest` subtype (CR 702.14b "Landwalk is an evasion ability"; 702.14c "can't be blocked as long as the defending player controls at least one land with the specified land type"), scoped `untilTime` end of turn (CR 611.1 continuous effect for a fixed period). Parenthetical reminder text is fully captured by the structured evasion body — no lossy drop, no IUnparsed, no UnstructuredEffect.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/TargetCreatureGainsLandwalkEffectRule.cs` — PASS. Target-scoped dual of the pre-existing `ThisCreatureGainsLandwalkEffectRule`; emits the existing `GainAbilityEffect`/`StaticAbility`/`EvasionEffect` shape (newAstNode=false). All three cited rules exist in rules-structure.json and match the modeling: CR 702.14 (Landwalk / evasion), CR 611.1 (continuous effect, fixed period), CR 602.1 (activated ability "[Cost]: [Effect.]"). Anchored `^…$` regex restricted to the five basic-land landwalk words; sits at Priority 997 above the generic GainAbilityEffectRule (995) which cannot claim these keywords anyway — collision-free.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/TargetCreatureGainsLandwalkEffectRule.cs#projection` — PASS. No new discriminator (effect/cost type, trigger, restriction) is introduced. The rule reuses `EvasionConditionType.DefendingPlayerControls` and `KeywordAbility.Forestwalk`, both present at baseSha and already covered by the self-grant sibling's PortWalk projection decision. No new `PortGraph`/`PortWalkProjection` entry or coarse-projection justification is required.

## Glossary gaps

None. `Landwalk` (-> rule 702.14) and `Islandwalk` are present in glossary.json.

## Process notes

Verified byte-identity of Input.OracleText against tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json (Ivy Dancer, {2}{G}, Dryad Shaman, 1/2, mono-green — all Attributes match). Dispatch's "Ivy Dancer" card name and the "forestwalk" fragment are consistent; the fixture lives at NEM/IvyDancer.json. Confirmed the reused discriminators and the sibling `ThisCreatureGainsLandwalkEffectRule` pre-exist at baseSha b1c7f836, so shared=[] holds and no new AST surface was added.

ALL PASS
