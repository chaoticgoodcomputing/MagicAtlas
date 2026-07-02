namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Domain — "This creature gets +N/+M for each basic land type among lands you
/// control." The self-P/T sibling of <see cref="SelfPTForEachRule"/> specialised
/// to the <b>domain</b> count (the number of distinct basic land types among the
/// controlled lands, CR 305.6). Two reasons this cannot be folded into the generic
/// rule: (a) the count is a specific game-value <see cref="DomainQuantity"/>, not an
/// object/counter/derived count the generic rule's <c>BuildCountQuantity</c> knows;
/// (b) the clause still carries its "Domain — " ability-word prefix (the static
/// parser does not pre-strip it), which the generic rule's <c>^This creature gets</c>
/// anchor cannot skip. This rule tolerates the optional italic ability-word prefix
/// (CR 207.2 — the prefix has no game function) and emits the detected word on
/// <see cref="Ability.AbilityWord"/>.
///
/// <para>
/// CR 305.6: "The basic land types are Plains, Island, Swamp, Mountain, and Forest.
/// If an object uses the words 'basic land type,' it's referring to one of these
/// subtypes. ..." The effect is a layer-7 P/T-modifying continuous effect
/// (CR 613.4) — a +N/+0-for-each, not a set-P/T.
/// </para>
/// </summary>
[StaticRule(Priority = 976)]
public sealed class DomainSelfPTForEachRule : IStaticRule
{
  // Optional italic ability-word em-dash prefix ("Domain — ", CR 207.2), then the
  // fixed domain phrase. Em dash is the literal U+2014. Anchored end-to-end so this
  // matches ONLY the domain-count self-buff and nothing a sibling should own.
  private static readonly Regex _pattern = new(
    @"^\s*(?:[A-Z][A-Za-z' ]+?\s+—\s+)?This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+basic\s+land\s+type\s+among\s+lands\s+you\s+control\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;

    var powerModifier = BuildSideModifier(power);
    var toughnessModifier = BuildSideModifier(toughness);

    return
    [
      new StaticAbility
      {
        AbilityWord = classification.AbilityWord,
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = powerModifier,
          ToughnessModifier = toughnessModifier,
        }],
      },
    ];
  }

  // Per-side dynamic modifier = (per-each increment) × (domain count). Mirrors
  // SelfPTForEachRule.BuildSideModifier: a zero increment is a literal 0; a +1
  // increment reuses the bare DomainQuantity (single-increment convention); any
  // other magnitude wraps the domain count in a multiply CalculatedQuantity whose
  // signed Operand carries both magnitude and sign as structure (no free text).
  private static Quantity BuildSideModifier(int increment)
  {
    if (increment == 0)
    {
      return LiteralQuantity.Of(0);
    }

    if (increment == 1)
    {
      return new DomainQuantity();
    }

    return new CalculatedQuantity
    {
      BaseQuantity = new DomainQuantity(),
      Operation = "multiply",
      Operand = increment,
    };
  }
}
