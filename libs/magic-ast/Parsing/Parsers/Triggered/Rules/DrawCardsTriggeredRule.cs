namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "draw a card" with optional "unless that player pays {N}" tail.
/// </summary>
[TriggeredRule]
public sealed class DrawCardsTriggeredRule : ITriggeredRule
{
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var lower = text.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"\bdraw\s+(a|one|two|three|four|five|\d+)\s+cards?\b"))
    {
      return false;
    }

    var isOptional = lower.Contains("you may draw") || lower.StartsWith("you may ");
    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(text) ?? 1;

    var unlessMatch = Regex.Match(
      text,
      @"unless\s+(?<who>that\s+player|you|an\s+opponent)\s+pays?\s+(?<cost>\{[^}]+\}(?:\{[^}]+\})*)",
      RegexOptions.IgnoreCase
    );

    UnlessClause? unless = null;
    if (unlessMatch.Success)
    {
      var who = unlessMatch.Groups["who"].Value.ToLowerInvariant().Trim();
      var costStr = unlessMatch.Groups["cost"].Value;
      ObjectReference player = who switch
      {
        "that player" => new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        "you" => ObjectReference.You(),
        _ => new ObjectReference { Kind = ObjectReferenceKind.Opponent },
      };
      var manaCost = TriggeredRuleHelpers.TryBuildManaCost(costStr);
      if (manaCost is not null)
      {
        unless = new UnlessClause { Player = player, Cost = manaCost };
      }
    }

    effect = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      IsOptional = isOptional,
      UnlessClause = unless,
    };
    return true;
  }
}
