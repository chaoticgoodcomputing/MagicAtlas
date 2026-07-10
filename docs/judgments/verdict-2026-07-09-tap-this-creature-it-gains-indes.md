# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files (1 fixture, 1 parse rule) + 1 projection check — branch `tap-this-creature-it-gains-indes`
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

Family: "Tap this creature. It gains indestructible until end of turn." on Drudge Sentinel (ISD).
Base: b1c7f836. Worker: reachedVia=ActivatedAbilityParser.ParseEffects -> TryParseMultiRuleEffects
(IMultiActivatedEffectRule) after StripTrailingReminder, newAstNode=false, shared=[].

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ISD/DrudgeSentinel.json` — PASS.
  - **Input.OracleText byte-identical** to oracle-cards.json: `{3}: Tap this creature. It gains indestructible until end of turn. (Damage and effects that say "destroy" don't destroy it.)`; ManaCost `{2}{B}`, TypeLine, 2/1, colors, color identity all match.
  - Gold models the activated ability as `{3}` mana cost + **two flat sibling effects**: `tap` targeting `Self`, and `gainAbility` targeting `Self` with `Duration untilTime Turn/End`. "It" in the second sentence refers back to "this creature" (the source permanent), so both `Self` targets are correct.
  - `GainedAbility` = `{Kind: static, KeywordSource: Indestructible, Effects: [keywordAbility Indestructible]}` — exactly the shape `ActivatedRuleHelpers.BuildGrantedKeywordAbility("indestructible")` emits; consistent with CR 702.12a ("Indestructible is a static ability").
  - No `unparsed` / `UnstructuredEffect` / `OtherX` nodes; no free text carrying rules structure. Trailing reminder text is correctly dropped (reminder is verbatim-by-design, no parsed semantics — not a lossy drop).
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/TapSelfThenGainsKeywordEffectRule.cs` — PASS. All three doc-comment citations cross-checked against rules-structure.json and match the modeling verbatim: **CR 701.26a** "To tap a permanent, turn it sideways from an upright position." (TapEffect); **CR 611.1** "A continuous effect modifies characteristics of objects … for a fixed or indefinite period." (until-end-of-turn grant); **CR 702.12a** "Indestructible is a static ability." (granted keyword). Rule emits only pre-existing nodes; unmodeled keywords return false and fall through to the residual path rather than emitting a half-structured grant. Mirrors the existing `ExileSelfThenReturnToBattlefieldRule`.
- `libs/magic-ast/Parsing/Parsers/Activated/Rules/TapSelfThenGainsKeywordEffectRule.cs#projection` — PASS. **No new discriminator** (effect/cost type, trigger event, or restriction) is introduced: `tap`, `gainAbility`, and `keywordAbility` effect types all pre-exist and `indestructible` is already handled by `BuildGrantedKeywordAbility`. The change is a new regex parse rule mapping to existing AST nodes (`newAstNode=false`), so initiative-03's exhaustiveness ratchet requires no new `PortGraph` case / `PortWalkProjection` entry / `known-coarse-projections.json` entry.

## Glossary gaps

(none) — "indestructible" is a standard glossary term (CR 702.12).

## Process notes

- No shared/generalization edits landed (worker `shared=[]`; diff is exactly the new rule file + new fixture), so there is nothing to check for over-generalization.
- Priority 948 is shared with `ExileAnotherCreatureThenReturnRule`, but both patterns are end-to-end anchored and mutually exclusive; rule-ordering is a parser-correctness matter (NUnit green), out of judge scope.

**Result: ALL PASS**
