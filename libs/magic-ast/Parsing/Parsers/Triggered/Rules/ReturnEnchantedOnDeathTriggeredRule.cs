namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "return that card to the battlefield under your control" — the Animate Dead /
/// Unhallowed Pact pattern. This effect appears as the resolution clause of a
/// "When enchanted creature dies" trigger on Auras that reanimate their host.
///
/// "That card" is a pronoun back-reference (Rule 113.8b) to the enchanted creature
/// that just moved from the battlefield to the graveyard. MAST models it as
/// <see cref="ObjectReferenceKind.It"/> — the generic anaphoric reference kind
/// ("it", "that card", "the creature" all denote the same game object).
///
/// The "under your control" clause is captured in <c>UnderControl = You</c> on the
/// <see cref="ReturnToBattlefieldEffect"/>. Rule 400.6: an object that returns to the
/// battlefield under a specific player's control enters under that player's control
/// as a replacement on zone-change resolution.
///
/// Variant: "return it to its owner's hand" is handled by
/// <see cref="ReturnSelfToHandOnDeathTriggeredRule"/> — the effect text is identical
/// regardless of whether the trigger subject is "this Aura" or "enchanted creature",
/// so no separate rule is needed for the hand-return variant.
///
/// Rule 303.4 (Aura attachment); Rule 700.4 ("dies"); Rule 400.6 (enters under control).
/// </summary>
[TriggeredRule]
public sealed class ReturnEnchantedOnDeathTriggeredRule : ITriggeredRule
{
  // "return that card to the battlefield under your control"
  // Optional "you may" prefix for forward-compatibility.
  private static readonly Regex _pattern = new(
    @"^(?<opt>you\s+may\s+)?return\s+that\s+card\s+to\s+the\s+battlefield\s+under\s+your\s+control$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new ReturnToBattlefieldEffect {
      // "that card" — pronoun back-reference to the enchanted creature (now in graveyard).
      Target = ObjectReference.It(),
      // "under your control" — the controller of this Aura gains control of the returned creature.
      UnderControl = ObjectReference.You()}, match.Groups["opt"].Success);
    return true;
  }
}
