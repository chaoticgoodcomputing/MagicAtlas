namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each opponent loses N life" — symmetric drain trigger (Marauding Blight-Priest pattern).
/// Covers triggered effects where all opponents lose a fixed life amount simultaneously.
///
/// <para>
/// Rule 119.3: "If a player would lose life, that player's life total is reduced by that
/// amount." Rule 102.2: "An opponent is each other player." In multiplayer, this effect
/// applies to each opponent simultaneously; the simultaneous-resolution ordering is
/// engine territory, not described by oracle text.
/// </para>
///
/// <para>
/// This rule handles the bare N-life form only. The compound drain pattern
/// "each opponent loses N life and you gain N life" is handled by
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser.TryParseEachOpponentLoseAndYouGainLife"/>
/// in the multi-effect composite path.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachOpponentLosesLifeTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^each\s+opponent\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text);
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
    effect = new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
    };
    return true;
  }
}
