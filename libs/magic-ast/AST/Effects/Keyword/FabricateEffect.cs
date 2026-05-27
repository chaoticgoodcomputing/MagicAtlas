namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Fabricate (Rule 702.118). A triggered keyword ability printed as "Fabricate N".
/// When this creature enters, put N +1/+1 counters on it or create N 1/1 colorless
/// Servo artifact creature tokens.
/// MAST records the keyword and its integer value; the enters trigger, the
/// choice between counters and tokens, and token creation are engine territory.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect and BackupEffect
/// shape — <see cref="Value"/> is the fabricate number lifted from the printed
/// oracle text.
/// </para>
/// </summary>
[OracleEffect("fabricate")]
public sealed record FabricateEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The fabricate value N printed on the card (e.g., "Fabricate 1" → 1).</summary>
  public required int Value { get; init; }

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
