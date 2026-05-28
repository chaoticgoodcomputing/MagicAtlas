namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Afterlife N: When this creature dies, create N 1/1 white and black Spirit
/// creature tokens with flying.
/// Rule 702.135. Integer-parameterized keyword marker.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Afterlife</c>
/// (including its inlined <c>ParseIntValue</c> guard); the <see cref="Combinator"/> is
/// the verbatim former <c>OracleParsers.Afterlife</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class AfterlifeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Afterlife")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Afterlife",
      Effects = [new AfterlifeEffect { Value = int.Parse(value.ToStringValue()) }],
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
