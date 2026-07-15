namespace MagicAST.Parsing.Parsers.Static;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST;
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
  //
  // The `effect` group spans the "instead" half ("those tokens plus … are
  // created instead") — the CREATE side. Carrying its span lets the port
  // projection give the emit (the created Squirrels) and the intercept (the
  // "if tokens would be created" condition, derived as the region before the
  // effect) distinct, clause-accurate highlights instead of one whole-line span.
  private static readonly Regex _tokenAugmentationPattern = new(
    @"^\s*If\s+one\s+or\s+more\s+tokens\s+would\s+be\s+created\s+under\s+your\s+control,\s+(?<effect>those\s+tokens\s+plus\s+that\s+many\s+(?<p>\d+)/(?<t>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens\s+are\s+created\s+instead\.?)\s*$",
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

    // The "instead" half's span in whole-oracle-text coordinates: the clause's
    // own offset plus the group's offset within the clause. Threaded onto the
    // replacement effect so the port projection can highlight the create side
    // (emit) distinctly from the "if tokens would be created" condition
    // (intercept), rather than one full-line span over both.
    var effectGroup = match.Groups["effect"];
    var effectSpan = new TextSpan(clause.SourceSpan.Start + effectGroup.Index, effectGroup.Length);

    var replacement = new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
    {
      SourceSpan = effectSpan,
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
          // The effect node carries the CREATE-side span so the projection's
          // trigger/effect boundary logic emits two clause-accurate ports: the
          // create (emit) over this span, and the "if tokens would be created"
          // condition (intercept) over the region before it.
          SourceSpan = effectSpan,
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
