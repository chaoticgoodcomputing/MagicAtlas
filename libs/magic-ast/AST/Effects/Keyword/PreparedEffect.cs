namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Prepared — a keyword state printed as "This creature enters prepared."
/// on the front face of a prepare-layout double-faced card
/// (Foundations Remastered and related sets). While a creature is prepared,
/// its controller may cast a copy of its attached spell; doing so unprepares it.
/// MAST records the keyword's presence; the prepared-state tracking and
/// spell-copy mechanics are engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Parameterless keyword marker. Unlike leading-keyword patterns ("Flying",
/// "Haste"), the prepared state appears mid-sentence ("This creature enters
/// prepared."); the combinator matches the full sentence shape rather than a
/// leading keyword token. The reminder text "(While it's prepared, you may cast
/// a copy of its spell. Doing so unprepares it.)" is consumed but not stored.
/// </para>
/// </summary>
[OracleEffect("prepared")]
public sealed record PreparedEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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
