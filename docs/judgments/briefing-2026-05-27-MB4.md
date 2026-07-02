# Mega-batch 4 — 40-agent dispatch briefing

**Date:** 2026-05-27
**Pre-batch state:** 1,024 tests / 10,686 cards (36.08%) / 30,764 lines (54.06%) / 31,495 abilities (54.64%)

## Agent roster

### GROUP KW: Keyword batches (4 agents, merge sequentially on KeywordDefinitions + OracleParsers)

**KW-1: Simple evasion/static keywords**
Keywords: Skulk, Horsemanship, Split Second, Battle Cry, Soulbond
Fixtures: Vampire Cutthroat (Skulk+Lifelink), Yellow Scarves General (Horsemanship+CantBlock), Spectral Gateguards (Soulbond+paired vigilance)

**KW-2: Parameterized keywords (N or conditional)**
Keywords: Renown N, Bloodthirst N, Sunburst, Fabricate N, Ingest
Fixtures: Rhox Maulers (Renown 2+Trample), Carnage Wurm (Bloodthirst 3+Trample), Fathom Feeder (Ingest+Devoid+Deathtouch)

**KW-3: Alternate cost / delayed keywords**
Keywords: Rebound, Unleash, Buyback, Learn, Retrace
Fixtures: Staggershock (Rebound+DealDamage), Rakdos Drake (Unleash+Flying), Elvish Fury (Buyback+ModifyPT), Igneous Inspiration (Learn+DealDamage), Flame Jab (Retrace+DealDamage)

**KW-4: Equipment / Aura / special keywords**
Keywords: Living Weapon, Totem Armor (Umbra Armor), Hideaway N, Mobilize N, Start Your Engines
Fixtures: Skinwing (LivingWeapon+Equip+buff), Felidar Umbra (TotemArmor+Enchant+Lifelink), Avenger of the Fallen (Mobilize X+Deathtouch)

### GROUP SP: Unique-file Spell Rules (16 agents, fully parallel)

**SP-1: DestroyTargetQualifiedCreatureRule** — "Destroy target tapped/attacking creature"
Fixture: Vengeance ("Destroy target tapped creature."), Immolating Glare ("Destroy target attacking creature.")

**SP-2: DestroyTargetPowerFilterRule** — "Destroy target creature with power N or greater/less"
Fixture: Divine Verdict-like cards, Wing Snare ("Destroy target creature with flying.")

**SP-3: ExileTargetQualifiedRule** — "Exile target creature with power N or less/greater"
Fixture: Reaver Ambush ("Exile target creature with power 3 or less."), Bring to Trial ("Exile target creature with power 4 or greater.")

**SP-4: ReturnGraveyardToBattlefieldRule** — "Return target creature card from graveyard to battlefield"
Fixture: Edgar's Awakening (single line: "Return target creature card from your graveyard to the battlefield.")

**SP-5: PutOnTopOfLibraryRule** — "Put target creature on top of owner's library"
Fixture: Time Ebb ("Put target creature on top of its owner's library.")

**SP-6: SearchLibraryToHandRule** — "Search your library for a card, put into hand, shuffle"
Fixture: Diabolic Tutor ("Search your library for a card, put that card into your hand, then shuffle.")

**SP-7: SearchLibraryLandToBattlefieldRule** — "Search for basic land, put onto battlefield tapped"
Fixture: Beneath the Sands ("Search your library for a basic land card, put it onto the battlefield tapped, then shuffle.")

**SP-8: DrawThenDiscardRule** — "Draw N cards, then discard M cards"
Fixture: Prying Eyes ("Draw four cards, then discard two cards."), Ghastly Discovery ("Draw two cards, then discard a card.")

**SP-9: TargetDealsDamageToSelfRule** — "Target creature deals damage to itself equal to its power"
Fixture: Repentance, Justice Strike

**SP-10: FightRule** — "Target creature you control fights target creature you don't control"
Fixture: Go for Blood

