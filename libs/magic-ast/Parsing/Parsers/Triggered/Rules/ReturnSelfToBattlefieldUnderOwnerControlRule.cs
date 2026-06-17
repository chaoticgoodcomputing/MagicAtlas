namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return it to the battlefield under its owner's control" — the Enduring Tenacity
/// / Glimmer-creature dies-and-returns pattern. The returning permanent is described
/// by the pronoun "it" (back-reference to the triggering object, CR 113.8b —
/// "it" in an ability always refers to the object the trigger is about), and it
/// re-enters under its owner's control.
///
/// <para>
/// "under its owner's control" rides on <see cref="ReturnToBattlefieldEffect.UnderControl"/>
/// as an <see cref="ObjectReferenceKind.Owner"/> reference (CR 400.6 — an object
/// that returns to the battlefield under a specific player's control enters under
/// that player's control). The target is <see cref="ObjectReferenceKind.It"/> (the
/// triggering object), matching the Persist / Undying / Animate Dead family of
/// return effects.
/// </para>
///
/// <para>
/// Distinct from <see cref="ReturnEnchantedOnDeathTriggeredRule"/> ("return that card
/// to the battlefield under your control" — Aura reanimation where the controller
/// gains control) and <see cref="ReturnExiledCardOnLeaveTriggeredRule"/> ("return
/// the exiled card to the battlefield under its owner's control" — exile–return
/// pairs). This rule handles the bare "return it … under its owner's control" form
/// used for non-keyword self-return triggers.
/// </para>
///
/// CR 400.6 (enters under owner's control); CR 113.8b ("it" refers to the trigger
/// subject).
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ReturnSelfToBattlefieldUnderOwnerControlRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^return\s+it\s+to\s+the\s+battlefield\s+under\s+its\s+owner(?:'s|s')\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      UnderControl = new ObjectReference { Kind = ObjectReferenceKind.Owner },
      Tapped = false,
    };
    return true;
  }
}
