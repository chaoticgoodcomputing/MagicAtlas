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
public sealed record DoesntUntapEffect : ContinuousEffect
{
  /// <summary>
  /// What doesn't untap (usually Self, but can be other permanents).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }

  /// <summary>
  /// Whose untap step: "your", "its controller's", etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? WhoseUntapStep { get; init; }
}
