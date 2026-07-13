# MAST judge — batch-7 (2026-07-10) remediation RE-JUDGE

**Date:** 2026-07-10
**Branch:** `feat/loop-trial` @ `7c4f2f8f` (base `d480b48b`)
**Scope:** 2 remediation items (1 AST/parser doc-comment citation fix, 1 mechanical dedup reconciliation across 1 AST node + 1 parser rule + 1 gold fixture)
**Result:** PASS

## Summary

- PASS: 2
- FAIL: 0

## PASS verdicts

- **L18 — Kirri return rule** (`.../Triggered/Rules/ReturnTargetTypeOrSubtypeFromGraveyardToHandTriggeredRule.cs`) — PASS.
  The spurious "CR 701.10 (return, the keyword action)" is gone; cross-check confirms CR 701.10 is actually *Double*, so the old citation was wrong. The replacement is accurate: CR 400.7 exists and is the zone-change rule ("An object that moves from one zone to another becomes a new object..."), and return-to-hand is indeed ordinary zone movement, not a CR 701 keyword action (there is no "Return" keyword action in CR 701). The only residual "701" token in the file is the correct negation "not a CR 701 keyword action." Source-zone CR 404.1 (graveyard) and destination CR 402.1 (hand) both present and match `Zone.Graveyard`/hand; CR 205.3 (subtypes, zero-or-more, OR-semantics) present and matches the mixed type-or-subtype disjunction split.

- **L02/L19 — EquippedStateCharacteristic reconciliation** (`.../AST/References/Characteristic.cs#EquippedStateCharacteristic` + L02 rule + KorBlademaster gold) — PASS.
  Reconciliation is faithful. `Characteristic.cs` carries **exactly one** `record EquippedStateCharacteristic : Characteristic` = the L19 superset with `required bool Equipped`; no duplicate, no bare-marker remnant, no conflict markers. `FromLabel` maps `"equipped" => {Equipped=true}` and `"unequipped" => {Equipped=false}`. L02's `EquippedSubtypeCreaturesHaveKeywordRule` now emits `new EquippedStateCharacteristic { Equipped = true }`. `KorBlademaster.json` gold serializes `{"CharacteristicType":"equipped","Equipped":true}` — byte-shape identical to Dalakos's `DalakosCrafterOfWonders.json`. Both golds preserve meaning: Kor Blademaster models `gainAbility` (double strike) to `Each` creature with `Subtypes:[Warrior]` + `Controller:You` + `equipped:true`; Dalakos models the flying/haste anthem over `equipped:true` creatures you control. CR 702.6 (Equip) cited in the node doc and present in the rules data.

## Process notes

Both items are remediation of prior-batch judge findings; no new discriminators introduced, so no PortWalk projection decision is in scope for either. Merge reconciliation left both equipped-state golds structurally aligned (single superset node, one serialization shape).
