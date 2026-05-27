namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each player loses N life." and "Each opponent loses N life." — spell-resolution
/// life-loss addressed to all players or all opponents (Rule 119.3, Rule 113.3a).
/// Examples: Crushing Disappointment, Risky Shortcut (EachPlayer);
/// Blood Tithe (EachOpponent).
/// </summary>
[SpellRule]
public sealed class LoseLifeEachSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^(?<scope>Each\s+player|Each\s+opponent)\s+loses\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var scope = m.Groups["scope"].Value;
    var amountText = m.Groups["amount"].Value;

    var playerKind = scope.ToLowerInvariant().Contains("opponent")
      ? ObjectReferenceKind.EachOpponent
      : ObjectReferenceKind.EachPlayer;

    Quantity amount;
    var amountLower = amountText.ToLowerInvariant();
    if (amountLower is "x" or "y" or "z")
    {
      amount = new VariableQuantity { Name = amountLower.ToUpperInvariant() };
    }
    else
    {
      amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(amountText));
    }

    effect = new LoseLifeEffect
    {
      Amount = amount,
      Player = new ObjectReference { Kind = playerKind },
    };
    return true;
  }
}
