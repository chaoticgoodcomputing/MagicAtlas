namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target instant or sorcery card from your graveyard to your hand."
///
/// Models the Regrowth-style regrowth-of-spells family (e.g. Relearn) as a
/// stand-alone sorcery — the same effect body as the triggered Archaeomancer /
/// Izzet Chronarch family, resolved as a plain one-shot zone-change instruction
/// (CR 608.2). Source zone is the graveyard (CR 404.1); destination is the hand
/// (CR 402.1); "target" is CR 115.1.
///
/// Zone = Graveyard encodes the source zone stated in oracle text;
/// Controller = You encodes "your graveyard".
/// CardTypes = ["instant", "sorcery"] encodes the type-disjunction.
///
/// Priority above the generic ReturnFromGraveyardToHandRule (default 50) because
/// this rule is more specific (type-disjunction shape); the two are mutually
/// exclusive in practice since the generic rule's filter alternation never matches
/// "instant or sorcery".
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ReturnInstantOrSorceryFromGraveyardSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+instant\s+or\s+sorcery\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["instant", "sorcery"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
