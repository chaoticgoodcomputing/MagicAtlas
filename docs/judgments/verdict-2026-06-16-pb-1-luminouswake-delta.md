# MAST delta judge — slice PB-1 / ROE/LuminousWake

**Date:** 2026-06-16
**Slice:** PB-1 — aura IsEnchanted + BearUmbra
**Target (delta):** tests/magic-ast-tests/Fixtures/HandParsedCards/ROE/LuminousWake.json
**Result:** PASS

## What the slice structured (this gold)

The triggered ability's filter "Whenever **enchanted creature** attacks or blocks" previously
carried the attachment as a free-text characteristic:

```
"Filter": { "CardTypes": ["creature"],
            "Characteristics": [{"CharacteristicType": "other", "Description": "enchanted"}] }
```

PB-1 added a flat `bool? IsEnchanted` to `AST/References/ObjectFilter.cs` (mirroring IsSelf/IsToken,
CR 303.4 / 702.5) and routed the "enchanted creature" branch in `TriggeredRuleHelpers.ParseObjectFilter`
to emit it. The gold now reads:

```
"Filter": { "CardTypes": ["creature"], "IsEnchanted": true }
```

## Delta checks

- (a) TARGET residual structured CORRECTLY: yes. The "enchanted creature" qualifier is now the
  structured `IsEnchanted:true` axis on the trigger filter, faithful to the real card. Oracle text
  in the fixture (`Enchant creature\nWhenever enchanted creature attacks or blocks, you gain 4 life.`)
  matches Scryfall oracle-cards.json verbatim. CR 303.4 (Aura) and CR 702.5 (Enchant) both exist in
  rules-structure.json and fit the modeling.
- (b) NO new free-text/unparsed residual: confirmed. No `Characteristic`, `other`, `Description`,
  `unparsed`, or `OtherCharacteristic` remains anywhere on this gold.
- (c) NO regression: the `AttacksOrBlocks` trigger, `creature` CardTypes, and the `gainLife` 4-to-You
  effect are all preserved. Remaining diff is regenerated serialization (key reordering, array
  formatting), the correct drop of `Power/Toughness: null` on an Aura, and a benign default
  `IsVariable:false` on manaCost — no semantic change.

## Projection decision (initiative 03)

`IsEnchanted` is a filter-level predicate, not a new effect/cost-type or trigger-event discriminator.
It mirrors the existing `IsSelf`/`IsToken`/`ExcludeSelf` filter bool axes, none of which carry
PortWalkProjection entries (PortWalkProjection.cs references no ObjectFilter bool axes). No PortWalk
projection decision is required for this filter refinement; absence is consistent with precedent.

## Verdict

- `tests/magic-ast-tests/Fixtures/HandParsedCards/ROE/LuminousWake.json` — PASS. Slice structured the
  "enchanted creature" trigger filter to `IsEnchanted:true`; no out-of-scope residual remains on this
  gold (LuminousWake's only qualifier was the enchanted-creature axis, which the slice fully claimed).

ALL PASS
