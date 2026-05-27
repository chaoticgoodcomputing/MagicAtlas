namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you get {E}", "you get {E}{E}", ... — energy-counter gain triggered effect.
/// Rule 107.14: Energy counters are a player resource represented by the {E} symbol.
/// Each "{E}" symbol in the oracle text represents one energy counter; we count
/// the symbols to derive the literal amount.
/// </summary>
/// <remarks>
/// Reminder text (e.g., "(two energy counters)") is stripped by
/// <c>TriggeredAbilityParser.ExtractTrailingReminder</c> upstream, so this rule
/// only sees the bare "you get {E}+" effect.
/// </remarks>
[TriggeredRule]
public sealed class GainEnergyTriggeredRule : ITriggeredRule
{
  // Allow whitespace between the {E} repetitions — the corpus is consistent but
  // an over-tight regex would reject reasonable variants.
  private static readonly Regex EnergyEffectRegex = new(
    @"^you\s+get\s+(?<symbols>(?:\{E\}\s*)+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex EnergySymbol = new(
    @"\{E\}",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = EnergyEffectRegex.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var count = EnergySymbol.Matches(m.Groups["symbols"].Value).Count;
    if (count <= 0)
    {
      return false;
    }

    effect = new GainEnergyEffect
    {
      Amount = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
