namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Activated-ability mill keyword action targeting any number of players.
/// CR 701.17a: "For a player to mill a number of cards, that player puts that many cards
/// from the top of their library into their graveyard."
///
/// Handles two oracle forms:
///   "Any number of target players each mill N cards."
///     → Count: LiteralQuantity(N), Player: Target+player+AnyAmount
///   "Any number of target players each mill cards equal to the number of cards in their graveyard."
///     → Count: CountQuantity(cards in target player's graveyard), Player: Target+player+AnyAmount
///
/// ANCHOR: Both patterns are anchored (^...$) to prevent matching substrings of
/// more-specific sibling effects.
///
/// For the singular "Target player mills N cards" form see <c>MillTargetPlayerEffectRule</c>.
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class AnyNumberOfTargetPlayersMillEffectRule : IActivatedEffectRule
{
  // "Any number of target players each mill N cards."
  private static readonly Regex FixedCountPattern = new(
    @"^Any\s+number\s+of\s+target\s+players\s+each\s+mill\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "Any number of target players each mill cards equal to the number of cards in their graveyard."
  private static readonly Regex GraveyardCountPattern = new(
    @"^Any\s+number\s+of\s+target\s+players\s+each\s+mill\s+cards?\s+equal\s+to\s+the\s+number\s+of\s+cards?\s+in\s+their\s+graveyard\s*\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The "any number of target players" reference — a targeted player reference
  /// with AnyAmountQuantity indicating an unbounded multi-target choice.
  /// </summary>
  private static readonly ObjectReference AnyNumberOfTargetPlayers = new()
  {
    Kind = ObjectReferenceKind.Target,
    Filter = ObjectFilter.Player(),
    Quantity = new AnyAmountQuantity(),
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();

    // Fixed count: "Any number of target players each mill N cards."
    var fixedMatch = FixedCountPattern.Match(trimmed);
    if (fixedMatch.Success)
    {
      return new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(fixedMatch.Groups["count"].Value)),
        Player = AnyNumberOfTargetPlayers,
      };
    }

    // Graveyard count: "Any number of target players each mill cards equal to the number of cards in their graveyard."
    if (GraveyardCountPattern.IsMatch(trimmed))
    {
      return new MillEffect
      {
        Count = new CountQuantity
        {
          CountOf = new ObjectFilter
          {
            CardTypes = ["card"],
            Zone = Zone.Graveyard,
            Owner = ControllerFilter.Target,
          },
        },
        Player = AnyNumberOfTargetPlayers,
      };
    }

    return null;
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
      var t => int.TryParse(t, out var n) ? n : 1,
    };
}
