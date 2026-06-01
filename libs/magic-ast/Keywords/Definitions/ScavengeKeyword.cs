namespace MagicAST.Keywords.Definitions;

using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using static MagicAST.Keywords.Definitions.KeywordCombinators;

/// <summary>
/// Scavenge [cost]: An activated ability that functions only while the card with scavenge
/// is in a graveyard.
///
/// <para>
/// CR 702.97a (verbatim): "Scavenge is an activated ability that functions only while the
/// card with scavenge is in a graveyard. 'Scavenge [cost]' means '[Cost], Exile this card
/// from your graveyard: Put a number of +1/+1 counters equal to the power of the card you
/// exiled on target creature. Activate only as a sorcery.'"
/// </para>
///
/// <para>
/// MAST models this as a fully decomposed <see cref="ActivatedAbility"/>:
/// <list type="bullet">
///   <item>
///     Costs: the printed mana cost + an <see cref="ExileCost"/> (this card, from
///     Graveyard, quantity 1).
///   </item>
///   <item>
///     Effects: a <see cref="PutCountersEffect"/> placing +1/+1 counters on a target
///     creature; the count is a <see cref="DerivedQuantity"/> keyed on
///     <see cref="DerivedKind.Power"/> of "the card you exiled".
///   </item>
///   <item>
///     Restrictions: <see cref="ActivationRestriction.OnlyAsSorcery"/>.
///   </item>
/// </list>
/// </para>
/// </summary>
[Keyword]
public sealed class ScavengeKeyword : IKeyword
{
  /// <inheritdoc/>
  public KeywordTier Tier => KeywordTier.Parameterized;

  /// <inheritdoc/>
  public KeywordDefinition? Definition => null;

  /// <inheritdoc/>
  public TokenListParser<OracleToken, Ability> Combinator { get; } = (
    from keyword in Keyword("Scavenge")
    from cost in ManaCostSymbols
    from reminder in OptionalReminder
    select (Ability)new ActivatedAbility
    {
      KeywordSource = KeywordAbility.Scavenge,
      Costs =
      [
        cost,
        new ExileCost
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["card"],
            Characteristics = [Characteristic.Other("this card")],
          },
          Quantity = LiteralQuantity.Of(1),
          FromZone = Zone.Graveyard,
        },
      ],
      Effects =
      [
        new PutCountersEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
            },
          },
          CounterType = "+1/+1",
          Count = new DerivedQuantity
          {
            DerivedFrom = DerivedKind.Power,
            Source = "the card you exiled",
          },
        },
      ],
      Restrictions = [ActivationRestriction.OnlyAsSorcery],
      IsManaAbility = false,
      Reminder = reminder,
    }
  );
}
