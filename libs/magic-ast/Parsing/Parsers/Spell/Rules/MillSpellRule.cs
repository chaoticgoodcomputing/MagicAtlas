namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Spell-resolution mill keyword action. Rule 701.17.
/// Handles five oracle clause forms:
/// <list type="bullet">
///   <item>"Mill N cards." — controller (Player = You).</item>
///   <item>"Target player mills N cards." — targeted player (Player = Target + player filter).</item>
///   <item>"Target opponent mills N cards." — targeted opponent (Player = Opponent).</item>
///   <item>"Target player mills half their library, rounded down." — half-library mill, rounded down
///     (Traumatize). CR 701.17a: to mill a number of cards is to put that many cards from the top of
///     the library into the graveyard.</item>
///   <item>"Target player mills half their library, rounded up." — half-library mill, rounded up.</item>
/// </list>
/// For the triggered-ability side, see <see cref="MagicAST.Parsing.Parsers.Triggered.Rules.MillTriggeredRule"/>.
/// </summary>
[SpellRule]
public sealed class MillSpellRule : ISpellRule
{
  private static readonly Regex SelfPattern = new(
    @"^Mill\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex TargetPattern = new(
    @"^Target\s+(?<target>player|opponent)\s+mills?\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "Target player mills half their library, rounded down/up."
  /// CR 701.17a — the count is half of the target player's library size, rounded in the named direction.
  /// Anchored (^ … $) to prevent substring collision with the two-sentence MaddeningCacophony form
  /// ("If this spell was kicked, instead each opponent mills half their library, rounded up.").
  /// </summary>
  private static readonly Regex TargetHalfLibraryPattern = new(
    @"^Target\s+(?<target>player|opponent)\s+mills?\s+half\s+their\s+library,?\s+rounded\s+(?<rounding>down|up)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var selfMatch = SelfPattern.Match(trimmed);
    if (selfMatch.Success)
    {
      effect = new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(selfMatch.Groups["count"].Value)),
        Player = ObjectReference.You(),
      };
      return true;
    }

    var halfMatch = TargetHalfLibraryPattern.Match(trimmed);
    if (halfMatch.Success)
    {
      var isOpponent = halfMatch.Groups["target"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);
      var player = isOpponent
        ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
        : ObjectReference.Target(ObjectFilter.Player());
      var rounding = halfMatch.Groups["rounding"].Value.ToLowerInvariant();

      var libraryCount = new CountQuantity
      {
        CountOf = new ObjectFilter
        {
          CardTypes = ["card"],
          Zone = Zone.Library,
          Controller = isOpponent ? ControllerFilter.Opponent : ControllerFilter.Target,
        },
      };
      var halfLibrary = new CalculatedQuantity
      {
        Operation = "half",
        BaseQuantity = libraryCount,
        Rounding = rounding,
      };

      effect = new MillEffect
      {
        Count = halfLibrary,
        Player = player,
      };
      return true;
    }

    var targetMatch = TargetPattern.Match(trimmed);
    if (targetMatch.Success)
    {
      var isOpponent = targetMatch.Groups["target"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);
      var player = isOpponent
        ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
        : ObjectReference.Target(ObjectFilter.Player());
      effect = new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(targetMatch.Groups["count"].Value)),
        Player = player,
      };
      return true;
    }

    return false;
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
