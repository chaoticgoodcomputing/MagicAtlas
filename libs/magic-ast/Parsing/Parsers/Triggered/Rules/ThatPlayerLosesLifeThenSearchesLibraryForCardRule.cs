namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player loses N life, searches their library for a card, puts it into
/// their hand, then shuffles" — Maralen of the Mornsong's each-player's-draw-step
/// trigger. Three conjoined actions taken by "that player" (Rule 603.2 — "that
/// player" resolves to the player identified by the trigger event, here the
/// player about to have their draw step): a life loss (CR 119.3), a library
/// search (CR 701.23a) that puts the found card into that player's hand, and the
/// mandatory post-search shuffle.
/// </summary>
/// <remarks>
/// Wrapped in a single <see cref="CompositeEffect"/> — mirroring the
/// single-sentence-conjunction convention used elsewhere for triggered
/// resolution text with more than one clause (<see cref="DiscardAndUntapAllLandsRule"/>).
/// The search is unrestricted ("for a card", no type/subtype qualifier), so its
/// <see cref="SearchLibraryEffect.Filter"/> carries no constraints — an empty
/// <see cref="ObjectFilter"/>. "Puts it into their hand, then shuffles" is folded
/// into the single <see cref="SearchLibraryEffect"/> (Destination = Hand) rather
/// than modelled as a separate shuffle effect, per the convention established by
/// the sibling search-to-hand rules (<see cref="SearchLibraryToHandTriggeredRule"/>).
/// <see cref="SearchLibraryEffect.Player"/> is set to
/// <see cref="ObjectReferenceKind.ThatPlayer"/> because the searcher here is the
/// player named by the trigger, not the ability's controller ("their library",
/// not "your library").
/// </remarks>
[TriggeredRule(Priority = 62)]
public sealed class ThatPlayerLosesLifeThenSearchesLibraryForCardRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life,\s*"
      + @"searches\s+their\s+library\s+for\s+a\s+card,\s*"
      + @"puts\s+it\s+into\s+their\s+hand,\s*"
      + @"then\s+shuffles\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();

    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.Parse(raw),
    };

    var thatPlayer = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer };

    effect = new CompositeEffect
    {
      Effects =
      [
        new LoseLifeEffect { Amount = LiteralQuantity.Of(amount), Player = thatPlayer },
        new SearchLibraryEffect
        {
          Filter = new ObjectFilter(),
          Count = LiteralQuantity.Of(1),
          Player = thatPlayer,
          Destination = SearchDestination.Hand,
          Revealed = false,
        },
      ],
    };
    return true;
  }
}
