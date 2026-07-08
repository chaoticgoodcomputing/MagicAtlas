namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Activated-ability graveyard-to-library-bottom zone change. CR 602.1: activated
/// abilities are written as "[Cost]: [Effect.]" — this rule recognizes the
/// post-colon effect fragment.
///
/// Handles the "any graveyard" targeted form (the source's own player is not
/// constrained), where the destination is explicitly the moved card's OWNER's
/// library rather than the activating player's:
///   "Put target card from a graveyard on the bottom of its owner's library."
///
/// Distinct from <see cref="PutTargetGraveyardCardOnBottomRule"/>, which handles
/// the "your graveyard" / "its owner's graveyard" possessive forms — those two
/// regexes are mutually exclusive (one requires a possessive before "graveyard",
/// this one requires the bare article "a"), so neither can shadow the other.
/// Not a defined keyword action (Rule 701.x has no "put on the bottom" entry);
/// this is a plain-language zone-change verb governed by CR 400.7 (a moved
/// object becomes a new object) and CR 401 (library-ordering rules). The
/// paradigmatic instance is Chrome Companion's "{2}, {T}: Put target card from
/// a graveyard on the bottom of its owner's library."
/// </summary>
[ActivatedEffectRule(Priority = 900)]
public sealed class PutTargetCardFromAGraveyardOnBottomOfOwnersLibraryRule : IActivatedEffectRule
{
  // "Put target card from a graveyard on the bottom of its owner's library."
  private static readonly Regex Pattern = new(
    @"^Put\s+target\s+(?<type>card|creature|artifact|enchantment|land|permanent|planeswalker)\s+from\s+a\s+graveyard\s+on\s+the\s+bottom\s+of\s+its\s+owner's\s+library\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var match = Pattern.Match(effectText.Trim());
    if (!match.Success)
    {
      return null;
    }

    var typeWord = match.Groups["type"].Value.ToLowerInvariant();

    return new PutOnBottomOfLibraryEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [typeWord],
          Zone = Zone.Graveyard,
        },
      },
    };
  }
}
