namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Registry of all standard Magic keyword definitions and their expansions.
/// Each keyword expands to a semantically equivalent ability subtree.
/// </summary>
public static class KeywordDefinitions
{
  // ═══════════════════════════════════════════════════════════════════════════
  // EVASION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Flying: This creature can't be blocked except by creatures with flying or reach.
  /// Rule 702.9
  /// </summary>
  public static KeywordDefinition Flying { get; } =
    new()
    {
      Name = "Flying",
      RuleReference = "702.9",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["flying", "reach"],
          },
        }],
      },
    };

  /// <summary>
  /// Fear: This creature can't be blocked except by artifact creatures and/or black creatures.
  /// Rule 702.36
  /// </summary>
  public static KeywordDefinition Fear { get; } =
    new()
    {
      Name = "Fear",
      RuleReference = "702.36",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Fear",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "black"],
          },
        }],
      },
    };

  /// <summary>
  /// Shadow: This creature can't be blocked except by creatures with shadow, and a
  /// creature without shadow can't be blocked by creatures with shadow.
  /// Rule 702.28. Mutual evasion: only shadow can block shadow. EvasionEffect with
  /// CanBeBlockedBy restricted to the "shadow" characteristic.
  /// </summary>
  public static KeywordDefinition Shadow { get; } =
    new()
    {
      Name = "Shadow",
      RuleReference = "702.28",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Shadow",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["shadow"],
          },
        }],
      },
    };

  /// <summary>
  /// Intimidate: This creature can't be blocked except by artifact creatures and/or
  /// creatures that share a color with it.
  /// Rule 702.13. EvasionEffect with CanBeBlockedBy covering the artifact-type and
  /// shares-a-color predicates; mirrors Fear (702.36) but substitutes the color-share
  /// predicate for the fixed black-color predicate.
  /// </summary>
  public static KeywordDefinition Intimidate { get; } =
    new()
    {
      Name = "Intimidate",
      RuleReference = "702.13",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Intimidate",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "shares a color"],
          },
        }],
      },
    };

  /// <summary>
  /// Menace: This creature can't be blocked except by two or more creatures.
  /// Rule 702.111. Evasion keyword whose distinguishing feature is a minimum
  /// blocker count rather than a characteristic filter on the blockers.
  /// </summary>
  public static KeywordDefinition Menace { get; } =
    new()
    {
      Name = "Menace",
      RuleReference = "702.111",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Menace",
        Effects = [new EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
          MinimumBlockers = 2,
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // COMBAT DAMAGE TIMING KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// First Strike: This creature deals combat damage before creatures without first strike.
  /// Rule 702.7
  /// </summary>
  public static KeywordDefinition FirstStrike { get; } =
    new()
    {
      Name = "First strike",
      RuleReference = "702.7",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
    };

  /// <summary>
  /// Double Strike: This creature deals both first-strike and regular combat damage.
  /// Rule 702.4
  /// </summary>
  public static KeywordDefinition DoubleStrike { get; } =
    new()
    {
      Name = "Double strike",
      RuleReference = "702.4",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DAMAGE-RELATED KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Lifelink: Damage dealt by this creature also causes you to gain that much life.
  /// Rule 702.15
  /// </summary>
  public static KeywordDefinition Lifelink { get; } =
    new()
    {
      Name = "Lifelink",
      RuleReference = "702.15",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new LifelinkEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DURABILITY KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Indestructible: This permanent can't be destroyed. Damage and "destroy"
  /// effects that would destroy it have no effect.
  /// Rule 702.12
  /// </summary>
  public static KeywordDefinition Indestructible { get; } =
    new()
    {
      Name = "Indestructible",
      RuleReference = "702.12",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Indestructible",
        Effects = [new IndestructibleEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // ENTERS-THE-BATTLEFIELD TRIGGERED KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Evolve: Whenever a creature you control enters, if that creature has greater
  /// power or toughness than this creature, put a +1/+1 counter on this creature.
  /// Rule 702.100. Although mechanically a triggered ability, MAST records it as
  /// a keyword marker — same approach as Prowess, Exalted, Cascade — and treats
  /// the canonical trigger / power-comparison / counter-placement as engine territory.
  /// </summary>
  public static KeywordDefinition Evolve { get; } =
    new()
    {
      Name = "Evolve",
      RuleReference = "702.100",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Evolve",
        Effects = [new EvolveEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // STACK RESTRICTION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Split second: As long as this spell is on the stack, players can't cast spells
  /// or activate abilities that aren't mana abilities.
  /// Rule 702.61. MAST records the keyword's presence; the stack-restriction
  /// semantics are engine territory.
  /// </summary>
  public static KeywordDefinition SplitSecond { get; } =
    new()
    {
      Name = "Split second",
      RuleReference = "702.61",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Split second",
        Effects = [new SplitSecondEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // COMBAT TRIGGERED KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Flanking: Whenever a creature without flanking blocks this creature, the blocking
  /// creature gets -1/-1 until end of turn.
  /// Rule 702.25. Although mechanically triggered, MAST models it as a keyword marker
  /// (same approach as Exalted); the trigger-and-debuff expansion is engine territory.
  /// </summary>
  public static KeywordDefinition Flanking { get; } =
    new()
    {
      Name = "Flanking",
      RuleReference = "702.25",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Flanking",
        Effects = [new FlankingEffect()],
      },
    };

  /// <summary>
  /// Melee: Whenever this creature attacks, it gets +1/+1 until end of turn for each
  /// opponent you attacked with a creature this combat.
  /// Rule 702.121. Although mechanically a triggered ability, MAST models it as a
  /// keyword marker (same approach as Flanking, Evolve, Exalted); the attack-count
  /// comparison and +1/+1 grant are engine territory.
  /// </summary>
  public static KeywordDefinition Melee { get; } =
    new()
    {
      Name = "Melee",
      RuleReference = "702.121",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Melee",
        Effects = [new MeleeEffect()],
      },
    };

  /// <summary>
  /// Mentor: Whenever this creature attacks, put a +1/+1 counter on target attacking
  /// creature with power less than this creature's power.
  /// Rule 702.134. Although mechanically a triggered ability, MAST records the
  /// keyword's presence only — the trigger / target-selection / counter-placement
  /// are engine territory (same approach as Evolve, Flanking, Exalted).
  /// </summary>
  public static KeywordDefinition Mentor { get; } =
    new()
    {
      Name = "Mentor",
      RuleReference = "702.134",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Mentor",
        Effects = [new MentorEffect()],
      },
    };

  /// <summary>
  /// Battle cry: Whenever this creature attacks, each other attacking creature gets
  /// +1/+0 until end of turn.
  /// Rule 702.91. Although mechanically a triggered ability, MAST models it as a
  /// keyword marker (same approach as Flanking, Evolve, Exalted); the attack trigger
  /// and pump expansion are engine territory.
  /// </summary>
  public static KeywordDefinition BattleCry { get; } =
    new()
    {
      Name = "Battle cry",
      RuleReference = "702.91",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Battle cry",
        Effects = [new BattleCryEffect()],
      },
    };

  /// <summary>
  /// Soulbond: You may pair this creature with another unpaired creature when either
  /// enters. They remain paired for as long as you control both of them.
  /// Rule 702.95. MAST records the keyword's presence; the pairing mechanics and any
  /// granted abilities are engine territory (same approach as Flanking and Evolve).
  /// </summary>
  public static KeywordDefinition Soulbond { get; } =
    new()
    {
      Name = "Soulbond",
      RuleReference = "702.95",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Soulbond",
        Effects = [new SoulbondEffect()],
      },
    };

  /// <summary>
  /// Myriad: Triggered keyword ability. Whenever this creature attacks, for each
  /// opponent other than defending player, you may create a token copy tapped and
  /// attacking that player or a planeswalker they control; exile the tokens at end
  /// of combat.
  /// Rule 702.116. MAST records keyword presence; the per-opponent copy-creation,
  /// tapped-and-attacking, and delayed-exile semantics are engine territory.
  /// Mirrors EvolveEffect and FlankingEffect: parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Myriad { get; } =
    new()
    {
      Name = "Myriad",
      RuleReference = "702.116",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Myriad",
        Effects = [new MyriadEffect()],
      },
    };

  /// <summary>
  /// Skulk: This creature can't be blocked by creatures with greater power.
  /// Rule 702.116b. An evasion keyword — MAST records the keyword's presence;
  /// the power-comparison blocking restriction is engine territory.
  /// </summary>
  public static KeywordDefinition Skulk { get; } =
    new()
    {
      Name = "Skulk",
      RuleReference = "702.116b",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Skulk",
        Effects = [new SkulkEffect()],
      },
    };

  /// <summary>
  /// Horsemanship: This creature can't be blocked except by creatures with horsemanship.
  /// Rule 702.32. An evasion keyword — MAST records the keyword's presence;
  /// the mutual-horsemanship blocking restriction is engine territory.
  /// </summary>
  public static KeywordDefinition Horsemanship { get; } =
    new()
    {
      Name = "Horsemanship",
      RuleReference = "702.32",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Horsemanship",
        Effects = [new HorsemanshipEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // COMBAT BEHAVIOR KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Vigilance: Attacking doesn't cause this creature to tap.
  /// Rule 702.20
  /// </summary>
  public static KeywordDefinition Vigilance { get; } =
    new()
    {
      Name = "Vigilance",
      RuleReference = "702.20",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new VigilanceEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // SPELL-CAST TRIGGERED KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Storm: When you cast this spell, copy it for each other spell that was cast
  /// before it this turn. You may choose new targets for the copies.
  /// Rule 702.40
  /// </summary>
  /// <remarks>
  /// Storm is rules-defined as a triggered ability. By the codebase convention
  /// of attaching keyword expansions to <see cref="StaticAbility"/> with
  /// KeywordSource set (mirroring Lifelink, Vigilance, etc.), the triggered
  /// semantics live in the rules engine, not the AST.
  /// </remarks>
  public static KeywordDefinition Storm { get; } =
    new()
    {
      Name = "Storm",
      RuleReference = "702.40",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Storm",
        Effects = [new StormEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // PROTECTION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Protection from [quality]: This permanent can't be blocked, targeted, dealt damage,
  /// enchanted, or equipped by anything with that quality.
  /// Rule 702.16
  /// </summary>
  public static KeywordDefinition Protection { get; } =
    new()
    {
      Name = "Protection",
      RuleReference = "702.16",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Quality,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Protection",
        Effects = [new ProtectionEffect { From = ParseProtectionQualities(parameter) }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // VEHICLE KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Crew N: Tap any number of other untapped creatures you control with total
  /// power N or more — this Vehicle becomes an artifact creature until end of turn.
  /// Rule 702.122. Category is Activated because the comp-rules expansion is an
  /// activated ability, but the oracle-text shorthand reads as a keyword
  /// followed by a numeric parameter.
  /// </summary>
  public static KeywordDefinition Crew { get; } =
    new()
    {
      Name = "Crew",
      RuleReference = "702.122",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Crew",
        Effects = [new CrewEffect
        {
          Power = new LiteralQuantity { Value = ParseCrewPower(parameter) },
        }],
      },
    };

  /// <summary>
  /// Saddle N: Tap any number of other untapped creatures you control with total
  /// power N or greater — this Mount becomes saddled until end of turn. Activate
  /// only as a sorcery.
  /// Rule 702.171. Category is Activated because the comp-rules expansion is an
  /// activated ability (702.171a), but the oracle-text shorthand reads as a keyword
  /// followed by a numeric threshold parameter. Structurally mirrors Crew (702.122)
  /// but applies to Mounts; the saddled designation is engine territory.
  /// </summary>
  public static KeywordDefinition Saddle { get; } =
    new()
    {
      Name = "Saddle",
      RuleReference = "702.171",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Saddle",
        Effects = [new SaddleEffect
        {
          Value = ParseSaddleValue(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // PARTNER KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Partner with [Name]: A pair-binding variant of the Partner keyword. The
  /// parameter is the literal name of the paired card (e.g., "Amy Pond").
  /// Rule 702.124.
  /// </summary>
  public static KeywordDefinition PartnerWith { get; } =
    new()
    {
      Name = "Partner with",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Name,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Partner with",
        Effects = [new PartnerEffect
        {
          PartnerType = PartnerType.PartnerWith,
          PartnerName = parameter?.Trim(),
        }],
      },
    };

  /// <summary>
  /// Partner (parameterless): You can have two commanders if both have partner.
  /// Rule 702.124. The bare form allows any two Partner commanders to pair up,
  /// as opposed to Partner with [Name] which binds two specific cards.
  /// </summary>
  public static KeywordDefinition Partner { get; } =
    new()
    {
      Name = "Partner",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Partner",
        Effects = [new PartnerEffect
        {
          PartnerType = PartnerType.Partner,
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // COST-MODIFIER KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Delve: Each card you exile from your graveyard while casting this spell pays for {1}.
  /// Rule 702.66. A parameterless cost-modifier keyword — MAST records the keyword's
  /// presence; the per-card graveyard-exile cost-reduction mechanic is engine territory.
  /// </summary>
  public static KeywordDefinition Delve { get; } =
    new()
    {
      Name = "Delve",
      RuleReference = "702.66",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Delve",
        Effects = [new DelveEffect()],
      },
    };

  /// <summary>
  /// Improvise: Each artifact you tap after you're done activating mana abilities pays for {1}.
  /// Rule 702.126. A parameterless cost-modifier keyword — MAST records the keyword's
  /// presence; the per-artifact cost-reduction mechanic is engine territory.
  /// </summary>
  public static KeywordDefinition Improvise { get; } =
    new()
    {
      Name = "Improvise",
      RuleReference = "702.126",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Improvise",
        Effects = [new ImproviseEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // CITY'S BLESSING KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Ascend: If you control ten or more permanents, you get the city's blessing
  /// for the rest of the game.
  /// Rule 702.131. Applies to both permanents (Rule 702.131b) and spells (Rule
  /// 702.131a). MAST records the keyword's presence; the city's-blessing
  /// designation and downstream effects are engine territory.
  /// </summary>
  public static KeywordDefinition Ascend { get; } =
    new()
    {
      Name = "Ascend",
      RuleReference = "702.131",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Ascend",
        Effects = [new AscendEffect()],
      },
    };

  /// <summary>
  /// Daybound: found on the front faces of day/night double-faced cards.
  /// Rule 702.145b. MAST records the keyword's presence and phase; the
  /// day/night transformation rules (Rule 731) are engine territory.
  /// </summary>
  public static KeywordDefinition Daybound { get; } =
    new()
    {
      Name = "Daybound",
      RuleReference = "702.145",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Daybound",
        Effects = [new DayNightEffect { Phase = DayNightPhase.Daybound }],
      },
    };

  /// <summary>
  /// Nightbound: found on the back faces of day/night double-faced cards.
  /// Rule 702.145e. MAST records the keyword's presence and phase; the
  /// day/night transformation rules (Rule 731) are engine territory.
  /// </summary>
  public static KeywordDefinition Nightbound { get; } =
    new()
    {
      Name = "Nightbound",
      RuleReference = "702.145",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Nightbound",
        Effects = [new DayNightEffect { Phase = DayNightPhase.Nightbound }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // KICKER KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Kicker [cost]: You may pay an additional [cost] as you cast this spell.
  /// Rule 702.33. Scope: single-cost kicker only (Rule 702.33a). Multi-cost
  /// kicker (Rule 702.33b) and Multikicker (Rule 702.33c) are deferred.
  /// </summary>
  public static KeywordDefinition Kicker { get; } =
    new()
    {
      Name = "Kicker",
      RuleReference = "702.33",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Kicker",
        Effects = [new KickerEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <summary>
  /// Affinity for [text]: This spell costs {1} less to cast for each [text] you control.
  /// Rule 702.41. The parameter is a free-text type/subtype label (e.g., "artifacts",
  /// "Cats", "Plains", "historic permanents"). MAST captures it as a structured
  /// <see cref="ObjectFilter"/> on the cost-reduction's per-object axis.
  /// </summary>
  public static KeywordDefinition Affinity { get; } =
    new()
    {
      Name = "Affinity",
      RuleReference = "702.41",
      Category = KeywordCategory.CostModifier,
      HasParameter = true,
      ParameterType = KeywordParameterType.CardType,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = $"Affinity for {parameter?.Trim()}",
        Effects = [new CostReductionEffect
        {
          Amount = LiteralQuantity.Of(1),
          PerObject = BuildAffinityFilter(parameter),
        }],
      },
    };

  /// <summary>
  /// Unearth [cost]: Return this card from your graveyard to the battlefield.
  /// It gains haste. Exile it at the beginning of the next end step or if it
  /// would leave the battlefield. Unearth only as a sorcery.
  /// Rule 702.84. Scope: mana-cost parameter (all known printings).
  /// </summary>
  public static KeywordDefinition Unearth { get; } =
    new()
    {
      Name = "Unearth",
      RuleReference = "702.84",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Unearth",
        Effects = [new UnearthEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // PLOT KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Plot [cost]: You may pay [cost] and exile this card from your hand. Cast it
  /// as a sorcery on a later turn without paying its mana cost. Plot only as a
  /// sorcery.
  /// Rule 702.170. Scope: mana-cost parameter (all known printings).
  /// The exile-from-hand, deferred-cast, and sorcery-speed restrictions are
  /// engine territory — MAST records the keyword's presence and cost only.
  /// </summary>
  public static KeywordDefinition Plot { get; } =
    new()
    {
      Name = "Plot",
      RuleReference = "702.170",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Plot",
        Effects = [new PlotEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // GRAVEYARD RECURSION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Undying: When this creature dies, if it had no +1/+1 counters on it,
  /// return it to the battlefield under its owner's control with a +1/+1
  /// counter on it.
  /// Rule 702.93. Mirror of Persist (Rule 702.78) with opposite polarity:
  /// Persist checks for no -1/-1 counters; Undying checks for no +1/+1
  /// counters. MAST records keyword presence; the dies-trigger, counter-check,
  /// and return-to-battlefield semantics are engine territory.
  /// </summary>
  public static KeywordDefinition Undying { get; } =
    new()
    {
      Name = "Undying",
      RuleReference = "702.93",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Undying",
        Effects = [new UndyingEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // MORPH FAMILY KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Megamorph [cost]: A variant of Morph (Rule 702.37b). The player may cast
  /// this card face down as a 2/2 colorless creature for {3}, and may turn it
  /// face up by paying its megamorph cost; when turned face up via the megamorph
  /// cost, a +1/+1 counter is placed on the permanent. MAST records the keyword
  /// and the megamorph cost; the cast-face-down rules, turn-face-up mechanics,
  /// and counter-placement are engine territory (descriptive-not-engine doctrine).
  /// Rule 702.37b.
  /// </summary>
  public static KeywordDefinition Megamorph { get; } =
    new()
    {
      Name = "Megamorph",
      RuleReference = "702.37b",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Megamorph",
        Effects = [new MegamorphEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // FADING / VANISHING / GRAFT KEYWORDS (counter-based permanence)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Fading N: This permanent enters with N fade counters on it. At the
  /// beginning of your upkeep, remove a fade counter from it. If you can't,
  /// sacrifice it.
  /// Rule 702.32. MAST records the keyword and its integer value; the
  /// counter-removal upkeep trigger and sacrifice-unless-counter mechanics
  /// are engine territory.
  /// </summary>
  public static KeywordDefinition Fading { get; } =
    new()
    {
      Name = "Fading",
      RuleReference = "702.32",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Fading",
        Effects = [new FadingEffect
        {
          Value = ParseIntValue("Fading", parameter),
        }],
      },
    };

  /// <summary>
  /// Vanishing N: This permanent enters with N time counters on it. At the
  /// beginning of your upkeep, remove a time counter from it. When the last
  /// is removed, sacrifice it.
  /// Rule 702.63. MAST records the keyword and its integer value; the
  /// counter-removal upkeep trigger and last-counter sacrifice mechanics
  /// are engine territory. Mirrors Fading (702.32) but uses time counters
  /// and triggers on the last counter rather than inability to remove.
  /// </summary>
  public static KeywordDefinition Vanishing { get; } =
    new()
    {
      Name = "Vanishing",
      RuleReference = "702.63",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Vanishing",
        Effects = [new VanishingEffect
        {
          Value = ParseIntValue("Vanishing", parameter),
        }],
      },
    };

  /// <summary>
  /// Graft N: This permanent enters with N +1/+1 counters on it. Whenever
  /// another creature enters, you may move a +1/+1 counter from this creature
  /// onto it.
  /// Rule 702.58. MAST records the keyword and its integer value; the
  /// enters-with-counters and optional counter-move triggered ability are
  /// engine territory.
  /// </summary>
  public static KeywordDefinition Graft { get; } =
    new()
    {
      Name = "Graft",
      RuleReference = "702.58",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Graft",
        Effects = [new GraftEffect
        {
          Value = ParseIntValue("Graft", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DREDGE KEYWORD
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Dredge N: If you would draw a card, you may mill N cards instead. If you
  /// do, return this card from your graveyard to your hand.
  /// Rule 702.52. MAST records the keyword and its integer value; the
  /// draw-replacement choice and mill-and-return mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Dredge { get; } =
    new()
    {
      Name = "Dredge",
      RuleReference = "702.52",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Dredge",
        Effects = [new DredgeEffect
        {
          Value = ParseIntValue("Dredge", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // OUTLAST KEYWORD
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Outlast {cost}: {cost}, {T}: Put a +1/+1 counter on this creature.
  /// Outlast only as a sorcery.
  /// Rule 702.107. Category is Activated because the comp-rules expansion is
  /// an activated ability (702.107a), but the oracle-text shorthand reads as a
  /// keyword followed by a mana-cost parameter. MAST records the keyword's
  /// presence and cost; the tap cost, sorcery-speed restriction, and
  /// counter-placement are engine territory.
  /// </summary>
  public static KeywordDefinition Outlast { get; } =
    new()
    {
      Name = "Outlast",
      RuleReference = "702.107",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Outlast",
        Effects = [new OutlastEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // CUMULATIVE UPKEEP KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Cumulative upkeep [cost]: At the beginning of your upkeep, put an age counter
  /// on this permanent, then sacrifice it unless you pay its upkeep cost for each
  /// age counter on it.
  /// Rule 702.24. Category is Triggered because the comp-rules expansion is a
  /// triggered ability that fires at the beginning of the controller's upkeep.
  /// The age-counter-scaling and sacrifice-unless-pay semantics are engine
  /// territory — MAST records the keyword's presence and the cost parameter only.
  /// </summary>
  public static KeywordDefinition CumulativeUpkeep { get; } =
    new()
    {
      Name = "Cumulative upkeep",
      RuleReference = "702.24",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Cumulative upkeep",
        Effects = [new CumulativeUpkeepEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // ENTRY CHOICE KEYWORDS (Riot)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Riot: This creature enters with your choice of a +1/+1 counter or haste.
  /// Rule 702.138. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Riot { get; } =
    new()
    {
      Name = "Riot",
      RuleReference = "702.138",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Riot",
        Effects = [new RiotEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // ATTACK COUNTER KEYWORDS (Training, Dethrone)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Training: Whenever this creature attacks with another creature with greater
  /// power, put a +1/+1 counter on this creature.
  /// Rule 702.151. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Training { get; } =
    new()
    {
      Name = "Training",
      RuleReference = "702.151",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Training",
        Effects = [new TrainingEffect()],
      },
    };

  /// <summary>
  /// Dethrone: Whenever this creature attacks the player with the most life or
  /// tied for most life, put a +1/+1 counter on it.
  /// Rule 702.107. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Dethrone { get; } =
    new()
    {
      Name = "Dethrone",
      RuleReference = "702.107",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Dethrone",
        Effects = [new DethroneEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // BLOCKED TRIGGER KEYWORDS (Afflict)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Afflict N: Whenever this creature becomes blocked, defending player loses N life.
  /// Rule 702.130. Integer-parameterized keyword marker.
  /// </summary>
  public static KeywordDefinition Afflict { get; } =
    new()
    {
      Name = "Afflict",
      RuleReference = "702.130",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Afflict",
        Effects = [new AfflictEffect
        {
          Value = ParseIntValue("Afflict", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DEATH TRIGGER KEYWORDS (Afterlife)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Afterlife N: When this creature dies, create N 1/1 white and black Spirit
  /// creature tokens with flying.
  /// Rule 702.135. Integer-parameterized keyword marker.
  /// </summary>
  public static KeywordDefinition Afterlife { get; } =
    new()
    {
      Name = "Afterlife",
      RuleReference = "702.135",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Afterlife",
        Effects = [new AfterlifeEffect
        {
          Value = ParseIntValue("Afterlife", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // ATTACK SUPPORT KEYWORDS (Enlist, Backup)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Enlist: As this creature attacks, you may tap an untapped creature you control
  /// that could have attacked. If you do, add its power to this creature's until
  /// end of turn.
  /// Rule 702.154. Although mechanically a static + triggered ability pair, MAST
  /// records it as a keyword marker — same approach as Evolve and Flanking.
  /// The tapping cost and power-addition are engine territory.
  /// </summary>
  public static KeywordDefinition Enlist { get; } =
    new()
    {
      Name = "Enlist",
      RuleReference = "702.154",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Enlist",
        Effects = [new EnlistEffect()],
      },
    };

  /// <summary>
  /// Backup N: When this creature enters, put N +1/+1 counters on target creature.
  /// If that is another creature, it also gains the non-backup abilities printed
  /// below this one until end of turn.
  /// Rule 702.165. MAST records the keyword and its integer value; the counter
  /// placement, ability-grant, and "printed below this one" scoping are engine
  /// territory.
  /// </summary>
  public static KeywordDefinition Backup { get; } =
    new()
    {
      Name = "Backup",
      RuleReference = "702.165",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Backup",
        Effects = [new BackupEffect
        {
          Value = ParseIntValue("Backup", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // POISON / COUNTER KEYWORDS (Toxic, Modular)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Toxic N: Whenever this creature deals combat damage to a player, that player
  /// gets N poison counters in addition to the damage.
  /// Rule 702.164. MAST records the keyword and its integer value; the
  /// poison-counter placement and interaction with combat damage are engine
  /// territory.
  /// </summary>
  public static KeywordDefinition Toxic { get; } =
    new()
    {
      Name = "Toxic",
      RuleReference = "702.164",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Toxic",
        Effects = [new ToxicEffect
        {
          Value = ParseIntValue("Toxic", parameter),
        }],
      },
    };

  /// <summary>
  /// Modular N: This permanent enters with N +1/+1 counters on it. When it is put
  /// into a graveyard from the battlefield, you may put a +1/+1 counter on target
  /// artifact creature for each +1/+1 counter on this permanent.
  /// Rule 702.43. MAST records the keyword and its integer value; the counter
  /// placement, death trigger, and optional transfer are engine territory.
  /// Explicitly named in BushidoEffect.cs as a future peer in the
  /// integer-parameterized keyword family.
  /// </summary>
  public static KeywordDefinition Modular { get; } =
    new()
    {
      Name = "Modular",
      RuleReference = "702.43",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Modular",
        Effects = [new ModularEffect
        {
          Value = ParseIntValue("Modular", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // SPELL-CAST RECURSION KEYWORDS (Rebound, Buyback, Retrace)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Rebound: If you cast this spell from your hand, exile it as it resolves.
  /// At the beginning of your next upkeep, you may cast this card from exile
  /// without paying its mana cost.
  /// Rule 702.88. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Rebound { get; } =
    new()
    {
      Name = "Rebound",
      RuleReference = "702.88",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Rebound",
        Effects = [new ReboundEffect()],
      },
    };

  /// <summary>
  /// Buyback [cost]: You may pay an additional [cost] as you cast this spell.
  /// If you do, put this card into your hand as it resolves.
  /// Rule 702.26. Scope: mana-cost parameter (all known printed instances).
  /// </summary>
  public static KeywordDefinition Buyback { get; } =
    new()
    {
      Name = "Buyback",
      RuleReference = "702.26",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Buyback",
        Effects = [new BuybackEffect
        {
          BuybackCost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // SPLIT-CARD KEYWORDS (Fuse)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Fuse: You may cast one or both halves of this card from your hand.
  /// Rule 702.102. Found on split cards from Dragon's Maze. MAST records the
  /// keyword's presence; the split-card casting modes and cost-combination
  /// mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Fuse { get; } =
    new()
    {
      Name = "Fuse",
      RuleReference = "702.102",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Fuse",
        Effects = [new FuseEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // BARGAIN KEYWORDS (Wilds of Eldraine / WOE)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Bargain: You may sacrifice an artifact, enchantment, or token as you cast
  /// this spell.
  /// Rule 702.166. MAST records the keyword's presence; the optional-sacrifice
  /// additional-cost and "bargained" designation gating conditional effects are
  /// engine territory.
  /// </summary>
  public static KeywordDefinition Bargain { get; } =
    new()
    {
      Name = "Bargain",
      RuleReference = "702.166",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Bargain",
        Effects = [new BargainEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // SPREE KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Spree: Choose one or more additional costs.
  /// Rule 702.172. Found on modal spells that require choosing at least one mode
  /// and paying its additional cost. MAST records the keyword's presence;
  /// the mode-selection, additional-cost payment, and multi-mode resolution
  /// mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Spree { get; } =
    new()
    {
      Name = "Spree",
      RuleReference = "702.172",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Spree",
        Effects = [new SpreeEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // JOB SELECT KEYWORDS (Final Fantasy)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Job select: When this Equipment enters, create a 1/1 colorless Hero
  /// creature token, then attach this to it.
  /// Rule 702.182. Found on Equipment cards from the Final Fantasy set.
  /// Although mechanically a triggered ability, MAST records it as a keyword
  /// marker — same approach as Living weapon (702.77); the ETB trigger,
  /// Hero-token creation, and auto-attach semantics are engine territory.
  /// </summary>
  public static KeywordDefinition JobSelect { get; } =
    new()
    {
      Name = "Job select",
      RuleReference = "702.182",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Job select",
        Effects = [new JobSelectEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // WARP KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Warp [cost]: You may cast this card from your hand for its warp cost. It
  /// enters the battlefield tapped.
  /// Rule 702.185. An alternative-cast keyword. MAST records the keyword and
  /// the warp cost; the alternative-cast and enters-tapped mechanics are engine
  /// territory — same approach as Plot (702.170) and Dash (702.109).
  /// </summary>
  public static KeywordDefinition Warp { get; } =
    new()
    {
      Name = "Warp",
      RuleReference = "702.185",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Warp",
        Effects = [new WarpEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // LIVING WEAPON KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Living weapon: When this Equipment enters, create a 0/0 black Phyrexian
  /// Germ creature token, then attach this to it.
  /// Rule 702.77. Although mechanically a triggered ability, MAST records it as
  /// a keyword marker — same approach as Evolve and Flanking; the ETB trigger,
  /// token-creation, and auto-attach semantics are engine territory.
  /// </summary>
  public static KeywordDefinition LivingWeapon { get; } =
    new()
    {
      Name = "Living weapon",
      RuleReference = "702.77",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Living weapon",
        Effects = [new LivingWeaponEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // TOTEM ARMOR KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Totem armor (oracle text: "Umbra armor"): If enchanted creature would be
  /// destroyed, instead remove all damage from it and destroy this Aura.
  /// Rule 702.102. The oracle text uses "Umbra armor" while the comp-rules name
  /// is "totem armor"; the keyword name stored here matches the comp-rules term.
  /// MAST records the keyword's presence; the replacement-effect semantics are
  /// engine territory.
  /// </summary>
  public static KeywordDefinition TotemArmor { get; } =
    new()
    {
      Name = "Totem armor",
      RuleReference = "702.102",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Totem armor",
        Effects = [new TotemArmorEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // COMBAT DAMAGE TRIGGERED COUNTER KEYWORDS (Renown, Bloodthirst, Sunburst, Fabricate, Ingest)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Renown N: When this creature deals combat damage to a player, if it isn't
  /// renowned, put N +1/+1 counters on it and it becomes renowned.
  /// Rule 702.112. MAST records the keyword and its integer value; the trigger,
  /// renowned designation, and counter-placement are engine territory.
  /// </summary>
  public static KeywordDefinition Renown { get; } =
    new()
    {
      Name = "Renown",
      RuleReference = "702.112",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Renown",
        Effects = [new RenownEffect
        {
          Value = ParseIntValue("Renown", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // HIDEAWAY KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Hideaway N: When this permanent enters, look at the top N cards of your
  /// library, exile one face down, then put the rest on the bottom in a random
  /// order.
  /// Rule 702.74. Category is Triggered because the comp-rules expansion is a
  /// triggered ability that fires when the permanent enters. MAST records the
  /// keyword and its integer lookahead count.
  /// </summary>
  public static KeywordDefinition Hideaway { get; } =
    new()
    {
      Name = "Hideaway",
      RuleReference = "702.74",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Hideaway",
        Effects = [new HideawayEffect
        {
          Amount = new LiteralQuantity { Value = ParseIntValue("Hideaway", parameter) },
        }],
      },
    };

  /// <summary>
  /// Retrace: You may cast this card from your graveyard by discarding a land
  /// card in addition to paying its other costs.
  /// Rule 702.75. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Retrace { get; } =
    new()
    {
      Name = "Retrace",
      RuleReference = "702.75",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Retrace",
        Effects = [new RetraceEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // CREATURE MECHANIC KEYWORDS (Unleash, Learn)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Unleash: You may have this creature enter with a +1/+1 counter on it.
  /// It can't block as long as it has a +1/+1 counter on it.
  /// Rule 702.97. Parameterless keyword marker.
  /// </summary>
  public static KeywordDefinition Unleash { get; } =
    new()
    {
      Name = "Unleash",
      RuleReference = "702.97",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Unleash",
        Effects = [new UnleashEffect()],
      },
    };

  /// <summary>
  /// Learn: You may reveal a Lesson card you own from outside the game and put
  /// it into your hand, or discard a card to draw a card.
  /// Rule 702.148. Parameterless keyword action marker.
  /// </summary>
  public static KeywordDefinition Learn { get; } =
    new()
    {
      Name = "Learn",
      RuleReference = "702.148",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Learn",
        Effects = [new LearnEffect()],
      },
    };

  /// <summary>
  /// Bloodthirst N: If an opponent was dealt damage this turn, this creature enters
  /// with N +1/+1 counters on it.
  /// Rule 702.54. MAST records the keyword and its integer value; the condition check
  /// and counter-placement on entry are engine territory.
  /// </summary>
  public static KeywordDefinition Bloodthirst { get; } =
    new()
    {
      Name = "Bloodthirst",
      RuleReference = "702.54",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Bloodthirst",
        Effects = [new BloodthirstEffect
        {
          Value = ParseIntValue("Bloodthirst", parameter),
        }],
      },
    };

  /// <summary>
  /// Sunburst: This permanent enters with a +1/+1 counter on it for each color of
  /// mana spent to cast it. (Non-creature artifacts use charge counters instead.)
  /// Rule 702.44. Parameterless keyword marker — MAST records keyword presence;
  /// the color-counting and counter-placement are engine territory.
  /// </summary>
  public static KeywordDefinition Sunburst { get; } =
    new()
    {
      Name = "Sunburst",
      RuleReference = "702.44",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Sunburst",
        Effects = [new SunburstEffect()],
      },
    };

  /// <summary>
  /// Fabricate N: When this creature enters, put N +1/+1 counters on it or create
  /// N 1/1 colorless Servo artifact creature tokens.
  /// Rule 702.118. MAST records the keyword and its integer value; the enters trigger,
  /// choice between counters and tokens, and token creation are engine territory.
  /// </summary>
  public static KeywordDefinition Fabricate { get; } =
    new()
    {
      Name = "Fabricate",
      RuleReference = "702.118",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Fabricate",
        Effects = [new FabricateEffect
        {
          Value = ParseIntValue("Fabricate", parameter),
        }],
      },
    };

  /// <summary>
  /// Ingest: Whenever this creature deals combat damage to a player, that player
  /// exiles the top card of their library.
  /// Rule 702.115. Parameterless keyword marker — MAST records keyword presence;
  /// the combat-damage trigger and exile-top-of-library action are engine territory.
  /// </summary>
  public static KeywordDefinition Ingest { get; } =
    new()
    {
      Name = "Ingest",
      RuleReference = "702.115",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Ingest",
        Effects = [new IngestEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // MOBILIZE KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Mobilize N: Whenever this creature attacks, create N tapped and attacking
  /// 1/1 red Warrior creature tokens. Sacrifice them at the beginning of the
  /// next end step.
  /// Rule 702.175 (Tarkir: Dragonstorm). Category is Triggered because the
  /// comp-rules expansion is a triggered ability. MAST records the keyword and
  /// its integer token-creation count.
  /// </summary>
  public static KeywordDefinition Mobilize { get; } =
    new()
    {
      Name = "Mobilize",
      RuleReference = "702.175",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Mobilize",
        Effects = [new MobilizeEffect
        {
          Amount = new LiteralQuantity { Value = ParseIntValue("Mobilize", parameter) },
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // START YOUR ENGINES KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Start your engines! (Aetherdrift): If you have no speed, it starts at 1.
  /// It increases once on each of your turns when an opponent loses life. Max
  /// speed is 4.
  /// MAST records the keyword's presence; the speed initialization and increment
  /// semantics are engine territory. The '!' is silently dropped by the
  /// tokenizer, so the combinator matches "Start your engines".
  /// </summary>
  public static KeywordDefinition StartYourEngines { get; } =
    new()
    {
      Name = "Start your engines",
      RuleReference = "702.178",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Start your engines",
        Effects = [new StartYourEnginesEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // CONVERGE ABILITY WORD
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Converge — ability word. Effects scale with the number of colors of mana
  /// spent to cast the spell. MAST records the ability-word marker; the
  /// color-counting and scaled-effect semantics are engine territory.
  /// Mirrors Sunburst (Rule 702.44) which describes the same color-counting
  /// concept as a keyword ability; Converge is an ability word (Rule 207.2c).
  /// </summary>
  public static KeywordDefinition Converge { get; } =
    new()
    {
      Name = "Converge",
      RuleReference = "207.2c",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Converge",
        Effects = [new ConvergeEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // FOR MIRRODIN KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// For Mirrodin! (Phyrexia: All Will Be One). When this Equipment enters,
  /// create a 2/2 red Rebel creature token, then attach this to it. Although
  /// mechanically a triggered ability, MAST records it as a keyword marker —
  /// same approach as Living Weapon (Rule 702.77); the ETB trigger,
  /// token-creation, and auto-attach semantics are engine territory.
  /// The '!' in oracle text is silently dropped by the tokenizer.
  /// </summary>
  public static KeywordDefinition ForMirrodin { get; } =
    new()
    {
      Name = "For Mirrodin",
      RuleReference = "702.77",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "For Mirrodin",
        Effects = [new ForMirrodinEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // PREPARED KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Prepared — keyword state printed as "This creature enters prepared." on
  /// the front face of prepare-layout double-faced cards. While prepared, the
  /// controller may cast a copy of the attached spell; doing so unprepares it.
  /// MAST records the keyword's presence; the prepared-state and copy-cast
  /// mechanics are engine territory per the descriptive-not-engine doctrine.
  /// </summary>
  public static KeywordDefinition Prepared { get; } =
    new()
    {
      Name = "Prepared",
      RuleReference = "702.177",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Prepared",
        Effects = [new PreparedEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DOCTOR'S COMPANION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Doctor's companion (Doctor Who Commander). You can have two commanders
  /// if the other is the Doctor. A variant of the Partner keyword restricted
  /// to Doctor-subtype commanders. MAST records the keyword's presence;
  /// the commander-pairing restriction is engine territory.
  /// Mirrors Partner (Rule 702.124) but with the Doctor-constraint.
  /// </summary>
  public static KeywordDefinition DoctorsCompanion { get; } =
    new()
    {
      Name = "Doctor's companion",
      RuleReference = "702.124",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Doctor's companion",
        Effects = [new DoctorsCompanionEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // FIREBENDING KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Firebending N (Avatar: The Last Airbender). A triggered keyword ability:
  /// whenever this creature attacks, add N {R}. This mana lasts until end of
  /// combat. MAST records the keyword and its integer value; the attack trigger,
  /// mana-addition, and end-of-combat duration are engine territory.
  /// Integer-parameterized keyword; mirrors the Bushido/Modular/Backup family.
  /// </summary>
  public static KeywordDefinition Firebending { get; } =
    new()
    {
      Name = "Firebending",
      RuleReference = "702.175",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Firebending",
        Effects = [new FirebendingEffect
        {
          Value = ParseIntValue("Firebending", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // RECONFIGURE KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Reconfigure [cost]: Attach to target creature you control; or unattach
  /// from a creature. Reconfigure only as a sorcery. While attached, this
  /// isn't a creature.
  /// Rule 702.173. Scope: mana-cost parameter (all known printings).
  /// The attach/unattach mechanics, sorcery-speed restriction, and
  /// creature-status switching are engine territory — MAST records the
  /// keyword's presence and cost only, mirroring the EquipEffect pattern.
  /// </summary>
  public static KeywordDefinition Reconfigure { get; } =
    new()
    {
      Name = "Reconfigure",
      RuleReference = "702.173",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Reconfigure",
        Effects = [new MagicAST.AST.Effects.Keyword.ReconfigureEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // DEVOUR KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Devour N: As this creature enters, you may sacrifice any number of
  /// creatures. This creature enters with N +1/+1 counters on it for each
  /// creature sacrificed this way.
  /// Rule 702.82. MAST records the keyword and its integer devour value;
  /// the sacrifice-on-entry, counter-placement, and optional semantics are
  /// engine territory.
  /// </summary>
  public static KeywordDefinition Devour { get; } =
    new()
    {
      Name = "Devour",
      RuleReference = "702.82",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Devour",
        Effects = [new DevourEffect
        {
          Value = ParseIntValue("Devour", parameter),
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // CONSPIRE KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Conspire: As you cast this spell, you may tap two untapped creatures you
  /// control that share a color with it. When you do, copy it.
  /// Rule 702.78. MAST records the keyword's presence; the tap-two-creatures
  /// additional cost and spell-copy triggered ability are engine territory.
  /// </summary>
  public static KeywordDefinition Conspire { get; } =
    new()
    {
      Name = "Conspire",
      RuleReference = "702.78",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Conspire",
        Effects = [new ConspireEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // JUMP-START KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Jump-start: You may cast this card from your graveyard by discarding a
  /// card in addition to paying its other costs. If you do, this card is
  /// exiled as it resolves.
  /// Rule 702.133. MAST records the keyword's presence; the graveyard-cast,
  /// discard additional cost, and exile-on-resolution machinery are engine
  /// territory.
  /// </summary>
  public static KeywordDefinition JumpStart { get; } =
    new()
    {
      Name = "Jump-start",
      RuleReference = "702.133",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Jump-start",
        Effects = [new JumpStartEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // AFTERMATH KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Aftermath: Cast this spell only from your graveyard. Then exile it.
  /// Rule 702.128. Found on the bottom half of split cards. MAST records
  /// the keyword's presence; the graveyard-only cast restriction and
  /// exile-on-resolution mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Aftermath { get; } =
    new()
    {
      Name = "Aftermath",
      RuleReference = "702.128",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Aftermath",
        Effects = [new AftermathEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // EXPLOIT KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Exploit: When this creature enters, you may sacrifice a creature.
  /// Rule 702.110. MAST records the keyword's presence; the ETB trigger,
  /// optional sacrifice, and downstream sacrifice-payoff abilities are
  /// engine territory.
  /// </summary>
  public static KeywordDefinition Exploit { get; } =
    new()
    {
      Name = "Exploit",
      RuleReference = "702.110",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Exploit",
        Effects = [new ExploitEffect()],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // PHASING / PROVOKE / CIPHER / HAUNT / CHAMPION KEYWORDS
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// Phasing: This permanent phases in or out before you untap during each of
  /// your untap steps. Rule 702.26. MAST records the keyword's presence;
  /// the phase-bookkeeping is engine territory.
  /// </summary>
  public static KeywordDefinition Phasing { get; } =
    new()
    {
      Name = "Phasing",
      RuleReference = "702.26",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Phasing",
        Effects = [new PhasingEffect { IsOptional = false }],
      },
    };

  /// <summary>
  /// Provoke: Whenever this creature attacks, you may have target creature
  /// the defending player controls untap and block it if able.
  /// Rule 702.39. MAST records the keyword's presence; the attack trigger
  /// and force-block mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Provoke { get; } =
    new()
    {
      Name = "Provoke",
      RuleReference = "702.39",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Provoke",
        Effects = [new ProvokeEffect { IsOptional = false }],
      },
    };

  /// <summary>
  /// Cipher: Then you may exile this spell card encoded on a creature you
  /// control. Whenever that creature deals combat damage to a player, its
  /// controller may cast a copy of the encoded card without paying its mana
  /// cost. Rule 702.99. MAST records the keyword's presence; the encoding,
  /// copy-creation, and free-cast mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Cipher { get; } =
    new()
    {
      Name = "Cipher",
      RuleReference = "702.99",
      Category = KeywordCategory.Static,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Cipher",
        Effects = [new CipherEffect { IsOptional = false }],
      },
    };

  /// <summary>
  /// Haunt: When this creature dies, exile it haunting target creature.
  /// Rule 702.55. MAST records the keyword's presence; the exile-on-death
  /// and haunt-trigger mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Haunt { get; } =
    new()
    {
      Name = "Haunt",
      RuleReference = "702.55",
      Category = KeywordCategory.Triggered,
      HasParameter = false,
      CreateExpansion = _ => new StaticAbility
      {
        KeywordSource = "Haunt",
        Effects = [new HauntEffect { IsOptional = false }],
      },
    };

  /// <summary>
  /// Champion a [type]: When this enters the battlefield, sacrifice it unless
  /// you exile another creature of the named type you control. When this
  /// leaves the battlefield, that card returns. Rule 702.71. MAST records
  /// the keyword's presence and the creature type parameter; the
  /// sacrifice-unless and return mechanics are engine territory.
  /// </summary>
  public static KeywordDefinition Champion { get; } =
    new()
    {
      Name = "Champion",
      RuleReference = "702.71",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.CardType,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = $"Champion a {parameter?.Trim() ?? "creature"}",
        Effects = [new ChampionEffect
        {
          CreatureType = parameter?.Trim() ?? "creature",
          IsOptional = false,
        }],
      },
    };

  // ═══════════════════════════════════════════════════════════════════════════
  // ALL DEFINITIONS (must be after individual definitions to avoid null refs)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// All registered keyword definitions.
  /// </summary>
  public static IReadOnlyList<KeywordDefinition> All { get; } =
    [
      Flying,
      Fear,
      Shadow,
      Intimidate,
      Menace,
      FirstStrike,
      DoubleStrike,
      Lifelink,
      Indestructible,
      Flanking,
      Melee,
      Vigilance,
      Storm,
      Protection,
      Crew,
      PartnerWith,
      Partner,
      Delve,
      Improvise,
      Ascend,
      Kicker,
      Unearth,
      Affinity,
      Evolve,
      Plot,
      Undying,
      Mentor,
      Myriad,
      Saddle,
      Megamorph,
      CumulativeUpkeep,
      Enlist,
      Toxic,
      Modular,
      Backup,
      Reconfigure,
      Rebound,
      Buyback,
      Retrace,
      Unleash,
      Learn,
      Renown,
      Bloodthirst,
      Sunburst,
      Fabricate,
      Ingest,
      Skulk,
      Horsemanship,
      SplitSecond,
      BattleCry,
      Soulbond,
      LivingWeapon,
      TotemArmor,
      Hideaway,
      Mobilize,
      StartYourEngines,
      Riot,
      Training,
      Dethrone,
      Afflict,
      Afterlife,
      Fuse,
      Bargain,
      Spree,
      JobSelect,
      Warp,
      Devour,
      Conspire,
      JumpStart,
      Aftermath,
      Exploit,
      Converge,
      ForMirrodin,
      Prepared,
      DoctorsCompanion,
      Firebending,
      Fading,
      Vanishing,
      Graft,
      Dredge,
      Outlast,
      Phasing,
      Provoke,
      Cipher,
      Haunt,
      Champion,
      // More keywords can be added here as needed
    ];

  /// <summary>
  /// Parses a mana-cost parameter string (e.g., "{W}", "{4}{G}", "{2}") into a
  /// <see cref="ManaCost"/>. Delegates to <see cref="ManaCostParser"/> which owns
  /// the mana-symbol lexing logic.
  /// </summary>
  private static ManaCost ParseManaCost(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Kicker requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }

  private static int ParseCrewPower(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Crew requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"Crew parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }

  private static int ParseSaddleValue(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Saddle requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"Saddle parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }

  /// <summary>
  /// Generic integer-parameter parser for keywords that carry a single numeric value
  /// (e.g., Toxic, Modular, Backup). Mirrors ParseCrewPower and ParseSaddleValue but
  /// takes the keyword name as context for the error message.
  /// </summary>
  private static int ParseIntValue(string keywordName, string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException($"{keywordName} requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"{keywordName} parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }

  /// <summary>
  /// Parses a protection parameter string into structured qualities.
  /// Handles formats like:
  /// - "red" → single color
  /// - "Demons and from Dragons" → multiple subtypes
  /// - "everything" → protection from everything
  /// </summary>
  private static IReadOnlyList<ProtectionQuality> ParseProtectionQualities(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Protection requires a quality parameter.", nameof(parameter));
    }

    // Handle "X and from Y" patterns (e.g., "Demons and from Dragons")
    var parts = parameter
      .Replace(" and from ", "|")
      .Replace(" and ", "|")
      .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    return parts.Select(ParseSingleQuality).ToList();
  }

  private static ProtectionQuality ParseSingleQuality(string quality)
  {
    var normalized = quality.ToLowerInvariant().Trim();

    // Check for "everything"
    if (normalized is "everything" or "all")
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Everything };
    }

    // Check for colors
    if (normalized is "white" or "blue" or "black" or "red" or "green")
    {
      return new ProtectionQuality { Kind = ProtectionQualityKind.Color, Value = normalized };
    }

    // Check for color characteristics
    if (normalized is "multicolored" or "monocolored" or "colorless")
    {
      return new ProtectionQuality
      {
        Kind = ProtectionQualityKind.Characteristic,
        Value = normalized,
      };
    }

    // Check for card types (lowercase in oracle text)
    if (
      normalized
      is "creatures"
        or "creature"
        or "artifacts"
        or "artifact"
        or "enchantments"
        or "enchantment"
        or "instants"
        or "instant"
        or "sorceries"
        or "sorcery"
        or "planeswalkers"
        or "planeswalker"
    )
    {
      // Normalize to singular
      var singular = normalized.TrimEnd('s');
      if (singular == "sorcerie")
      {
        singular = "sorcery";
      }

      return new ProtectionQuality { Kind = ProtectionQualityKind.CardType, Value = singular };
    }

    // Default: treat as a subtype (e.g., "Demons", "Dragons", "Goblins")
    // Subtypes are capitalized in oracle text
    return new ProtectionQuality { Kind = ProtectionQualityKind.Subtype, Value = quality };
  }

  /// <summary>
  /// Maps the literal "Affinity for X" parameter text to a structured
  /// <see cref="ObjectFilter"/> for the per-object cost-reduction axis. Rule 702.41.
  /// Three branches keyed by lexical convention in oracle text:
  /// - Lowercase card-type plurals ("artifacts", "creatures", "enchantments", ...)
  ///   → <c>CardTypes</c> singular.
  /// - Basic-land subtype labels (the five plus their plurals — "Plains" is its own
  ///   plural, the other four pluralize) → <c>Subtypes</c> as the basic-land token.
  /// - Capitalized plural subtype labels ("Cats", "Humans", "Frogs", ...)
  ///   → <c>Subtypes</c> singular.
  /// Anything else falls through to a <c>Characteristics</c> entry preserving the
  /// raw lowercase form (e.g., "historic permanents", "snow lands"); composite
  /// shapes are out of scope for this batch's parser surface.
  /// </summary>
  private static ObjectFilter BuildAffinityFilter(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Affinity requires a type parameter.", nameof(parameter));
    }

    var raw = parameter.Trim();

    // Card-type plurals: lowercase in oracle text. Singularize via trailing-s strip
    // (with the sorceries→sorcery special case mirroring Protection).
    var cardTypes = new[]
    {
      "artifacts",
      "creatures",
      "enchantments",
      "instants",
      "sorceries",
      "lands",
      "planeswalkers",
      "battles",
    };
    if (cardTypes.Contains(raw))
    {
      var singular = raw.TrimEnd('s');
      if (singular == "sorcerie")
      {
        singular = "sorcery";
      }
      else if (singular == "batt") // "battles" → "battle"
      {
        singular = "battle";
      }
      else if (singular == "land")
      {
        // "lands" already strips to "land"; keep as-is.
      }

      return new ObjectFilter
      {
        CardTypes = [singular],
        Controller = ControllerFilter.You,
      };
    }

    // Basic-land subtype labels. "Plains" is its own plural; the others
    // pluralize regularly. Match either form, normalize to the singular
    // subtype as it appears on a basic land's type line.
    var basicLandPlural = new Dictionary<string, string>(StringComparer.Ordinal)
    {
      ["Plains"] = "Plains",
      ["Islands"] = "Island",
      ["Swamps"] = "Swamp",
      ["Mountains"] = "Mountain",
      ["Forests"] = "Forest",
    };
    if (basicLandPlural.TryGetValue(raw, out var basicSubtype))
    {
      return new ObjectFilter
      {
        Subtypes = [basicSubtype],
        Controller = ControllerFilter.You,
      };
    }

    // Capitalized plural subtype labels (creature/artifact/land subtypes other
    // than basics): "Cats", "Humans", "Frogs", "Equipment", "Gates", "Towns", ...
    // Heuristic: starts with a capital letter; singularize by trailing-s strip
    // (irregular plurals are out of scope — none in the current corpus's
    // single-word Affinity surface).
    if (char.IsUpper(raw[0]) && !raw.Contains(' '))
    {
      var singular = raw.EndsWith("s") ? raw[..^1] : raw;
      return new ObjectFilter
      {
        Subtypes = [singular],
        Controller = ControllerFilter.You,
      };
    }

    // Fallback: preserve the raw text as a free-form characteristic. Multi-word
    // ("historic permanents", "snow lands", "artifact creatures") and unknown
    // shapes land here. Surfaces such cards for follow-up parsing rather than
    // silently mis-routing them through a singular card-type or subtype branch.
    return new ObjectFilter
    {
      Characteristics = [raw.ToLowerInvariant()],
      Controller = ControllerFilter.You,
    };
  }
}
