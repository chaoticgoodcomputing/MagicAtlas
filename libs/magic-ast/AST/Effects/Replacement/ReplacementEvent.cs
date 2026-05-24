namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization;

/// <summary>
/// Base type for events that can be replaced.
/// </summary>
[PolymorphicBase("EventType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<ReplacementEvent>))]
public abstract record ReplacementEvent
{
  /// <summary>
  /// Filter for what objects/players this event applies to.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AffectedObjects { get; init; }

  /// <summary>
  /// Whose control/ownership this applies to.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Controller { get; init; }
}
