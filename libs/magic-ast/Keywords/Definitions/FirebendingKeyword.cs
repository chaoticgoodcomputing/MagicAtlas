namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Firebending N (Avatar: The Last Airbender). A triggered keyword ability:
/// whenever this creature attacks, add N {R}. This mana lasts until end of
/// combat. MAST records the keyword and its integer value; the attack trigger,
/// mana-addition, and end-of-combat duration are engine territory.
/// Integer-parameterized keyword; mirrors the Bushido/Modular/Backup family.
/// Variable-value printings ("Firebending X, where X is ...") are not matched
/// by this parser.
/// Rule 702.175.
/// </summary>
[Keyword]
public sealed class FirebendingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Firebending")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Firebending",
      Effects = [new FirebendingEffect { Value = int.Parse(value.ToStringValue()) }],
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
