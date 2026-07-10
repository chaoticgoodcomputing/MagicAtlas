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
/// "Cumulative upkeep—Pay N life." (Inner Sanctum) — the life-cost-templated
/// printing of cumulative upkeep, decomposed into its full triggered ability per
/// CR 702.24a rather than the opaque keyword-effect shape used by the mana-cost
/// combinator in <see cref="MagicAST.Keywords.Definitions.CumulativeUpkeepKeyword"/>.
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
/// Mirrors <see cref="MagicAST.Keywords.Definitions.EchoKeyword"/> (also CR "is a
/// triggered ability", also an upkeep sacrifice-unless-pay tax): a
/// <see cref="TriggeredAbility"/> with an upkeep <see cref="TriggerCondition"/>, an
/// <see cref="OtherCondition"/> intervening-if for "this permanent is on the
/// battlefield", and two ordered <see cref="Effects"/>: putting the age counter
/// (<see cref="PutCountersEffect"/>), then the <see cref="PreventableEffect"/>
/// wrapping a <see cref="SacrificeEffect"/> whose <see cref="UnlessClause"/> cost is
/// a <see cref="ScaledCost"/> — the stated per-age-counter cost
/// (<see cref="PayLifeCost"/>) scaled by a <see cref="CounterCountQuantity"/> of age
/// counters on the permanent (the "for each age counter on it" multiplier).
/// </para>
///
/// <para>
/// A dedicated <c>[StaticRule]</c> rather than an edit to the mana-only
/// <c>CumulativeUpkeepKeyword</c> combinator (mirrors the
/// <c>DashStaticRule</c> / <c>DashKeyword</c> split): the em-dash "—Pay N life."
/// cost shape is a distinct printed template from the mana-symbol shape
/// ("Cumulative upkeep {G}"), so this rule leaves the existing mana-cost keyword
/// path (and its gold fixtures) untouched while adding coverage for the life-cost
/// template. ANCHORED (^…$) on the full oracle line (cost text plus optional
/// trailing reminder) so it cannot match as a substring of a broader clause.
/// </para>
/// </summary>
[StaticRule(Priority = 60)]
public sealed class CumulativeUpkeepPayLifeStaticRule : IStaticRule
{
  private static readonly GameTime UpkeepTime = new()
  {
    Part = TurnPart.Upkeep,
    Edge = TimeBoundary.Beginning,
    Whose = ControllerFilter.You,
  };

  private const string InterveningIfText = "this permanent is on the battlefield";

  // Matches: "Cumulative upkeep—Pay N life." with optional trailing reminder text.
  private static readonly Regex _pattern = new(
    @"^\s*Cumulative\s+upkeep\s*—\s*Pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.?\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!StaticRuleHelpers.TryParseSmallCount(match.Groups["amount"].Value, out var lifeAmount))
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
        InterveningIf = new OtherCondition { Text = InterveningIfText },
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
                PerUnit = new PayLifeCost { Amount = LiteralQuantity.Of(lifeAmount) },
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
