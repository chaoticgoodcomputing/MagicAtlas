namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may have that player lose N life. If you do, you gain N life." —
/// an optional triggered drain where the controller optionally applies life-loss
/// to the player who triggered the ability (a "that player" back-reference),
/// and if they do, the controller gains life (Bloodchief Ascension, ZEN).
///
/// <para>
/// The "you may" makes the life-loss optional (CR 117.7: "if you do" clauses
/// are optional branches). The controller chooses whether to have "that player"
/// (CR 109.5 — the player identified by the trigger condition's filter, e.g.
/// the opponent whose graveyard received a card) lose life. If the controller
/// opts in, they also gain an equal amount of life.
/// </para>
///
/// <para>
/// Modelled as an <see cref="OptionalEffect"/> whose <see cref="OptionalEffect.Inner"/>
/// is <c>loseLife</c> (player = <see cref="ObjectReferenceKind.ThatPlayer"/>) and
/// whose <see cref="OptionalEffect.IfYouDo"/> is <c>gainLife</c> (player = You).
/// The "If you do" follow-up is the canonical structured <c>IfYouDo</c> slot of
/// <see cref="OptionalEffect"/> (ADR 0005 — clause modifiers are composition).
/// </para>
///
/// <para>
/// Representative card: Bloodchief Ascension (ZEN) — "… you may have that player
/// lose 2 life. If you do, you gain 2 life."
/// Rule citations: CR 117.7 (optional effects / "if you do"), CR 119.3 (life totals),
/// CR 109.5 ("that player").
/// </para>
/// </summary>
[TriggeredRule]
public sealed class MayHaveThatPlayerLoseLifeYouGainRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+have\s+that\s+player\s+lose\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.\s+If\s+you\s+do,\s+you\s+gain\s+(?<gain>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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
    var gainAmount = ParseAmount(m.Groups["gain"].Value);

    effect = new OptionalEffect
    {
      Inner = new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseAmount),
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      },
      IfYouDo = new GainLifeEffect
      {
        Amount = LiteralQuantity.Of(gainAmount),
        Player = ObjectReference.You(),
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
