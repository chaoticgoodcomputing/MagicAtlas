namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "tap this [permanent-noun] unless you pay N life" — the upkeep-tap-tax
/// pattern (Carnophage, Sewer Rats, Wall of Vipers, and other black one-drops).
/// The "unless you pay" gate is a cost-or-consequence: the source is tapped
/// (Rule 701.26a — Tap: "To tap a permanent, turn it sideways from an upright
/// position.") unless its controller chooses to pay the stated life cost, and
/// paying a cost is never automatic (Rule 118.5 — "it's not automatically paid").
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "At the beginning of your upkeep"
///   effect  = "tap this creature unless you pay 1 life"
/// </para>
///
/// <para>
/// The self-noun varies by the card's type — "this creature", "this permanent",
/// "this artifact", "this enchantment", "this Aura" — all name the same object:
/// the source bearing the ability (Rule 109.2 — self-reference). The noun is
/// descriptive type-flavour and carries no AST distinction, so the pattern
/// accepts the full set and the produced shape is identical regardless of which
/// noun was printed. MAST models the reference as
/// <see cref="ObjectReferenceKind.Self"/>, matching
/// <see cref="SacrificeSelfUnlessPayRule"/>.
/// </para>
///
/// <para>
/// Produces a <see cref="TapEffect"/> with Target = Self wrapped in a
/// <see cref="MagicAST.AST.Effects.Core.PreventableEffect"/> whose
/// <see cref="UnlessClause"/> Player is You and Cost is a
/// <see cref="PayLifeCost"/> for the stated amount. Mirrors
/// <see cref="SacrificeSelfUnlessPayRule"/> on the tap side.
/// </para>
///
/// <para>
/// Rule citations: 701.26a (Tap), 118.5 (paying a cost is not automatic).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TapSelfUnlessPayLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^tap\s+this\s+(?:creature|permanent|artifact|enchantment|aura)\s+unless\s+you\s+pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var amount = TriggeredRuleHelpers.ParseWordOrDigitCount(m.Groups["amount"].Value);
    if (amount is null)
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(
      new TapEffect { Target = ObjectReference.Self() },
      new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = new PayLifeCost { Amount = LiteralQuantity.Of(amount.Value) },
      });
    return true;
  }
}
