namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Parsers;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Casualty N: two abilities per CR 702.153a.
///
/// <para>
/// CR 702.153a (verbatim): "Casualty is a keyword that represents two abilities. The first
/// is a static ability that functions while the spell with casualty is on the stack. The
/// second is a triggered ability that functions while the spell with casualty is on the
/// stack. Casualty N means 'As an additional cost to cast this spell, you may sacrifice a
/// creature with power N or greater,' and 'When you cast this spell, if a casualty cost was
/// paid for it, copy it. If the spell has any targets, you may choose new targets for the
/// copy.' Paying a spell's casualty cost follows the rules for paying additional costs in
/// rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.CasualtyStaticRule"/> (priority 1001), which
/// returns both abilities as a list. This keyword file keeps the combinator live as a
/// fallback, emitting only the PRIMARY additional-cost static ability (the cost half). The
/// <see cref="Definition"/> is null because <see cref="IKeywordExpander.Expand"/> can only
/// return a single <see cref="Ability"/> and Casualty decomposes into two.
/// </para>
///
/// <para>Combinator-only keyword — no <c>KeywordDefinition</c> entry exists in the
/// legacy <c>KeywordDefinitions.All</c> list for this keyword.</para>
/// </summary>
[Keyword]
public sealed class CasualtyKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Casualty decomposes into
  /// two abilities (CR 702.153a). The oracle-text parser handles the full two-ability
  /// output via CasualtyStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Casualty")
    from n in Token.EqualTo(OracleToken.Number)
    from reminder in OptionalReminder
    let power = int.Parse(n.ToStringValue())
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Casualty,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = new SacrificeCost
            {
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                PowerComparison = new Comparison
                {
                  Operator = ComparisonOperator.GreaterThanOrEqual,
                  Value = power,
                },
              },
              Quantity = LiteralQuantity.Of(1),
            },
            IsOptional = true,
            Repeatable = false,
          },
        },
      ],
    }
  );
}
