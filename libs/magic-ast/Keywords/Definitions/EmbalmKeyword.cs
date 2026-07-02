namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Embalm is an activated ability that functions while the card with embalm is in a
/// graveyard. (CR 702.128a verbatim): "Embalm is an activated ability that functions
/// while the card with embalm is in a graveyard. 'Embalm [cost]' means '[Cost], Exile
/// this card from your graveyard: Create a token that's a copy of this card, except
/// it's white, it has no mana cost, and it's a Zombie in addition to its other types.
/// Activate only as a sorcery.'"
///
/// <para>
/// MAST models this as a single <see cref="ActivatedAbility"/> with two costs:
/// the mana cost parameter and an <see cref="ExileCost"/> (this card from graveyard).
/// The effect is a <see cref="CreateTokenEffect"/> with a copy-override
/// <see cref="TokenDefinition"/>: <c>IsCopy = true</c>, overrides white
/// (<c>Colors = ["W"]</c>) and Zombie subtype added (<c>Subtypes = ["Zombie"]</c>).
/// "No mana cost" and "in addition to its other types" are copy-rules engine concerns —
/// only the structured overrides are modelled.
/// </para>
/// </summary>
[Keyword]
public sealed class EmbalmKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Embalm")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Embalm,
      Costs =
      [
        cost,
        new ExileCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["card"],
            IsSelf = true,
          },
          FromZone = Zone.Graveyard,
          Quantity = LiteralQuantity.Of(1),
        },
      ],
      Effects =
      [
        new CreateTokenEffect
        {
          Player = ObjectReference.You(),
          Count = LiteralQuantity.Of(1),
          Token = new TokenDefinition
          {
            IsCopy = true,
            Colors = ["W"],
            Subtypes = ["Zombie"],
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );
}
