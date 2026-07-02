# MAST judge — batch verdict (flailing-manticore)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-flailing-manticore
**Base:** 90209551
**Scope:** 1 fixture (ODY/FlailingManticore.json) + 1 AST node (ActivatedAbility.WhoMayActivate / ActivationPermission) + 1 projection decision
**Result:** PASS

## Summary

- PASS: 3
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ODY/FlailingManticore.json` — PASS. Oracle text matches oracle-cards.json verbatim (Flying, first strike + two `{1}` self-pump/shrink abilities, each ending "Any player may activate this ability"). Both activated abilities gain `WhoMayActivate: AnyPlayer`; the permission sentence is fully extracted from effect text (no free-text/unparsed residual — the only `RawText` is the top-level verbatim Oracle field). `modifyPT` with `Duration untilTime Turn/End` composites the "until end of turn" timing separately from the effect (no baked-in timing). No regression: Flying evasion, first-strike combat-damage timing, and both `+1/+1` / `-1/-1` siblings all present and correct. CR 602.2 / CR 602.1b.
- `libs/magic-ast/AST/Abilities/ActivatedAbility.cs#WhoMayActivate/ActivationPermission` — PASS. New `ActivationPermission` enum (`Controller` default, `AnyPlayer`) models CR 602.2's "unless the object specifically says otherwise" branch and CR 602.1b's activation-instructions slot ("may state which players can activate that ability"). Correctly framed as a permission BROADENING, distinct from `ActivationRestriction` (narrowing). Discriminator `AnyPlayer` matches CR wording. Cited rules exist in rules-structure.json and match the modeling.
- `mast-tdd/2026-07-02-flailing-manticore#projection:ActivationPermission.AnyPlayer` — PASS (projection sensibility). The branch adds no PortGraph case / coarse-projection entry for the new permission field; that is the sensible decision. Activation-permission broadening is inert for interaction recall — no flow rule reads who-may-activate events, and single-controller combo reconstruction is unaffected by opponents *also* being able to activate. Consistent with the many existing "no flow rule reads ... permission yet" coarse carve-outs (CR 606.3 loyalty-activation, boast-limit override). Nothing a flow rule would clearly want was parked as coarse.

## Glossary gaps

(none)

## Process notes

Task brief cited only the `+1/+1` arm; the real card has a symmetric `-1/-1` arm as well, and the fixture correctly models both (each with the any-player permission). CR 602.2 and 602.1b were cross-referenced directly against rules-structure.json and both exist with matching text. Only the fixture + the ActivatedAbility node + parser were touched (3 files, additive); no out-of-axis nodes or other fixtures changed.
