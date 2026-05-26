namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Bushido (Rule 702.45). A triggered keyword ability printed as
/// "Bushido N (Whenever this creature blocks or becomes blocked, it gets +N/+N
/// until end of turn.)". Although Bushido is mechanically a triggered ability,
/// MAST records it as a keyword marker — same approach as Prowess, Exalted,
/// Cascade — and treats the canonical trigger-and-buff expansion as engine
/// territory.
/// </summary>
/// <remarks>
/// First integer-parameterized keyword effect in the AST; <see cref="Value"/>
/// is the bushido number lifted from the printed oracle text. Cycling/Equip/
/// Morph etc. are parameterized by a <c>Cost</c>; Bushido is parameterized by
/// a single integer. Future integer-parameterized keywords (Soulshift,
/// Annihilator, Devour, Vanishing, Fading, Modular, etc.) should follow this
/// shape rather than reaching for the <c>Quantity</c> wrapper — the value is
/// a literal printed numeral, not a derived count.
///
/// <para>
/// The containing <c>StaticAbility</c> records <c>KeywordSource = "Bushido"</c>
/// so the keyword's identity survives normalization.
/// </para>
/// </remarks>
[OracleEffect("bushido")]
public sealed record BushidoEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The bushido value N printed on the card (e.g., "Bushido 2" → 2).</summary>
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
