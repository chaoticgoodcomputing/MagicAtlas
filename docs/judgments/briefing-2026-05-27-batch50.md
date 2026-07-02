# MAST batch 50 briefing — ETB attach triggered rule

**Date:** 2026-05-27
**Yield cluster:** #8 — `<TRIG> this <SUBTYPE> enters, attach it to target <TYPE> you control.`
**Marginal yield:** 10 cards

---

## Family 1: ETB attach triggered rule

**Failure signal:** `TriggeredAbilityParser.Parse` can't parse `"this Equipment"` as a self-reference
filter (only `"this creature"`, `"this land"`, `"this artifact"`, etc. are handled), so the whole
ETB ability falls through to `UnparsedAbility`. Additionally, no `ITriggeredRule` handles
`"attach it to target [type] you control"` effect text.

### Cards in this family

1. **Cliffhaven Kitesail** — `When this Equipment enters, attach it to target creature you control.` (OtherUnparsedClusters=0)
2. **Bramble Armor** — `When this Equipment enters, attach it to target creature you control.` (OtherUnparsedClusters=0)
3. **Pirate's Cutlass** — `When this Equipment enters, attach it to target Pirate you control.` (OtherUnparsedClusters=0)

### Relevant rules

- **Rule 701.3 (Attach)** — "To take an Aura, Equipment, or Fortification from where it currently is
  and put it onto a specified object or player." MAST records the oracle-text instruction descriptively;
  the zone-change semantics are engine territory.
- **Rule 702.6 (Equip)** — "A keyword ability that lets a player attach an Equipment to a creature
  they control." The ETB attach is a separate triggered ability that fires when the Equipment enters
  the battlefield, not a keyword; MAST models it as a `TriggeredAbility` with an `AttachEffect`.
- **Rule 603.1–603.2** — Triggered abilities use "When", "Whenever", or "At". These are "When" triggers
  on the `Enters` event.

### AST types in scope

- **`TriggeredAbility`** — `[OracleAbility("triggered")]`. Fields: `Trigger: TriggerCondition`, `Effects: IReadOnlyList<Effect>`. Source: `libs/magic-ast/AST/Abilities/TriggeredAbility.cs`.
- **`TriggerCondition`** — `Timing: TriggerTiming (When)`, `Event: TriggerEvent (Enters)`, `Filter: ObjectFilter`. `Filter.CardTypes = ["artifact"]` or `Filter.Subtypes = ["Equipment"]` — use `CardTypes = ["artifact"]` with subtype noted only when the text says "this Equipment" specifically (MAST records what the text says; the subtype word in oracle is the self-reference cue).
- **`AttachEffect`** *(NEW)* — `[OracleEffect("attach")]`. Fields: `Target: ObjectReference`. The `Target` is `Kind = Target, Filter = ObjectFilter { CardTypes = ["creature"], Controller = You }` for the standard pattern, or `Filter = ObjectFilter { Subtypes = ["Pirate"], Controller = You }` for Pirate's Cutlass.
- **`EquipEffect`** — `[OracleEffect("equip")]`. Already handled by the static ability parser. Not modified.

### Expected generalization

One `AttachTriggeredRule` file handles `"attach it to target [filter] you control"` by extracting the
target type/subtype and building an `AttachEffect`. The parser trigger-condition fix is in
`ParseObjectFilter` — add `"equipment"` to the subtype self-reference list alongside `"aura"`.

### Anti-patterns

- Do NOT model the ETB attach as an `EquipEffect`. They are distinct oracle constructs: Equip is an
  activated ability keyword; this is a separate triggered ability that happens to also attach.
- Do NOT encode the rules-engine semantics of attach (zone change, "legalattach" checks, etc.).
  The MAST `AttachEffect` records only what oracle text says: "attach it to target [filter]".
- Do NOT add a `Duration` to `AttachEffect` — attachment is a permanent state change, not a
  duration-limited effect.

### Glossary gaps

None — "Attach" and "Equipment" are both present in glossary.json.
