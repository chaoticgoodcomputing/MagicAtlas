namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Keyword;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Buyback {cost}: You may pay an additional {cost} as you cast this spell. If you do,
/// put this card into your hand as it resolves. Rule 702.26. MAST records the keyword
/// and the buyback cost; the conditional-hand-return resolution is engine territory.
///
/// <para>
/// Exemplar of the <b>mana-cost-parameterized</b> keyword shape (Stage A template). The
/// <see cref="Definition"/> is the verbatim former <c>KeywordDefinitions.Buyback</c>
/// (including its inlined <c>ParseManaCost</c> guard); the <see cref="Combinator"/> is
/// the verbatim former <c>OracleParsers.Buyback</c>, with its inline mana-symbol
/// matcher replaced by the shared <see cref="KeywordCombinators.ManaCostSymbols"/>
/// (behaviour-identical — produces the same <see cref="ManaCost"/>).
/// </para>
/// </summary>
[Keyword]
public sealed class BuybackKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Buyback",
      RuleReference = "702.26",
      Category = KeywordCategory.Static,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new StaticAbility
      {
        KeywordSource = "Buyback",
        Effects = [new BuybackEffect
        {
          BuybackCost = ParseManaCost(parameter),
        }],
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, StaticAbility> Combinator { get; } = (
    from keyword in Keyword("Buyback")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select new StaticAbility
    {
      KeywordSource = "Buyback",
      Effects = [new BuybackEffect { BuybackCost = cost }],
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
      throw new ArgumentException("Buyback requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
