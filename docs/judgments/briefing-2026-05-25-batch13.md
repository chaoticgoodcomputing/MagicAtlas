# MAST batch 13 — briefing (FINAL BATCH of 10-batch autonomous run)

## 1. Amazing Acrobatics — `{1}{U}{U}` Instant
**Oracle:** `Choose one or both —\n• Counter target spell.\n• Tap one or two target creatures.`
**Gold:** ModalAbility `ModeSelection{Minimum:1, Maximum:2}` (NOT 1/1 — "or both" = up-to-2), with two ModalOptions:
- SpellAbility{[CounterSpellEffect{Target:Target({CardTypes:["spell"]})}]}
- SpellAbility{[TapEffect{Target:Target({CardTypes:["creature"]})), Count: UpToQuantity{Minimum:1, Maximum:2}}]}. The "one or two" is the Count quantity on the TapEffect (existing TapEffect.Count field from Mishra's Helix).
- AllowDuplicates:false.

## 2. Ikiral Outrider — `{1}{W}` Creature — Cat Soldier — 1/1, Keywords ["Level Up"], Layout leveler
**Oracle:** `Level up {4} ({4}: Put a level counter on this. Level up only as a sorcery.)\nLEVEL 1-3\n2/6\nVigilance\nLEVEL 4+\n3/10\nVigilance`
**Gold:** LevelUpAbility (existing) with cost {4} mana, two stanzas:
- {MinLevel:1, MaxLevel:3, Power:Fixed 2/Raw"2", Toughness:Fixed 6/Raw"6", Abilities:[StaticAbility{KeywordSource:"Vigilance", Effect:VigilanceEffect{}}]}
- {MinLevel:4, Power:Fixed 3/Raw"3", Toughness:Fixed 10/Raw"10", Abilities:[StaticAbility{KeywordSource:"Vigilance", Effect:VigilanceEffect{}}]}
- LayoutAttribute "leveler".
Reference: ZulaportEnforcer / CaravanEscort. Note both stanzas have an inner ability (Vigilance) — extension beyond Zulaport's empty-stanza shape.

## 3. Rain of Rust — `{3}{R}{R}` Sorcery
**Oracle:** `Choose one —\n• Destroy target artifact.\n• Destroy target land.\nEntwine {3}{R} (reminder)`
**Gold:** Two abilities:
- ModalAbility Min:1/Max:1 with two ModalOptions (each SpellAbility wrapping DestroyEffect on artifact/land).
- StaticAbility wrapping EntwineEffect{Cost:ManaCost{Symbols:[generic 3, colored R]}}, KeywordSource:"Entwine".
- AllowDuplicates:false on the modal.
Reference: RoadOfReturn (existing — modal + Entwine pattern).

## 4. Ready to Rumble — `{4}{R}` Sorcery
**Oracle:** `Choose one —\n• Ready to Rumble deals 5 damage to target creature or planeswalker.\n• Destroy target artifact.`
**Gold:** ModalAbility Min:1/Max:1, two ModalOptions:
- SpellAbility{[DealDamageEffect{Source:Self, Amount:literal 5, Target:Target(Filter:{CardTypes:["creature","planeswalker"]})}]}. Self-by-name source. Type-disjunction target.
- SpellAbility{[DestroyEffect{Target:Target({CardTypes:["artifact"]})}]}.
- AllowDuplicates:false.
