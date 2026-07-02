namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "target player loses N life and you gain N life" — Blood Artist / drain-on-death shape.
///
/// <para>
/// Distinct from <c>TryParseTargetOpponentLoseAndYouGainLife</c> (which requires "target
/// opponent") — this rule handles the more general "target player" form, which allows
/// targeting any player (including yourself). Blood Artist uses this phrasing because
/// the drain applies to any chosen player, not constrained to an opponent.
/// </para>
///
/// <para>
/// The two effects (lose life, gain life) are wrapped in a <see cref="CompositeEffect"/>
/// because <see cref="ITriggeredRule"/> returns a single <see cref="Effect"/>; the
/// composite preserves both effects as structured children rather than collapsing to
/// free text. CR 700.4: "The term dies means 'is put into a graveyard from the
/// battlefield.'" CR 603.2 (triggered ability event matching).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TargetPlayerLoseAndYouGainLifeRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^target\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+you\s+gain\s+(?<gain>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
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

    var loseAmount = ParseAmount(m.Groups["amount"].Value);
    var gainAmount = ParseAmount(m.Groups["gain"].Value);

    effect = new CompositeEffect
    {
      Effects =
      [
        new LoseLifeEffect
        {
          Amount = LiteralQuantity.Of(loseAmount),
          Player = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter { CardTypes = ["player"] },
          },
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
