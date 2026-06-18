namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
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
  [FreeTextField]
  public required string AbilityText { get; init; }
}

/// <summary>
/// "except the token isn't legendary" — removes one or more supertypes from the
/// copy (Helm of the Host strips Legendary so its copies aren't culled by the
/// legend rule, CR 704.5j). The negation analogue of <see cref="TypeAdder"/>:
/// that node ADDS card-type/subtype tokens, this one REMOVES supertypes from the
/// copiable values the token would otherwise inherit (CR 707.2). A structured
/// list rather than free text — the removed supertype is rules-meaningful.
/// </summary>
[CopyModificationKind("supertypeRemover")]
public sealed record SupertypeRemover : CopyModification
{
  /// <summary>
  /// Supertypes removed from the copy (e.g. "Legendary").
  /// </summary>
  public required IReadOnlyList<string> Supertypes { get; init; }
}

/// <summary>
/// "except it's [color] in addition to its other colors" — adds one or more colors
/// to the copy without removing its existing colors (CR 105.3: "If an effect gives
/// an object a new color, the new color replaces all previous colors … unless the
/// effect said the object became that color 'in addition' to its other colors.").
/// Distinct from a color-replacement modification: this node signals the additive
/// "in addition" form, so the copy keeps every color the original had plus the
/// named colors. Colors are encoded as single-letter codes (W/U/B/R/G).
/// </summary>
[CopyModificationKind("colorAdder")]
public sealed record ColorAdder : CopyModification
{
  /// <summary>
  /// Colors added to the copy, e.g. ["B"] for "black in addition to its other colors".
  /// </summary>
  public required IReadOnlyList<string> Colors { get; init; }
}

/// <summary>
/// "except it has [triggered/activated/static ability]" — adds a fully-structured
/// triggered or other complex ability to the copy token (CR 707.2 copiable values).
/// Used when the "except" clause is a quoted triggered ability such as
/// <c>"At the beginning of the end step, exile this token."</c> (Heat Shimmer,
/// Twinflame) — a full triggered ability whose structure is rules-meaningful and
/// therefore cannot be held as free text in <see cref="AbilityAdder.AbilityText"/>.
/// Rule CR 707.2: "when copying an object, the copy acquires the copiable values
/// of the original object's characteristics … abilities listed in the definition
/// of that object" — an "except it has [ability]" clause overrides the printed
/// abilities the token would otherwise inherit.
/// </summary>
[CopyModificationKind("triggeredAbilityAdder")]
public sealed record TriggeredAbilityAdder : CopyModification
{
  /// <summary>
  /// The structured triggered ability added to the copy token.
  /// </summary>
  public required TriggeredAbility Ability { get; init; }
}
