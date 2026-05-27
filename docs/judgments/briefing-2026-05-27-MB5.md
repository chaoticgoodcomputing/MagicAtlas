# Mega-batch 5 — 40-agent dispatch briefing

**Date:** 2026-05-27
**Pre-batch state:** 1,186 tests / 11,324 cards (38.24%) / 31,842 lines (55.95%) / 32,575 abilities (56.51%)

## Agent roster

### GROUP KW: Keyword batches (4 agents, merge sequentially)

**KW-5:** Riot, Training, Afflict N, Afterlife N, Dethrone
**KW-6:** Devour N, Conspire, Jump-start, Aftermath, Exploit
**KW-7:** Converge, For Mirrodin!, Prepared, Doctor's Companion, Firebending N
**KW-8:** Fuse, Bargain, Spree, Job Select, Warp

### GROUP SP: Unique-file Spell Rules (16 agents, fully parallel)

**SP-17:** DiscardTargetPlayerRule — "Target player discards a card"
**SP-18:** ReturnCardFromGraveyardRule — "Return target card from your graveyard to your hand"
**SP-19:** LookAtTopPutInHandRule — "Look at top three, put one in hand, rest in graveyard"
**SP-20:** SearchLibraryToTopRule — "Search library, shuffle, put card on top"
**SP-21:** TargetDealsPowerDamageRule — "Target creature deals damage equal to its power to target creature"
**SP-22:** TapUpToNTargetsRule — "Tap up to two target creatures"
**SP-23:** TargetPlayerDrawsLosesLifeRule — "Target player draws two cards and loses 2 life"
**SP-24:** CreateTreasureTokenSpellRule — "Create a Treasure token"
**SP-25:** ModifyPTVariableXRule — "Target creature gets +X/+0 until end of turn"
**SP-26:** CreaturesCantBlockThisTurnRule — "Creatures without flying can't block this turn"
**SP-27:** OpponentCreaturesGetMinusRule — "Creatures opponents control get -1/-1 until end of turn"
**SP-28:** DestroyTargetTriTypeRule — "Destroy target artifact, enchantment, or creature with flying"
**SP-29:** ChangeTargetRule — "Change the target of target spell"
**SP-30:** CounterAbilityRule — "Counter target activated/triggered ability"
**SP-31:** ShuffleIntoLibraryRule — "Its owner shuffles it into their library"
**SP-32:** SurveilThenDrawRule — "Surveil N, then draw N cards"

### GROUP TR: Unique-file Triggered Rules (6 agents)

**TR-11:** ReturnEnchantedOnDeathRule — "When enchanted creature dies, return that card"
**TR-12:** CreateSpecificTokenTriggeredRule — specific named tokens (Golem, Servo)
**TR-13:** CastMulticoloredReturnRule — "Whenever you cast multicolored, return from GY"
**TR-14:** FaceUpCounterRule — "When turned face up, put counter on each"
**TR-15:** BlocksTriggerEffects — triggered effects from blocks triggers
**TR-16:** MustBeBlockedAllRule — "All creatures able to block [this/target] creature do so"

### GROUP SA: StaticAbilityParser extensions (5 agents, merge sequentially)

**SA-5:** ArrestPattern — "can't attack or block, and activated abilities can't be activated"
**SA-6:** PhantomMechanic — "prevent damage, remove counter" replacement
**SA-7:** CantBeBlockedByMoreThanOne — "can't be blocked by more than one creature"
**SA-8:** SoulbondPairedEffect — "As long as paired, both have [keyword]"
**SA-9:** AuraCompositeKWandKW — "Enchanted creature gets +N/+N and has KW and KW"

### GROUP SC: Special patterns (5 agents)

**SC-3:** KickerConditionalEntry — "If kicked, enters with N counters"
**SC-4:** LandChooseColorCombo — "This land enters tapped. As it enters, choose a color"
**SC-5:** ConditionalTappedLand — "If you control N+ lands, enters tapped"
**SC-6:** CantAttackUnlessLand — "Can't attack unless defending player controls [land type]"
**SC-7:** LifeGainReplacement — "If you would gain life, gain plus 1 instead"

### GROUP TP: TriggeredAbilityParser extensions (2 agents, merge sequentially)

**TP-4:** BlocksTriggerCondition — "Whenever this creature blocks"
**TP-5:** CastFilteredSpellTrigger — "Whenever you cast a [type] spell" extended filters

### GROUP AC: ActivatedAbilityParser (1 agent)

**AC-2:** More activated ability effect extensions
