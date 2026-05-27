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
  /// Menace: This creature can't be blocked except by two or more creatures.
  /// Rule 702.110. Evasion keyword whose distinguishing feature is a minimum
  /// blocker count rather than a characteristic filter on the blockers.
  /// </summary>
  public static KeywordDefinition Menace { get; } =
    new()
    {
      Name = "Menace",
      RuleReference = "702.110",
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
  // ALL DEFINITIONS (must be after individual definitions to avoid null refs)
  // ═══════════════════════════════════════════════════════════════════════════

  /// <summary>
  /// All registered keyword definitions.
  /// </summary>
  public static IReadOnlyList<KeywordDefinition> All { get; } =
    [
      Flying,
      Menace,
      FirstStrike,
      DoubleStrike,
      Lifelink,
      Indestructible,
      Flanking,
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
