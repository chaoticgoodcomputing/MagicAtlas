# MAST judge — batch verdict (winter-soldier)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-winter-soldier
**Scope:** 1 fixture (`WinterSoldierBuckyBarnes.json`) + 1 parser rule (`SelfNameEntersTappedRule.cs`, not an AST-node change)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/MSC/WinterSoldierBuckyBarnes.json` — PASS.
  Target line "Winter Soldier enters tapped." is modeled as
  `StaticAbility{ Kind:static, When:asThisEnters, Effects:[ TapEffect{ Target:Self } ] }`.
  - **Right node/discriminator:** `Kind:static` matches CR 603.6d ("Such text is a static ability — not a triggered ability"); effect discriminator `tap` is the correct action.
  - **No baked-in timing:** the "as it enters" timing lives in the separate `When:asThisEnters` qualifier — the correct timing-wrapper + plain-effect composite, NOT a `tapOnEntry`-style conflation.
  - **Faithful to card:** oracle text, `{W}`, `Legendary Creature — Human Soldier Hero`, 2/2, colors/identity all verified verbatim against `oracle-cards.json`. Self-reference (`Target.Kind:Self`) correctly captures the by-name short-name self-reference.
  - **Matches corpus canon:** identical to ColdsteelHeart's "This artifact enters tapped." ability and ~40 other enters-tapped fixtures.
  - **No residual / no regression:** no `unparsed`, no free-text `Characteristics`, no escape hatch; brand-new fixture with the card's single ability modeled as a single static ability; attributes (manaCost/colors/colorIdentity/creatureStats) and TypeLine parse are standard and correct.

- `mast-tdd/2026-07-02-winter-soldier#projection` — PASS.
  No new discriminator (effect/cost type, trigger event, restriction) is introduced. The branch adds only a
  parser rule that reuses the existing `tap` effect, `asThisEnters` timing, and `Self` reference. No PortWalk
  projection decision is required; the exhaustiveness ratchet has nothing new to enforce.

## Rule cross-reference

- **CR 603.6d** — present in `rules-structure.json`, verbatim: *"Some permanents have text that reads … '[This permanent] enters tapped.' Such text is a static ability — not a triggered ability — whose effect occurs as part of the event that puts the permanent onto the battlefield."* Matches `Kind:static` + `When:asThisEnters`.
- **CR 614.1d** — present, verbatim: *"Continuous effects that read '[This permanent] enters …' … are replacement effects."* Consistent with the entry-tapped replacement modeling.

## Glossary gaps

None.

## Process notes

Card is a custom-set (MSC) entry but is present in the testbed `oracle-cards.json`; all printed characteristics were confirmed against it. The oracle uses the pre-comma short name ("Winter Soldier") while the full name is "Winter Soldier, Bucky Barnes" — correctly abstracted via `Target.Kind:Self`.

**ALL PASS**
