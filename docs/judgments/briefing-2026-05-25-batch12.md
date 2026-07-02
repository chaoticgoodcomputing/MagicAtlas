# MAST batch 12 — briefing

## 1. Boon of Safety — `{W}` Instant — `Put a shield counter on target creature. (reminder)\nScry 1.`
**Cost:** `{W}`. Two SpellAbilities (per `\n`-split):
1. SpellAbility wrapping `PutCountersEffect{CounterType:"shield", Count:literal 1, Target:Target(creature)}`.
2. SpellAbility wrapping `ScryEffect{Count:literal 1, Player:You}`.
**Note:** the parenthetical reminder text is descriptive flavor — don't model as a separate ability.

## 2. Drill Too Deep — `{1}{R}` Sorcery — Modal
**Oracle:** `Choose one —\n• Put five charge counters on target Spacecraft or Planet you control.\n• Destroy target artifact.`
**Gold:** ModalAbility Min:1/Max:1, two ModalOptions:
- SpellAbility{[PutCountersEffect{CounterType:"charge", Count:literal 5, Target:Target(Filter:{Subtypes:["Spacecraft","Planet"], Controller:You})}]}. Subtypes disjunction.
- SpellAbility{[DestroyEffect{Target:Target(Filter:{CardTypes:["artifact"]})}]}.

## 3. Heritage Reclamation — `{1}{G}` Sorcery
**Oracle:** `Choose one —\n• Destroy target artifact.\n• Destroy target enchantment.\n• Exile up to one target card from a graveyard. Draw a card.`
**Gold:** ModalAbility Min:1/Max:1, three ModalOptions:
- SpellAbility{[DestroyEffect{Target:Target(artifact)}]}
- SpellAbility{[DestroyEffect{Target:Target(enchantment)}]}
- SpellAbility{Effects:[ExileEffect{Target:UpToQuantity 1 + creature filter w/ Zone:Graveyard}, DrawCardsEffect{Count:1, Player:You}]} — multi-effect bundling per `feedback_mast_multi_effect_per_clause` (the period+space within the bullet is in-clause).

## 4. Caravan Escort — `{W}` Creature — Soldier
**Oracle:** `Level up {2} ({2}: Put a level counter on this. Level up only as a sorcery.)\nLEVEL 1-4\n2/2\nLEVEL 5+\n5/5\nFirst strike`
**Cost:** `{W}`. Power/Toughness: `1/1`. Type: "Creature — Soldier" with Keywords `["Level Up"]`. Layout: `leveler`.
**Gold:** LevelUpAbility (existing from batch 3) with:
- LevelUpCost: ActivatedAbility with Costs=[mana 2], Effects=[], IsManaAbility=false, KeywordSource="Level up".
- Stanzas: two LevelStanza entries:
  - {MinLevel:1, MaxLevel:4, Power:Fixed 2/Raw "2", Toughness:Fixed 2/Raw "2", Abilities:[]}
  - {MinLevel:5, Power:Fixed 5/Raw "5", Toughness:Fixed 5/Raw "5", Abilities:[StaticAbility{KeywordSource:"First strike", Effect:CombatDamageTimingEffect{Timing:"First"}}]}
- LayoutAttribute: "leveler"

Reference fixture: `tests/magic-ast-tests/Data/HandParsedCards/ZulaportEnforcer.json` (already in main, similar shape).
