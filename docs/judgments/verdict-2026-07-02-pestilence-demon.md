# MAST judge — batch verdict (pestilence-demon)

**Date:** 2026-07-02
**Scope:** 1 fixture (M10/PestilenceDemon.json) + 1 projection check
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M10/PestilenceDemon.json` — PASS. Oracle text verified verbatim against oracle-cards.json ("Flying\n{B}: This creature deals 1 damage to each creature and each player."). The `{B}` activated ability is modeled as a `composite` of two `dealDamage` effects, both `Source: Self`, `Amount: literal 1` — one targeting `Each` with a `creature` filter and one targeting `EachPlayer`. This is a faithful decomposition of the symmetric ping: "each creature" (source included, no `Other` exclusion, matching the card hitting itself) + "each player". Cost/effect split is clean (CR 602.1), non-combat damage source is Self (CR 120.1), player damage → life loss (CR 119.2). Describe-not-execute: no timing baked into the effect. Flying sibling (static `evasion`) preserved; attributes (manaCost MV 8, colors, colorIdentity, creatureStats 7/6) all correct. No `unparsed` node, no `unparsed` EffectType, no rules-meaningful free text.
- `mast-tdd/2026-07-02-pestilence-demon#projection` — PASS. Branch touches only the fixture + a new parser rule (`SelfDealsDamageToEachCreatureAndPlayerEffectRule.cs`) that emits the existing `CompositeEffect`/`DealDamageEffect` shape with existing `ObjectReferenceKind.Each`/`EachPlayer`. No new effect/cost/trigger/restriction discriminator is introduced, so no PortWalk projection decision is required.

## Rule-citation cross-reference

- CR 602.1 — exists; "Activated abilities have a cost and an effect." Matches the cost/effect split.
- CR 120.1 — exists; "Objects can deal damage to ... creatures ... and players ... the source of that damage." Matches Source: Self dealing damage.
- CR 119.2 — exists; "Damage dealt to a player normally causes that player to lose that much life." Matches EachPlayer target.

## Glossary gaps

None.

## Process notes

New fixture (added file) — no prior version to regress against. Both abilities present and correct; nothing dropped, added, or inverted. The parser rule doc-comment's guards (does not match "each opponent" / "half life total") are consistent with the fixture's specific "each creature and each player" phrasing.

ALL PASS
