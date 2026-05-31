namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "If [event] would [happen], [alternative] instead"
/// Rule 614
/// </summary>
[OracleEffect("replacement")]
public sealed record ReplacementEffect : Effect
{
  /// <summary>
  /// The structured event being replaced/modified.
  /// </summary>
  public required ReplacementEvent Event { get; init; }

  /// <summary>
  /// Whether the original event still occurs (true for augmentation like Chatterfang,
  /// false for pure replacement like "exile it instead").
  /// </summary>
  public bool OriginalEventOccurs { get; init; }

  /// <summary>
  /// The effect(s) that happen instead of or in addition to the original event.
  /// Null when the replacement is expressed purely as a <see cref="Modifier"/>
  /// on the original event (e.g., Doubling Season's "twice that many" — the
  /// original event's shape is preserved, only the quantity changes).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? Replacement { get; init; }

  /// <summary>
  /// Optional modifier to the original event (e.g., "twice that many" for Doubling Season).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ReplacementModifier? Modifier { get; init; }
}
