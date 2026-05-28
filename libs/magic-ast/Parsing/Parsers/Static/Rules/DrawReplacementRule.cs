namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 980)]
public sealed class DrawReplacementRule : IStaticRule
{
  private static readonly Regex _drawReplacementPattern = new(
    @"^\s*If\s+you\s+would\s+draw\s+a\s+card,\s+draw\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _drawReplacementPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value.ToLowerInvariant();
    if (!StaticRuleHelpers.TryParseSmallCount(countText, out var count))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.DrawCardEvent
          {
            Player = ObjectReference.You(),
          },
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.CardFlow.DrawCardsEffect
          {
            Count = MagicAST.AST.Quantities.LiteralQuantity.Of(count),
            Player = ObjectReference.You(),
          },
        }],
      },
    ];
  }
}
