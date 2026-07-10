namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Spells you cast from anywhere other than your hand cost {N} less to cast."
/// — a static cost reduction (CR 601.2f) scoped to the controller's spells whose
/// zone of origin is anything BUT hand (Savvy Trader).
///
/// <para>
/// The affected-objects filter is <c>CardTypes=["spell"], Controller=You,
/// ExcludedZone=Hand</c>: a spell's current zone is always the stack (CR
/// 111.6/109.5), so the printed "from anywhere other than your hand" clause
/// names the pre-cast origin zone, not the spell's present location — the
/// negative counterpart of <see cref="ObjectFilter.ExcludedZone"/>'s spell-filter
/// special case (see its doc-comment). Sibling of <see cref="TypeSpellCostReductionRule"/>
/// (same <see cref="CostReductionEffect"/> + <c>AffectedObjects</c> shape), but
/// scoped by cast-zone rather than a type/color adjective.
/// </para>
///
/// Anchored (^...$) to the exact "Spells you cast from anywhere other than your
/// hand cost {N} less to cast." surface so it cannot fire as a substring of a
/// differently-scoped cost-reduction sibling.
///
/// CR 601.2f (verbatim, excerpt): "If the object any part of this cost is paid
/// with is defined ... the total cost is used to determine the legality of the
/// spell's being cast and is the amount of resources that will actually be
/// spent when the spell is cast. ... Some effects instruct a player to add or
/// subtract an amount from the total cost of a spell rather than modify the
/// mana cost..."
/// </summary>
[StaticRule]
public sealed class SpellsCastFromNonHandZoneCostReductionRule : IStaticRule
{
  private static readonly Regex Pattern = new(
    @"^\s*Spells\s+you\s+cast\s+from\s+anywhere\s+other\s+than\s+your\s+hand\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = Pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amount = int.Parse(match.Groups["amount"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new CostReductionEffect { Amount = LiteralQuantity.Of(amount) }],
        AffectedObjects = new ObjectFilter
        {
          CardTypes = ["spell"],
          Controller = ControllerFilter.You,
          ExcludedZone = Zone.Hand,
        },
      },
    ];
  }
}
