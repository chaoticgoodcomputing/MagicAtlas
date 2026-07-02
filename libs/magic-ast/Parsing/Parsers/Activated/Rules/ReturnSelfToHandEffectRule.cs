namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return this {creature|permanent|Aura} to its owner's hand." — battlefield-self
/// bounce (CR 402, hand zone). Matches the self-referential phrasing found on
/// creatures like Darting Merfolk and Auras like Mourning/Conviction. Distinct
/// from <c>ReturnToBattlefieldEffectRule</c> (which handles graveyard-source
/// reanimation) and the sibling F5 rule (which handles "from your graveyard"
/// sourced returns).
///
/// CR 402: Return to hand is a plain zone change to the Hand zone; no dedicated
/// keyword action exists for it.
/// CR 303.4(m): "An ability of a permanent that refers to the 'enchanted [object
/// or player]' refers to whatever object or player that permanent is attached to,
/// even if the permanent with the ability isn't an Aura." — the self-subject here
/// is the Aura itself ("this Aura"), not the enchanted object, so Self is correct.
///
/// Anchor: "return this &lt;noun&gt; to its owner's hand" where &lt;noun&gt; is a
/// self-referential permanent-type word (creature, permanent, Aura, enchantment,
/// artifact, land).
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class ReturnSelfToHandEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Regex.Match(
      trimmed,
      @"^return\s+this\s+(?:creature|permanent|aura|enchantment|artifact|land)\s+to\s+its\s+owner's\s+hand$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    return new ReturnToHandEffect
    {
      Target = ObjectReference.Self(),
    };
  }
}
