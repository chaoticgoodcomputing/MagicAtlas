namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Eternalize [cost]: activated ability from the graveyard — single ActivatedAbility node.
///
/// <para>
/// CR 702.129a (verbatim): "Eternalize is an activated ability that functions while the
/// card with eternalize is in a graveyard. 'Eternalize [cost]' means '[Cost], Exile this
/// card from your graveyard: Create a token that's a copy of this card, except it's black,
/// it's 4/4, it has no mana cost, and it's a Zombie in addition to its other types.
/// Activate only as a sorcery.'"
/// </para>
///
/// <para>
/// Costs: mana cost + ExileCost (this card from graveyard).
/// Effects: CreateTokenEffect with IsCopy=true and black/4/4/Zombie overrides.
/// Restriction: OnlyAsSorcery.
/// "No mana cost" and "in addition to its other types" are copy-rules handled by the engine.
/// </para>
/// </summary>
[Keyword]
public sealed class EternalizeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Eternalize")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Eternalize,
      Costs =
      [
        cost,
        new ExileCost
        {
          Filter = new ObjectFilter
          {
            Characteristics =
            [
              new OtherCharacteristic { Description = "this card" },
            ],
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
            Colors = ["B"],
            Power = "4",
            Toughness = "4",
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
