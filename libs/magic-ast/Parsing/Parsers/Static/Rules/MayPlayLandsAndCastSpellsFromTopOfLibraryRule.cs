namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Parses the standalone, unrestricted permission sentence "You may play lands
/// and cast spells from the top of your library." (no card-type/subtype
/// eligibility restriction, and no attached alternative-cost rider) and
/// produces a <see cref="StaticAbility"/> with a
/// <see cref="MayPlayFromTopOfLibraryEffect"/> granting both
/// <see cref="PlayFromTopAction.PlayLands"/> and
/// <see cref="PlayFromTopAction.CastSpells"/> (Magus of the Future).
///
/// <para>
/// This is the same permission grant Bolas's Citadel makes, but printed as a
/// single, standalone sentence with no eligibility restriction and no
/// alternative-cost rider — CR 604.2 (continuous effect from a static
/// ability): "Static abilities create continuous effects … These effects are
/// active as long as the permanent with the ability remains on the
/// battlefield." CR 305.1 and CR 601.2 govern playing lands and casting
/// spells respectively; this ability extends both permissions to cards on top
/// of the library. <see cref="MayPlayFromTopOfLibraryEffect.SpellAltCost"/>
/// is left null (no alternative cost applies).
/// </para>
///
/// <para>
/// Fully anchored (^…$) so it does not misfire on the Bolas's Citadel
/// two-sentence form ("… from the top of your library. If you cast a spell
/// this way, pay life equal to its mana value rather than pay its mana
/// cost."), which is handled by <see cref="MayPlayFromTopOfLibraryRule"/> and
/// is a single clause containing additional text beyond this shape (oracle
/// clauses are split on newlines, not sentence boundaries, so the Citadel
/// permission + rider sentence remain one clause and this pattern's trailing
/// `$` anchor declines it).
/// </para>
/// </summary>
[StaticRule(Priority = 941)]
public sealed class MayPlayLandsAndCastSpellsFromTopOfLibraryRule : IStaticRule
{
  // Fully anchored so this only matches the bare, unrestricted permission
  // sentence — not the Bolas's Citadel two-sentence form (MayPlayFromTopOfLibraryRule)
  // and not the lands-only or eligibility-restricted forms (MayPlayLandsFromTopOfLibraryRule,
  // MayCastFromTopOfLibraryRule).
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+play\s+lands\s+and\s+cast\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
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
          },
        ],
      },
    ];
  }
}
