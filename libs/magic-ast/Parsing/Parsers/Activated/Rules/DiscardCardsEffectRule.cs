namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Discard N cards" — "Discard up to two cards", "Discard a legendary card".
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class DiscardCardsEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("discard"))
    {
      return null;
    }

    // Parse "up to N"
    var upToMatch = Regex.Match(effectText, @"up to (\w+)", RegexOptions.IgnoreCase);
    int count;
    if (upToMatch.Success)
    {
      count = ActivatedRuleHelpers.ParseNumberWord(upToMatch.Groups[1].Value) ?? 1;
    }
    else
    {
      count = ActivatedRuleHelpers.ParseNumberWord(effectText) ?? 1;
    }

    // Check for filter (e.g., "a legendary card")
    ObjectFilter? filter = null;
    if (lower.Contains("legendary"))
    {
      filter = new ObjectFilter { Supertypes = ["legendary"] };
    }

    return new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      Filter = filter,
      Random = false,
    };
  }
}
