# MAST judge — batch verdict

**Date:** 2026-05-27
**Scope:** 5 files (3 fixtures, 1 AST node, 1 KeywordDefinitions update)
**Result:** PASS

## Summary

- PASS: 5
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/AST/Effects/Keyword/PlotEffect.cs` — PASS. Models Rule 702.170a with required `Cost` parameter; doc-comment correctly scopes MAST to keyword + cost and defers exile/deferred-cast to engine territory; polymorphic `Cost` base mirrors KickerEffect / UnearthEffect / BestowEffect / EchoEffect.
- `libs/magic-ast/Keywords/KeywordDefinitions.cs` (Plot entry) — PASS. `RuleReference = "702.170"` verified against `rules-structure.json`; `HasParameter = true`, `ParameterType = ManaCost`, `Category = Static`; `CreateExpansion` produces `StaticAbility { KeywordSource = "Plot", Effects = [PlotEffect { Cost }] }` matching gold AST shape.
- `tests/magic-ast-tests/Data/HandParsedCards/OTJ/DjinnOfFoolsFall.json` — PASS. Flying + Plot {3}{U}. Flying uses established `evasion` shape with `flying`/`reach` blocker characteristics; Plot ability records `KeywordSource: "Plot"` + `EffectType: "plot"` + ManaCost {3}{U}. No `unparsed`. Discriminator `"plot"` matches Rule 702.170 terminology.
- `tests/magic-ast-tests/Data/HandParsedCards/OTJ/SpinewoodsPaladin.json` — PASS. Trample + ETB gainLife + Plot {3}{G}. Trample as bare keyword static (CR 702.19); ETB trigger uses `Timing: "When"` + `Event: "Enters"` with `creature` filter (CR 603.6a / 603.10); gainLife with literal Quantity 3 and `Player.Kind: "You"`; Plot mirrors Djinn shape with {3}{G}. No `unparsed`.
- `tests/magic-ast-tests/Data/HandParsedCards/OTJ/SlickshotShowOff.json` — PASS. Flying + haste + modifyPT trigger + Plot {1}{R}. Lowercase `"haste"` KeywordSource is a parser-preservation pattern with precedent at `9ED/HillGigas.json` line 41 — doctrinally acceptable mid-line case preservation, not a defect. SpellCast trigger uses `Whenever` + `noncreature`/`Controller: You` filter; modifyPT carries `Self` target + +2/+0 + `untilEndOfTurn` duration. No `unparsed`.

## Glossary gaps

None. "Plot" is in `glossary.json` with citation to Rule 702.170.

## Process notes

- Rule 702.170 verified literally in `rules-structure.json`: "Plot is a keyword ability that functions while the card with plot is in a player's hand..." The descriptive-not-executive doctrine is correctly applied — the AST captures keyword presence + cost only; the special-action exile (702.170b), the "plotted card" state (702.170a/d), and the deferred-cast timing (702.170d) are engine concerns.
- Lowercase `"haste"` KeywordSource on Slickshot Show-Off is consistent with HillGigas precedent and reflects oracle-text case preservation when a keyword appears mid-line in a comma-separated keyword list. If this ever becomes a normalization concern, it is uniform across the corpus and orthogonal to this batch.
- Test count delta 656 → 662 (+6 = 3 fixtures × 2 tests) consistent with no regressions and three new fixtures landing.
