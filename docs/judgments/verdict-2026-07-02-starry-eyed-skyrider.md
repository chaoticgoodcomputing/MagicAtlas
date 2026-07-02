# MAST judge — batch verdict

**Date:** 2026-07-02
**Scope:** 1 branch (`mast-tdd/2026-07-02-starry-eyed-skyrider`) — delta-judge of regenerated gold fixture `StarryEyedSkyrider.json`, target axis: attack-triggered grant-flying
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/StarryEyedSkyrider.json#attack-trigger` — PASS. Oracle line "Whenever this creature attacks, another target creature you control gains flying until end of turn" is modeled as a `triggered` ability with a distinct `Trigger` node (`Timing: Whenever`, `Event: Attacks`, `Filter.IsSelf: true`) composed with a plain `gainAbility` effect — timing is NOT baked into the effect (CR 603.1). Target is `Kind: Target` with filter `creature` + `Controller: You` ("you control", CR 109.5) + `ExcludeSelf: true` ("another"). `GainedAbility` is a structured flying evasion static ability (`CanBeBlockedBy` = creatures with flying/reach, CR 702.9a/702.9b) — first-class, not a keyword-reference escape hatch, and consistent with the card's top-level Flying line. `Duration` is `untilTime` end-of-turn (CR 611.2a duration-as-stated, CR 514.2 cleanup). No free-text/unparsed residual on this axis; sibling lines (Flying keyword static; "Attacking tokens you control have flying" static) both present, in order, and faithful — no dropped/added/inverted ability.
- `mast-tdd/2026-07-02-starry-eyed-skyrider#projection` — PASS. Branch introduces no new discriminator: the two added parser rules map surface text onto pre-existing `GainAbilityEffect`/`gainAbility`, the `Attacks` trigger event, and `CombatState.Attacking` (all present on base 90209551). No PortWalk projection decision required; nothing for the ratchet to enforce or for the judge to fault.

## Glossary gaps

(none — "attacking creature" is in glossary.json; "flying" and "token" both covered by CR)

## Process notes

All cited CR rules cross-referenced against `rules-structure.json` and confirmed present with matching text: 603.1 (triggered ability = trigger condition + effect), 611.2a (continuous effect lasts as stated, e.g. "until end of turn"), 514.2 ("until end of turn" effects end at cleanup), 109.5 ("you"/"your" = controller), 702.9a/702.9b (flying is evasion; can't be blocked except by flying/reach). The doc-comment rules also cite 604.1/508/205.3/111.1 for the sibling static line — all confirmed present and consistent. Oracle text verified against oracle-cards.json — exact match, including all three lines. This is a new fixture (add), so there is no prior-version baseline to regress against; all three oracle lines are represented.
