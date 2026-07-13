namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Return target sorcery card from your graveyard to your hand." — Déjà Vu.
///
/// A single-type graveyard-recursion sorcery, sibling to the type-disjunction
/// families <see cref="ReturnInstantOrSorceryFromGraveyardSpellRule"/> ("instant
/// or sorcery") and <see cref="ReturnArtifactOrEnchantmentFromGraveyardSpellRule"/>
/// ("artifact or enchantment"), but for the bare "sorcery" type alone. Resolved
/// as a plain one-shot zone-change instruction (CR 608.2). Source zone is the
/// graveyard (CR 404.1); destination is the hand (CR 402.1); "target" is CR 115.1.
///
/// Zone = Graveyard encodes the source zone stated in oracle text; Controller =
/// You encodes "your graveyard". CardTypes = ["sorcery"] encodes the single type
/// filter.
///
/// Priority above the generic ReturnFromGraveyardToHandRule (default 50) — that
/// rule's filter alternation (permanent|creature|artifact|enchantment|land|nonland
/// permanent) never includes "sorcery", so the two are mutually exclusive in
/// practice, but the elevated priority keeps this rule's more specific intent
/// unambiguous and consistent with its type-disjunction siblings.
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ReturnSorceryFromGraveyardSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Return\s+target\s+sorcery\s+card\s+from\s+your\s+graveyard\s+to\s+your\s+hand$",
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
          CardTypes = ["sorcery"],
          Zone = Zone.Graveyard,
          Controller = ControllerFilter.You,
        },
      },
    };
    return true;
  }
}
