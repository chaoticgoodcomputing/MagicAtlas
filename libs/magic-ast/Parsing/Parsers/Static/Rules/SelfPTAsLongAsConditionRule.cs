namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This creature gets -N/-M as long as [condition]." — a conditional P/T modifier
/// applied to the source permanent itself (self subject). Paradigm card: CLB Halam Djinn —
/// "This creature gets -2/-2 as long as red is the most common color among all permanents
/// or is tied for most common."
///
/// <para>
/// This is a <b>static</b> continuous effect with an <see cref="AsLongAsDuration"/>: the
/// P/T modification applies only while the stated condition holds (CR 604.2 — static
/// abilities create continuous effects that are active as long as the permanent with the
/// ability remains on the battlefield and has the ability). The -N/-M is a layer-7c
/// modification (CR 613.4c). The condition is parsed by
/// <see cref="MagicAST.Parsing.ConditionParser"/>; conditions not yet structured emit an
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// <para>
/// A near-verbatim sibling of <see cref="EquippedPTAsLongAsConditionRule"/>, differing only
/// in the subject ("This creature/permanent" instead of "Equipped creature") and the target
/// (<see cref="ObjectReference.Self()"/> instead of
/// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>). Emits a single
/// <see cref="StaticAbility"/> with one <see cref="ModifyPTEffect"/>. Both the P and T
/// modifiers accept an explicit '+'/'-' sign, so "-2/-2" round-trips.
/// </para>
///
/// <para>
/// Priority 964 — deliberately BELOW the generic <see cref="AsLongAsStaticGrantRule"/> (968),
/// whose self-P/T pattern hard-requires '+' signs and therefore declines the negative form.
/// This rule claims only that negative self form that currently falls through; the generic
/// rule keeps first crack at the positive/ability-word self forms it already handles. The
/// pattern is end-anchored (^…$) so it cannot steal a longer clause.
/// </para>
/// </summary>
[StaticRule(Priority = 964)]
public sealed class SelfPTAsLongAsConditionRule : IStaticRule
{
  // "This creature/permanent gets ±N/±M as long as [condition]."
  // Both P and T accept an explicit sign (+/-) followed by one or more digits.
  // The condition runs from "as long as " to end-of-clause (period stripped by the
  // suffix anchoring).
  private static readonly Regex _pattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+as\s+long\s+as\s+(?<cond>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);
    var match = _pattern.Match(rawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var conditionText = match.Groups["cond"].Value.Trim();
    var duration = new AsLongAsDuration
    {
      Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
    };

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
          Duration = duration,
        }],
      },
    ];
  }
}
