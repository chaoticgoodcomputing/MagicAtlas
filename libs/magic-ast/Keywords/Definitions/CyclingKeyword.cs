namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Cycling {cost}: [Cost], Discard this card: Draw a card.
///
/// <para>
/// CR 702.29a (verbatim): "Cycling is an activated ability that functions only while
/// the card with cycling is in a player's hand. 'Cycling [cost]' means '[Cost],
/// Discard this card: Draw a card.'"
/// </para>
///
/// <para>
/// The hand-only functional restriction is engine territory — MAST does not add a
/// zone-restriction field. The full cost decomposition is recorded: a mana cost plus
/// a <see cref="DiscardCost"/> (this card), and the effect is a
/// <see cref="DrawCardsEffect"/> (draw 1). Combinator-only: no matching
/// <c>KeywordDefinitions</c> entry exists in the legacy registry.
/// </para>
/// </summary>
[Keyword]
public sealed class CyclingKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Cycling")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = "Cycling",
      Costs =
      [
        cost,
        new DiscardCost
        {
          Filter = new ObjectFilter { CardTypes = ["card"] },
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new DrawCardsEffect
        {
          Count = LiteralQuantity.Of(1),
          Player = new ObjectReference { Kind = ObjectReferenceKind.You },
        },
      ],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );
}
