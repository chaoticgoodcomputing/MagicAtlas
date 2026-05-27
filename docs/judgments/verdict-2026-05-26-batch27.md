# MAST judge — batch 27 verdict

**Date:** 2026-05-26
**Mode:** verify (judge-pass-2)
**Scope:** 7 files (6 fixtures, 1 new AST node)
**Result:** PASS

## Summary

- PASS: 7
- FAIL: 0

## FAIL verdicts

None.

## PASS verdicts

### Family A — Ascend keyword

- `libs/magic-ast/AST/Effects/Keyword/AscendEffect.cs` — PASS. Doc-comment cites Rule 702.131 with sub-cites to 702.131a (spell-Ascend) and 702.131b (permanent-Ascend); both subrules exist verbatim in `rules-structure.json`. Parameterless shape matches the descriptive doctrine: the rule's "ten or more permanents" threshold and the city's-blessing designation (702.131c) are not encoded — explicitly disclaimed as engine territory in the doc-comment. Discriminator `"ascend"` matches the glossary term lowercase. Inherits the four standard trait interfaces (`IOptionalEffect`, `IDurativeEffect`, `IPreventableEffect`) consistent with `DelveEffect`/`ConvokeEffect`/`ImproviseEffect` precedent.

- `tests/magic-ast-tests/Data/HandParsedCards/RIX/SkymarcharAspirant.json` — PASS. Ascend ability shape `{ Kind: "static", KeywordSource: "Ascend", Effects: [{ EffectType: "ascend", IsOptional: false }], Reminder: {...} }` matches the parameterless-keyword precedent (`MeetingOfMinds` Convoke, `BecomeImmense` Delve) with the reminder-text addition appropriate for Ascend's printed reminder. Sibling ability — `gainAbility` of Flying gated by `asLongAs` Duration referencing "you have the city's blessing" — correctly models "This creature has flying as long as you have the city's blessing" per 702.9 Flying via the evasion sub-effect with the standard `CanBeBlockedBy { CardTypes: ["creature"], Characteristics: ["flying", "reach"] }` filter. Self-target reference is correct for the "This creature" subject.

- `tests/magic-ast-tests/Data/HandParsedCards/RIX/StormFleetSwashbuckler.json` — PASS. Identical Ascend shape. Double strike sibling correctly modeled as `gainAbility` of a `KeywordSource: "Double strike"` static ability containing `EffectType: "combatDamageTiming", Timing: "Both"` per Rule 702.4. `asLongAs` Duration gates the grant.

- `tests/magic-ast-tests/Data/HandParsedCards/RIX/DuskCharger.json` — PASS. Identical Ascend shape. PT-grant sibling uses `EffectType: "modifyPT"` with literal +2/+2 quantities and `asLongAs` Duration on "you have the city's blessing" — established AST convention for conditional PT grants. No engine semantics (layer 7 ordering, etc.) leak into the descriptive shape.

### Family B — Gain-control-of-enchanted (Aura body)

- `tests/magic-ast-tests/Data/HandParsedCards/MindControl.json` — PASS. Enchant ability uses the established `KeywordSource: "Enchant"` + `EffectType: "enchantRestriction"` shape with `LegalTargets.CardTypes: ["creature"]` per Rule 702.5a (Enchant defines what the Aura can target). Gain-control body emits `Effects: [{ EffectType: "gainControl", Target: { Kind: "EnchantedOrEquipped" } }]` — descriptive minimum that matches the briefing's spec. No `Duration` (indefinite-while-attached is implied by the Aura attachment model per 303.4b/c, properly left out per the descriptive-not-executive doctrine). No `IsOptional`/`IfYouDo`/`UnlessClause` — correct, the oracle line carries none.

- `tests/magic-ast-tests/Data/HandParsedCards/Confiscate.json` — PASS. Same shape with `LegalTargets.CardTypes: ["permanent"]`, exercising the parser's broader Enchant-type discriminant. GainControl body identical to MindControl. Note: "permanent" is a card-type catch-all rather than a single CR card type (CR 300+), but this matches existing AST convention for "Enchant permanent" oracle lines and is not a new precedent set by this batch.

- `tests/magic-ast-tests/Data/HandParsedCards/StealEnchantment.json` — PASS. Same shape with `LegalTargets.CardTypes: ["enchantment"]` — third Enchant-type variant. GainControl body identical. The `EnchantedOrEquipped` ObjectReference kind is appropriate for the Aura-body subject regardless of what the Aura attaches to per 303.4b.

## Glossary gaps

None. `Ascend`, `City's Blessing`, `Enchant`, `Flying`, and `Double Strike` are all present in `glossary.json`. The "control" semantics modeled by `GainControlEffect` are not a single glossary term in this dataset — Section 723 covers "Controlling Another Player" (the Mindslaver flavor, not the Aura flavor), and Aura-body control transfer is governed by general continuous-effects rules (611) plus 303.4e (Aura controller separate from enchanted object's controller). The descriptive AST shape (`gainControl` + `EnchantedOrEquipped`) sidesteps the rules-engine question of layer/timestamp ordering, which is correct per MAST doctrine.

## Process notes

- **Family A spell-vs-static collapse verified.** Per check-item 7 in the briefing: Rule 702.131a (spell-Ascend on instants/sorceries) and 702.131b (static-Ascend on permanents) collapse to a single parameterless `AscendEffect` in the AST. The doc-comment explicitly acknowledges both flavors. The three RIX fixtures are all permanents (702.131b territory), but the AST type is correctly card-type-agnostic — a future spell-Ascend fixture (e.g., `Pride of Conquerors`) will reuse the same node without modification.

- **Briefing rule-cite drift, no AST impact.** The briefing text cites "Rule 612 Continuous effects that change control" for Family B. In `rules-structure.json`, rule 612 is "Text-Changing Effects." The correct CR home for Aura-body control transfer is 611 (Continuous Effects) general + 303.4e (Aura controller separation). No AST file or fixture asserts the 612 citation, so this is briefing-prose drift only — no artifact in scope misrepresents a rule. Flagging here so a future briefing audit can correct the reference.

- **Engine-territory disclaimers honored across both families.** Ascend: no "ten or more permanents" condition, no city's-blessing flag. GainControl: no temporary-control duration, no 612.4-style detachment semantics, no controller-change layer logic. Both AST surfaces stay descriptive.

- **Continues the no-HALT streak from batches 23–26.** Six straight clean verdicts.
