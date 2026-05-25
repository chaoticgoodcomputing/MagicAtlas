# MAST batch 9 — briefing

**Date:** 2026-05-25 **Scope:** 4 candidates.

---

## 1. Hisoka's Defiance — `{1}{U}` Instant — `Counter target Spirit or Arcane spell.`
**Rules:** 701.6 (counter); 109.1 (Subtypes are characteristic; Spirit + Arcane are subtypes, not card types).
**Gold:** SpellAbility wrapping CounterSpellEffect with Target.Filter `{ CardTypes: ["spell"], Subtypes: ["Spirit", "Arcane"] }`.
**Anti-pattern:** Don't put Spirit/Arcane in `CardTypes` — those are card types. Use `Subtypes`.

## 2. Nullify — `{U}{U}` Instant — `Counter target creature or Aura spell.`
**Rules:** 701.6; "creature" is a card type, "Aura" is a subtype.
**Gold:** SpellAbility wrapping CounterSpellEffect with Target.Filter `{ CardTypes: ["spell", "creature"], Subtypes: ["Aura"] }`. The card-type disjunction is "spell OR creature spell"; Aura is a parallel subtype constraint. Note: This is interpretively "Counter (creature OR Aura) spell" — both qualifiers narrow what counts as a valid target.
**Alternative reading:** Could also be `{ CardTypes: ["spell"], Characteristics: ["creature or Aura"] }` — but `Characteristics` is the free-text anti-pattern. Prefer the structured shape above.

## 3. Gift of Strands — `{3}{G}` Enchantment — Aura
**Oracle:** `Flash\nEnchant creature\nWhen this Aura enters, scry 2.\nEnchanted creature gets +3/+3.`
**Rules:** 702.10 (Flash); 702.5 (Enchant); 603.6c (ETB trigger); 113.6 (granted P/T modification on enchanted permanent).
**Gold:** Four abilities:
1. Static / Flash keyword (existing).
2. Static / Enchant creature (existing EnchantRestrictionEffect).
3. Triggered ETB / ScryEffect (existing).
4. Static / `ModifyPTEffect { Target: ObjectReference{Kind:EnchantedOrEquipped}, PowerModifier: literal 3, ToughnessModifier: literal 3 }` (no Duration — anthem-style permanent modification on the enchanted creature).
**Anti-pattern:** Don't include `Duration` on the anthem ModifyPT — it's permanent for as long as the Aura is attached.

## 4. Spell Snuff — `{1}{U}{U}` Instant
**Oracle:** `Counter target spell.\nFateful hour — If you have 5 or less life, draw a card.`
**Rules:** 701.6; 702.83 Fateful hour ability word (the modal-style ability word that grants additional effect when life ≤ 5).
**Gold:** Two SpellAbilities (`\n`-separated):
1. SpellAbility wrapping CounterSpellEffect (Target.Filter `{ CardTypes: ["spell"] }`).
2. SpellAbility with `AbilityWord: "Fateful hour"`, `Effects: [DrawCardsEffect{...}]`, AND **`InterveningIf` field** on the SpellAbility (wait — SpellAbility doesn't have InterveningIf; that's on TriggeredAbility only).

**Design decision:** Fateful hour on a SpellAbility — does the conditional "if you have 5 or less life" live on the ability or on the effect? Looking at the existing structure: SpellAbility has `Effects: List<Effect>` + `Instructions: List<string>?`. No structured conditional. The effect itself doesn't have an InterveningIf.

**Helper choice:** model as SpellAbility with `Instructions: ["If you have 5 or less life"]` + DrawCardsEffect. NOT ideal — Instructions is a free-text string list, anti-pattern territory. But it's the existing AST shape until we add structured conditions to SpellAbility (a wider refactor).

**Alternative — defer this card.** If it can't be modeled without anti-patterns, that's a doctrine FAIL. Recommend the helper either:
(a) Use Instructions: ["If you have 5 or less life,"] as documented free-text fallback (accept the anti-pattern, document it).
(b) Skip Spell Snuff entirely and pick a different 4th card.

Helper should make the call based on which is less bad. If (a), make sure to surface the doctrine concern in the manifest.
