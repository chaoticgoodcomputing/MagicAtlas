namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Mobilize N: Whenever this creature attacks, create N tapped and attacking
/// 1/1 red Warrior creature tokens. Sacrifice them at the beginning of the
/// next end step.
/// Rule 702.175 (Tarkir: Dragonstorm). Category is Triggered because the
/// comp-rules expansion is a triggered ability. MAST records the keyword and
/// its integer token-creation count.
/// </summary>
[Keyword]
public sealed class MobilizeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Mobilize")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Mobilize",
      Effects = [new MobilizeEffect
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
