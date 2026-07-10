namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Spells you cast cost {N} less to cast." (Stone Calendar) — the flat,
/// UNqualified cost-reduction base case: every spell the controller casts,
/// no colour/type/zone/history qualifier.
///
/// <para>
/// CR 118.7: "What a player actually needs to do to pay a cost may be
/// changed or reduced by effects." CR 118.7a: "Effects that reduce a cost by
/// an amount of generic mana affect only the generic mana component of that
/// cost."
/// </para>
///
/// <para>
/// Sibling of <see cref="TypeSpellCostReductionRule"/> (which requires a
/// leading capitalised type/colour noun before "spells you cast") — this rule
/// is the base case with NO qualifying noun at all, so the affected-objects
/// filter is the bare <c>CardTypes: ["spell"], Controller: You</c> shape with
/// no further narrowing axis set. Anchored (^…$) so it only matches the
/// literal, unqualified "Spells you cast cost {N} less to cast." sentence and
/// falls through (returns null) for every qualified variant (those are each
/// their own sibling rule's job) — this keeps the base case from ever
/// shadowing a more specific sentence.
/// </para>
/// </summary>
[StaticRule(Priority = 981)]
public sealed class UnrestrictedSpellCostReductionRule : IStaticRule
{
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
        Effects =
        [
          new MagicAST.AST.Effects.Resource.CostReductionEffect
          {
            Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          },
        ],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.You,
        },
      },
    ];
  }

  // "Spells you cast cost {1} less to cast." — anchored ^…$, no leading
  // qualifying noun (that's every sibling rule's job) so this only claims
  // the fully-unqualified sentence.
  private static readonly Regex _pattern = new(
    @"^\s*Spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );
}
