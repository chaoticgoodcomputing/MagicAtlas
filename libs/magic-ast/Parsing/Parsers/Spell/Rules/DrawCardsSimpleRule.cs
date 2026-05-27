namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Draw-cards spell-resolution rule. Handles three oracle clause forms:
/// <list type="bullet">
///   <item>"Draw [N] card(s)." — controller draws (Player = You).</item>
///   <item>"Target player draws [N] card(s)." — targeted player draws (Player = Target + player filter).</item>
///   <item>"Target opponent draws [N] card(s)." — targeted opponent draws (Player = Opponent).</item>
/// </list>
/// Count tokens: literal words (one–ten), decimal digits, or variable slots (X/Y/Z).
/// </summary>
[SpellRule]
public sealed class DrawCardsSimpleRule : ISpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+|X|Y|Z";

  private static readonly Regex SelfPattern = new(
    $@"^Draw\s+(?<count>{CountTokens})\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex TargetPattern = new(
    $@"^Target\s+(?<target>player|opponent)\s+draws?\s+(?<count>{CountTokens})\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var selfMatch = SelfPattern.Match(trimmed);
    if (selfMatch.Success)
    {
      effect = new DrawCardsEffect
      {
        Count = ParseCount(selfMatch.Groups["count"].Value),
        Player = ObjectReference.You(),
      };
      return true;
    }

    var targetMatch = TargetPattern.Match(trimmed);
    if (targetMatch.Success)
    {
      var isOpponent = targetMatch.Groups["target"].Value.Equals(
        "opponent", StringComparison.OrdinalIgnoreCase
      );
      var player = isOpponent
        ? new ObjectReference { Kind = ObjectReferenceKind.Opponent }
        : ObjectReference.Target(ObjectFilter.Player());
      effect = new DrawCardsEffect
      {
        Count = ParseCount(targetMatch.Groups["count"].Value),
        Player = player,
      };
      return true;
    }

    return false;
  }

  private static Quantity ParseCount(string raw)
  {
    var lower = raw.ToLowerInvariant();
    if (lower is "x" or "y" or "z")
    {
      return new VariableQuantity { Name = lower.ToUpperInvariant() };
    }
    var n = lower switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ when int.TryParse(lower, out var d) => d,
      _ => 1,
    };
    return LiteralQuantity.Of(n);
  }
}
