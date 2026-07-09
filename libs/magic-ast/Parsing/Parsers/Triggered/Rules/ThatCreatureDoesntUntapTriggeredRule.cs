namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.References;

/// <summary>
/// Standalone "freeze" back-reference effect on the resolution half of a triggered
/// ability: "that creature doesn't untap during its controller's next untap step."
/// (Cleric of Chill Depths — "Whenever this creature blocks a creature, that creature
/// doesn't untap during its controller's next untap step.").
///
/// <para>
/// Unlike <see cref="TapAndFreezeBackReferenceTriggeredRule"/> ("tap that creature and
/// it doesn't untap …", which pairs a tap with the freeze), this covers the lone
/// doesn't-untap instruction with NO tap — the creature the trigger named is simply
/// prevented from untapping. The back-reference "that creature" resolves to
/// <see cref="ObjectReferenceKind.ThatCreature"/> — the creature identified by the
/// trigger's condition (CR 509.1, the creature this creature blocked), the same
/// convention Inferno Elemental uses for "deals 3 damage to that creature".
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
public sealed class ThatCreatureDoesntUntapTriggeredRule : ITriggeredRule
{
  // "that creature doesn't untap during its controller's next untap step"
  private static readonly Regex _pattern = new(
    @"^that\s+creature\s+doesn'?t\s+untap\s+during\s+(?<whose>its\s+controller'?s)\s+next\s+untap\s+step$",
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
      Target = new ObjectReference { Kind = ObjectReferenceKind.ThatCreature },
      WhoseUntapStep = "its controller's next",
    };
    return true;
  }
}
