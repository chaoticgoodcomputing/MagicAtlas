namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Afflict N: Whenever this creature becomes blocked, defending player loses N life.
/// Rule 702.130. Integer-parameterized keyword marker.
///
/// <para>
/// The <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Afflict</c>
/// (including its inlined <c>ParseIntValue</c> guard); the <see cref="Combinator"/> is
/// the verbatim former <c>OracleParsers.Afflict</c>.
/// </para>
/// </summary>
[Keyword]
public sealed class AfflictKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition { get; } =
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

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Afflict")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Afflict",
      Effects = [new AfflictEffect { Value = int.Parse(value.ToStringValue()) }],
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
