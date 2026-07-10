namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes an "Impending N—[cost]" oracle line into the four abilities defined by
/// CR 702.176a (mirroring the <see cref="DashStaticRule"/> / <see cref="BuybackStaticRule"/>
/// multi-ability precedent).
///
/// <para>
/// CR 702.176a (verbatim): "Impending is a keyword that represents four abilities. The
/// first is a static ability that functions while the spell with impending is on the
/// stack. The second is static ability that creates a replacement effect that may apply
/// to the permanent with impending as it enters the battlefield from the stack. The third
/// is a static ability that functions on the battlefield. The fourth is a triggered
/// ability that functions on the battlefield. \"Impending N-[cost]\" means \"You may
/// choose to pay [cost] rather than pay this spell's mana cost,\" \"If you chose to pay
/// this permanent's impending cost, it enters with N time counters on it,\" \"As long as
/// this permanent's impending cost was paid and it has a time counter on it, it's not a
/// creature,\" and \"At the beginning of your end step, if this permanent's impending cost
/// was paid and it has a time counter on it, remove a time counter from it.\" Casting a
/// spell for its impending cost follows the rules for paying alternative costs in rules
/// 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// four-ability decomposition takes precedence over the single-ability keyword combinator
/// path. The combinator in <see cref="MagicAST.Keywords.Definitions.ImpendingKeyword"/>
/// remains live as a fallback, emitting only the primary alternative-cast static ability
/// (the keyword expander returns a single <c>Ability</c>; Impending decomposes into four).
/// </para>
///
/// <para>
/// The four abilities are modelled by composing existing primitives — no timing is baked
/// into an effect discriminator:
/// <list type="number">
///   <item>Ability 1: <see cref="AlternativeCastEffect"/> (cast from hand at the impending
///     cost). The interaction-relevant half — a cast permission a flow arm can read.</item>
///   <item>Ability 2: an <c>AsThisEnters</c> replacement (CR 614.1c) — a
///     <see cref="PutCountersEffect"/> placing N time counters, gated on the impending
///     cost having been paid (<see cref="KeywordCostPaidCondition"/>).</item>
///   <item>Ability 3: a <see cref="LoseTypeEffect"/> removing the "creature" card type,
///     for as long as (<see cref="AsLongAsDuration"/>) the impending cost was paid AND the
///     permanent has a time counter (<see cref="AllCondition"/> of
///     <see cref="KeywordCostPaidCondition"/> + <see cref="ObjectHasCounterCondition"/>).</item>
///   <item>Ability 4: an end-step <see cref="TriggeredAbility"/> (CR 603.4 intervening-if)
///     removing one time counter (<see cref="RemoveCountersEffect"/>).</item>
/// </list>
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class ImpendingStaticRule : IStaticRule
{
  // Matches: "Impending N—{cost}" with optional trailing reminder text. The dash may be an
  // em-dash (printed form, e.g. "Impending 5—{1}{B}") or a hyphen (CR form "Impending
  // N-[cost]"). The cost group captures one or more mana symbols.
  private static readonly Regex _pattern = new(
    @"^\s*Impending\s+(?<n>\d+)\s*[—–\-]\s*(?<cost>(?:\{[^}]+\})+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private const string TimeCounter = "time";

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!int.TryParse(match.Groups["n"].Value, out var n) || n <= 0)
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

    // The compound gate shared by abilities 3 and 4 (CR 702.176a): "this permanent's
    // impending cost was paid AND it has a time counter on it." Reference-not-resolution:
    // the keyword-cost-paid reference keys on the same card's Impending cost ability.
    Condition CostPaidAndHasTimeCounter() => new AllCondition
    {
      Conditions =
      [
        new KeywordCostPaidCondition { Keyword = KeywordAbility.Impending },
        new ObjectHasCounterCondition
        {
          Subject = ObjectReference.Self(),
          CounterType = TimeCounter,
        },
      ],
    };

    // Ability 1 (CR 702.176a, first clause): the alternative-cast static ability.
    // "You may choose to pay [cost] rather than pay this spell's mana cost." Cast from hand.
    // The reminder text rides on this primary ability (matching the combinator path).
    var altCastAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Impending,
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

    // Ability 2 (CR 702.176a, second clause): the enters-with-N-time-counters replacement,
    // gated on the impending cost having been paid. "If you chose to pay this permanent's
    // impending cost, it enters with N time counters on it." A self-replacement effect that
    // applies as the permanent enters (CR 614.1c) — When = AsThisEnters carries the timing;
    // the effect names only the action (place N time counters).
    var entersWithCountersAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Impending,
      When = StaticTimingKind.AsThisEnters,
      Condition = new KeywordCostPaidCondition { Keyword = KeywordAbility.Impending },
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          Count = LiteralQuantity.Of(n),
          CounterType = TimeCounter,
        },
      ],
    };

    // Ability 3 (CR 702.176a, third clause): the conditional type-loss static ability that
    // functions on the battlefield. "As long as this permanent's impending cost was paid
    // and it has a time counter on it, it's not a creature." A layer-4 type-change (CR
    // 205.1a): the permanent loses the "creature" card type for as long as the gate holds.
    var notACreatureAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Impending,
      Effects =
      [
        new LoseTypeEffect
        {
          Subject = ObjectReference.Self(),
          LostType = "creature",
          Duration = new AsLongAsDuration { Condition = CostPaidAndHasTimeCounter() },
        },
      ],
    };

    // Ability 4 (CR 702.176a, fourth clause): the end-step triggered ability that functions
    // on the battlefield. "At the beginning of your end step, if this permanent's impending
    // cost was paid and it has a time counter on it, remove a time counter from it." Timing
    // rides on the trigger's clock point (GameTime); the intervening-if (CR 603.4) carries
    // the compound gate.
    var removeCounterAtEndStep = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Impending,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.At,
        Event = new GameTime
        {
          Part = TurnPart.End,
          Edge = TimeBoundary.Beginning,
          Whose = ControllerFilter.You,
        },
      },
      InterveningIf = CostPaidAndHasTimeCounter(),
      Effects =
      [
        new RemoveCountersEffect
        {
          Target = ObjectReference.Self(),
          Count = LiteralQuantity.Of(1),
          CounterType = TimeCounter,
        },
      ],
    };

    return [altCastAbility, entersWithCountersAbility, notACreatureAbility, removeCounterAtEndStep];
  }
}
