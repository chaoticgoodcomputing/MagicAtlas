# MAST judge — batch verdict (delta)

**Date:** 2026-07-02
**Branch:** mast-tdd/2026-07-02-phoenix-of-ash
**Task:** phoenix-of-ash — "escapes-with-counter payoff ('This creature escapes with a +1/+1 counter on it')"
**Scope:** 1 fixture (THB/PhoenixOfAsh.json), 2 supporting AST/parser files, 1 projection decision
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## Verification

- **Oracle text**: matches oracle-cards.json verbatim (mana cost {1}{R}{R}, Creature — Phoenix, 2/2, all four oracle lines).
- **CR citations**: CR 702.138b ("A spell or permanent 'escaped' if ... cast from a graveyard with an escape ability") and CR 702.138c ("An ability that reads '[This permanent] escapes with [counters]' means 'If this permanent escaped, it enters with [those counters]'") both exist **verbatim** in rules-structure.json and match the modeling.

## PASS verdicts

- `tests/.../THB/PhoenixOfAsh.json#escapes-with-counter` — PASS. Target line decomposes exactly per CR 702.138c into a static replacement ability: `When: asThisEnters` (the "enters with" replacement, sibling of EntersWithCountersRule) + a **plain** `putCounters` effect (Target Self, CounterType +1/+1, Count literal 1 — "a" = 1) + `Condition: escaped` (the "If this permanent escaped" gate, CR 702.138b). Timing is a separate composable field, NOT baked into the effect (no `putCountersOnEntry`); `escaped` is a structured marker condition, not free text; describe-not-execute (ADR 0004 reference-not-resolution). No unparsed/OtherX/free-text residual on this axis.
- `mast-tdd/2026-07-02-phoenix-of-ash#projection-decision` — PASS. The one new discriminator is `ConditionKind("escaped")`; PortWalk's exhaustiveness ratchet dispatches only on effectType/costType/triggerEvent/restriction, so a Condition needs no projection decision. The produced effect `putCounters` pre-exists on base and is already semantically projected (`emit:counter:<type>:<scope>`) in PortWalkProjection.cs. No coarse/insensible parking.

## Regression check (new fixture)

All five abilities present and faithful, none dropped/added/inverted: Flying (evasion), Haste (keywordAbility), `{2}{R}` activated +2/+0 until end of turn (modifyPT), Escape—{2}{R}{R} exile three (escape + reminder), and the escapes-with-counter payoff. Sibling escape-cost nuances ("other", "from your graveyard") are a different axis, not this task's target.

## Glossary gaps

None.

## Process notes

The `escaped` Condition is a genuinely new discriminator but lands outside the projection ratchet by design (conditions are gates, not dispatched effect/cost/trigger/restriction kinds). The counter-granting effect reuses the already-projected `putCounters`, so interaction-layer recall is unaffected.

ALL PASS
