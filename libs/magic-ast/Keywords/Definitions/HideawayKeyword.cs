namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Hideaway N: When this permanent enters, look at the top N cards of your library,
/// exile one face down, then put the rest on the bottom in a random order.
/// Rule 702.74. Category is Triggered because the comp-rules expansion is a triggered
/// ability that fires when the permanent enters. MAST records the keyword and its
/// integer lookahead count.
/// </summary>
[Keyword]
public sealed class HideawayKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Hideaway")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Hideaway",
      Effects = [new HideawayEffect
      {
        Amount = new LiteralQuantity { Value = int.Parse(value.ToStringValue()) },
      }],
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