**SP-11: PutCounterOnEachRule** — "Put a +1/+1 counter on each creature you control"
Fixture: Titania's Boon

**SP-12: DealDamageToEachRule** — "[Source] deals N damage to each creature/player"
Fixture: Rain of Embers ("Rain of Embers deals 1 damage to each creature and each player.")

**SP-13: ReturnMultipleFromGraveyardRule** — "Return up to N target [type] cards from graveyard to hand"
Fixture: Sanguine Indulgence, Macabre Reconstruction

**SP-14: DestroyAllExtendedRule** — "Destroy all [qualified] creatures"
Fixture: "Destroy all creatures. They can't be regenerated."

**SP-15: CounterUnlessPaysRule** — "Counter target spell unless its controller pays {N}"
Fixture: Simple "Counter target spell unless its controller pays {2}"

**SP-16: EachOpponentSacrificesRule** — "Each opponent sacrifices a creature/permanent"
Fixture: Perilous Predicament, Visions of Ruin

### GROUP TR: Unique-file Triggered Rules (10 agents, fully parallel)

**TR-1: DiscardTriggeredRule** — "deals combat damage, that player discards"
**TR-2: LookAtTopCardsTriggeredRule** — "enters, look at top N, put back in order"
**TR-3: AuraReturnToHandTriggeredRule** — "When this Aura goes to graveyard, return to hand"
**TR-4: TransformTriggeredRule** — "if [spell condition], transform this creature"
**TR-5: TapUntapTargetTriggeredRule** — "enters, tap or untap target creature"
**TR-6: ExileTargetSimpleTriggeredRule** — "exile target creature/permanent" (no until-leaves)
**TR-7: ReturnTargetToOwnerHandTriggeredRule** — "enters, return target [nonland] permanent to hand"
**TR-8: SearchLibraryGeneralTriggeredRule** — "enters, search library for [type], put into hand"
**TR-9: GainControlTriggeredRule** — "gain control of target creature until end of turn"
**TR-10: ConditionalPayEffectTriggeredRule** — "you may pay {N}. If you do, [effect]"

### GROUP SA: StaticAbilityParser extensions (4 agents, merge sequentially)

**SA-1: TokenAnthemExtension** — "Creature tokens get +N/+N", "Face-down creatures get +N/+N"
**SA-2: EquipmentConditionalExtension** — "As long as equipped, it gets/has"
**SA-3: LordKeywordGrantExtension** — "[Filter] you control have [keyword]"
**SA-4: BroadAnthemExtension** — "Creatures you control get +N/+N" (no subtype filter)

### GROUP TP: TriggeredAbilityParser trigger conditions (3 agents, merge sequentially)

**TP-1: BeginningOfCombatTrigger** — "At the beginning of combat on your turn"
**TP-2: NonSelfETBTrigger** — "Whenever a creature you control enters" / "Whenever another creature enters"
**TP-3: DrawCardTrigger** — "Whenever you draw a card"

### GROUP AC: ActivatedAbilityParser (1 agent)

**AC-1: SacrificeCreatureActivatedExtension** — "Sacrifice a creature: [effect]" activated patterns

### GROUP SC: Special (2 agents)

**SC-1: EnchantReminderStrip** — "Enchant creature (Target a creature...)" reminder text
**SC-2: ChooseColorOnEntry** — "As this [type] enters, choose a color."

## Merge protocol

1. Merge GROUP SP + TR (unique files) — auto-merge, no conflicts
2. NUnit gate
3. Merge GROUP KW sequentially (KW-1 → KW-2 → KW-3 → KW-4)
4. NUnit gate
5. Merge GROUP SA sequentially (SA-1 → SA-2 → SA-3 → SA-4)
6. NUnit gate
7. Merge GROUP TP sequentially (TP-1 → TP-2 → TP-3)
8. Merge GROUP AC + SC
9. Final NUnit gate
10. Regenerate GLOSSARY
11. Re-run triage
