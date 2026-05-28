namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Plot [cost]: You may pay [cost] and exile this card from your hand. Cast it
/// as a sorcery on a later turn without paying its mana cost. Plot only as a
/// sorcery.
/// Rule 702.170. MAST records the keyword's presence and cost only;
/// the exile-from-hand, deferred-cast, and sorcery-speed restrictions are
/// engine territory.
/// </summary>
[Keyword]
public sealed class PlotKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Plot",
      RuleReference = "702.170",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Plot",
        Effects = [new PlotEffect
        {
          Cost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Plot")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Plot",
      Effects = [new PlotEffect { Cost = cost }],
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
      throw new ArgumentException("Plot requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
