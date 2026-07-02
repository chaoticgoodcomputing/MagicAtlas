namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Ripple N: When you cast this spell, you may reveal the top N cards of
/// your library, or, if there are fewer than N cards in your library, you
/// may reveal all the cards in your library. If you reveal cards from your
/// library this way, you may cast any of those cards with the same name as
/// this spell without paying their mana costs, then put all revealed cards
/// not cast this way on the bottom of your library in any order.
/// Rule 702.60. MAST records the keyword and its integer ripple value;
/// the reveal, free-cast, and bottoming semantics are engine territory.
/// </summary>
[Keyword]
public sealed class RippleKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Ripple",
      RuleReference = "702.60",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = KeywordAbility.Ripple,
        Effects = [new RippleEffect
        {
          Value = ParseIntValue("Ripple", parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Ripple")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Ripple,
      Effects = [new RippleEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Integer-parameter guard, inlined from the former <c>KeywordDefinitions.ParseIntValue</c>.
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
}
