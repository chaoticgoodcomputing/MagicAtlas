namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target spell [you don't control|an opponent controls|you control] to its owner's hand."
///
/// Handles the special case of returning a spell on the stack (Zone: Stack, CardTypes: ["spell"])
/// to its owner's hand. This differs from <see cref="ReturnTargetToHandRule"/> which handles
/// permanent types (creature, artifact, land, etc.) but not spells.
///
/// Rule 400.1 — zones; Rule 608.2b — spell on the stack is a game object with a controller.
/// "A spell you don't control" means the spell is controlled by an opponent (CR 108.4).
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ReturnTargetSpellToHandRule : ISpellRule
{
  // "Return target spell [you don't control | an opponent controls | you control] to its owner's hand."
  private static readonly Regex Pattern = new(
    @"^Return\s+target\s+spell(?:\s+(?<ctrl>you\s+don't\s+control|an?\s+opponent\s+controls?|you\s+control))?\s+to\s+its?\s+owner'?s\s+hands?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var ctrlGroup = m.Groups["ctrl"].Value.ToLowerInvariant();

    ControllerFilter? controller = ctrlGroup switch
    {
      var s when s.Contains("don't control") || s.Contains("opponent controls") => ControllerFilter.Opponent,
      var s when s.Contains("you control") => ControllerFilter.You,
      _ => null,
    };

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = controller,
          Zone = Zone.Stack,
        },
      },
    };
    return true;
  }
}
