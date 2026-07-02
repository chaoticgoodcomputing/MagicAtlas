namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.References;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Buyback [cost]: two static abilities per CR 702.27a.
///
/// <para>
/// CR 702.27a (verbatim): "Buyback appears on some instants and sorceries. It represents
/// two static abilities that function while the spell is on the stack. 'Buyback [cost]'
/// means 'You may pay an additional [cost] as you cast this spell' and 'If the buyback
/// cost was paid, put this spell into its owner's hand instead of into that player's
/// graveyard as it resolves.'"
/// </para>
///
/// <para>
/// Oracle-text parsing is handled by
/// <see cref="MagicAST.Parsing.Parsers.Static.BuybackStaticRule"/> (priority 1001),
/// which returns both abilities as a list. This keyword file keeps the combinator live as
/// a fallback but no longer uses the deleted <c>BuybackEffect</c> opaque marker: it emits
/// only the PRIMARY additional-cast-cost static ability ("You may pay an additional [cost]
/// as you cast this spell"). The <see cref="Definition"/> is null because
/// <see cref="IKeywordExpander.Expand"/> can only return a single <see cref="Ability"/> and
/// Buyback decomposes into two.
/// </para>
/// </summary>
[Keyword]
public sealed class BuybackKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  /// <remarks>
  /// Null: the keyword expander returns a single Ability, but Buyback decomposes into two
  /// static abilities (CR 702.27a). The oracle-text parser handles the full two-ability
  /// output via BuybackStaticRule.
  /// </remarks>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Buyback")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Buyback,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = cost,
            IsOptional = true,
          },
        },
      ],
    }
  );
}
