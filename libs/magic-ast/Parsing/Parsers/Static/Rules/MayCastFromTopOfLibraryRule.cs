namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Recognises "You may cast artifact spells and colorless spells from the top
/// of your library." and produces a <see cref="StaticAbility"/> with two
/// <see cref="MayPlayFromLibraryEffect"/> instances — one for artifact spells
/// and one for colorless spells.
///
/// <para>
/// The oracle line is a single sentence but names two disjoint eligibility
/// classes: artifact spells (CardTypes=["artifact"]) and colorless spells
/// (IsColorless=true). MAST models these as two sibling
/// <see cref="MayPlayFromLibraryEffect"/> nodes under one
/// <see cref="StaticAbility"/>: the pair expresses "you have permission to cast
/// from the top if the card is an artifact OR if the card is colorless". The
/// effects sit on the same ability rather than two separate abilities because
/// the oracle sentence is one continuous permission grant, not two discrete
/// abilities (CR 604.2 — topology not annotation).
/// </para>
///
/// <para>
/// CR 604.2: "Static abilities create continuous effects … These effects are
/// active as long as the permanent with the ability remains on the battlefield."
/// </para>
/// </summary>
[StaticRule(Priority = 940)]
public sealed class MayCastFromTopOfLibraryRule : IStaticRule
{
  // Matches "You may cast artifact spells and colorless spells from the top of
  // your library." The card types and color constraint are fixed for Mystic Forge;
  // extend to a parameterized match when other cards with this line appear.
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+artifact\s+spells\s+and\s+colorless\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
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
          // Permission to cast artifact spells from the top of the library.
          new MayPlayFromLibraryEffect
          {
            Cards = new ObjectFilter
            {
              CardTypes = ["artifact"],
              Zone = Zone.Library,
              Controller = ControllerFilter.You,
            },
          },
          // Permission to cast colorless spells from the top of the library.
          // "Colorless" is the absence of any color (CR 105.1): IsColorless=true
          // encodes this without adding a color restriction entry (which would
          // misrepresent it as "has the color colorless").
          new MayPlayFromLibraryEffect
          {
            Cards = new ObjectFilter
            {
              IsColorless = true,
              Zone = Zone.Library,
              Controller = ControllerFilter.You,
            },
          },
        ],
      },
    ];
  }
}
