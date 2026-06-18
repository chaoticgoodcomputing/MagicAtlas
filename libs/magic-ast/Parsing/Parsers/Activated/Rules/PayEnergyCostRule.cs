namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Quantities;

/// <summary>
/// "Pay N {E}" — energy-counter payment as an activation cost. Rule 107.14: The
/// energy symbol is {E}; each represents one energy counter removed from the player.
///
/// <para>
/// Oracle text may use either the word-number form ("Pay eight {E}") or the
/// multi-symbol form ("Pay {E}{E}{E}{E}{E}{E}{E}{E}"). In the word-number form,
/// the word (e.g. "eight") encodes the count and a single {E} names the currency
/// — the {E} symbol count need not equal the word value. In the multi-symbol form,
/// the count is derived by counting the {E} symbols (and no leading word is present).
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

  // Form 2: "{E}{E}..." only — multi-symbol form with no leading word number.
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
