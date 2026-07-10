namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it unless you discard a land card" — the ETB sacrifice-tax whose
/// alternative payment is discarding a land card (Fallow Wurm). Structurally the
/// sibling of <see cref="SacrificeItUnlessDiscardRandomRule"/> ("sacrifice it unless
/// you discard a card at random"): the <see cref="UnlessClause"/> cost is a
/// <see cref="DiscardCost"/>, but here the discarded card is a land card chosen by the
/// player (not a random discard), so <see cref="DiscardCost.Random"/> is left false and
/// the filter narrows to land cards.
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "When this creature enters"
///   effect  = "sacrifice it unless you discard a land card"
/// </para>
///
/// <para>
/// The "unless you [pay a cost]" gate is a cost-or-consequence: the sacrifice
/// (CR 701.21a — "To sacrifice a permanent, its controller moves it from the
/// battlefield directly to its owner's graveyard.") happens unless its controller
/// chooses to pay the stated cost, and paying a cost is never automatic (CR 118.5 —
/// "The same is true for an ability … it won't [pay] itself automatically"). The stated
/// cost is a discard (CR 701.9a — "To discard a card, move it from its owner's hand to
/// that player's graveyard.") restricted to a land card.
/// </para>
///
/// <para>
/// Produces a <see cref="PreventableEffect"/> with Inner = <see cref="SacrificeEffect"/>
/// (Target = It, the pronoun referring back to the trigger subject, matching
/// <see cref="SacrificeItUnlessDiscardRandomRule"/>) and an <see cref="UnlessClause"/>
/// whose Player is You and whose Cost is a one-card land <see cref="DiscardCost"/>.
/// </para>
///
/// <para>
/// Representative card: Fallow Wurm. Rule citations: 701.21a (Sacrifice), 118.5
/// (paying a cost is not automatic), 701.9a (Discard).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeItUnlessDiscardLandRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+unless\s+you\s+discard\s+a\s+land\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    var discardCost = new DiscardCost
    {
      Filter = new ObjectFilter { CardTypes = ["land"] },
      Quantity = LiteralQuantity.Of(1),
    };

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      new SacrificeEffect { Target = ObjectReference.It() },
      new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = discardCost,
      });
    return true;
  }
}
