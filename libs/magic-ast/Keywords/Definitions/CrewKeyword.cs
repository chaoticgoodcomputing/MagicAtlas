namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Crew N: Tap any number of other untapped creatures you control with total power N
/// or more — this Vehicle becomes an artifact creature until end of turn.
/// Rule 702.122. Category is Activated because the comp-rules expansion is an activated
/// ability (702.122a), but the oracle-text shorthand reads as a keyword followed by a
/// numeric parameter. MAST records the keyword's presence and cost; the tap-and-animate
/// mechanics are engine territory.
/// </summary>
[Keyword]
public sealed class CrewKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Crew",
      RuleReference = "702.122",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.Number,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = KeywordAbility.Crew,
        Effects = [new CrewEffect
        {
          Power = new LiteralQuantity { Value = ParseCrewPower(parameter) },
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Crew")
    from n in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Crew,
      Effects = [new CrewEffect
      {
        Power = new LiteralQuantity { Value = int.Parse(n.ToStringValue()) },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Crew-power parameter guard, inlined from the former
  /// <c>KeywordDefinitions.ParseCrewPower</c>.
  /// </summary>
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
}
