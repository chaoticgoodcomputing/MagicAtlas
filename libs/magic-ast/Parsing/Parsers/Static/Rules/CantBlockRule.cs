namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 959)]
public sealed class CantBlockRule : IStaticRule
{
  private static readonly Regex _cantBlockPattern = new(
    @"^\s*This\s+(?:creature|land|permanent)\s+can'?t\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Named-card self-reference: "[CardName] can't block." — the subject is the card
  // referring to itself by name (CR 201.5: a card's name in its own text means that
  // object), e.g. Norin, Swift Survivalist's "Norin can't block." Mirrors the
  // self-by-name arm of CantBeBlockedRule. The negative lookahead excludes the
  // leading tokens used by the OTHER static "can't ... block" shapes that also start
  // with a capitalized word — "Enchanted"/"Equipped" (Aura/Equipment bodies, handled
  // by EnchantedCantAttackOrBlockRule with an explicit EnchantedOrEquipped target) and
  // "Target"/"Each"/"All"/"Other"/"Any"/"That" (quantifier/targeting shapes) — so this
  // pattern cannot steal their clauses even though CantBlockRule's priority (959) runs
  // before EnchantedCantAttackOrBlockRule's (958). "This" is already handled by the
  // pattern above and is excluded here defensively. No trailing qualifier is allowed
  // (anchored $ right after "block."), so any "can't block by/because/..." variant
  // falls through untouched. Target stays null (unset) — per CantBlockEffect's
  // documented convention, null means the restriction applies to the static ability's
  // own controlling object (Self), exactly like the "This creature" form; the literal
  // card name never rides into the AST.
  private static readonly Regex _cantBlockSelfByNamePattern = new(
    @"^\s*(?!This\b|Enchanted\b|Equipped\b|Target\b|Each\b|All\b|Other\b|Any\b|That\b)[A-Z][^,\n]+?\s+can'?t\s+block\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (_cantBlockPattern.IsMatch(clause.RawText) || _cantBlockSelfByNamePattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Combat.CantBlockEffect()],
        },
      ];
    }
    return null;
  }
}
