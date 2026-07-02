# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 fixture (`M19/PlagueMare.json`) on branch `mast-tdd/2026-07-02-plague-mare` — 2 judged targets (effect line + projection)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

_none_

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/M19/PlagueMare.json#triggered-modifyPT` — PASS. Oracle "When this creature enters, creatures your opponents control get -1/-1 until end of turn" verified verbatim against oracle-cards.json. Modeled as `Kind: triggered` with a distinct `Trigger` node (`Timing: When`, `Event: Enters`, `Filter{creature, IsSelf}`) composed with a plain `modifyPT` effect — timing is NOT baked into the effect (satisfies the composite-timing doctrine). The effect uses -1/-1 `literal` power/toughness modifiers, i.e. a layer-7c P/T modification (CR 613.4c, verified verbatim), not a set-P/T; `Target.Kind: Each` with `Filter{creature, Controller: Opponent}` faithfully renders "creatures your opponents control"; `Duration untilTime Turn/End` renders "until end of turn". CR 603.1 (triggered-ability structure) verified verbatim and matches the model. No unparsed/free-text residual. The static sibling "This creature can't be blocked by white creatures" is preserved as `cantBeBlocked` with `BlockedByFilter{creature, Colors:[W]}` (out-of-axis, intact). Attributes intact; no dropped/added/inverted ability.
- `tests/magic-ast-tests/Fixtures/HandParsedCards/M19/PlagueMare.json#projection` — PASS. No new discriminator is introduced. `modifyPT` is a pre-existing effect discriminator (`ModifyPTEffect.cs`, `[OracleEffect("modifyPT")]`); the `Enters` trigger event, `Controller: Opponent` filter, and `Each` target kind all pre-exist. The new `MassModifyPTTriggeredRule` merely delegates to the existing `MassAnthemSpellRule` and emits the exact node the spell parser already produces for the identical surface (Cower in Fear / Make Obsolete). No PortWalk projection decision is required (ratchet enforces presence only for new discriminators).

## Glossary gaps

_none_

## Process notes

Fixture is a new file (no prior version), so "regression" is vacuous — evaluated instead for completeness: both abilities of the card are modeled, correctly, with no residual. Both cited CR rules (603.1, 613.4c) exist in rules-structure.json and their text matches the modeling.
