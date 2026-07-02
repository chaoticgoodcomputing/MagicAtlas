namespace MagicAST.Parsing.Parsers.Static;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;

[StaticRule(Priority = 978)]
public sealed class TokenAugmentationReplacementRule : IStaticRule
{
  // Token-augmentation pattern (Chatterfang / Doubling-Season family). The
  // capture groups describe the ADDITIONAL token's printed face: power,
  // toughness, color, and creature subtype. The leading clause ("one or more
  // tokens would be created under your control") is fixed; per-event variation
  // (e.g. "twice that many" for Doubling Season) is intentionally NOT covered
  // here — that's a separate replacement-modifier shape.
  private static readonly Regex _tokenAugmentationPattern = new(
    @"^\s*If\s+one\s+or\s+more\s+tokens\s+would\s+be\s+created\s+under\s+your\s+control,\s+those\s+tokens\s+plus\s+that\s+many\s+(?<p>\d+)/(?<t>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens\s+are\s+created\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _tokenAugmentationColorMap = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _tokenAugmentationPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!_tokenAugmentationColorMap.TryGetValue(
      match.Groups["color"].Value.ToLowerInvariant(),
      out var colorCode))
    {
      return null;
    }

    var subtypeRaw = match.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(subtypeRaw[0]) + subtypeRaw[1..];

    var replacement = new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
    {
      Player = MagicAST.AST.References.ObjectReference.You(),
      Count = new MagicAST.AST.Quantities.CalculatedQuantity
      {
        Expression = "that many",
        Operation = "match",
      },
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = match.Groups["p"].Value,
        Toughness = match.Groups["t"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        IsCopy = false,
      },
    };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            MinimumQuantity = 1,
          },
          OriginalEventOccurs = true,
          Replacement = replacement,
        }],
      },
    ];
  }
}
