namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// Single-sentence "tap then freeze" back-reference effect, as it appears on the
/// resolution half of a triggered ability:
/// "tap that creature and it doesn't untap during its controller's next untap step."
/// (Kashi-Tribe Warriors — the combat-damage-to-a-creature trigger family).
///
/// <para>
/// Two conjoined instructions on one sentence: a <see cref="TapEffect"/> on the
/// back-referenced creature ("that creature") and a <see cref="DoesntUntapEffect"/>
/// on that same creature ("it"). Both back-references resolve to
/// <see cref="ObjectReferenceKind.It"/> — the creature the trigger condition just
/// named (the one dealt combat damage). The pair is wrapped in a single
/// <see cref="CompositeEffect"/>, matching the single-sentence-conjunction
/// convention already used on the triggered side
/// (<see cref="EtbTeamPumpTriggeredRule"/> for "get +N/+M and gain &lt;keyword&gt;").
/// </para>
///
/// <para>
/// CR 502.3 (Untap Step): "Third, the active player determines which permanents
/// they control will untap. Then they untap them all simultaneously... Normally,
/// all of a player's permanents untap, but effects can keep one or more of a
/// player's permanents from untapping." The "next untap step" wording is recorded
/// on <see cref="DoesntUntapEffect.WhoseUntapStep"/> as "its controller's next".
/// </para>
///
/// <para>
/// Mirrors <see cref="MagicAST.Parsing.Parsers.Spell.Rules.TapAndFreezeRule"/>
/// (the two-sentence spell form, "Tap target creature. It doesn't untap...") but
/// covers the one-sentence "and"-joined back-reference form that surfaces in
/// triggered-ability resolution text, where the creature is not re-targeted.
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class TapAndFreezeBackReferenceTriggeredRule : ITriggeredRule
{
  // "tap that creature and it doesn't untap during its controller's next untap step"
  private static readonly Regex _pattern = new(
    @"^tap\s+that\s+creature\s+and\s+it\s+doesn'?t\s+untap\s+during\s+(?<whose>its\s+controller'?s)\s+next\s+untap\s+step$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();

    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects = new List<Effect>
      {
        new TapEffect { Target = ObjectReference.It(), IsOptional = false },
        new DoesntUntapEffect
        {
          Target = ObjectReference.It(),
          WhoseUntapStep = "its controller's next",
          IsOptional = false,
        },
      },
      IsOptional = false,
    };
    return true;
  }
}
