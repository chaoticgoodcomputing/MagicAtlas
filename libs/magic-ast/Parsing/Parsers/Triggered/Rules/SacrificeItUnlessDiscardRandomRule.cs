namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it unless you discard a card at random" — the ETB
/// sacrifice-tax whose alternative payment is a random discard rather than mana
/// (Balduvian Horde). Structurally identical to the mana-tax
/// <see cref="SacrificeUnlessPayTriggeredRule"/> ("sacrifice it unless you pay {C}")
/// except the <see cref="UnlessClause"/> cost is a <see cref="DiscardCost"/> whose
/// card is chosen at random rather than a <see cref="ManaCost"/>.
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "When this creature enters"
///   effect  = "sacrifice it unless you discard a card at random"
/// </para>
///
/// <para>
/// The "unless you [pay a cost]" gate is a cost-or-consequence: the sacrifice
/// (CR 701.21a — Sacrifice) happens unless its controller chooses to pay the stated
/// cost, and paying a cost is never automatic (CR 118.5). The stated cost here is a
/// random discard — CR 701.9b: "By default, effects that cause a player to discard a
/// card allow the affected player to choose which card to discard. Some effects,
/// however, require a random discard or allow another player to choose which card is
/// discarded." The randomness is carried structurally by <see cref="DiscardCost.Random"/>.
/// </para>
///
/// <para>
/// Produces a <see cref="PreventableEffect"/> with Inner = <see cref="SacrificeEffect"/>
/// (Target = It, the pronoun referring back to the trigger subject, matching
/// <see cref="SacrificeUnlessPayTriggeredRule"/>) and an <see cref="UnlessClause"/>
/// whose Player is You and whose Cost is a one-card random <see cref="DiscardCost"/>.
/// </para>
///
/// <para>
/// Representative card: Balduvian Horde (ALL). Rule citations: 701.21a (Sacrifice),
/// 118.5 (paying a cost is not automatic), 701.9b (random discard variant).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeItUnlessDiscardRandomRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+unless\s+you\s+discard\s+a\s+card\s+at\s+random$",
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
      Filter = new ObjectFilter { CardTypes = ["card"] },
      Quantity = LiteralQuantity.Of(1),
      Random = true,
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
