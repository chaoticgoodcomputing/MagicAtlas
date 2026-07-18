namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may have that player lose N life" — a bare optional drain with no "If you do"
/// follow-up, where "that player" is the pronoun back-reference to the player identified
/// by the trigger condition's filter (CR 109.5), e.g. the opponent whose creature entered.
///
/// <para>
/// Distinct from <see cref="MayHaveThatPlayerLoseLifeYouGainRule"/> (which requires the
/// trailing "If you do, you gain N life" clause — Bloodchief Ascension). Here there is no
/// follow-up: the "you may" makes the life-loss itself an optional triggered effect (CR
/// 603.5 — "Some triggered abilities' effects are optional... The choice is made when the
/// ability resolves"), full stop. Modelled as an <see cref="OptionalEffect"/> whose
/// <see cref="OptionalEffect.Inner"/> is <c>loseLife</c> (player =
/// <see cref="ObjectReferenceKind.ThatPlayer"/>) and whose <see cref="OptionalEffect.IfYouDo"/>
/// is omitted (null).
/// </para>
///
/// <para>
/// Representative card: Suture Priest (NPH) — "Whenever a creature an opponent controls
/// enters, you may have that player lose 1 life."
/// Rule citations: CR 603.5 (optional triggered effects), CR 119.3 (life totals), CR 109.5
/// ("that player").
/// </para>
/// </summary>
[TriggeredRule]
public sealed class MayHaveThatPlayerLoseLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+have\s+that\s+player\s+lose\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var loseAmount = ParseAmount(m.Groups["amount"].Value);

    effect = new OptionalEffect
    {
      Inner = new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseAmount),
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      },
    };
    return true;
  }

  private static int ParseAmount(string raw) => raw.ToLowerInvariant() switch
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
}
