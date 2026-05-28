namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Target player loses N life." — targeted life-loss as an activated-ability
/// effect (Rule 119), covering drain artifacts like Onyx Goblet.
/// </summary>
[ActivatedEffectRule(Priority = 983)]
public sealed class LoseLifeEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^Target\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(rawAmount),
    };

    return new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
  }
}
