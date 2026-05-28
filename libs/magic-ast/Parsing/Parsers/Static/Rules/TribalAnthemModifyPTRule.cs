namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 970)]
public sealed class TribalAnthemModifyPTRule : IStaticRule
{
  // Capitalised subtype (oracle text capitalises creature subtypes), followed by
  // "creatures" (lowercase plural card-type noun) and a controller clause. The
  // leading "Other " is what distinguishes this from the would-be inclusive
  // tribal anthem; without it the source itself would be in the filter and the
  // shape would need a different gold (no card hits that yet).
  private static readonly Regex _tribalAnthemModifyPTPattern = new(
    @"^\s*Other\s+(?<sub>[A-Z][a-z]+)\s+creatures\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _tribalAnthemModifyPTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = match.Groups["sub"].Value;
    var ctrl = match.Groups["ctrl"].Value.ToLowerInvariant();
    var controller = ctrl.StartsWith("you")
      ? ControllerFilter.You
      : ControllerFilter.Opponent;
    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Subtypes = [subtype],
              Controller = controller,
              Characteristics = ["other"],
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
