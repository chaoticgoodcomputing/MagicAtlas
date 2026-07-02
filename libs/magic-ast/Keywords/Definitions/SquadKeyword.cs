namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Squad {cost} (CR 702.157a): "As an additional cost to cast this spell, you may
/// pay {cost} any number of times" and "When this creature enters, if its squad cost
/// was paid, create a token that's a copy of it for each time its squad cost was
/// paid." Two linked abilities. This combinator is the single-ability fallback path:
/// it emits only the PRIMARY additional-cost static ability (the cost half). The full
/// two-ability decomposition is produced by
/// <see cref="MagicAST.Parsing.Parsers.Static.SquadStaticRule"/> (priority 1001, fires
/// first). Combinator-only: no matching <c>KeywordDefinitions</c> entry exists in the
/// legacy registry, so <see cref="Definition"/> is <c>null</c>.
/// </summary>
[Keyword]
public sealed class SquadKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Squad")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Squad,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = cost,
            IsOptional = true,
            Repeatable = true,
          },
        },
      ],
      Reminder = reminder,
    }
  );
}
