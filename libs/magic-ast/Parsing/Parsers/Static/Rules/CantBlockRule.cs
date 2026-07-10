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
    @"^\s*(?<subject>(?!This\b|Enchanted\b|Equipped\b|Target\b|Each\b|All\b|Other\b|Any\b|That\b)[A-Z][^,\n]+?)\s+can'?t\s+block\.?\s*$",
    RegexOptions.Compiled
  );

  // A genuine self-by-name subject is a PROPER NOUN — the card's own (short) name
  // (Norin, Skrelv, Homura, Ozox). It is NOT a class of permanents. Any type/color/
  // state/relational common word, or a bare plural token, means the subject is a
  // board-wide restriction ("Creatures can't block.", "Black creatures…", "Beasts…",
  // "Untapped creatures…", "Goaded creatures your opponents control…") that this rule
  // MUST NOT steal — doing so would null Target to Self and silently drop the typed
  // subject filter (the sibling-mislabel overfit the offline edge-diff gate can't catch).
  private static readonly Regex _boardWideSubjectWord = new(
    @"\b(creature|creatures|land|lands|artifact|artifacts|enchantment|enchantments|planeswalker|planeswalkers|permanent|permanents|token|tokens|battle|battles|spell|spells|card|cards|white|blue|black|red|green|colorless|multicolored|monocolored|attacking|blocking|blocked|tapped|untapped|goaded|enchanted|equipped|legendary|basic|nonbasic|snow|with|without|other|another|your|their|each|all|any|that|control|controls|opponent|opponents)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static bool IsProperNounSelfReference(string subject)
  {
    subject = subject.Trim();
    // A typed/colored/state/relational word ⇒ a class of permanents, not a name.
    if (_boardWideSubjectWord.IsMatch(subject))
    {
      return false;
    }
    // A bare plural single token (e.g. "Beasts", a creature-type plural) is never a
    // card's self-name short form; proper-noun names (Norin/Skrelv/Homura/Ozox) aren't
    // plurals. Multi-word proper names (rare) are allowed through.
    if (!subject.Contains(' ') && subject.EndsWith("s", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    return true;
  }

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var selfMatch = _cantBlockSelfByNamePattern.Match(clause.RawText);
    var isSelfByName = selfMatch.Success && IsProperNounSelfReference(selfMatch.Groups["subject"].Value);
    if (_cantBlockPattern.IsMatch(clause.RawText) || isSelfByName)
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
