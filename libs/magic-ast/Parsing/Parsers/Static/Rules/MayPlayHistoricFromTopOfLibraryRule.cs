namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may play historic lands and cast historic spells from the top of
/// your library." (Crystal Skull, Isu Spyglass) and produces a
/// <see cref="StaticAbility"/> with a <see cref="MayPlayFromTopOfLibraryEffect"/>
/// granting both <see cref="PlayFromTopAction.PlayLands"/> and
/// <see cref="PlayFromTopAction.CastSpells"/>, restricted to historic cards.
///
/// <para>
/// CR 700.6: "The term historic refers to an object that has the legendary
/// supertype, the artifact card type, or the Saga subtype." Historic is a named
/// game quality rather than a printed type/subtype, so the eligibility
/// restriction is captured as <see cref="ObjectFilter.IsHistoric"/> on the
/// ability's <see cref="StaticAbility.AffectedObjects"/> — the same axis and
/// same shape as the sibling <c>HistoricSpellCostReductionRule</c>. Both actions
/// share the single "historic" restriction as printed (one eligibility class
/// scoping both the land-play and spell-cast permission), so a single
/// <see cref="MayPlayFromTopOfLibraryEffect"/> with both actions plus one
/// <c>AffectedObjects</c> filter models the sentence faithfully, rather than
/// splitting into two sibling effects (there is no filtered land-play-permission
/// effect node — <see cref="MayPlayFromLibraryEffect"/> is cast-only).
/// </para>
///
/// <para>
/// The parenthetical "(Artifacts, legendaries, and Sagas are historic.)" is
/// reminder text (CR 207.2) restating CR 700.6 and is stripped before matching,
/// mirroring <c>HistoricSpellCostReductionRule</c>'s handling of the identical
/// reminder on Jhoira's Familiar.
/// </para>
///
/// <para>
/// Fully anchored (^…$) after reminder-text stripping so this does not collide
/// with the unrestricted <see cref="MayPlayLandsAndCastSpellsFromTopOfLibraryRule"/>
/// or the Bolas's Citadel two-sentence <see cref="MayPlayFromTopOfLibraryRule"/>
/// sibling forms — neither of those surfaces contains the literal "historic"
/// tokens this pattern requires.
/// </para>
/// </summary>
[StaticRule(Priority = 943)]
public sealed class MayPlayHistoricFromTopOfLibraryRule : IStaticRule
{
  // Matches "You may play historic lands and cast historic spells from the top
  // of your library." after reminder-text stripping. Anchored (^…$) to prevent
  // substring collisions with the unrestricted and Bolas's-Citadel sibling forms.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+play\s+historic\s+lands\s+and\s+cast\s+historic\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var stripped = StaticRuleHelpers.StripReminderText(clause.RawText);
    if (!_pattern.IsMatch(stripped))
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
          },
        ],
        AffectedObjects = new ObjectFilter { IsHistoric = true },
      },
    ];
  }
}
