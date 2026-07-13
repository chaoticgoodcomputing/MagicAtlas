namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;

/// <summary>
/// Domain — "This spell costs {N} less to cast for each basic land type among
/// lands you control." The self-cost-reduction sibling of
/// <see cref="CostReductionForEachRule"/> specialised to the <b>domain</b> count
/// (the number of distinct basic land types among the controlled lands, CR
/// 305.6). Two reasons this cannot be folded into the generic rule, mirroring
/// <see cref="DomainSelfPTForEachRule"/>: (a) the count is a specific
/// game-value <see cref="DomainQuantity"/>, not an object filter the generic
/// rule's <c>PerObject</c> knows how to count — "among lands you control"
/// counts distinct land <i>types</i>, not lands, so no <c>ObjectFilter</c>
/// over lands expresses it; (b) the clause still carries its "Domain — "
/// ability-word prefix (the static parser does not pre-strip it), which the
/// generic rule's <c>^This\s+spell\s+costs</c> anchor cannot skip. This rule
/// tolerates the optional italic ability-word prefix (CR 207.2 — the prefix
/// has no game function) and emits the detected word on
/// <see cref="Ability.AbilityWord"/>.
///
/// <para>
/// CR 118.7 (verbatim): "What a player actually needs to do to pay a cost may
/// be changed or reduced by effects. If the mana component of a cost is
/// reduced to nothing by cost reduction effects, it's considered to be {0}.
/// Paying a cost changed or reduced by an effect counts as paying the
/// original cost."
/// </para>
///
/// <para>
/// CR 305.6: "The basic land types are Plains, Island, Swamp, Mountain, and
/// Forest. If an object uses the words 'basic land type,' it's referring to
/// one of these subtypes. …" Domain counts how many of these five distinct
/// basic land types appear among the controlled lands.
/// </para>
/// </summary>
[StaticRule(Priority = 989)]
public sealed class DomainSpellCostReductionRule : IStaticRule
{
  // Optional italic ability-word em-dash prefix ("Domain — ", CR 207.2), then the
  // fixed domain cost-reduction phrase. Em dash is the literal U+2014. Anchored
  // end-to-end so this matches ONLY the domain-count self-cost-reduction and
  // nothing a sibling should own — in particular it never overlaps the generic
  // per-object CostReductionForEachRule, whose known filter-phrase switch does
  // not (and should not) recognise "basic land type among lands you control".
  private static readonly Regex _pattern = new(
    @"^\s*(?:[A-Z][A-Za-z' ]+?\s+—\s+)?This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+for\s+each\s+basic\s+land\s+type\s+among\s+lands\s+you\s+control\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        AbilityWord = classification.AbilityWord,
        Effects = [new CostReductionEffect
        {
          Amount = BuildAmountQuantity(amount),
        }],
      },
    ];
  }

  // Total reduction = (per-each increment) × (domain count). Mirrors
  // DomainSelfPTForEachRule.BuildSideModifier: a +1 increment (the printed
  // convention, e.g. Stratadon) reuses the bare DomainQuantity directly since
  // multiplying by 1 is the identity; any other magnitude wraps the domain
  // count in a multiply CalculatedQuantity whose Operand carries the
  // magnitude as structure (no free text).
  private static Quantity BuildAmountQuantity(int increment)
  {
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
