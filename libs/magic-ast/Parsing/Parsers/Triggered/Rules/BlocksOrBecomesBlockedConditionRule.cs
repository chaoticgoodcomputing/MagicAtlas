namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever this creature blocks or becomes blocked" — combined combat trigger
/// (CR 702.45 Bushido). Fires from the perspective of BOTH the blocking creature
/// (Rule 509 — it declared as a blocker) AND the attacked creature (Rule 509 —
/// a blocker was assigned to it). Modelled as a single
/// <see cref="TriggerEvent.BlocksOrBecomesBlocked"/> event. Tried before the
/// individual Blocks/BecomesBlocked rules (higher priority) so the disjunction
/// is not partially matched.
///
/// <para>
/// CR 702.45 (verbatim): "Bushido is a triggered ability. 'Bushido N' means
/// 'Whenever this creature blocks or becomes blocked, it gets +N/+N until end
/// of turn.'"
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 987)]
public sealed class BlocksOrBecomesBlockedConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("blocks or becomes blocked"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BlocksOrBecomesBlocked,
      Filter = new ObjectFilter { CardTypes = ["creature"] },
    };
  }
}
