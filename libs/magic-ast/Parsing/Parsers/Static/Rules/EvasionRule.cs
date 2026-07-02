namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 993)]
public sealed class EvasionRule : IStaticRule
{
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+except\s+by\s+(?<tail>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var tail = match.Groups["tail"].Value.ToLowerInvariant();
    var colors = new List<string>();
    var colorMap = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMap)
    {
      if (Regex.IsMatch(tail, $@"\b{name}\b"))
      {
        colors.Add(code);
      }
    }
    var cardTypes = new List<string>();
    foreach (var t in new[] { "creature", "permanent", "artifact" })
    {
      if (Regex.IsMatch(tail, $@"\b{t}s?\b"))
      {
        cardTypes.Add(t);
      }
    }

    var canBeBlockedBy = new ObjectFilter
    {
      CardTypes = cardTypes.Count > 0 ? cardTypes : null,
      Colors = colors.Count > 0 ? colors : null,
    };

    return
    [
      new StaticAbility
      {
        Effects = [new EvasionEffect { CanBeBlockedBy = canBeBlockedBy }],
      },
    ];
  }
}
