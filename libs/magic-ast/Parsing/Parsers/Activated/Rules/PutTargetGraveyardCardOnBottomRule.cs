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
/// Handles the targeted single-card form:
///   "Put target card from your graveyard on the bottom of your library."
///
/// Not a defined keyword action (Rule 701.x has no "put on the bottom" entry); this
/// is a plain-language zone-change verb governed by CR 400.7 (a moved object becomes
/// a new object) and CR 401 (library-ordering rules). The paradigmatic instance is
/// Barkform Harvester's "{2}: Put target card from your graveyard on the bottom of
/// your library."
/// </summary>
[ActivatedEffectRule(Priority = 900)]
public sealed class PutTargetGraveyardCardOnBottomRule : IActivatedEffectRule
{
  // "Put target card from your graveyard on the bottom of your library."
  private static readonly Regex Pattern = new(
    @"^Put\s+target\s+(?<type>card|creature|artifact|enchantment|land|permanent|planeswalker)\s+from\s+(?<possessive>your|its\s+owner's)\s+graveyard\s+on\s+the\s+bottom\s+of\s+(?<libraryPossessive>your|its\s+owner's)\s+library\s*\.?\s*$",
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
    var isYours = match.Groups["possessive"].Value.Equals("your", StringComparison.OrdinalIgnoreCase);

    return new PutOnBottomOfLibraryEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = [typeWord],
          Zone = Zone.Graveyard,
          Controller = isYours ? ControllerFilter.You : ControllerFilter.Any,
        },
      },
    };
  }
}
