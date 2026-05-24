namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[Permanent] doesn't untap during your untap step."
/// A specific type of replacement effect that's common enough to warrant its own type.
/// </summary>
[OracleEffect("doesntUntap")]
public sealed record DoesntUntapEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// What doesn't untap (usually Self, but can be other permanents).
  /// </summary>
  [JsonPropertyName("target")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// Whose untap step: "your", "its controller's", etc.
  /// </summary>
  [JsonPropertyName("whoseUntapStep")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WhoseUntapStep { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
