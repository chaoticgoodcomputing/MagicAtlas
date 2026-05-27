namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Fading N (Rule 702.32). A keyword ability that limits how long a permanent
/// remains on the battlefield. Oracle form: "Fading N (This creature enters
/// with N fade counters on it. At the beginning of your upkeep, remove a fade
/// counter from it. If you can't, sacrifice it.)".
///
/// <para>
/// MAST records the keyword's presence and its integer value (the number of
/// fade counters the permanent enters with); the counter-removal upkeep trigger
/// and sacrifice-unless-counter mechanics are engine territory.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and the
/// Modular/Backup family. <see cref="Value"/> is the fading number lifted from
/// the printed oracle text.
/// </para>
/// </summary>
[OracleEffect("fading")]
public sealed record FadingEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The fading value N printed on the card (e.g., "Fading 3" → 3).</summary>
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
