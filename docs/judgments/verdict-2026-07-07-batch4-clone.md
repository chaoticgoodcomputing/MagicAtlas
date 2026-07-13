# MAST judge — batch verdict (batch4-clone)

**Date:** 2026-07-07
**Branch:** `mast-tdd/2026-07-07-copy-on-enter` (base `b6b3d402`)
**Scope:** 2 files (1 AST/parser rule, 1 fixture) + 1 projection decision → 3 items
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `libs/magic-ast/Parsing/Parsers/Static/Rules/EnterAsCopyRule.cs` — PASS. Clone's "You may have this creature enter as a copy of any creature on the battlefield" modeled as `StaticAbility{When: asThisEnters}` → `OptionalEffect` → `BecomesCopyEffect{Subject: Self, CopyTarget: {Kind: Any, Filter: {creature, Battlefield}}}`. All three axes (timing / optionality / action) are separate composable nodes — no timing baked into the effect. Cites CR 707.2 (whose own worked example is Clone), 603.6d, 614.1c, 115.1 — all present in `rules-structure.json` and all matching the modeling.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/STH/Clone.json` — PASS. Gold AST mirrors the rule exactly; oracle text is verbatim from CR 707.2's Clone example; typed filter (not free text); no `Duration` field, correctly encoding the permanent (non-`until end of turn`) copy; no `unparsed`/`optional`-hole nodes.
- `EnterAsCopyRule.cs#projection(becomesCopy)` — PASS. Branch adds no new discriminator; the reused `becomesCopy` already carries a justified coarse carve-out in `known-coarse-projections.json`. Coarse is sensible — a copy of an arbitrary battlefield creature can't be statically lowered into a flow-consumable edge, so no flow rule is deprived.

## Judged questions (dispatch checklist)

- **BecomesCopyEffect vs CopyEffect** — CORRECT. The `BecomesCopyEffect` doc explicitly contrasts with `CopyEffect` ("creates a NEW token/spell copy", CR 707.1). Clone *is* the copy — no new object — so an in-place become-a-copy (CR 707.2 copiable values / 707.6 remain-in-place) is the right node, not a create-a-copy effect.
- **asThisEnters timing** — CORRECT. CR 603.6d: "As [this permanent] enters …" text "is a static ability—not a triggered ability." CR 614.1c: such effects "are replacement effects." `StaticAbility{When: asThisEnters}` captures the static replacement, not an ETB trigger.
- **ObjectReferenceKind.Any** — CORRECT. "any creature on the battlefield" carries no "target" keyword; `Any` is documented as an "indefinite controller-choice reference … Not targeted … Distinct from Target (Rule 115.1)." Filter (`CardTypes:["creature"], Zone: Battlefield`) is typed, not free text.
- **"may" → OptionalEffect** — CORRECT. `OptionalEffect{Inner: …}` models the optional replacement.
- **Sibling-mislabel** — CLEAN. Regex is anchored `^…$`; the copy-source sub-pattern is separately anchored `^any … on the battlefield$` and returns `null` (declines) on any unrecognised phrase, so "copy … except [modification]" siblings fall past the anchor and correctly decline rather than mis-parse. Matches only "enter as a copy" lines. No `IUnparsed`/`UnparsedEffect`/`Diagnostics`; descriptive not executive; no dropped sibling (Clone is a single sentence, fully modeled).

## Glossary gaps

None new.

## Process notes

- **CR 117.7 parenthetical (non-blocking).** The `EnterAsCopyRule` doc-comment and the `OptionalEffect` node both cite CR 117.7 for the "may" optionality, but CR 117.7 in `rules-structure.json` is about casting/activating "in response to," not optional actions. This is a pre-existing codebase convention inherited from the `OptionalEffect` node (not introduced by this branch), it is a secondary parenthetical rather than the node's load-bearing citation, and it is present-and-non-contradictory — so it does not meet the FAIL bar (absent-from-data OR contradictory). Recommend the maintainers re-point `OptionalEffect`'s citation to the appropriate optional-action rule in a cleanup pass; flagging here for visibility only.
- **Set folder (out of scope).** Fixture lives under `STH/`; Clone's set attribution is a card-identity detail, not a rules-accuracy concern, and the oracle text is correct for Clone. No verdict impact.
