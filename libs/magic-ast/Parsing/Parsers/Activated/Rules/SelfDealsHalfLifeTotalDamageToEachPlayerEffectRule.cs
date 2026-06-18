namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals damage to each player equal to half that player's life total, rounded down."
/// — the Heartless Hidetsugu activated-ability shape.
///
/// <para>
/// CR 602.1: "Activated abilities have a cost and an effect. They are written as
/// '[Cost]: [Effect.]'" — this rule fires after the cost ({T}) is stripped,
/// receiving only the effect clause.
/// </para>
///
/// <para>
/// The amount is a <see cref="CalculatedQuantity"/>
/// wrapping a <see cref="DerivedQuantity"/> (<c>DerivedFrom=LifeTotal</c>)
/// with <c>Operation="half"</c> and <c>Rounding="down"</c> (CR 107.1a:
/// "If a spell or ability could generate a fractional number, the spell or ability
/// will tell you whether to round up or down.").
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. This is generally detrimental to the object or player that receives
/// that damage. An object that deals damage is the source of that damage."
/// </para>
///
/// <para>
/// GUARD: fully anchored (^ … $). Matches only the named-subject form ending in
/// "each player equal to half that player's life total, rounded down" — does NOT
/// match "each opponent" (handled by
/// <see cref="SelfDealsDamageToEachOpponentEffectRule"/>).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 987)]
public sealed class SelfDealsHalfLifeTotalDamageToEachPlayerEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^(?<subject>[A-Z]\S.*?)\s+deals\s+damage\s+to\s+each\s+player\s+equal\s+to\s+half\s+that\s+player's\s+life\s+total,\s+rounded\s+down$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Pattern.Match(trimmed);
    if (!m.Success)
      return null;

    // Subject must start with an uppercase letter — it is the card's own name (Self).
    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
      return null;

    // Amount: half that player's life total, rounded down.
    // DerivedQuantity{LifeTotal} is the base; CalculatedQuantity halves it.
    var lifeTotal = new DerivedQuantity
    {
      DerivedFrom = DerivedKind.LifeTotal,
    };

    var halfLifeTotal = new CalculatedQuantity
    {
      BaseQuantity = lifeTotal,
      Operation = "half",
      Rounding = "down",
    };

    return new DealDamageEffect
    {
      Amount = halfLifeTotal,
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
    };
  }
}
