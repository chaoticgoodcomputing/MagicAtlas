namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target artifact or enchantment card from your graveyard to your hand."
///
/// The Argivian Find family — a Regrowth-style graveyard recursion instant, but with an
/// artifact-or-enchantment type-disjunction (parallel to <see cref="ReturnInstantOrSorceryFromGraveyardSpellRule"/>
/// for instant/sorcery). Resolved as a plain one-shot zone-change instruction (CR 608.2).
/// Source zone is the graveyard (CR 404.1); destination is the hand (CR 402.1); "target" is CR 115.1.
///
/// Zone = Graveyard encodes the source zone stated in oracle text;
/// Controller = You encodes "your graveyard".
/// CardTypes = ["artifact", "enchantment"] encodes the type-disjunction — the filter is satisfied
/// when the object has ANY of the listed types (OR semantics, CR 115.3).
///
/// Priority above the generic ReturnFromGraveyardToHandRule because this rule is more specific
/// (type-disjunction shape); the two are mutually exclusive in practice since the generic rule's
/// single-filter alternation never matches "artifact or enchantment".
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ReturnArtifactOrEnchantmentFromGraveyardSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+artifact\s+or\s+enchantment\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
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
          CardTypes = ["artifact", "enchantment"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
