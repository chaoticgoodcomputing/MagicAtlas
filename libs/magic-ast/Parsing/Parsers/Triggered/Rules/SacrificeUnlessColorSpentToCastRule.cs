namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it unless {COLOR} was spent to cast it" — the Plaxmanta-family ETB
/// self-sacrifice gated on which mana color paid the casting cost (CR 601.2f–h;
/// CR 701.21a — Sacrifice).
///
/// <para>
/// Distinct from <see cref="SacrificeUnlessPayTriggeredRule"/> ("sacrifice it
/// unless you pay {COST}"): that shape is a payable cost with a live decision
/// at resolution (<see cref="UnlessClause"/>: <c>Player</c> + <c>Cost</c>).
/// "Unless {COLOR} was spent to cast it" offers no choice at resolution — it's
/// a lookup against a fact already fixed when the spell was cast (CR 601.2h).
/// Modelled instead as a <see cref="ConditionalEffect"/> gated on a
/// <see cref="ManaSpentToCastCondition"/> (<c>WasSpent = false</c>) whose
/// <c>Then</c> is the sacrifice — this is effect-level (CR 603), NOT an
/// intervening-if (CR 603.4): the ability always triggers on ETB, and the
/// color-spent check happens at resolution.
/// </para>
///
/// <para>
/// "It" refers back to the entering creature named as the trigger subject
/// (<see cref="ObjectReference.It()"/>), matching the pronoun-reference
/// convention used in <see cref="SacrificeUnlessPayTriggeredRule"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeUnlessColorSpentToCastRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+unless\s+\{(?<color>[WUBRG])\}\s+was\s+spent\s+to\s+cast\s+it$",
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

    effect = new ConditionalEffect
    {
      Condition = new ManaSpentToCastCondition
      {
        Color = m.Groups["color"].Value.ToUpperInvariant(),
        WasSpent = false,
      },
      Then = new SacrificeEffect { Target = ObjectReference.It() },
    };
    return true;
  }
}
