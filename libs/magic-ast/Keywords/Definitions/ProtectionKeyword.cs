namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Protection from [quality]: This permanent can't be blocked, targeted, dealt damage,
/// enchanted, or equipped by anything with that quality.
/// Rule 702.16. MAST records the keyword and its quality parameter; the DEBT
/// restriction semantics are engine territory.
/// </summary>
[Keyword]
public sealed class ProtectionKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Protection")
    from from_ in Keyword("from")
    from first in _protectionQuality!
    from rest in _additionalProtectionQuality.Try().Many()
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Protection",
      Effects = [new ProtectionEffect { From = new[] { first }.Concat(rest).ToArray() }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parses additional protection qualities after "and from".
  /// Pattern: "and" "from" quality
  /// </summary>
  private static readonly TokenListParser<
    OracleToken,
    ProtectionQuality
  > _additionalProtectionQuality =
    from and in Token.EqualTo(OracleToken.And)
    from from_ in Keyword("from")
    from quality in _protectionQuality!
    select quality;

  /// <summary>
  /// Parses a single protection quality (color, subtype, etc.).
  /// Examples: "red", "Demons", "everything"
  /// </summary>
  private static readonly TokenListParser<OracleToken, ProtectionQuality> _protectionQuality = Token
    .EqualTo(OracleToken.Word)
    .Select(token =>
    {
      var value = token.ToStringValue();
      var normalized = value.ToLowerInvariant();

      // Special case: "everything"
      if (normalized == "everything")
      {
        return new ProtectionQuality { Kind = ProtectionQualityKind.Everything };
      }

      // Colors: red, blue, white, black, green
      if (normalized is "red" or "blue" or "white" or "black" or "green" or "colorless")
      {
        return new ProtectionQuality { Kind = ProtectionQualityKind.Color, Value = normalized };
      }

      // Card types: creatures, artifacts, enchantments, instants, sorceries
      var cardTypes = new[]
      {
        "creatures",
        "artifacts",
        "enchantments",
        "instants",
        "sorceries",
        "planeswalkers",
      };
      if (cardTypes.Contains(normalized))
      {
        // Singularize (remove trailing 's')
        var singular = normalized.EndsWith("s") ? normalized[..^1] : normalized;
        return new ProtectionQuality { Kind = ProtectionQualityKind.CardType, Value = singular };
      }

      // Characteristics: multicolored, monocolored, etc. — lowercase
      // single-word qualifiers that aren't a color name. Oracle text
      // capitalizes creature-type names (Demons, Dragons), so a lowercase
      // token here means a state predicate rather than a type.
      var characteristics = new[] { "multicolored", "monocolored", "colored" };
      if (characteristics.Contains(normalized))
      {
        return new ProtectionQuality
        {
          Kind = ProtectionQualityKind.Characteristic,
          Value = normalized,
        };
      }

      // Otherwise, assume it's a subtype (capitalized in oracle text)
      // Examples: "Demons", "Dragons", "Elves"
      return new ProtectionQuality { Kind = ProtectionQualityKind.Subtype, Value = value };
    });

  /// <summary>
  /// Parses a protection parameter string like "red", "Demons", or "red and from green"
  /// into a list of <see cref="ProtectionQuality"/> values.
  /// Inlined from the former <c>KeywordDefinitions.ParseProtectionQualities</c>.
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
