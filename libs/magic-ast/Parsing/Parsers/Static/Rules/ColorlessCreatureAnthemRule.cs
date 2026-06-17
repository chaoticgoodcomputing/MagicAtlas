namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Colorless creatures you control get +N/+M." — continuous P/T anthem (CR 613.1, layer 7c)
/// scoped to colorless creatures the controller owns.
///
/// Colorless is not a color (Rule 105.1: "Colorless is not a color. Effects that specifically
/// affect colorless permanents or spells use the term 'colorless'.") so the filter encodes
/// colorlessness via <see cref="ObjectFilter.IsColorless"/> rather than the Colors axis.
/// This rule runs at priority 970 (above <see cref="LordPTBuffRule"/> at 969) to intercept the
/// "Colorless creatures" filter noun before LordPTBuffRule can misclassify "Colorless" as a
/// creature subtype. LordPTBuffRule's [Subtype] creatures branch would treat "Colorless" as a
/// subtype name (producing Subtypes: ["Colorless"]) rather than the correct IsColorless: true filter.
/// </summary>
[StaticRule(Priority = 970)]
public sealed class ColorlessCreatureAnthemRule : IStaticRule
{
  // "Colorless creatures you control get +N/+M." - exactly this shape.
  private static readonly Regex _pattern = new(
    @"^\s*Colorless\s+creatures\s+you\s+control\s+get\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["psign"].Value + match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["tsign"].Value + match.Groups["t"].Value);

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
              Controller = ControllerFilter.You,
              IsColorless = true,
            },
          },
          PowerModifier = LiteralQuantity.Of(power),
          ToughnessModifier = LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
