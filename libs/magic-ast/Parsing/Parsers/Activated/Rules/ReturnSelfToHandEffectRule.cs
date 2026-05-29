namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return this {creature|permanent} to its owner's hand." — battlefield-self
/// bounce (CR 402, hand zone). Matches the self-referential phrasing found on
/// creatures like Darting Merfolk. Distinct from <c>ReturnToBattlefieldEffectRule</c>
/// (which handles graveyard-source reanimation) and the sibling F5 rule (which
/// handles "from your graveyard" sourced returns).
///
/// Anchor: "to its owner's hand" with no "graveyard" in the text.
/// </summary>
[ActivatedEffectRule(Priority = 989)]
public sealed class ReturnSelfToHandEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = Regex.Match(
      trimmed,
      @"^return\s+this\s+(?:creature|permanent)\s+to\s+its\s+owner's\s+hand$",
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
