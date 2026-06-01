namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Conspire: two abilities per CR 702.78a.
///
/// <para>
/// CR 702.78a (verbatim): "Conspire is a keyword that represents two abilities... 'Conspire'
/// means 'As an additional cost to cast this spell, you may tap two untapped creatures you
/// control that each share a color with it' and 'When you cast this spell, if its conspire
/// cost was paid, copy it. If the spell has any targets, you may choose new targets for the
/// copy.'"
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.ConspireStaticRule"/> (priority 1001), which
/// returns both abilities as a list. This keyword file keeps the combinator live as a
/// fallback: it emits only the PRIMARY additional-cost static ability ("As an additional
/// cost to cast this spell, you may tap two untapped creatures you control"). The
/// "each share a color with it" relational predicate has no <c>ObjectFilter</c> axis and is
/// omitted (no-free-text, no-new-axis). The <see cref="Definition"/> is null because
/// <see cref="IKeywordExpander.Expand"/> can only return a single <see cref="Ability"/> and
/// Conspire decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class ConspireKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Simple;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Conspire decomposes into two
  /// abilities (CR 702.78a). The oracle-text parser handles the full two-ability output via
  /// ConspireStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Conspire")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Conspire,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = new TapPermanentsCost
            {
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
              },
              Quantity = LiteralQuantity.Of(2),
            },
            IsOptional = true,
          },
        },
      ],
    }
  );
}
