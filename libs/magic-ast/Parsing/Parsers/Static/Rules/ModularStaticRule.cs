namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Modular N" oracle line into the two abilities defined by
/// CR 702.43a (mirroring the <see cref="DashStaticRule"/> /
/// <see cref="ReconfigureStaticRule"/> multi-ability precedent).
///
/// <para>
/// CR 702.43a (verbatim): "Modular represents both a static ability and a
/// triggered ability. 'Modular N' means 'This permanent enters with N +1/+1
/// counters on it' and 'When this permanent is put into a graveyard from the
/// battlefield, you may put a +1/+1 counter on target artifact creature for each
/// +1/+1 counter on this permanent.'"
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so
/// the two-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in
/// <see cref="MagicAST.Keywords.Definitions.ModularKeyword"/> remains live as a
/// fallback, emitting only the PRIMARY enters-with-counters static ability (the
/// keyword expander returns a single <c>Ability</c>; Modular decomposes into two).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class ModularStaticRule : IStaticRule
{
  // Matches: "Modular {N}" (N a bare integer) with optional trailing reminder text.
  private static readonly Regex _pattern = new(
    @"^\s*Modular\s+(?<value>\d+)\s*(?<reminder>\(.*\))?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private const string PlusOnePlusOne = "+1/+1";

  /// <inheritdoc/>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!int.TryParse(match.Groups["value"].Value, out var n))
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.43a, first clause): the enters-with-counters static ability.
    // "This permanent enters with N +1/+1 counters on it." A self-replacement effect
    // that applies as the permanent enters (CR 603.6d / 614.1c) — timing lives on the
    // StaticAbility.When qualifier, never baked into the effect discriminator. Unlike
    // Bloodthirst this entry is UNCONDITIONAL, so there is no Condition. The reminder
    // text rides on this primary ability (matching the combinator path).
    var entersWithCountersAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Modular,
      When = StaticTimingKind.AsThisEnters,
      Reminder = reminder,
      Effects =
      [
        new PutCountersEffect
        {
          Target = ObjectReference.Self(),
          CounterType = PlusOnePlusOne,
          Count = LiteralQuantity.Of(n),
        },
      ],
    };

    // Ability 2 (CR 702.43a, second clause): the dies move-counters triggered ability.
    // "When this permanent is put into a graveyard from the battlefield, you may put a
    // +1/+1 counter on target artifact creature for each +1/+1 counter on this
    // permanent." "you may" -> optional (EffectWrap.Optional). The count "for each
    // +1/+1 counter on this permanent" is a CounterCountQuantity over the source's own
    // +1/+1 counters. Target = target artifact creature.
    var diesMoveCountersAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Modular,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.Dies,
        // CR 109 self-binding (§6): "this permanent" — the source's own death, so a cross-card
        // sacrifice does not subsume it (operator returns No → bridge falls to Amber, not GREEN).
        Filter = new ObjectFilter { CardTypes = ["permanent"], IsSelf = true },
      },
      Effects =
      [
        EffectWrap.Optional(
          new PutCountersEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Target,
              Filter = new ObjectFilter { CardTypes = ["artifact", "creature"] },
            },
            CounterType = PlusOnePlusOne,
            Count = new CounterCountQuantity
            {
              CounterType = PlusOnePlusOne,
              On = ObjectReference.Self(),
            },
          },
          isOptional: true
        ),
      ],
    };

    return [entersWithCountersAbility, diesMoveCountersAbility];
  }
}
