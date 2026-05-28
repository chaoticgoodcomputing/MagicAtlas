namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may look at the top card of your library any time." — Rule 701.12 (look).
/// A continuous static permission that lets the controller inspect the top card
/// of their library whenever they choose, rather than only at times when they
/// could cast or play something. This is a persistent visibility grant, not a
/// one-shot look or a triggered look.
///
/// <para>
/// MAST records this as a static ability (the permission persists as long as the
/// source is on the battlefield — Rule 604.3). The "You may" preamble makes the
/// effect permissive (<see cref="IsOptional"/> = <c>true</c>); the controller is
/// never forced to look. There are no parameters: the subject is always "You" (the
/// controller), the zone is always the top of the library, and the timing grant is
/// "any time" — all of which are captured by the discriminator alone.
/// </para>
/// </summary>
[OracleEffect("lookAtTopCardAnyTime")]
public sealed record LookAtTopCardAnyTimeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
