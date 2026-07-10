namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target player loses N life" — a bare targeted life-loss effect on a triggered
/// ability, with no accompanying gain-life clause (Infectious Host: "When this
/// creature dies, target player loses 2 life.").
///
/// <para>
/// Sibling of <see cref="TargetPlayerLoseAndYouGainLifeRule"/> (which requires the
/// trailing "and you gain N life" conjunct, the Blood Artist drain shape) and
/// <see cref="ThatPlayerLosesLifeRule"/> (which uses the anaphoric "that player"
/// pronoun instead of an explicit "target player"). The anchored pattern ends
/// immediately after "life", so it never matches the "and you gain" compound
/// sentence handled by <see cref="TargetPlayerLoseAndYouGainLifeRule"/>.
/// </para>
///
/// <para>
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life,
/// that player's life total is adjusted accordingly." CR 603.2 (triggered ability
/// event matching).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TargetPlayerLosesLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
    return true;
  }
}
