namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "(you may) return this card from your graveyard to the battlefield attached to
/// that creature" — the recursion-clause of the Dragon-Aura cycle (Dragon Fangs /
/// Dragon Scales / Dragon Breath / Dragon Wings / Dragon Shadow). This Aura, sitting
/// in its owner's graveyard, returns itself to the battlefield and re-attaches to the
/// big creature that just entered (the antecedent of "that creature" is the trigger's
/// own entering creature, CR 603.2).
///
/// <para>
/// Produces a <see cref="CompositeEffect"/> of two atomic effects sharing one
/// resolution sequence:
/// <list type="number">
///   <item><see cref="ReturnToBattlefieldEffect"/> — the Aura returns itself
///   (<see cref="ObjectReferenceKind.Self"/>) FROM the graveyard
///   (<c>Zone = Graveyard</c>, <c>Controller = You</c>). This is a reanimation-style
///   return (CR 603.2 / CR 614): a graveyard self-return, NOT an exile-blink, so the
///   filter deliberately carries NO <see cref="ObjectFilter.ExiledWith"/> marker
///   (that marker is reserved for CR 406.6 linked-exile returns — see Cloudshift).</item>
///   <item><see cref="AttachEffect"/> — attaches this Aura to "it"
///   (<see cref="ObjectReferenceKind.It"/>), the creature that just entered and
///   triggered the ability. CR 701.3 (Attach); CR 303.4 (Aura attachment).</item>
/// </list>
/// </para>
///
/// <para>
/// The leading "you may" is optional (CR 117.7): when present, the composite is
/// wrapped in an <see cref="OptionalEffect"/>. The whole text is end-anchored and
/// gated on the distinctive "from your graveyard to the battlefield attached to that
/// creature" phrase, so it is narrow to this Aura-return family.
/// </para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class ReturnSelfFromGraveyardAttachedToThatCreatureRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^(?<may>you\s+may\s+)?return\s+this\s+card\s+from\s+your\s+graveyard\s+to\s+the\s+battlefield\s+attached\s+to\s+that\s+creature$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.').Trim());
    if (!m.Success)
    {
      return false;
    }

    // Effect 1: the Aura returns itself from the graveyard to the battlefield.
    // Graveyard self-return (reanimation), NOT an exile-blink — no ExiledWith marker.
    var returnSelf = new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Self,
        Filter = new ObjectFilter
        {
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };

    // Effect 2: attach this Aura to "that creature" (the entering creature, "it").
    var attach = new AttachEffect
    {
      Target = ObjectReference.It(),
    };

    var composite = new CompositeEffect
    {
      Effects = [returnSelf, attach],
    };

    // "you may" → optional (CR 117.7); bare imperative → unwrapped composite.
    effect = m.Groups["may"].Success
      ? new OptionalEffect { Inner = composite }
      : composite;
    return true;
  }
}
