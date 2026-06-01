namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Outlast [cost]: activated ability per CR 702.107a.
///
/// <para>
/// CR 702.107a (verbatim): "Outlast is an activated ability. 'Outlast [cost]' means
/// '[Cost], {T}: Put a +1/+1 counter on this creature. Activate only as a sorcery.'"
/// </para>
/// </summary>
[Keyword]
public sealed class OutlastKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition Definition { get; } =
    new()
    {
      Name = "Outlast",
      RuleReference = "702.107a",
      Category = KeywordCategory.Activated,
      HasParameter = true,
      ParameterType = KeywordParameterType.ManaCost,
      CreateExpansion = parameter => new ActivatedAbility
      {
        KeywordSource = KeywordAbility.Outlast,
        Costs =
        [
          ParseManaCost(parameter),
          new TapCost(),
        ],
        Effects =
        [
          new PutCountersEffect
          {
            Target = ObjectReference.Self(),
            CounterType = "+1/+1",
            Count = LiteralQuantity.Of(1),
          },
        ],
        Restrictions = [ActivationRestriction.OnlyAsSorcery],
        IsManaAbility = false,
      },
    };

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Outlast")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Outlast,
      Costs = [cost, new TapCost()],
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          CounterType = "+1/+1",
          Count = LiteralQuantity.Of(1),
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
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
      throw new ArgumentException("Outlast requires a mana cost parameter.", nameof(parameter));
    }

    var parsed = new ManaCostParser().Parse(parameter.Trim());
    return new ManaCost { Symbols = parsed.Symbols.ToList() };
  }
}
