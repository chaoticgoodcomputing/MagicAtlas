namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You lose N life." — spell-resolution controller life-loss (Rule 119.3).
/// Covers the trailing sentence in oracle texts like Diresight, Cruel Truths,
/// and Risky Research ("Surveil N, then draw M cards. You lose N life.").
/// </summary>
[SpellRule]
public sealed class YouLoseLifeSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^You\s+lose\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    effect = new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value)),
      Player = ObjectReference.You(),
    };
    return true;
  }
}
