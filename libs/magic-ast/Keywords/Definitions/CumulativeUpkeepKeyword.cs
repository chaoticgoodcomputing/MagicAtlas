namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Cumulative upkeep [cost]: At the beginning of your upkeep, put an age counter on
/// this permanent, then sacrifice it unless you pay its upkeep cost for each age counter
/// on it. Rule 702.24. Category is Triggered because the comp-rules expansion is a
/// triggered ability that fires at the beginning of the controller's upkeep. The
/// age-counter-scaling and sacrifice-unless-pay semantics are engine territory — MAST
/// records the keyword's presence and the cost parameter only.
/// </summary>
[Keyword]
public sealed class CumulativeUpkeepKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Cumulative upkeep",
      RuleReference = "702.24",
      Category = KeywordCategory.Triggered,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Cumulative upkeep",
        Effects = [new CumulativeUpkeepEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from cumulative in Keyword("Cumulative")
    from upkeep in Keyword("upkeep")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = "Cumulative upkeep",
      Effects = [new CumulativeUpkeepEffect
      {
        Cost = cost,
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Mana-cost-parameter parser, inlined from the former
  /// <c>KeywordDefinitions.ParseManaCost</c>.
  /// </summary>
  private static ManaCost ParseManaCost(string? parameter)
  {
    if (string.IsNullOrWhiteSpace(parameter))
    {
      throw new ArgumentException("Cumulative upkeep requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
