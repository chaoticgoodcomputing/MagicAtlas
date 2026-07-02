# MAST batch 10 — briefing

**Date:** 2026-05-25 **Scope:** 4 candidates.

## 1. Myr Moonvessel — `{1}` Artifact Creature — Myr — Power 1, Toughness 1
**Oracle:** `When this creature dies, add {C}.`
**Rules:** 603.6c (dies trigger); 605 (mana abilities); 107.4d ({C}).
**Gold:** TriggeredAbility w/ Trigger{Timing:When,Event:Dies,Filter:{CardTypes:["creature"]}} (self-by-type, existing) + Effects:[AddManaEffect{Mana:"{C}", IsManaAbility unset on effect — it's on the *ability*}].
Wait — dies-trigger that adds mana IS a mana ability per Rule 605.1b ("a triggered ability that triggers from a mana ability"). But more carefully: this is a DIES-triggered ability, not an activated mana ability. CR 605.1b says triggered mana abilities trigger from mana abilities. Myr Moonvessel's trigger is from dying, not from a mana ability — so it's NOT a mana ability. It's a regular triggered ability whose effect happens to be adding mana.
Just: TriggeredAbility {Dies trigger, AddManaEffect{Mana:"{C}"}}. No IsManaAbility flag needed (that's an ActivatedAbility-only field).

## 2. Sachi, Daughter of Seshiro — `{2}{G}{G}` Legendary Creature — Snake Shaman
**Oracle:** `Other Snake creatures you control get +0/+1.\nShamans you control have "{T}: Add {G}{G}."`
**Two abilities:**
- Static / ModifyPTEffect with Target.Kind=Each, Filter `{CardTypes:["creature"], Subtypes:["Snake"], Controller:You, Characteristics:["other"]}` (the "other" excludes Sachi itself). PowerModifier=0, ToughnessModifier=1, no Duration.
- Static / GainAbilityEffect with Target.Kind=Each, Filter `{Subtypes:["Shaman"], Controller:You}` (no CardTypes — "Shamans you control" with Shaman as subtype). GainedAbility=ActivatedAbility{Costs:[TapCost], Effects:[AddManaEffect{Mana:"{G}{G}"}], IsManaAbility:true}.

**Anti-pattern:** Filter for "Shamans" — Shaman is a creature subtype; CardTypes implicit. Match existing TelekineticSliver convention (Batch 5 had `Filter: { Subtypes: ["Sliver"] }` with no CardTypes).

## 3. Coliseum Behemoth — `{4}{R}` Creature — Beast — power 4, toughness 4
**Oracle:** `Trample\nWhen this creature enters, choose one —\n• Destroy target artifact or enchantment.\n• Draw a card.`
**Three abilities:**
- Static / Trample keyword.
- Triggered ETB modal (NEW shape — triggered modal where each mode is a SpellAbility-like effect bundle). Use the `ModalEffect` from Batch 3 (Ao the Dawn Sky pattern). Trigger When/Enters/creature-self. Effects: [ModalEffect{ModeSelection:{Min:1,Max:1}, Modes:[ModalOption{Ability:SpellAbility wrapping DestroyEffect type-disjunction[artifact,enchantment]}, ModalOption{Ability:SpellAbility wrapping DrawCardsEffect}]}].

This exercises ModalEffect inside a triggered ability, which is exactly what ModalEffect was added for in batch 3 but the Ao card was deferred — Coliseum Behemoth is a simpler exemplar.

## 4. Gaddock Teeg — `{G}{W}` Legendary Creature — Kithkin Advisor — 2/2
**Oracle:** `Noncreature spells with mana value 4 or greater can't be cast.\nNoncreature spells with {X} in their mana costs can't be cast.`
**Two abilities, both static "X can't be cast" restrictions:**
- Static / new effect? Or existing? Check `libs/magic-ast/AST/Effects/` for any "cant cast" / "spell restriction" type. If absent, **NEW AST TYPE** `CantBeCastEffect` (parallel to CantBeCounteredEffect). Cite Rule 601.5 (legality of spells).
- Two instances with different filter shapes:
  - First: Filter `{ CardTypes: ["spell"], Characteristics: ["noncreature"], ManaValueComparison: {Operator: GreaterThanOrEqual, Value: 4} }`. Uses the existing `ObjectFilter.ManaValueComparison`.
  - Second: Filter `{ CardTypes: ["spell"], Characteristics: ["noncreature", "with {X} in their mana costs"] }` — uses free-text for the variable-X qualifier (no structured shape for "has X in mana cost" yet).

**Helper:** create CantBeCastEffect with discriminator `cantBeCast`. Cite Rule 601.5.
