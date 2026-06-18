namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing;

/// <summary>
/// Decomposes a "Casualty N" oracle line into the two abilities defined by
/// CR 702.153a (mirroring the <see cref="ReplicateStaticRule"/> /
/// <see cref="SquadStaticRule"/> multi-ability precedent).
///
/// <para>
/// CR 702.153a (verbatim): "Casualty is a keyword that represents two abilities.
/// The first is a static ability that functions while the spell with casualty is on
/// the stack. The second is a triggered ability that functions while the spell with
/// casualty is on the stack. Casualty N means 'As an additional cost to cast this
/// spell, you may sacrifice a creature with power N or greater,' and 'When you cast
/// this spell, if a casualty cost was paid for it, copy it. If the spell has any
/// targets, you may choose new targets for the copy.' Paying a spell's casualty cost
/// follows the rules for paying additional costs in rules 601.2b and 601.2f-h."
/// </para>
///
/// <para>
/// Priority 1001 — fires before <see cref="KeywordListRule"/> (priority 1000) so the
/// two-ability decomposition takes precedence over the single-ability keyword
/// combinator path. The combinator in
/// <see cref="MagicAST.Keywords.Definitions.CasualtyKeyword"/> remains live as a
/// fallback, emitting only the primary additional-cost static ability (the keyword
/// expander returns a single <see cref="Ability"/>; Casualty decomposes into two).
/// </para>
///
/// <para>
/// "you may choose new targets for the copy" is recorded on the <see cref="CopyEffect"/>
/// via <see cref="CopyEffect.MayChooseNewTargets"/> = <c>true</c> per the CR. The
/// decision to retarget is engine territory, but the PERMISSION is oracle-stated and
/// descriptively meaningful — not omitted (contrast Replicate where the same clause is
/// engine-territory only per its rule text).
/// </para>
/// </summary>
[StaticRule(Priority = 1001)]
public sealed class CasualtyStaticRule : IStaticRule
{
  // Matches: "Casualty N" (integer) with optional trailing reminder text.
  // Anchored (^ … $) to prevent substring collision with other clauses.
  private static readonly Regex _pattern = new(
    @"^\s*Casualty\s+(?<n>\d+)\s*(?<reminder>\(.*\))?\s*$",
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

    if (!int.TryParse(match.Groups["n"].Value, out var power))
    {
      return null;
    }

    Parenthetical? reminder = null;
    var reminderGroup = match.Groups["reminder"];
    if (reminderGroup.Success && reminderGroup.Value.Length > 0)
    {
      reminder = new Parenthetical { Text = reminderGroup.Value };
    }

    // Ability 1 (CR 702.153a, first clause): the optional sacrifice additional-cost static ability.
    // "As an additional cost to cast this spell, you may sacrifice a creature with power N or greater."
    // The reminder text rides on the primary ability (matching the combinator path). The
    // synthesized cost omits SourceSpan — identity rides on KeywordSource.
    var costAbility = new StaticAbility
    {
      KeywordSource = KeywordAbility.Casualty,
      Reminder = reminder,
      Effects =
      [
        new AdditionalCastCostEffect
        {
          AdditionalCost = new AdditionalCost
          {
            Cost = new SacrificeCost
            {
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                PowerComparison = new Comparison
                {
                  Operator = ComparisonOperator.GreaterThanOrEqual,
                  Value = power,
                },
              },
              Quantity = LiteralQuantity.Of(1),
            },
            IsOptional = true,
            Repeatable = false,
          },
        },
      ],
    };

    // Ability 2 (CR 702.153a, second clause): the cast-copy triggered ability, gated on the
    // casualty cost having been paid. "When you cast this spell, if a casualty cost was paid
    // for it, copy it. If the spell has any targets, you may choose new targets for the copy."
    // MayChooseNewTargets = true records the oracle-stated permission (CR 702.153a).
    var copyAbility = new TriggeredAbility
    {
      KeywordSource = KeywordAbility.Casualty,
      Trigger = new TriggerCondition
      {
        Timing = TriggerTiming.When,
        Event = TriggerEvent.SpellCast,
      },
      InterveningIf = new KeywordCostPaidCondition { Keyword = KeywordAbility.Casualty },
      Effects =
      [
        new CopyEffect
        {
          Target = ObjectReference.Self(),
          MayChooseNewTargets = true,
        },
      ],
    };

    return [costAbility, copyAbility];
  }
}
