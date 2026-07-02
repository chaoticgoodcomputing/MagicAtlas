# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** rubblebelt-runner
**Branch:** mast-tdd/2026-07-02-rubblebelt-runner (base 176e495)
**Scope:** 1 fixture + 1 projection decision (task: "an evasion restriction static — 'This creature can't be blocked by creature tokens'")
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/GTC/RubblebeltRunner.json` — PASS. Oracle text verified verbatim against oracle-cards.json ("This creature can't be blocked by creature tokens."). Modeled as one `static` ability with a single `cantBeBlocked` effect carrying `BlockedByFilter: { CardTypes: ["creature"], IsToken: true }` — the right node/discriminator, faithful to the card, describe-not-execute, with no timing baked in. "creature tokens" is decomposed into the structured token predicate axis (`IsToken: true` per CR 111.1) combined with the implicit `creature` card type — not a free-text card-type/subtype string. Mirrors the sibling `CantBeBlockedRule` color/subtype/power arms. No `unparsed` Kind or `EffectType`, no free-text residual on any axis. Out-of-axis nodes (manaCost {1}{R}{G}→MV 3, colors G/R, colorIdentity, 3/3 creatureStats, TypeLine Lizard Warrior) all match Input. New fixture — no prior ability to drop/invert/regress.

- `mast-tdd/2026-07-02-rubblebelt-runner#projection` — PASS. No NEW discriminator is introduced: the branch reuses the pre-existing `cantBeBlocked` effect type (already used by the sibling rule) and the pre-existing `ObjectFilter.IsToken` axis (present on base 176e495, with equality + subsumption axes already wired, cited CR 111). `cantBeBlocked` already carries an explicit coarse projection decision in `libs/mast-interaction/known-coarse-projections.json` ("no flow rule consumes it yet"). That coarse choice is sensible — an evasion/blocking restriction is genuinely inert for combo/flow reconstruction; no flow rule would want it as a produced/consumed resource. Ratchet presence is satisfied; the choice is not insensible.

## Rule cross-reference

- **CR 509.1b** — exists. Text: "The defending player checks each creature they control to see whether it's affected by any restrictions (effects that say a creature can't block, or that it can't block unless some condition is met). If any restrictions are being disobeyed, the declaration of blockers is illegal." CR 509.1 preamble further defines evasion abilities ("a static ability an attacking creature has that restricts what can block it"). Matches the modeling exactly.
- **CR 111.1** — exists. Text: "A token is a marker used to represent any permanent that isn't represented by a card." Grounds the `IsToken` predicate-axis encoding (token is not a card type/subtype), matching the doc-comment's rationale.

## Glossary gaps

None.

## Process notes

The new parser file `CantBeBlockedByTokensRule.cs` (Priority 956) is a separate collision-free rule from the sibling `CantBeBlockedRule` (Priority 955) — parser correctness is out of judge scope (NUnit's job), but the AST shape it emits is judged above and is correct.

**Result: ALL PASS**
