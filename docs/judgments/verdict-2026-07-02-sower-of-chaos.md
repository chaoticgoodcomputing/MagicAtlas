# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (SowerOfChaos.json), 1 parser rule (CantBlockThisTurnEffectRule.cs, background), 1 projection check
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/SowerOfChaos.json` — PASS. Oracle text is verbatim (`{2}{R}: Target creature can't block this turn.`, confirmed against oracle-cards.json). Activated ability: cost `{2}{R}` (mana, generic 2 + colored R), single effect `cantBlock` on `Target` with `Filter.CardTypes: ["creature"]` ("target creature"), and a SEPARATE composed `Duration` = until end of turn ("this turn"). Timing is a distinct node, not swallowed into the discriminator (`cantBlock`, not `cantBlockThisTurn`). Matches CR 509.1b (a blocker-side "can't block" restriction) and CR 602.1 (activated ability cost/effect form) — both cited rules exist in rules-structure.json and their text matches the modeling.
- `mast-tdd/2026-07-02-sower-of-chaos#projection` — PASS. No new discriminator; `cantBlock`/`CantBlockEffect` already exists at base 176e495 (siblings `CantBlockThisTurnTriggeredRule`, `CreaturesCantBlockThisTurnRule`, static `CantBlockRule`). Branch adds only an activated-ability parser path to the existing effect, so no new PortWalk/PortGraph projection decision is required.

## Regression check

New fixture (added, not modified). Sower of Chaos has exactly one printed ability, faithfully captured; no ability dropped/added/inverted. Out-of-axis attributes all correct: manaCost `{3}{R}` (ManaValue 4), colors/colorIdentity `R`, creatureStats P4/T3, TypeLine Creature — Devil. `IsManaAbility: false` correct.

## Glossary gaps

None.

## Process notes

Free-text/unparsed sweep: no `"Kind": "unparsed"` and no `"EffectType": "unparsed"` anywhere in the ability body; the target subject is structured (`CardTypes: ["creature"]`), not prose. Lowercase card-type value matches the codebase's structured-filter convention.
