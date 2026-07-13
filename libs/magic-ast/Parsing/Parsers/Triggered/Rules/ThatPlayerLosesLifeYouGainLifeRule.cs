namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player loses N life and you gain N life" — symmetry-drain trigger keyed on a
/// prior "an opponent casts …"/"a player casts …" trigger event, where "that player" is
/// the pronoun for the player whose action fired the trigger (Yawgmoth's Edict pattern).
///
/// <para>
/// Distinct from <see cref="TargetPlayerLoseAndYouGainLifeRule"/> (which requires the
/// explicit "target player" wording and maps to <see cref="ObjectReferenceKind.Target"/>).
/// Here the affected player is a back-reference to the triggering player, encoded as
/// <see cref="ObjectReferenceKind.ThatPlayer"/> (mirroring <see cref="ThatPlayerLosesLifeRule"/>),
/// so no target is chosen. The trailing "and you gain N life" is what separates this from
/// the bare <see cref="ThatPlayerLosesLifeRule"/> (whose pattern is end-anchored after
/// "life" and therefore never matches this longer surface).
/// </para>
///
/// <para>
/// The two effects (lose life, gain life) are wrapped in a <see cref="CompositeEffect"/>
/// because <see cref="ITriggeredRule"/> returns a single <see cref="Effect"/>; the
/// composite preserves both as structured children rather than collapsing to free text.
/// </para>
///
/// <para>
/// Representative card: Yawgmoth's Edict — "Whenever an opponent casts a white spell, that
/// player loses 1 life and you gain 1 life."
/// Rule citations: CR 119.3 (an effect that causes a player to gain/lose life adjusts that
/// player's life total), CR 603.2 (triggered ability event matching), CR 109.5 (pronoun
/// back-reference to the player identified by the trigger).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThatPlayerLosesLifeYouGainLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+you\s+gain\s+(?<gain>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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

    effect = new CompositeEffect
    {
      Effects =
      [
        new LoseLifeEffect
        {
          Amount = LiteralQuantity.Of(loseAmount),
          Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        },
        new GainLifeEffect
        {
          Amount = LiteralQuantity.Of(gainAmount),
          Player = ObjectReference.You(),
        },
      ],
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
