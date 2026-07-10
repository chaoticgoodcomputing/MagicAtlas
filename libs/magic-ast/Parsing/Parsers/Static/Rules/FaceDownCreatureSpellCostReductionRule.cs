namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Face-down creature spells you cast cost {N} less to cast." — Obscuring Aether. A
/// cost-reduction static ability (CR 601.2f) scoped to creature spells that are cast
/// face down (CR 708.1: "Some cards allow spells and permanents to be face down.").
///
/// <para>
/// CR 118.7: "What a player actually needs to do to pay a cost may be changed or
/// reduced by effects." CR 118.7a: "Effects that reduce a cost by an amount of
/// generic mana affect only the generic mana component of that cost." (Flagging a
/// citation mismatch with the dispatch brief: the brief cited CR 118.9, which per the
/// local comprehensive-rules corpus covers alternative costs, not cost reduction —
/// this rule instead cites CR 118.7, matching the sibling
/// <see cref="ColoredCreatureSpellCostReductionRule"/>'s convention; see worker
/// report.)
/// </para>
///
/// <para>
/// Sibling of <see cref="ColoredCreatureSpellCostReductionRule"/> (same
/// <c>CardTypes=["spell","creature"]</c> + <c>Controller=You</c> shape) but filtered
/// by <see cref="ObjectFilter.IsFaceDown"/> instead of <see cref="ObjectFilter.Colors"/>.
/// Anchored (^…$) to prevent false-positive substring matches inside longer oracle
/// lines.
/// </para>
/// </summary>
[StaticRule(Priority = 982)]
public sealed class FaceDownCreatureSpellCostReductionRule : IStaticRule
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
          CardTypes = ["spell", "creature"],
          IsFaceDown = true,
          Controller = ControllerFilter.You,
        },
      },
    ];
  }

  // "Face-down creature spells you cast cost {1} less to cast."
  // Anchored ^…$ to prevent substring false-positives (CR 118.7a).
  private static readonly Regex _pattern = new(
    @"^\s*Face-down\s+creature\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );
}
