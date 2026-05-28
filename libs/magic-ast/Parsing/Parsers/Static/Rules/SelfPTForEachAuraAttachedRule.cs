namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "This creature gets +N/+N for each Aura attached to it." — the Kor
/// Spiritdancer family. Distinct from <see cref="SelfPTForEachRule"/> in two
/// ways: (1) the counted set is "Aura attached to it" (an attachment
/// relationship, not a "you control" board count), and (2) the per-Aura
/// increment may exceed 1 (Spiritdancer is +2/+2 per Aura), which
/// <see cref="SelfPTForEachRule"/> explicitly defers to a "distinct family
/// shape" by returning null on multiplier &gt; 1.
///
/// <para>
/// The dynamic amount is "N × (number of Auras attached)". A per-each
/// increment of 1 reuses a bare <see cref="CountQuantity"/> (matching the
/// single-increment convention); an increment &gt; 1 wraps that count in a
/// <see cref="CalculatedQuantity"/> with <c>Operation = "multiply"</c>,
/// mirroring the established CalculatedQuantity-wrapping convention (e.g. the
/// "negate" wrapper in <c>ActivatedRuleHelpers.ParseSignedModifier</c>). No
/// new <c>Quantity</c> subtype is introduced — both discriminators
/// (<c>count</c>, <c>calculated</c>) already exist.
/// </para>
/// </summary>
[StaticRule(Priority = 976)]
public sealed class SelfPTForEachAuraAttachedRule : IStaticRule
{
  // "This creature gets +N/+M for each Aura attached to it."
  // The filter phrase after "for each" is captured verbatim into CountOf.
  private static readonly Regex _pattern = new(
    @"^\s*This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>Aura\s+attached\s+to\s+it)\.?\s*$",
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

    var filterPhrase = match.Groups["filter"].Value.Trim();

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = BuildModifier(power, filterPhrase),
          ToughnessModifier = BuildModifier(toughness, filterPhrase),
        }],
      },
    ];
  }

  /// <summary>
  /// Builds the per-side dynamic modifier. A zero increment is a literal 0; a
  /// ±1 increment is a bare <see cref="CountQuantity"/> over the filter; a
  /// larger magnitude wraps the count in a "multiply" <see cref="CalculatedQuantity"/>.
  /// </summary>
  private static Quantity BuildModifier(int increment, string filterPhrase)
  {
    if (increment == 0)
    {
      return LiteralQuantity.Of(0);
    }

    var count = new CountQuantity { CountOf = filterPhrase };
    if (Math.Abs(increment) == 1)
    {
      return count;
    }

    var sign = increment < 0 ? "-" : "+";
    return new CalculatedQuantity
    {
      Expression = $"{sign}{Math.Abs(increment)} for each {filterPhrase}",
      BaseQuantity = count,
      Operation = "multiply",
    };
  }
}
