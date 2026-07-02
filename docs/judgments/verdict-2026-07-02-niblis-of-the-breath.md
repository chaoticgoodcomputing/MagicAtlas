# MAST judge — batch verdict

**Date:** 2026-07-02
**Batch:** niblis-of-the-breath
**Branch:** mast-tdd/2026-07-02-niblis-of-the-breath (base 176e495d)
**Scope:** 1 fixture + 1 projection check
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## FAIL verdicts

(none)

## PASS verdicts

- `tests/magic-ast-tests/Fixtures/HandParsedCards/DKA/NiblisOfTheBreath.json` — PASS.
  Oracle text verified against oracle-cards.json verbatim: `Flying\n{U}, {T}: You may tap or untap target creature.`
  (mana `{2}{U}`, Creature — Spirit, 2/1). The target activated ability is structured correctly:
  `Kind: activated`, `Costs: [mana {U}, tap]` (matches `{U}, {T}:`), `IsManaAbility: false`,
  and `Effects: [optional{ Inner: tapOrUntap{ Target: creature } }]` — the "you may" is captured by
  the `optional` wrapper and "tap or untap target creature" by the `tapOrUntap` effect over an
  `ObjectReference{Kind: Target, Filter: CardTypes:["creature"]}`. Describe-not-execute (the
  discriminator names the tap-or-untap *choice*, not a timing/execution), no baked-in timing, no
  free-text or `"unparsed"` residual anywhere. No regression: the Flying evasion sibling and all
  Attributes (manaCost/colors/colorIdentity/creatureStats) are intact.
  CR 701.26a/b cited by the parser rule both exist in rules-structure.json and match verbatim.

- `mast-tdd/2026-07-02-niblis-of-the-breath#projection` — PASS.
  No new discriminator introduced. `TapOrUntapEffect` (libs/magic-ast/AST/Effects/Control/) and the
  `tapOrUntap` EffectType already exist on base, and `known-coarse-projections.json` already carries a
  present, defensible coarse entry ("no flow rule consumes it yet"). The branch adds only a new
  activated-position parser rule (`TapOrUntapTargetActivatedEffectRule`) and the fixture; the
  projection ratchet is already satisfied and the coarse choice is sensible.

## Glossary gaps

(none)

## Process notes

- Diff touches exactly two files: `TapOrUntapTargetActivatedEffectRule.cs` (new parser rule, Priority 995)
  and the new `NiblisOfTheBreath.json` fixture. Nothing out-of-axis changed.
- The fixture is newly added (137 insertions, no deletions) — no prior gold to regress from; both the
  Flying and activated abilities are present and correctly ordered.

## Result

ALL PASS
