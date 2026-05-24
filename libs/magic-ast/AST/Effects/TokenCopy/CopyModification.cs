namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A single "except"-clause that modifies the copy produced by
/// <see cref="CopyEffect"/>. Concrete modifications include power/toughness
/// overrides, type additions, and ability additions.
/// </summary>
[PolymorphicBase("ModificationType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<CopyModification>))]
public abstract record CopyModification;

/// <summary>
/// "except it's [P]/[T]" — overrides the copy's printed power and toughness.
/// </summary>
[CopyModificationKind("powerToughnessOverride")]
public sealed record PowerToughnessOverride : CopyModification
{
  public required Quantity Power { get; init; }

  public required Quantity Toughness { get; init; }
}

/// <summary>
/// "except it's a [Type] in addition to its other types" — adds card-type or
/// subtype tokens to the copy without removing existing ones.
/// </summary>
[CopyModificationKind("typeAdder")]
public sealed record TypeAdder : CopyModification
{
  /// <summary>
  /// Card types added (e.g. "Artifact", "Creature").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? CardTypes { get; init; }

  /// <summary>
  /// Subtypes added (e.g. "Vehicle", "Equipment").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }
}

/// <summary>
/// "except it has [keyword/ability]" — adds an ability to the copy. Free-text
/// for now; future refinement can carry a structured Ability node.
/// </summary>
[CopyModificationKind("abilityAdder")]
public sealed record AbilityAdder : CopyModification
{
  /// <summary>
  /// The ability text gained, e.g. "flying", "haste".
  /// </summary>
  public required string AbilityText { get; init; }
}
