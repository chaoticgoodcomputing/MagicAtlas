namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "[Name] enters with a number of +1/+1 counters on it equal to the amount of
/// mana spent to cast it." — the VARIABLE-count sibling of
/// <see cref="EntersWithCountersRule"/> (Gyrus, Waker of Corpses: "Gyrus enters
/// with a number of +1/+1 counters on it equal to the amount of mana spent to
/// cast it."). <see cref="EntersWithCountersRule"/> only recognises a literal/word
/// count or the bare variable "X"; here the count is instead a DERIVED quantity —
/// "the amount of mana spent to cast it" — so it needs its own matcher rather than
/// an extra alternative on that rule's count group.
///
/// <para>
/// CR 614.12 (verbatim, head): "Some replacement effects modify how a permanent
/// enters the battlefield. (See rules 614.1c-d.) Such effects may come from the
/// permanent itself if they affect only that permanent (as opposed to a general
/// subset of permanents that includes it). They may also come from other sources."
/// Gyrus's replacement affects only itself (the self-only <c>AsThisEnters</c>
/// family, matching <see cref="EntersWithCountersRule"/>'s own citation), so
/// <see cref="StaticTimingKind.AsThisEnters"/> is used, not the general-subset
/// <c>AsObjectEnters</c> shape (<see cref="OtherSubtypeEntersWithCounterRule"/>).
/// </para>
///
/// <para>
/// "the amount of mana spent to cast it" is modelled as
/// <see cref="ManaSpentToCastQuantity"/> — the numeric-total sibling of
/// <see cref="MagicAST.AST.Abilities.ManaSpentToCastCondition"/> (which reads a
/// boolean "was this color spent"). Reference-not-resolution (ADR 0004): MAST
/// records the reference to the casting event's total mana paid; the engine reads
/// the actual amount at resolution, matching the existing convention for
/// mana-spent facts (CR 601.2f/601.2h — the total cost is determined and paid
/// once, fixing "the amount of mana spent" as a historical fact of the cast).
/// </para>
///
/// <para>
/// The counter placement itself reuses <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/>
/// exactly as <see cref="EntersWithCountersRule"/> does, with <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect.Count"/>
/// set to the derived quantity instead of a <see cref="LiteralQuantity"/>/<see cref="VariableQuantity"/>.
/// The timing wrapper (<see cref="StaticTimingKind.AsThisEnters"/>) and the effect
/// (<c>putCounters</c>) stay the same composable nodes as the fixed-count sibling —
/// only the <em>count</em> shape differs.
/// </para>
///
/// <para>
/// ANCHORED (^…$) and specific to the "+1/+1 counters ... equal to the amount of
/// mana spent to cast it" surface, so it cannot collide with
/// <see cref="EntersWithCountersRule"/>'s literal/word/X counts or with any other
/// "enters with counters equal to ..." derived-count sibling (e.g. a hypothetical
/// "equal to its power" card, which would need its own <c>DerivedQuantity</c> rule).
/// Priority 950 matches <see cref="EntersWithCountersRule"/>'s specificity tier.
/// </para>
/// </summary>
[StaticRule(Priority = 950)]
public sealed class EntersWithCountersEqualToManaSpentRule : IStaticRule
{
  // "[Name] enters with a number of +1/+1 counters on it equal to the amount of
  // mana spent to cast it." — the subject prefix is captured liberally (any
  // non-empty leading words before "enters with"), matching the self-reference
  // convention established by EntersWithCountersRule.
  private static readonly Regex _pattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+a\s+number\s+of\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\s+equal\s+to\s+the\s+amount\s+of\s+mana\s+spent\s+to\s+cast\s+it\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(StaticRuleHelpers.StripReminderText(clause.RawText));
    if (!match.Success)
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value;

    return
    [
      new StaticAbility
      {
        When = StaticTimingKind.AsThisEnters,
        Effects = [new MagicAST.AST.Effects.Counter.PutCountersEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          Count = new ManaSpentToCastQuantity(),
          CounterType = counterType,
        }],
      },
    ];
  }
}
