namespace MagicAST.Parsing.Combinators;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

/// <summary>
/// Composable parser combinators for Magic: The Gathering oracle text.
/// This module provides reusable building blocks that can be combined to parse complex ability patterns.
/// </summary>
/// <remarks>
/// Design philosophy:
/// - Small, focused parsers that do one thing well
/// - Compose via monadic combinators (Select, Then, Or, Many)
/// - Token-based, not string-based
/// - Type-safe and declarative
/// </remarks>
public static class OracleParsers
{
  #region Primitives

  /// <summary>
  /// Parses a specific keyword token (case-insensitive word match).
  /// Returns the matched token for use in further combinators.
  /// </summary>
  private static TokenListParser<OracleToken, Token<OracleToken>> Keyword(string keyword)
  {
    return Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t => t.ToStringValue().Equals(keyword, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Parses optional reminder text (parenthesized content).
  /// </summary>
  private static readonly TokenListParser<OracleToken, Parenthetical?> _optionalReminder = Token
    .EqualTo(OracleToken.ReminderText)
    .Select(t => (Parenthetical?)new Parenthetical { Text = t.ToStringValue() })
    .OptionalOrDefault();

  #endregion

  #region Simple Keywords

  /// <summary>
  /// Parser for the "Flying" keyword.
  /// Pattern: "Flying" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flying = (
    from keyword in Keyword("Flying")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flying",
      Effect = new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Vigilance" keyword.
  /// Pattern: "Vigilance" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Vigilance = (
    from keyword in Keyword("Vigilance")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Vigilance",
      Effect = new VigilanceEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Trample" keyword.
  /// Pattern: "Trample" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Trample = (
    from keyword in Keyword("Trample")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Trample",
      Effect = new TrampleEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Haste" keyword.
  /// Pattern: "Haste" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Haste = (
    from keyword in Keyword("Haste")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Haste",
      Effect = new HasteEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Lifelink" keyword.
  /// Pattern: "Lifelink" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Lifelink = (
    from keyword in Keyword("Lifelink")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Lifelink",
      Effect = new LifelinkEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Reach" keyword.
  /// Pattern: "Reach" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Reach = (
    from keyword in Keyword("Reach")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Reach",
      Effect = new ReachEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Flash" keyword.
  /// Pattern: "Flash" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flash = (
    from keyword in Keyword("Flash")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flash",
      Effect = new TimingModificationEffect
      {
        Modification = TimingModificationType.Grant,
        Timing = TimingWindow.Instant,
      },
      Reminder = reminder,
    }
  );

  #endregion

  #region Combat Timing Keywords

  /// <summary>
  /// Parser for "First strike" keyword (handles both "First strike" and "First Strike").
  /// Pattern: "First" "strike" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> FirstStrike = (
    from first in Keyword("First")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "First strike",
      Effect = new CombatDamageTimingEffect { Timing = CombatDamageTiming.First },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Double strike" keyword.
  /// Pattern: "Double" "strike" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> DoubleStrike = (
    from double_ in Keyword("Double")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Double strike",
      Effect = new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both },
      Reminder = reminder,
    }
  );

  #endregion

  #region Landwalk Keywords

  /// <summary>
  /// Creates a landwalk parser for a specific land type.
  /// </summary>
  private static TokenListParser<OracleToken, StaticAbility> Landwalk(
    string keywordName,
    string landType
  )
  {
    return from keyword in Keyword(keywordName)
      from reminder in _optionalReminder
      select new StaticAbility
      {
        KeywordSource = keywordName,
        Effect = new EvasionEffect
        {
          UnblockableCondition = new EvasionCondition
          {
            ConditionType = EvasionConditionType.DefendingPlayerControls,
            PermanentFilter = new ObjectFilter { Subtypes = [landType] },
          },
        },
        Reminder = reminder,
      };
  }

  /// <summary>Parser for "Forestwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Forestwalk = Landwalk(
    "Forestwalk",
    "Forest"
  );

  /// <summary>Parser for "Islandwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Islandwalk = Landwalk(
    "Islandwalk",
    "Island"
  );

  /// <summary>Parser for "Mountainwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Mountainwalk = Landwalk(
    "Mountainwalk",
    "Mountain"
  );

  /// <summary>Parser for "Plainswalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Plainswalk = Landwalk(
    "Plainswalk",
    "Plains"
  );

  /// <summary>Parser for "Swampwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Swampwalk = Landwalk(
    "Swampwalk",
    "Swamp"
  );

  #endregion

  #region Parameterized Keywords

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
  /// Parser for "Protection from X" keyword.
  /// Pattern: "Protection" "from" quality ["and" "from" quality]*
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Protection = (
    from keyword in Keyword("Protection")
    from from_ in Keyword("from")
    from first in _protectionQuality!
    from rest in _additionalProtectionQuality.Try().Many()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Protection",
      Effect = new ProtectionEffect { From = new[] { first }.Concat(rest).ToArray() },
      Reminder = reminder,
    }
  );

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

      // Otherwise, assume it's a subtype (capitalized in oracle text)
      // Examples: "Demons", "Dragons", "Elves"
      return new ProtectionQuality { Kind = ProtectionQualityKind.Subtype, Value = value };
    });

  #endregion

  #region Composite Parsers

  /// <summary>
  /// Parses any simple keyword ability.
  /// Tries each keyword parser in sequence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> SimpleKeyword = Flying
    .Try()
    .Or(Vigilance)
    .Or(Trample)
    .Or(Haste)
    .Or(Lifelink)
    .Or(Reach)
    .Or(Flash)
    .Or(FirstStrike)
    .Or(DoubleStrike)
    .Or(Forestwalk)
    .Or(Islandwalk)
    .Or(Mountainwalk)
    .Or(Plainswalk)
    .Or(Swampwalk);

  /// <summary>
  /// Parses any parameterized keyword ability.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> ParameterizedKeyword =
    Protection.Try();

  /// <summary>
  /// Parses any keyword ability (simple or parameterized).
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> AnyKeyword = SimpleKeyword
    .Try()
    .Or(ParameterizedKeyword);

  /// <summary>
  /// Parses a comma-separated list of keyword abilities.
  /// Example: "Flying, vigilance, trample"
  /// </summary>
  public static readonly TokenListParser<OracleToken, IReadOnlyList<StaticAbility>> KeywordList =
    AnyKeyword
      .ManyDelimitedBy(Token.EqualTo(OracleToken.Comma))
      .Select(arr => (IReadOnlyList<StaticAbility>)arr);

  #endregion
}
