namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Spells you cast from your graveyard cost {N} less to cast." — a static cost
/// reduction (CR 601.2f) scoped to the controller's spells whose zone of origin
/// is their own graveyard (Patrician Geist).
///
/// <para>
/// The affected-objects filter is <c>CardTypes=["spell"], Controller=You,
/// Zone=Graveyard</c>: a spell's current zone is always the stack (CR
/// 111.6/109.5), so on a <c>CardTypes=["spell"]</c> filter <see cref="ObjectFilter.Zone"/>
/// is unambiguous shorthand for the pre-cast origin zone (see that property's
/// doc-comment, and <see cref="ObjectFilter.ExcludedZone"/>'s parallel negative
/// case). This is the POSITIVE counterpart of
/// <see cref="SpellsCastFromNonHandZoneCostReductionRule"/> (Savvy Trader's
/// "from anywhere other than your hand", which negates via <c>ExcludedZone</c>);
/// here the origin zone is named directly rather than excluded. Sibling of
/// <see cref="TypeSpellCostReductionRule"/> (same <see cref="CostReductionEffect"/>
/// + <c>AffectedObjects</c> shape), scoped by cast-zone rather than a type/color
/// adjective.
/// </para>
///
/// Anchored (^...$) to the exact "Spells you cast from your graveyard cost {N}
/// less to cast." surface so it cannot fire as a substring of a
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
public sealed class SpellsCastFromGraveyardCostReductionRule : IStaticRule
{
  private static readonly Regex Pattern = new(
    @"^\s*Spells\s+you\s+cast\s+from\s+your\s+graveyard\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
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
          Zone = Zone.Graveyard,
        },
      },
    ];
  }
}
