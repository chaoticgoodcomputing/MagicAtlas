namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Base type for events that can be replaced.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "eventType")]
[JsonDerivedType(typeof(TokenCreationEvent), "tokenCreation")]
[JsonDerivedType(typeof(CounterPlacementEvent), "counterPlacement")]
[JsonDerivedType(typeof(DamageEvent), "damage")]
[JsonDerivedType(typeof(DestructionEvent), "destruction")]
[JsonDerivedType(typeof(DeathEvent), "death")]
[JsonDerivedType(typeof(ZoneChangeEvent), "zoneChange")]
[JsonDerivedType(typeof(LifeChangeEvent), "lifeChange")]
[JsonDerivedType(typeof(UntapEvent), "untap")]
[JsonDerivedType(typeof(GenericEvent), "generic")]
public abstract record ReplacementEvent
{
  /// <summary>
  /// Filter for what objects/players this event applies to.
  /// </summary>
  [JsonPropertyName("affectedObjects")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AffectedObjects { get; init; }

  /// <summary>
  /// Whose control/ownership this applies to.
  /// </summary>
  [JsonPropertyName("controller")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Controller { get; init; }
}
