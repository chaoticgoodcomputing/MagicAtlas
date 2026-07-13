namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "put that card on top of your library" — the resolution clause of a
/// creature-put-into-your-graveyard-from-the-battlefield trigger (Mortuary).
///
/// <para>
/// "That card" anaphorically back-references the creature card named by the
/// trigger condition, now resident in the graveyard — the same generic
/// back-reference kind used elsewhere for "it"/"that card"/"the creature"
/// (<see cref="ObjectReferenceKind.It"/>). Maps to <see cref="PutOnTopOfLibraryEffect"/>
/// (Rule 701 — general zone-change actions): the effect always moves the
/// target to the top of its owner's library, and because the paired trigger
/// (<see cref="CreatureToGraveyardFromBattlefieldConditionRule"/>) already
/// restricts the event to a creature put into a graveyard YOU own, "your
/// library" and "its owner's library" name the same person here — no separate
/// "whose library" axis is needed on the effect.
/// </para>
///
/// <para>
/// Canonical card: Mortuary (VIS) — "Whenever a creature is put into your
/// graveyard from the battlefield, put that card on top of your library."
/// </para>
///
/// Anchored (^…$) so this cannot match as a substring of a longer, more
/// specific resolution clause.
/// </summary>
[TriggeredRule]
public sealed class PutThatCardOnTopOfYourLibraryTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^put\s+that\s+card\s+on\s+top\s+of\s+your\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new PutOnTopOfLibraryEffect { Target = ObjectReference.It() };
    return true;
  }
}
