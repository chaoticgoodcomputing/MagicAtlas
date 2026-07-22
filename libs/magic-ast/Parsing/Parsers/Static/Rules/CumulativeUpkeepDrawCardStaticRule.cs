namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Cumulative upkeep—Draw a card." (Psychic Vortex) — the draw-cost-templated
/// printing of cumulative upkeep, decomposed into its full triggered ability per
/// CR 702.24a exactly like the life-cost sibling
/// <see cref="CumulativeUpkeepPayLifeStaticRule"/>, but with the per-age-counter
/// cost being a card draw (<see cref="DrawCardsCost"/>) rather than
/// <see cref="PayLifeCost"/>.
///
/// <para>
/// CR 702.24a (verbatim): "Cumulative upkeep is a triggered ability that imposes an
/// increasing cost on a permanent. 'Cumulative upkeep [cost]' means 'At the
/// beginning of your upkeep, if this permanent is on the battlefield, put an age
/// counter on this permanent. Then you may pay [cost] for each age counter on it.
/// If you don't, sacrifice it.' If [cost] has choices associated with it, each
/// choice is made separately for each age counter, then either the entire set of
/// costs is paid, or none of them is paid. Partial payments aren't allowed."
/// </para>
///
/// <para>
/// A dedicated sibling <c>[StaticRule]</c> — mirroring the split
/// <see cref="CumulativeUpkeepPayLifeStaticRule"/> already documents (also the
/// precedent of <c>DashStaticRule</c>/<c>DashKeyword</c>) — rather than widening
/// the life-cost rule's regex: the em-dash "—Draw N card(s)." cost shape is a
/// distinct printed template from both the mana-symbol shape ("Cumulative upkeep
/// {G}") and the life-cost shape ("—Pay N life."), so this rule adds coverage for
/// the draw-cost template while leaving the existing life-cost rule (and its gold
/// fixture, Inner Sanctum) completely untouched. ANCHORED (^…$) on the full oracle
/// line (cost text plus optional trailing reminder) so it cannot match as a
/// substring of a broader clause.
/// </para>
/// </summary>
[StaticRule(Priority = 60)]
public sealed class CumulativeUpkeepDrawCardStaticRule : IStaticRule
{
  private static readonly GameTime UpkeepTime = new()
  {
    Part = TurnPart.Upkeep,
    Edge = TimeBoundary.Beginning,
    Whose = ControllerFilter.You,
  };

  // Matches: "Cumulative upkeep—Draw a card." / "Cumulative upkeep—Draw N cards."
  // with optional trailing reminder text.
  private static readonly Regex _pattern = new(
    @"^\s*Cumulative\s+upkeep\s*—\s*Draw\s+(?<amount>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\.?\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!StaticRuleHelpers.TryParseSmallCount(match.Groups["amount"].Value, out var drawAmount))
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    return
    [
      new TriggeredAbility
      {
        KeywordSource = KeywordAbility.CumulativeUpkeep,
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.At,
          Event = new TimeOccurrence { Time = UpkeepTime },
        },
        InterveningIf = new ObjectInZoneCondition { Reference = ObjectReference.Self(), Zone = Zone.Battlefield },
        Effects =
        [
          new PutCountersEffect
          {
            Target = ObjectReference.Self(),
            CounterType = "age",
            Count = LiteralQuantity.Of(1),
          },
          new PreventableEffect
          {
            Inner = new SacrificeEffect { Target = ObjectReference.Self() },
            Unless = new UnlessClause
            {
              Player = new ObjectReference { Kind = ObjectReferenceKind.Controller },
              Cost = new ScaledCost
              {
                PerUnit = new DrawCardsCost { Quantity = LiteralQuantity.Of(drawAmount) },
                Count = new CounterCountQuantity
                {
                  CounterType = "age",
                  On = ObjectReference.Self(),
                },
              },
            },
          },
        ],
        Reminder = reminder,
      },
    ];
  }
}
