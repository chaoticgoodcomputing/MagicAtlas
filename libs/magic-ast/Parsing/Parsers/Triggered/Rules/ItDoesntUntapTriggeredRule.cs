namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// Standalone "freeze" back-reference effect on the resolution half of a triggered
/// ability: "it doesn't untap during its controller's next untap step." (Stitcher's
/// Graft: "Whenever equipped creature attacks, it doesn't untap during its controller's
/// next untap step.").
///
/// <para>
/// Sibling of <see cref="ThatCreatureDoesntUntapTriggeredRule"/> ("that creature doesn't
/// untap …"), but the pronoun here is "it" — referring back to the trigger's SUBJECT
/// (the creature that attacked), not a third object named separately in the trigger
/// condition. Maps to <see cref="ObjectReferenceKind.It"/>, matching the pronoun
/// convention used elsewhere for the trigger subject (e.g. <c>SacrificeTriggeredRule</c>'s
/// "sacrifice it").
/// </para>
///
/// <para>
/// CR 502.3 (Untap Step, verbatim): "Third, the active player determines which
/// permanents they control will untap. Then they untap them all simultaneously. This
/// turn-based action doesn't use the stack. Normally, all of a player's permanents
/// untap, but effects can keep one or more of a player's permanents from untapping."
/// The "next untap step" wording is recorded on
/// <see cref="DoesntUntapEffect.WhoseUntapStep"/> as "its controller's next".
/// </para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ItDoesntUntapTriggeredRule : ITriggeredRule
{
  // "it doesn't untap during its controller's next untap step"
  private static readonly Regex _pattern = new(
    @"^it\s+doesn'?t\s+untap\s+during\s+(?<whose>its\s+controller'?s)\s+next\s+untap\s+step$",
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

    effect = new DoesntUntapEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      WhoseUntapStep = "its controller's next",
    };
    return true;
  }
}
