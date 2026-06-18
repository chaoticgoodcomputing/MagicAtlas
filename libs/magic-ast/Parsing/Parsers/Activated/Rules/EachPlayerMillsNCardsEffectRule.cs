namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each player mills N cards." — broadcast mill keyword action applied to every
/// player simultaneously (CR 701.17a: "For a player to mill a number of cards, that
/// player puts that many cards from the top of their library into their graveyard.").
///
/// <para>
/// Used by Syr Konrad, the Grim's activated ability: "{1}{B}: Each player mills a card."
/// The symmetrical form applies the same mill count to the controller AND each opponent,
/// modelled as a single <see cref="MillEffect"/> with
/// <see cref="ObjectReferenceKind.EachPlayer"/> as the recipient — matching the
/// "each player" subject in oracle text.
/// </para>
///
/// <para>
/// Anchored (^ … $) to prevent substring collision with the more-targeted
/// <c>MillTargetPlayerEffectRule</c> ("target player mills N cards") and the
/// self-mill form handled elsewhere. Distinct from "target player mills" (targeted
/// activation) and "each opponent mills" (opponent-only spread).
/// </para>
///
/// <para>
/// CR 701.17a (verbatim): "For a player to mill a number of cards, that player puts
/// that many cards from the top of their library into their graveyard."
/// CR 603.2: triggered abilities fire on each matching mill event.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class EachPlayerMillsNCardsEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Each\s+player\s+mills?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    return new MillEffect
    {
      Count = LiteralQuantity.Of(ParseCount(m.Groups["count"].Value)),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
    };
  }

  private static int ParseCount(string token) =>
    token.ToLowerInvariant() switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.TryParse(token, out var n) ? n : 1,
    };
}
