namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "As long as this artifact is on the battlefield, it has all activated
/// abilities of all land cards in all graveyards." — the Mirran Safehouse
/// continuous static ability that grants Self every activated ability
/// belonging to any land card currently sitting in any player's graveyard.
///
/// <para>
/// The leading "As long as this artifact is on the battlefield" clause states
/// the default lifespan of any static ability of a permanent (CR 604.2: a
/// permanent's static ability generates a continuous effect that is active
/// only while the source remains on the battlefield with that ability) — it
/// is not modelled as an explicit <c>Duration</c>/<c>Condition</c>, matching
/// the sibling grant rules (<see cref="MarvinHasAllActivatedAbilitiesRule"/>,
/// <see cref="FoodsHaveActivatedAbilitiesOfExiledCreaturesRule"/>) which carry
/// the same implicit battlefield-scoping without a stated clause at all.
/// </para>
///
/// <para>
/// Reuses <see cref="HasAllAbilitiesOfControlledCreaturesEffect"/> — the
/// Marvin, Murderous Mimic node for "[Subject] has all [abilityKind] abilities
/// of [SourceFilter]." That node's <c>SourceFilter</c> is a general
/// <see cref="ObjectFilter"/>, not restricted to controlled creatures, so it
/// faithfully carries this card's "all land cards in all graveyards" source
/// set too (<c>CardTypes=["land"], Zone=Graveyard</c> — no <c>Owner</c>,
/// matching every player's graveyard, per <c>Terravore</c>'s identical "land
/// cards in all graveyards" filter shape).
/// </para>
///
/// <para>
/// <b>Reference, not resolution (ADR 0004):</b> <c>SourceFilter</c> names the
/// class of graveyard cards declaratively; it does not pre-resolve which
/// cards are currently in that set.
/// </para>
///
/// <para>
/// <b>CR 113.3</b> (abilities): "Abilities of an object can affect the game
/// in ways other than by generating game actions." Mirran Safehouse's static
/// ability continuously grants Self the class of activated abilities carried
/// by every land card in every graveyard.
/// <br/>
/// <b>CR 602.1c</b>: "An activated ability is the only kind of ability that
/// can be activated."
/// </para>
///
/// <para>
/// Anchored (^…$) to prevent matching substrings of longer ability lines.
/// Priority 996, matching the sibling ability-acquisition rules — above the
/// generic keyword-grant fallback (967) so this more specific shape is
/// claimed first.
/// </para>
/// </summary>
[StaticRule(Priority = 996)]
public sealed class MirranSafehouseGraveyardLandAbilitiesRule : IStaticRule
{
  // "As long as this artifact is on the battlefield, it has all activated
  //  abilities of all land cards in all graveyards."
  private static readonly Regex Pattern = new(
    @"^\s*As\s+long\s+as\s+this\s+artifact\s+is\s+on\s+the\s+battlefield,\s+it\s+has\s+all\s+activated\s+abilities\s+of\s+all\s+land\s+cards\s+in\s+all\s+graveyards\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!Pattern.IsMatch(clause.RawText))
    {
      return null;
    }

    // Subject: Self — Mirran Safehouse itself (the card bearing this ability).
    var subject = ObjectReference.Self();

    // SourceFilter: "all land cards in all graveyards" — every land card in
    // every player's graveyard. No Owner/Controller axis: "all graveyards"
    // means every graveyard regardless of who owns it, mirroring Terravore's
    // "the number of land cards in all graveyards" filter.
    var sourceFilter = new ObjectFilter
    {
      CardTypes = ["land"],
      Zone = Zone.Graveyard,
    };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new HasAllAbilitiesOfControlledCreaturesEffect
          {
            Subject = subject,
            AbilityKind = "activated",
            SourceFilter = sourceFilter,
          },
        ],
      },
    ];
  }
}
