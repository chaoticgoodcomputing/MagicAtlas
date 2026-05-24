namespace MagicAST.AST.Effects.Traits;

using System.Text.Json.Serialization;

/// <summary>
/// Describes an effect whose oracle text carries a duration clause — e.g.,
/// "until end of turn", "until your next turn", "as long as you control...".
/// The trait captures the *presence* of such a clause; the actual scoping
/// rules are a runtime concern handled by consumers of the AST.
///
/// <para>MAST is not a rules engine — this trait describes what the card
/// text says, not when the effect actually ends in play.</para>
/// </summary>
public interface IDurativeEffect
{
  /// <summary>
  /// The duration clause associated with this effect, or null if none
  /// is declared in the oracle text.
  /// </summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  Duration? Duration { get; init; }
}
