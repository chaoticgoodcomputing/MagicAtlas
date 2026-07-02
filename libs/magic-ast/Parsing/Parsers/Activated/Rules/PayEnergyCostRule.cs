namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;

/// <summary>
/// "Pay N {E}" — energy-counter payment as an activation cost. Rule 107.14: The
/// energy symbol is {E}; each represents one energy counter removed from the player.
///
/// <para>
/// Oracle text may use three forms:
/// <list type="bullet">
///   <item>Word-number form: "Pay eight {E}" — word encodes count, symbol names currency.</item>
///   <item>Pay-symbols form: "Pay {E}{E}{E}" — "Pay" prefix + pure symbol repetition;
///     count derived from symbol occurrences (e.g., Whirler Virtuoso, Aetherworks Marvel).</item>
///   <item>Symbol-only form: "{E}{E}{E}" — no "Pay" prefix; count from symbol occurrences.</item>
/// </list>
/// MAST always emits the declared amount as a LiteralQuantity.
/// </para>
/// </summary>
[ActivatedCostRule(Priority = 1001)]
public sealed class PayEnergyCostRule : IActivatedCostRule
{
  // Form 1: "Pay <word-or-digit> {E}" — word number + one or more {E} symbols.
  // The word encodes the count; the {E} symbol(s) name the resource.
  private static readonly Regex _wordPattern = new(
    @"^Pay\s+(?<amount>(?:one|two|three|four|five|six|seven|eight|nine|ten|\d+))\s+(?:\{E\}\s*)+$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Form 2: "Pay {E}{E}..." — "Pay" prefix with pure symbol repetition (no word number).
  // Count derived by counting the {E} symbols. (e.g. "Pay {E}{E}{E}" on Whirler Virtuoso.)
  private static readonly Regex _paySymbolsPattern = new(
    @"^Pay\s+(?:\{E\}\s*)+$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Form 3: "{E}{E}..." only — multi-symbol form with no leading word or "Pay".
  private static readonly Regex _symbolOnlyPattern = new(
    @"^(?:\{E\}\s*)+$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _energySymbol = new(@"\{E\}", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  public Cost? TryMatch(string costText)
  {
    var trimmed = costText.Trim();

    // Try word-number form first ("Pay eight {E}").
    var wm = _wordPattern.Match(trimmed);
    if (wm.Success)
    {
      var rawAmount = wm.Groups["amount"].Value.ToLowerInvariant();
      int amount = rawAmount switch
      {
        "one"   => 1,
        "two"   => 2,
        "three" => 3,
        "four"  => 4,
        "five"  => 5,
        "six"   => 6,
        "seven" => 7,
        "eight" => 8,
        "nine"  => 9,
        "ten"   => 10,
        _       => int.TryParse(rawAmount, out var n) ? n : 0,
      };
      if (amount > 0)
      {
        return new PayEnergyCost { Amount = LiteralQuantity.Of(amount) };
      }
    }

    // Try "Pay {E}{E}..." form (Pay prefix + pure symbol repetition).
    if (_paySymbolsPattern.IsMatch(trimmed))
    {
      var count = _energySymbol.Matches(trimmed).Count;
      if (count > 0)
      {
        return new PayEnergyCost { Amount = LiteralQuantity.Of(count) };
      }
    }

    // Try symbol-only form ("{E}{E}{E}...").
    if (_symbolOnlyPattern.IsMatch(trimmed))
    {
      var count = _energySymbol.Matches(trimmed).Count;
      if (count > 0)
      {
        return new PayEnergyCost { Amount = LiteralQuantity.Of(count) };
      }
    }

    return null;
  }
}
