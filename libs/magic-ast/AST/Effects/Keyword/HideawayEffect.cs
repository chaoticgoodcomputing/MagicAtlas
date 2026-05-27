namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Hideaway N (Rule 702.74). A triggered keyword ability printed as
/// "Hideaway N (When this [permanent] enters, look at the top N cards of
/// your library, exile one face down, then put the rest on the bottom in
/// a random order.)". MAST records the keyword and its integer lookahead
/// count; the ETB trigger, look/exile/shuffle semantics are engine territory
/// per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; the parameter is lifted into a
/// <see cref="Quantity"/> (typically a <see cref="LiteralQuantity"/>) to
/// mirror the Crew and Saddle shapes rather than the raw-int Bushido/Toxic/
/// Backup shapes, since the printed value governs a count of cards looked at.
/// </para>
/// </summary>
[OracleEffect("hideaway")]
public sealed record HideawayEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The number of cards to look at from the top of the library (N in "Hideaway N").
  /// </summary>
  public required Quantity Amount { get; init; }

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
