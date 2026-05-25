# MAST batch 11 — briefing

## 1. Resplendent Mentor — `{4}{W}` Creature — Cleric, P/T 1/4
**Oracle:** `White creatures you control have "{T}: You gain 1 life."`
**Gold:** StaticAbility wrapping GainAbilityEffect{Target.Kind:Each, Filter:{CardTypes:["creature"], Colors:["W"], Controller:You}, GainedAbility:ActivatedAbility{Costs:[TapCost], Effects:[GainLifeEffect{Amount:literal 1, Player:You}], IsManaAbility:false}}.
**Anti-pattern:** color filter for "White creatures" — use `Colors: ["W"]`, not free-text.

## 2. Null Elemental Blast — `{C}` Instant
**Oracle:** `Choose one —\n• Counter target multicolored spell.\n• Destroy target multicolored permanent.`
**Cost:** `{C}` — colorless cost; verify ManaSymbol uses `Kind: colorless`. Empty Colors[]/ColorIdentity[].
**Gold:** ModalAbility with ModeSelection{Min:1,Max:1}, Modes=[
  ModalOption{Ability:SpellAbility{Effects:[CounterSpellEffect{Target:Target(Filter:{CardTypes:["spell"], IsMulticolored:true})}]}},
  ModalOption{Ability:SpellAbility{Effects:[DestroyEffect{Target:Target(Filter:{CardTypes:["permanent"], IsMulticolored:true})}]}}
], AllowDuplicates:false.

## 3. Recuperate — `{3}{W}` Sorcery
**Oracle:** `Choose one —\n• You gain 6 life.\n• Prevent the next 6 damage that would be dealt to target creature this turn.`
**Gold:** ModalAbility Min:1,Max:1 with two ModalOptions:
- SpellAbility{Effects:[GainLifeEffect{Amount:literal 6, Player:You}]}
- SpellAbility{Effects:[PreventDamageEffect{Amount:literal 6, Target:Target(creature), Duration:UntilEndOfTurnDuration}]}

PreventDamageEffect exists — check its field shape. Cite Rule 615 (prevention effects).

## 4. Take Down — `{G}` Instant
**Oracle:** `Choose one —\n• Take Down deals 4 damage to target creature with flying.\n• Take Down deals 1 damage to each creature with flying.`
**Gold:** ModalAbility Min:1,Max:1 with two ModalOptions:
- SpellAbility{Effects:[DealDamageEffect{Source:Self, Amount:literal 4, Target:Target(creature with flying — use Characteristics:["with flying"] OR a structured "has flying" indicator)}]}
- SpellAbility{Effects:[DealDamageEffect{Source:Self, Amount:literal 1, Target:Each(creature with flying)}]}

**Anti-pattern question:** "with flying" — is this Characteristics free-text or structured? Characteristics is the documented escape hatch; helper should default to Characteristics:["with flying"] until a structured "has-keyword" filter axis is justified.
