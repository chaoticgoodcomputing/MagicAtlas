namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Gift a card (CR 702.174): "Gift a [something]" is a keyword representing two
/// abilities. CR 702.174a — the first is always the static additional cost "As an
/// additional cost to cast this spell, you may choose an opponent." CR 702.174b — on an
/// instant or sorcery spell the second is "If this spell's gift cost was paid, [effect]",
/// and CR 702.174e — "'Gift a card' means the effect is 'The chosen player draws a card.'"
///
/// <para>The keyword ability is emitted as a <see cref="StaticAbility"/> (CR 702.174a: the
/// first ability is a static ability) with <see cref="Ability.KeywordSource"/> =
/// <see cref="KeywordAbility.Gift"/> and two composed effects:</para>
/// <list type="number">
///   <item>an <see cref="OptionalEffect"/> ("you may") wrapping a
///   <see cref="ChoosePlayerEffect"/> — the promise of a gift to an opponent (CR 702.174a);
///   the opponent restriction is inherent to the gift keyword, so the reused
///   <see cref="ChoosePlayerEffect"/> ("choose a player") carries the declaration only
///   (batch-5 descriptive-not-engine convention);</item>
///   <item>a <see cref="GiftEffect"/> whose <c>Gifted</c> is a one-card
///   <see cref="DrawCardsEffect"/> by the chosen player
///   (<see cref="ControllerFilter.ChosenPlayer"/>) — the "Gift a card" payoff
///   (CR 702.174b/e).</item>
/// </list>
/// The verbatim reminder text is preserved on <see cref="Ability.Reminder"/> (CR 207.2).
///
/// <para>Parameterized tier: "Gift" is followed by "a card". <see cref="Definition"/> is
/// null (combinator-only, like Flashback / Unleash) — the DTO keyword list is not expanded,
/// and the parameterized payoff is recognized by the combinator, not the expander.</para>
/// </summary>
[Keyword]
public sealed class GiftKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from kw in Keyword("Gift")
    from a in Keyword("a")
    from card in Keyword("card")
    from reminder in OptionalReminder
    select (Ability)new StaticAbility
    {
      KeywordSource = KeywordAbility.Gift,
      Reminder = reminder,
      Effects =
      [
        new OptionalEffect { Inner = new ChoosePlayerEffect { Scope = ControllerFilter.Opponent } },
        new GiftEffect
        {
          Gifted = new DrawCardsEffect
          {
            Count = LiteralQuantity.Of(1),
            Player = new ObjectReference
            {
              Kind = ObjectReferenceKind.Designated,
              Filter = new ObjectFilter { Controller = ControllerFilter.ChosenPlayer },
            },
          },
        },
      ],
    }
  );
}
