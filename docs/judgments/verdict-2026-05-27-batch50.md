# MAST batch 50 verdict

**Date:** 2026-05-27
**Batch:** ETB attach triggered rule

## Summary

**PROCEED** — 0 FAIL verdicts.

## Items reviewed

### AttachEffect (new AST type)

**PASS.** `[OracleEffect("attach")]` in `libs/magic-ast/AST/Effects/Modification/AttachEffect.cs`.

- Correctly models Rule 701.3 ("To take an Aura, Equipment, or Fortification from where it currently
  is and put it onto a specified object or player") descriptively — records the oracle instruction
  without encoding zone-change mechanics or legality checks.
- `Target: ObjectReference` is the correct field for the attach destination.
- Does not include a `Duration` field — appropriate, since attachment is a permanent state change.
- Implements all four standard trait interfaces (`IOptionalEffect, IDurativeEffect, IPreventableEffect`)
  consistent with the rest of the `Effect` hierarchy.
- Correctly distinguished from `EquipEffect` (which models the activated Equip keyword, Rule 702.6).

### AttachTriggeredRule (new parser rule)

**PASS.** `[TriggeredRule]` in `libs/magic-ast/Parsing/Parsers/Triggered/Rules/AttachTriggeredRule.cs`.

- Regex matches `"attach it to target <filter> you control"` at the effect-fragment level.
- Correctly distinguishes card-type targets (e.g., "creature") from subtype targets (e.g., "Pirate")
  and routes each to the appropriate `ObjectFilter` field.
- `Controller = You` correctly set from the "you control" qualifier.
- Does not touch `EquipEffect` parsing, which is handled by the static ability parser separately.

### ParseObjectFilter — equipment subtype self-reference

**PASS.** Added `"equipment"` to the `selfType` list alongside `"aura"` in `TriggeredAbilityParser.cs`.

- Correctly treats `"this Equipment"` as a self-reference, returning `CardTypes = ["equipment"]`.
- Follows the established `"this aura"` convention — records the word oracle text uses
  (descriptive), not the rules-canonical card type (`"artifact"`).

### Fixtures

**PASS** — all three fixtures correctly model their oracle text:

- **CliffhavenKitesail** — ETB attach + `gainAbility` flying + Equip {2}. Colorless artifact.
- **BrambleArmor** — ETB attach + `modifyPT +2/+1` + Equip {4}. Green artifact.
- **PiratesCutlass** — ETB attach (target Pirate) + `modifyPT +2/+1` + Equip {2}. Colorless artifact.
  Correctly uses `Subtypes: ["Pirate"]` (not `CardTypes`) for the creature-type restriction.
