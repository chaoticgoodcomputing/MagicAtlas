namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.Effects;
using MagicAST.AST.Effects.TokenCopy;

/// <summary>
/// "investigate." — Rule 701.16a keyword action on the triggered side.
/// Matches the bare keyword action when it appears as the entire effect body
/// of a triggered ability (e.g. Thraben Inspector's ETB trigger).
///
/// CR 701.16a: "Investigate" means "Create a Clue token." See rule 111.10f.
/// CR 603.6a: Enters-the-battlefield abilities trigger when a permanent enters
/// the battlefield. These are written, "When [this object] enters, . . . "...
///
/// REUSE-ONLY: emits the existing <see cref="InvestigateEffect"/> node.
/// No new AST types are introduced.
/// </summary>
[TriggeredRule]
public sealed class InvestigateTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!trimmed.Equals("investigate", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    effect = MagicAST.AST.Effects.Core.EffectWrap.Optional(new InvestigateEffect {}, false);
    return true;
  }
}
