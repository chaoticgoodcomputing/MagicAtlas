# MAST judge — batch-3 (2026-07-10) remediation re-judge

**Date:** 2026-07-10
**Scope:** 15 targets (5 golds, 8 AST/parser nodes, 1 coarse-projection entry, 1 relations wiring) on `feat/loop-trial` HEAD
**Result:** PASS

## Summary

- PASS: 15
- FAIL: 0

Remediation commits inspected: `4056d8a0` (H05/H18 cites; H19 structured Keyword), `fa01c843` (ObjectFilter dedup), `d575a18a` (draw-and-lose-life rule + back-prop 2 golds).

## Per-item verdicts (CR cross-referenced against rules-structure.json)

### 1. H05 self-subtype-add — citation fix — PASS
- `RevealTopMayPutSharesCreatureTypeRestToBottomEffect.cs`, its triggered rule, and `known-coarse-projections.json` all now cite **CR 401.4** for the random-order bottom remainder. CR 401.4 exists ("If an effect puts two or more cards in a specific position in a library at the same time, the owner … may arrange them in any order") and is the governing rule for placing multiple cards at a library position. The old **CR 400.4** ("Cards with certain card types can't enter certain zones") was wrong-topic and is gone.
- `SelfIsAlsoSubtypesRule.cs` now cites **CR 205.1b** (verbatim) for additive "is also" retention. CR 205.1b = prior types retained via "in addition to its other types" / "still a [type]" — the correct additive semantics. The old **CR 205.1a** is the type-*set-replaces* case, which contradicted the additive modeling. Fix is correct.

### 2. H18 tap-other-permanent-cost — citation fix — PASS
- `TapSingularSubtypeCostRule.cs` now cites **CR 701.26 / 701.26a** (701.26 "Tap and Untap"; 701.26a "To tap a permanent, turn it sideways… Only untapped permanents can be tapped"). Both exist and match a tap cost. The old **CR 701.20** (Reveal) was wrong-topic and is gone.

### 3. H19 equipped-pt-loses-keyword — structured keyword — PASS
- `LoseAbilityEffect` gains a structured `KeywordAbility? Keyword`; `AbilityText` is demoted to an optional `[FreeTextField]` reserved for genuine ability-SCOPES. `AttachedModifyPTAndLosesKeywordRule.BuildLoseKeyword` routes an enum-parseable keyword to `Keyword`, else `AbilityText`.
- Magebane Armor gold now carries `"Keyword":"Flying"` (no free-text `AbilityText`), and the `(MagebaneArmor, AbilityText)` whitelist-freetext carve-out was removed (verified in the diff). This resolves the original doctrine FAIL — a bare keyword is now structured, not inlined prose.
- AnimateDead ("enchant creature card in a graveyard") and VraskaBetrayalsSting ("all other abilities") correctly retain `AbilityText` — both are genuine, non-enum-expressible ability scopes; unaffected, whitelist entries justified.

### 4. NEW rule + back-prop — PASS
- `YouDrawCardAndLoseLifeTriggeredRule.cs`: anchored `^you draw N card(s) and lose M life$`, **Priority 120** (beats the unanchored `DrawCardsTriggeredRule` default Priority 50 that was silently dropping the lose-life conjunct), emits `CompositeEffect[DrawCardsEffect, LoseLifeEffect]`. Cites **CR 121.1** (draw) + **CR 119.3** (lose life) — both exist and match. Corpus sweep of triggered "draw…and lose…life" forms confirms the `^…$` anchor + fixed count-token set correctly EXCLUDES the over-match siblings: variable-X ("draw X cards and lose X life, where X is…"), different-subject ("each player draws…", "target player draws…"), and compound ("mill three cards, draw a card, and lose 2 life"). Only the literal fixed-N forms match.
- ElegyAcolyte.json (back-propped from whitelisted-unparsed): Lifelink + aggregate combat-damage trigger (`DealsCombatDamageToPlayer`, creature/You, `MinimumCount:1`) → composite draw+lose; Void end-step trigger → `createToken` 2/2 colorless Robot. No `unparsed`/`UnstructuredEffect` (grep 0).
- VoidforgedTitan.json (back-propped from lossy drawCards-only): Void end-step trigger → composite draw+lose. No unparsed.
- The `(VoidforgedTitan, OtherCondition)` and `(ElegyAcolyte, OtherCondition)` whitelist entries are the pre-existing PB-7 Void structured-condition buckets (the `InterveningIf` disjunction), tracked debt unrelated to the lose-life fix — justified and out of remediation scope, per the dispatch.

### 5. DEDUP soundness — PASS
- `ObjectFilter.SharesCreatureTypeWith` is now defined exactly once (`ObjectFilter.cs:146`); the H05/H07 duplicate was removed.
- `ObjectFilterRelations.cs` references it in two axis-mapping call sites — `UndecidedAxis` (518) and `SupUndecidedAxis` (855). These are the two axis-classifier functions, not a duplicate property. Correct wiring.

## FAIL verdicts

None.

## Glossary gaps

None surfaced.

## Process notes (out of scope — not FAILs)

- **Sibling reveal/type nodes still carry the pre-remediation citation pattern.** `RevealTopPutMatchingToHandEffect.cs`, `RevealUntilEffect.cs`, `AbundanceRevealEffect.cs`, `TopLookPutOntoBattlefieldEffect.cs`, `KinnanTopLookEffectRule.cs`, `LookAtTopNPutOneInHandRestBottomActivatedRule.cs` still cite **CR 400.4** for "random order" bottom placement — the same wrong-topic citation the H05 fix just corrected. Most notably `IsChosenCreatureTypeInAdditionRule.cs:20` cites **CR 205.1a** for "in addition to its other types" retention, which is exactly the additive case that H05's fix moved to **CR 205.1b** (205.1a is the set-replaces case). None are in this batch's scope; worth a follow-up citation sweep.
- The Elegy/Voidforged `ConditionType:"other"` + free-text `Text` intervening-if is a tracked PB-7 structured-condition-bucket debt (whitelist-freetext.json), explicitly declared justified by the dispatch; a future initiative should structure the Void "left the battlefield / spell was warped this turn" disjunction.

## Result

**ALL PASS — PROCEED.** The batch-3 remediation resolves all cited FAILs: the H05 (401.4 / 205.1b) and H18 (701.26/701.26a) citation corrections are grounded in rules-structure.json; H19's bare keyword is now structured (`Keyword:"Flying"`) with the free-text carve-out removed; the anchored draw-and-lose-life rule faithfully recovers the dropped lose-life conjunct in both back-propped golds with no unparsed residue; and the ObjectFilter dedup leaves a single sound definition.
