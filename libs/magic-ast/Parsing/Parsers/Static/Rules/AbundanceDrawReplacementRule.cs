namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Abundance optional draw-replacement shape:
/// "If you would draw a card, you may instead choose land or nonland and reveal
///  cards from the top of your library until you reveal a card of the chosen kind.
///  Put that card into your hand and put all other cards revealed this way on the
///  bottom of your library in any order."
///
/// <para>
/// This is a draw-replacement effect (CR 614.11) whose replacement action is optional
/// ("you may instead") — the controller may let the draw proceed normally. The
/// replacement action itself is the Abundance-specific "choose land or nonland + reveal
/// until found + put rest on bottom in any order" atomic action, modelled as
/// <see cref="AbundanceRevealEffect"/>.
/// </para>
///
/// <para>
/// Priority 981: one above <see cref="DrawReplacementRule"/> (980), so this more
/// specific "may instead" variant is checked first and the general "draw N instead"
/// rule is not reached for Abundance's oracle text.
/// </para>
/// </summary>
[StaticRule(Priority = 981)]
public sealed class AbundanceDrawReplacementRule : IStaticRule
{
  /// <summary>
  /// Matches the full two-sentence oracle text verbatim (anchored):
  /// "If you would draw a card, you may instead choose land or nonland and reveal
  ///  cards from the top of your library until you reveal a card of the chosen kind.
  ///  Put that card into your hand and put all other cards revealed this way on the
  ///  bottom of your library in any order."
  ///
  /// The pattern is fully anchored (^ … $) to prevent substring matches inside
  /// more-specific sibling effects.
  /// </summary>
  private static readonly Regex _pattern = new(
    @"^\s*If\s+you\s+would\s+draw\s+a\s+card,\s+you\s+may\s+instead\s+choose\s+land\s+or\s+nonland\s+and\s+reveal\s+cards?\s+from\s+the\s+top\s+of\s+your\s+library\s+until\s+you\s+reveal\s+a\s+card\s+of\s+the\s+chosen\s+kind\.\s*Put\s+that\s+card\s+into\s+your\s+hand\s+and\s+put\s+all\s+other\s+cards?\s+revealed\s+this\s+way\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+any\s+order\.?\s*$",
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
          new ReplacementEffect
          {
            Event = new DrawCardEvent
            {
              Player = ObjectReference.You(),
            },
            OriginalEventOccurs = false,
            IsOptional = true,
            Replacement = new AbundanceRevealEffect
            {
              Player = ObjectReference.You(),
            },
          },
        ],
      },
    ];
  }
}
