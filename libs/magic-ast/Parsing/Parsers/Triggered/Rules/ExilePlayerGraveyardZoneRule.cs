namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "exile target player's graveyard" / "exile target opponent's graveyard" /
/// "exile each opponent's graveyard" / "exile each player's graveyard" — the
/// graveyard-hate family that exiles the entire contents of a player's graveyard
/// zone (Angel of Finality, Bojuka Bog, Hedonist's Trove, Dauntless Scrapbot).
///
/// <para>
/// CR 701.13a: "To exile an object, move it to the exile zone from wherever it is."
/// The action exiles every card in the named graveyard, so the effect's target is
/// the whole zone's contents rather than a single chosen card — modeled as an
/// <see cref="ObjectReferenceKind.Each"/> reference (all cards) whose filter pins
/// the graveyard zone and the owning player. This mirrors the established
/// whole-zone exile shape used by "Exile all creature cards from all graveyards"
/// (<c>Kind=Each</c> + <c>Zone=Graveyard</c>) and the target-player graveyard
/// scoping used by Riverchurn Monument (<c>Owner=Target</c> on a graveyard
/// filter). The player scope rides the filter's <see cref="ObjectFilter.Owner"/>
/// axis (CR 108.3 — cards in a graveyard are owned by that player):
/// "target player's" → <c>Owner=Target</c>; "target opponent's" /
/// "each opponent's" → <c>Owner=Opponent</c>.
/// </para>
///
/// Anchored (^…$) so it only fires on the bare whole-graveyard-exile body and
/// never inside a longer composite clause a more-specific rule should handle.
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class ExilePlayerGraveyardZoneRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^exile\s+(?<scope>target\s+player|target\s+opponent|each\s+opponent|each\s+player)'?s?\s+graveyard$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var scope = match.Groups["scope"].Value.ToLowerInvariant();
    var owner = scope.Contains("opponent") ? ControllerFilter.Opponent : ControllerFilter.Target;

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Owner = owner,
          Zone = Zone.Graveyard,
        },
      },
    };
    return true;
  }
}
