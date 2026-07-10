namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain N life and get {E}[{E}…]" — the single-sentence composite ETB/attack
/// trigger effect that both gains life AND grants energy counters (Guide of Souls:
/// "Whenever another creature you control enters, you gain 1 life and get {E}").
///
/// <para>
/// The two conjoined effects are modelled as a <see cref="CompositeEffect"/> wrapping a
/// <see cref="GainLifeEffect"/> and a <see cref="GainEnergyEffect"/>, mirroring the gold
/// shape for other single-sentence "X and Y" trigger bodies (Blood Artist's "target player
/// loses 1 life and you gain 1 life" → a single composite of the two effects).
/// </para>
///
/// <para>
/// CR 119.3 / CR 701.20 (gain life); CR 107.14 (energy counters — the {E} symbol
/// represents one energy counter each); CR 122.1 (counters are a player/permanent
/// resource marker). The trailing reminder "(an energy counter)" is normally stripped by
/// <c>TriggeredAbilityParser.ExtractTrailingReminder</c> upstream; an optional trailing
/// parenthetical is tolerated so the bare and inline-reminder forms both match.
/// </para>
///
/// <para>ANCHORED (^…$): the whole "you gain N life and get {E}…" sentence is anchored so
/// it cannot substring-match into a more specific sibling.</para>
/// </summary>
[TriggeredRule]
public sealed class GainLifeAndGetEnergyTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+gain\s+(?<life>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+get\s+(?<energy>(?:\{E\}\s*)+)(?:\s*\([^)]*\))?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _energySymbol = new(
    @"\{E\}",
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

    var lifeRaw = m.Groups["life"].Value.ToLowerInvariant();
    int life = lifeRaw switch
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
      _ => int.TryParse(lifeRaw, out var n) ? n : 1,
    };

    var energyCount = _energySymbol.Matches(m.Groups["energy"].Value).Count;
    if (energyCount <= 0)
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects = new List<Effect>
      {
        new GainLifeEffect
        {
          Amount = LiteralQuantity.Of(life),
          Player = ObjectReference.You(),
        },
        new GainEnergyEffect
        {
          Amount = LiteralQuantity.Of(energyCount),
          Player = ObjectReference.You(),
        },
      },
    };
    return true;
  }
}
