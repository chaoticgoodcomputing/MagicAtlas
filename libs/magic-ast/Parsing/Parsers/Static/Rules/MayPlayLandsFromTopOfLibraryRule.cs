namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.Parsing;

/// <summary>
/// Parses "You may play lands from the top of your library." and produces a
/// <see cref="StaticAbility"/> with a <see cref="MayPlayFromTopOfLibraryEffect"/>
/// whose <see cref="MayPlayFromTopOfLibraryEffect.Actions"/> list contains only
/// <see cref="PlayFromTopAction.PlayLands"/>.
///
/// <para>
/// CR 305.2: "A player can normally play one land during their turn; however,
/// continuous effects may increase this number." This rule extends the play-land
/// permission so eligible land cards may come from the top of the library rather
/// than (or in addition to) the hand. The "You may" preamble makes the permission
/// elective — the controller is not required to play lands from the top.
/// </para>
///
/// <para>
/// The effect node is <see cref="MayPlayFromTopOfLibraryEffect"/> (not
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/> wrapping a separate
/// effect) because "may" is semantically built into the effect type — it is a
/// permission grant, not a wrapped optional action. This follows the same
/// convention as <see cref="MayPlayLandsFromGraveyardRule"/>, which uses
/// <see cref="MayPlayFromGraveyardEffect"/> directly without an
/// <see cref="MagicAST.AST.Effects.Core.OptionalEffect"/> wrapper.
/// </para>
///
/// <para>
/// The pattern is fully anchored (^…$) so it does not misfire on
/// "You may play lands and cast spells from the top of your library."
/// (Bolas's Citadel), which is handled by <see cref="MayPlayFromTopOfLibraryRule"/>
/// and contains additional text beyond this shape.
/// </para>
/// </summary>
[StaticRule(Priority = 942)]
public sealed class MayPlayLandsFromTopOfLibraryRule : IStaticRule
{
  // Fully anchored to avoid shadowing the Bolas's Citadel two-sentence form
  // handled by MayPlayFromTopOfLibraryRule (Priority = 940, same band but
  // non-overlapping due to different regex surface).
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+play\s+lands\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
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
            Actions = [PlayFromTopAction.PlayLands],
          },
        ],
      },
    ];
  }
}
