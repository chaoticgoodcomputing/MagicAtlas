namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Saddle N: Tap any number of other untapped creatures you control with total
/// power N or greater — this Mount becomes saddled until end of turn. Activate
/// only as a sorcery.
/// Rule 702.171. Category is Activated because the comp-rules expansion is an
/// activated ability (702.171a), but the oracle-text shorthand reads as a keyword
/// followed by a numeric threshold parameter. Structurally mirrors Crew (702.122)
/// but applies to Mounts; the saddled designation is engine territory.
/// </summary>
[Keyword]
public sealed class SaddleKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Saddle",
      RuleReference = "702.171",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Saddle",
        Effects = [new SaddleEffect
        {
          Value = ParseSaddleValue(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Saddle")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Saddle",
      Effects = [new SaddleEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Saddle-specific integer-parameter guard, inlined from the former
  /// <c>KeywordDefinitions.ParseSaddleValue</c>.
  /// </summary>
  private static int ParseSaddleValue(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Saddle requires a numeric parameter.", nameof(parameter));
    }

    if (!int.TryParse(parameter.Trim(), out var value))
    {
      throw new ArgumentException(
        $"Saddle parameter must be an integer, got '{parameter}'.",
        nameof(parameter)
      );
    }

    return value;
  }
}
