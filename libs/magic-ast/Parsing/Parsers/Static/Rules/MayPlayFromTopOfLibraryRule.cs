namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Recognises the Bolas's Citadel two-sentence permission:
/// "You may play lands and cast spells from the top of your library.
///  If you cast a spell this way, pay life equal to its mana value rather than pay its mana cost."
///
/// <para>
/// This is a static ability (Rule 604 — continuous effect) that grants two simultaneous
/// permissions: (a) play lands from the top of the library, and (b) cast spells from the
/// top of the library, with (c) an alternative cost rider that replaces the mana cost of
/// any spell cast this way with "pay life equal to its mana value" (CR 118.9b — alternative
/// cost; CR 202.3 — mana value). MAST describes the permission; evaluation of what the top
/// card is at a given moment is engine territory (ADR 0003 describe-not-execute).
/// </para>
/// </summary>
[StaticRule(Priority = 940)]
public sealed class MayPlayFromTopOfLibraryRule : IStaticRule
{
  // Matches the exact two-sentence oracle text printed on Bolas's Citadel.
  // Sentence 1: "You may play lands and cast spells from the top of your library."
  // Sentence 2: "If you cast a spell this way, pay life equal to its mana value rather than pay its mana cost."
  // The trailing period on sentence 2 is optional for formatting variants.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+play\s+lands\s+and\s+cast\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\."
    + @"\s+If\s+you\s+cast\s+a\s+spell\s+this\s+way,\s+pay\s+life\s+equal\s+to\s+its\s+mana\s+value"
    + @"\s+rather\s+than\s+pay\s+its\s+mana\s+cost\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MayPlayFromTopOfLibraryEffect
          {
            Actions = [PlayFromTopAction.PlayLands, PlayFromTopAction.CastSpells],
            SpellAltCost = TopOfLibrarySpellAltCost.PayLifeEqualToManaValue,
          },
        ],
      },
    ];
  }
}
