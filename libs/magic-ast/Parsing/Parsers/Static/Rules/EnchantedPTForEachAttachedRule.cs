namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Enchanted creature gets +N/+M for each &lt;subtype&gt; [and &lt;subtype&gt;]
/// attached to it." — Strong Back's "+2/+2 for each Aura and Equipment attached
/// to it". The counted set is the relational "attached to it" axis (CR 303/301
/// Aura/Equipment attachment), and the per-item increment exceeds 1, so the
/// dynamic amount is a <see cref="CalculatedQuantity"/> with a structured
/// <see cref="CalculatedQuantity.Operand"/> ("multiply the attached-count by N").
///
/// <para>
/// Sibling to <see cref="EnchantedPTForEachRule"/> (which handles the "you
/// control" board count at increment 1) and <see cref="SelfPTForEachAuraAttachedRule"/>
/// (the "This creature" subject). This rule covers the Enchanted/Equipped subject
/// over an attached-to-it relational count at increment &gt; 1.
/// </para>
/// </summary>
[StaticRule(Priority = 977)]
public sealed class EnchantedPTForEachAttachedRule : IStaticRule
{
  private static readonly Regex _pattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>.+?\s+attached\s+to\s+it)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = (match.Groups["psign"].Value == "-" ? -1 : 1) * int.Parse(match.Groups["p"].Value);
    var toughness = (match.Groups["tsign"].Value == "-" ? -1 : 1) * int.Parse(match.Groups["t"].Value);

    var filter = StaticRuleHelpers.BuildObjectCountFilter(match.Groups["filter"].Value.Trim());
    if (filter is null)
    {
      return null;
    }

    var powerModifier = BuildModifier(power, filter);
    var toughnessModifier = BuildModifier(toughness, filter);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          PowerModifier = powerModifier,
          ToughnessModifier = toughnessModifier,
        }],
      },
    ];
  }

  // A zero increment is a literal 0; a ±1 increment is a bare count; a larger
  // magnitude wraps the count in a "multiply" CalculatedQuantity carrying a
  // structured Operand (no free-text Expression).
  private static Quantity BuildModifier(int increment, ObjectFilter filter)
  {
    if (increment == 0)
    {
      return LiteralQuantity.Of(0);
    }

    var count = new CountQuantity { CountOf = filter };
    if (Math.Abs(increment) == 1)
    {
      return count;
    }

    return new CalculatedQuantity
    {
      BaseQuantity = count,
      Operation = "multiply",
      Operand = Math.Abs(increment),
    };
  }
}
