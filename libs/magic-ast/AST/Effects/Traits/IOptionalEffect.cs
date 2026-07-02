namespace MagicAST.AST.Effects.Traits;

using System.Text.Json.Serialization;

/// <summary>
/// Describes an effect that the controller may choose whether to perform
/// (the "You may..." prefix in oracle text). The optional follow-up
/// <see cref="IfYouDo"/> captures the "If you do, [Y]" continuation pattern
/// where a secondary effect is contingent on the controller having chosen
/// to perform the main effect.
///
/// <para>
/// This trait describes *what the card text says*, not how/when the
/// decision is resolved at runtime — MAST is not a rules engine.
/// </para>
/// </summary>
public interface IOptionalEffect
{
  /// <summary>
  /// Whether this effect carries a "You may" prefix in oracle text.
  /// </summary>
  bool IsOptional { get; init; }

  /// <summary>
  /// Optional secondary effect that runs "if you do" perform the main
  /// effect. Null when no continuation clause is present.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  Effect? IfYouDo { get; init; }

  /// <summary>
  /// Optional secondary effect that runs "if you don't" perform the main
  /// effect. Mirrors <see cref="IfYouDo"/>. Captures the per-player fork
  /// pattern (Rule 117.7): "Each player may [X]. Each player who doesn't [Y]."
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  Effect? IfYouDoNot { get; init; }
}
