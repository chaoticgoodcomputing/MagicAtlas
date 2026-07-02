namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// Recognises the "Other creatures you control with [keyword] get +X/+Y" anthem shape.
///
/// A continuous P/T modification (CR 613.1, layer 7c) scoped to permanents
/// the controller owns that carry a particular keyword. The keyword predicate
/// is a "has [keyword]" filter encoded on <see cref="ObjectFilter.Characteristics"/>
/// (e.g. <c>"flying"</c>), consistent with how game-state predicates like
/// "tapped" and "with a +1/+1 counter" are encoded by sibling rules.
///
/// Example oracle text: "Other creatures you control with flying get +1/+0."
///   → ObjectFilter { CardTypes: ["creature"], Controller: You,
///                    ExcludeSelf: true, Characteristics: ["flying"] }
/// </summary>
[StaticRule(Priority = 971)]
public sealed class WithKeywordAnthemModifyPTRule : IStaticRule
{
  // Anchored pattern capturing:
  //   "other" qualifier (implicit — always present in this shape)
  //   "creatures you control with <kw>" noun phrase
  //   "+X/+Y" P/T delta
  // The keyword group captures a single lowercase word (e.g. "flying", "trample").
  // Reminder text, if any, is stripped by the caller before matching.
  private static readonly Regex _pattern = new(
    @"^\s*Other\s+creatures\s+you\s+control\s+with\s+(?<kw>[a-z][a-z ]*?)\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var kw = match.Groups["kw"].Value.Trim().ToLowerInvariant();
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
              Controller = ControllerFilter.You,
              ExcludeSelf = true,
              Characteristics = [Characteristic.FromLabel(kw)],
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }
}
