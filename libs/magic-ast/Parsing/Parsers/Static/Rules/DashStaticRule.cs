namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Dash [cost]" oracle line into the three abilities defined by
/// CR 702.109a (mirroring the <see cref="ReconfigureStaticRule"/> multi-ability
/// precedent).
///
/// <para>
/// CR 702.109a (verbatim): "Dash represents three abilities: two static abilities
/// that function while the card with dash is on the stack, one of which may create
/// a delayed triggered ability, and a static ability that functions while the object
/// with dash is on the battlefield. 'Dash [cost]' means 'You may cast this card by
/// paying [cost] rather than its mana cost,' 'If this spell's dash cost was paid,
/// return the permanent this spell becomes to its owner's hand at the beginning of
/// the next end step,' and 'As long as this permanent's dash cost was paid, it has
/// haste.' Casting a spell for its dash cost follows the rules for paying alternative
/// costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// three-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in <see cref="MagicAST.Keywords.Definitions.DashKeyword"/>
/// remains live as a fallback, emitting only the primary alternative-cast static
/// ability (the keyword expander returns a single <c>Ability</c>; Dash decomposes
/// into three).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class DashStaticRule : IStaticRule
{
  // Matches: "Dash {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{1}{R}".
  private static readonly Regex _pattern = new(
    @"^\s*Dash\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
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

    var costStr = match.Groups["cost"].Value;
    ManaCost cost;
    try
    {
      var parsed = new ManaCostParser().Parse(costStr);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      cost = new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.109a, first clause): the alternative-cast static ability.
    // "You may cast this card by paying [cost] rather than its mana cost." The
    // reminder text rides on the primary ability (matching the combinator path).
    var altCastAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Dash,
      Reminder = reminder,
      Effects =
      [
        new AlternativeCastEffect
        {
          FromZone = Zone.Hand,
          Cost = cost,
        },
      ],
    };

    // Ability 2 (CR 702.109a, second clause): the end-step delayed return, gated on
    // the dash cost having been paid. "If this spell's dash cost was paid, return the
    // permanent this spell becomes to its owner's hand at the beginning of the next
    // end step." Modeled as a conditioned static ability whose effect creates a
    // delayed triggered ability (CR 603.7) — timing lives on the delayed trigger's
    // clock point, never baked into the return effect.
    var delayedReturnAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Dash,
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Dash },
      Effects =
      [
        new CreateDelayedTriggerEffect
        {
          DelayedTrigger = new DelayedTriggeredAbility
          {
            Trigger = new TriggerCondition
            {
              Timing = TriggerTiming.At,
              Event = new GameTime
              {
                Part = TurnPart.End,
                Edge = TimeBoundary.Beginning,
                When = TimeRelation.Next,
              },
            },
            Effects =
            [
              new ReturnToHandEffect { Target = ObjectReference.Self() },
            ],
          },
        },
      ],
    };

    // Ability 3 (CR 702.109a, third clause): the conditional-haste static ability that
    // functions on the battlefield. "As long as this permanent's dash cost was paid, it
    // has haste." Modeled as the established as-long-as keyword grant (mirroring
    // AsLongAsStaticGrantRule): a GainAbilityEffect granting Haste with an
    // AsLongAsDuration whose condition is the dash-cost-paid reference.
    var conditionalHasteAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Dash,
      Effects =
      [
        new GainAbilityEffect
        {
          Target = ObjectReference.Self(),
          GainedAbility = StaticRuleHelpers.MapKeywordToStaticAbility("haste")!,
          Duration = new AsLongAsDuration
          {
            Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Dash },
          },
        },
      ],
    };

    return [altCastAbility, delayedReturnAbility, conditionalHasteAbility];
  }
}
