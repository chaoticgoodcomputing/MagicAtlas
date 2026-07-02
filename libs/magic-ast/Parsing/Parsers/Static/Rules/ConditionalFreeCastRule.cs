namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Parses "If an opponent controls a Forest and you control a Swamp, you may cast
/// this spell without paying its mana cost." — Deepwood Legate's conditional
/// free-cast permission. Per CR 118.9, "you may cast [this object] without paying
/// its mana cost" IS an alternative cost (the no-cost alternative to the spell's
/// mana cost); the leading "If X and Y" is the board-state gate on when that
/// alternative is available.
///
/// <para>
/// CR 118.9 (verbatim): "Some spells have alternative costs. An alternative cost is
/// a cost listed in a spell's text, or applied to it from another effect, that its
/// controller may pay rather than paying the spell's mana cost. Alternative costs are
/// usually phrased, 'You may [action] rather than pay [this object's] mana cost,' or
/// 'You may cast [this object] without paying its mana cost.' Note that some
/// alternative costs are listed in keywords; see rule 702."
/// </para>
///
/// <para>
/// CR 118.9b (verbatim): "Alternative costs are generally optional. An effect that
/// allows you to cast a spell may require a certain alternative cost to be paid." —
/// grounds the optional "you MAY cast".
/// CR 118.9c (verbatim): "An alternative cost doesn't change a spell's mana cost,
/// only what its controller has to pay to cast it. Spells and abilities that ask for
/// that spell's mana cost still see the original value."
/// CR 601.3 (verbatim): "A player can begin to cast a spell only if a rule or effect
/// allows that player to cast it and no rule or effect prohibits that player from
/// casting it." — the permission-to-cast rule this free-cast permission grants
/// against.
/// </para>
///
/// <para>
/// The two board predicates are joined by "and", so they must both hold: the gate is
/// modelled as an <see cref="AllCondition"/> (the honest AND) carried on
/// <see cref="StaticAbility.Condition"/> — a continuous casting-permission gate —
/// rather than a <see cref="MagicAST.AST.Effects.ConditionalEffect"/> wrapper. The
/// permission itself reuses <see cref="CastWithoutPayingEffect"/> verbatim (its doc
/// string IS this oracle phrase), targeting the source spell (<c>Self</c>).
/// </para>
///
/// <para>
/// Forest/Swamp are LAND SUBTYPES (CR 205.3i), so they go on
/// <see cref="ObjectFilter.Subtypes"/> — mirroring
/// <c>ConditionParser.NounToFilter</c>, which routes non-card-type nouns to Subtypes;
/// no <c>CardTypes:["land"]</c> is added. The "a"/"an" quantifier is a "one or more"
/// existence check (mirroring <c>ConditionParser.Quant</c>): GreaterThanOrEqual 1.
/// The CountConditions are built inline here rather than via ConditionParser, which
/// handles neither "opponent controls" nor "and"-joined compounds.
/// </para>
///
/// <para>
/// The surface phrase is fully anchored (^…$): Deepwood Legate's free-cast line is a
/// single standalone ability line, and no sibling trigger or effect shares this exact
/// opening, so the anchoring rules out substring collisions. Priority 90 (high-
/// specificity band): the anchoring makes the exact value non-load-bearing.
/// </para>
/// </summary>
[StaticRule(Priority = 90)]
public sealed class ConditionalFreeCastRule : IStaticRule
{
  // Matches Deepwood Legate's shape:
  //   "If an opponent controls a Forest and you control a Swamp, you may cast this
  //    spell without paying its mana cost."
  // <opp> is the land subtype the opponent controls, <you> the one you control.
  // Anchored ^ and $ to prevent substring matches against any sibling clause.
  private static readonly Regex _pattern = new(
    @"^\s*If\s+an\s+opponent\s+controls\s+an?\s+(?<opp>[A-Za-z]+)\s+and\s+you\s+control\s+an?\s+(?<you>[A-Za-z]+),\s*you\s+may\s+cast\s+this\s+spell\s+without\s+paying\s+its\s+mana\s+cost\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var opponentSubtype = match.Groups["opp"].Value;
    var yourSubtype = match.Groups["you"].Value;

    // "a [subtype]" → one or more of that permanent exist (existence gate).
    var atLeastOne = new Comparison
    {
      Operator = ComparisonOperator.GreaterThanOrEqual,
      Value = 1,
    };

    var condition = new AllCondition
    {
      Conditions =
      [
        // "an opponent controls a Forest"
        new CountCondition
        {
          Filter = new ObjectFilter
          {
            Subtypes = [opponentSubtype],
            Controller = ControllerFilter.Opponent,
          },
          Count = atLeastOne,
        },
        // "you control a Swamp"
        new CountCondition
        {
          Filter = new ObjectFilter
          {
            Subtypes = [yourSubtype],
            Controller = ControllerFilter.You,
          },
          Count = atLeastOne,
        },
      ],
    };

    return
    [
      new StaticAbility
      {
        Condition = condition,
        Effects =
        [
          new CastWithoutPayingEffect { Target = ObjectReference.Self() },
        ],
      },
    ];
  }
}
