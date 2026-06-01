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
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Blitz [cost]" oracle line into the three abilities defined by
/// CR 702.152a (the sacrifice-variant sibling of <see cref="DashStaticRule"/>;
/// mirroring the <see cref="ReconfigureStaticRule"/> multi-ability precedent).
///
/// <para>
/// CR 702.152a (verbatim): "Blitz represents three abilities: two static abilities
/// that function while the card with blitz is on the stack, one of which may create
/// a delayed triggered ability, and a static ability that functions while the object
/// with blitz is on the battlefield. 'Blitz [cost]' means 'You may cast this card by
/// paying [cost] rather than its mana cost,' 'If this spell's blitz cost was paid,
/// sacrifice the permanent this spell becomes at the beginning of the next end step,'
/// and 'As long as this permanent's blitz cost was paid, it has haste and \"When this
/// permanent is put into a graveyard from the battlefield, draw a card.\"' Casting a
/// spell for its blitz cost follows the rules for paying alternative costs in rules
/// 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// three-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in <see cref="MagicAST.Keywords.Definitions.BlitzKeyword"/>
/// remains live as a fallback, emitting only the primary alternative-cast static
/// ability (the keyword expander returns a single <c>Ability</c>; Blitz decomposes
/// into three).
/// </para>
///
/// <para>
/// Differs from Dash only in the second and third abilities: Blitz's end-step delayed
/// trigger <em>sacrifices</em> the permanent (rather than returning it to hand), and its
/// conditional battlefield static grants TWO abilities — haste plus a "put into a
/// graveyard from the battlefield, draw a card" triggered ability ("put into a graveyard
/// from the battlefield" is the <see cref="TriggerEvent.Dies"/> event, CR 700.4).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class BlitzStaticRule : IStaticRule
{
  // Matches: "Blitz {cost}" with optional trailing reminder text.
  // The cost group captures one or more mana symbols, e.g. "{1}{B}".
  private static readonly Regex _pattern = new(
    @"^\s*Blitz\s+(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
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

    // Ability 1 (CR 702.152a, first clause): the alternative-cast static ability.
    // "You may cast this card by paying [cost] rather than its mana cost." The
    // reminder text rides on the primary ability (matching the combinator path).
    // Identical to Dash's first ability.
    var altCastAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Blitz,
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

    // Ability 2 (CR 702.152a, second clause): the end-step delayed sacrifice, gated on
    // the blitz cost having been paid. "If this spell's blitz cost was paid, sacrifice
    // the permanent this spell becomes at the beginning of the next end step." Modeled
    // as a conditioned static ability whose effect creates a delayed triggered ability
    // (CR 603.7) — timing lives on the delayed trigger's clock point, never baked into
    // the sacrifice effect. Differs from Dash (which RETURNS to hand): Blitz SACRIFICES.
    var delayedSacrificeAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Blitz,
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Blitz },
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
              new SacrificeEffect { Target = ObjectReference.Self() },
            ],
          },
        },
      ],
    };

    // Ability 3 (CR 702.152a, third clause): the conditional static ability that
    // functions on the battlefield. "As long as this permanent's blitz cost was paid,
    // it has haste and \"When this permanent is put into a graveyard from the
    // battlefield, draw a card.\"" Differs from Dash (which grants only haste): Blitz
    // grants TWO abilities. Modeled as two GainAbilityEffects, each gated by the same
    // AsLongAsDuration whose condition is the blitz-cost-paid reference (mirroring
    // AsLongAsStaticGrantRule / Dash's single-grant shape). "Put into a graveyard from
    // the battlefield" is the established Dies trigger event (CR 700.4).
    var blitzPaidDuration = new AsLongAsDuration
    {
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Blitz },
    };

    var conditionalGrantAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Blitz,
      Effects =
      [
        new GainAbilityEffect
        {
          Target = ObjectReference.Self(),
          GainedAbility = StaticRuleHelpers.MapKeywordToStaticAbility("haste")!,
          Duration = blitzPaidDuration,
        },
        new GainAbilityEffect
        {
          Target = ObjectReference.Self(),
          GainedAbility = new TriggeredAbility
          {
            Trigger = new TriggerCondition
            {
              Timing = TriggerTiming.When,
              Event = TriggerEvent.Dies,
              Filter = new ObjectFilter { CardTypes = ["creature"] },
            },
            Effects =
            [
              new DrawCardsEffect
              {
                Count = LiteralQuantity.Of(1),
                Player = ObjectReference.You(),
              },
            ],
          },
          Duration = blitzPaidDuration,
        },
      ],
    };

    return [altCastAbility, delayedSacrificeAbility, conditionalGrantAbility];
  }
}
