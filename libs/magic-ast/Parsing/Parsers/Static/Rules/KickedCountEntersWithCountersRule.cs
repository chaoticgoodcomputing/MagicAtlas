namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "This [type] enters with a [counterType] counter on it for each time it was kicked."
/// (Skitter of Lizards). The kicked-count-scaled sibling of
/// <see cref="EntersWithCountersRule"/> (a literal/X count): the number of counters is the
/// number of times the spell was kicked, not the article "a".
///
/// <para>
/// The count is a <see cref="KeywordCostPaidCountQuantity"/> keyed on
/// <see cref="KeywordAbility.Kicker"/> — a multikicker cost is a kicker cost, so
/// "for each time it was kicked" references Kicker. Reference-not-resolution (ADR 0004):
/// the count names the producing keyword's identity, NOT a variable threaded from the
/// Multikicker <c>AdditionalCastCostEffect</c> producer on the same card (mirrors
/// CreateTokenRule's Wolfbriar handling of the same "for each time it was kicked" phrase).
/// </para>
///
/// <para>
/// Rules: 614.1c (enters-with replacement is a static ability applied as the permanent
/// enters), 702.33d ("If a spell's controller declares the intention to pay any of that
/// spell's kicker costs, that spell has been 'kicked.' If a spell has two kicker costs or
/// has multikicker, it may be kicked multiple times.").
/// </para>
/// </summary>
[StaticRule(Priority = 953)]
public sealed class KickedCountEntersWithCountersRule : IStaticRule
{
  // Subject prefix captured liberally as "any non-empty leading words before 'enters
  // with'" (consistent with EntersWithCountersRule's named/"This [type]" self-reference,
  // collapsed to Self). The count phrase is the singular article "a"/"an" (one counter
  // per kick); counter type is "+1/+1", "-1/-1", or any named counter. The trailing
  // "for each time it/this [type] was kicked" suffix re-keys the count onto Kicker.
  private static readonly Regex _kickedCountEntersWithCountersPattern = new(
    @"^\s*\S.+?\s+enters\s+with\s+an?\s+(?<counterType>[\w/+-]+)\s+counters?\s+on\s+it\s+for\s+each\s+time\s+(?:it|this\s+\w+)\s+was\s+kicked\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _kickedCountEntersWithCountersPattern.Match(
      StaticRuleHelpers.StripReminderText(clause.RawText)
    );
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
          Count = new KeywordCostPaidCountQuantity { Keyword = KeywordAbility.Kicker },
          CounterType = counterType,
        }],
      },
    ];
  }
}
