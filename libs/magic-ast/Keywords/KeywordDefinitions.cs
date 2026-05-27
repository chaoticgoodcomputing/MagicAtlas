namespace MagicAST.Keywords;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

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
      Vigilance,
      Storm,
      Protection,
      Crew,
      PartnerWith,
      // More keywords can be added here as needed
    ];

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
}
