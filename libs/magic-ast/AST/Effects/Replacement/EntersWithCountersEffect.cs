namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This creature enters with N +1/+1 counters on it." (Rule 614.1c — a
/// self-replacement effect modifying how the permanent enters the
/// battlefield.) MAST records the oracle-level declarative shape; the
/// replacement-effect machinery (Rule 614 layering, the implicit
/// "instead of entering with no counters" replacement) is engine territory.
/// </summary>
/// <remarks>
/// Distinct from <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/>:
/// PutCounters models an explicit "put N counters on X" action that resolves
/// from the stack or from a triggered ability. EntersWithCounters is the
/// static replacement-effect form printed on a creature describing the
/// state in which it arrives on the battlefield.
///
/// <para>
/// <see cref="Count"/> uses the polymorphic <c>Quantity</c> type so both
/// literal printings ("with three +1/+1 counters") and variable printings
/// ("with X +1/+1 counters", "with a +1/+1 counter for each color of mana
/// spent to cast it") share one node. <see cref="CounterType"/> is the
/// counter kind as printed (e.g. "+1/+1", "loyalty", "charge").
/// </para>
/// </remarks>
[OracleEffect("entersWithCounters")]
public sealed record EntersWithCountersEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>How many counters the permanent enters with.</summary>
  public required Quantity Count { get; init; }

  /// <summary>The counter kind as printed (e.g. "+1/+1", "loyalty", "charge").</summary>
  public required string CounterType { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
