namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
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
/// "except it has [ability]" — adds an ability to the copy whose structure has no
/// dedicated node yet. Free-text escape hatch: the keyword case is structured by
/// <see cref="KeywordAbilityAdder"/>, quoted triggered/activated abilities by
/// <see cref="TriggeredAbilityAdder"/>/<see cref="ActivatedAbilityAdder"/>. Only a
/// residual "except it has [X]" clause that matches none of those falls here.
/// </summary>
[CopyModificationKind("abilityAdder")]
public sealed record AbilityAdder : CopyModification
{
  /// <summary>
  /// The ability text gained, e.g. an unrecognised or complex ability body.
  /// </summary>
  [FreeTextField]
  public required string AbilityText { get; init; }
}

/// <summary>
/// "except it has [keyword]" / "that token gains [keyword]" — adds one or more
/// structured keyword abilities (CR 702) to the copy token (CR 707.2 copiable
/// values). The keyword analogue of <see cref="TriggeredAbilityAdder"/> and
/// <see cref="ActivatedAbilityAdder"/>: used when the "except"/"gains" clause names
/// a bare keyword ability such as "haste" (Kiki-Jiki, Heat Shimmer, Electroduplicate,
/// Helm of the Host) — a rules-meaningful keyword identity (e.g. CR 702.10 Haste, which
/// lets the token attack and use {T} abilities the turn it is created) rather than the
/// free text held by <see cref="AbilityAdder.AbilityText"/>. Keywords are the typed
/// <see cref="KeywordAbility"/> members so the grant is casing-proof and matchable
/// (ADR 0001), and a multi-keyword "except it has flying and haste" collapses to one
/// modification carrying the list.
/// </summary>
[CopyModificationKind("keywordAbilityAdder")]
public sealed record KeywordAbilityAdder : CopyModification
{
  /// <summary>The keyword abilities granted to the copy (e.g. [Haste]).</summary>
  public required IReadOnlyList<KeywordAbility> Keywords { get; init; }
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

/// <summary>
/// "except it enters with an additional [count] [counter type] counter(s) on it"
/// — adds counters to the copy as part of a copy-on-entry replacement (CR 707.2
/// copiable values; CR 614.12 covers the enters-the-battlefield-as-a-copy
/// replacement effect this modification rides on). Distinct from
/// <see cref="MagicAST.AST.Effects.ZoneChange.CounterPlacement"/> (a zone-change
/// counter attachment on exile/move effects) and from
/// <see cref="MagicAST.AST.Effects.Counter.PutCountersEffect"/> (a standalone
/// put-counters effect): this node is scoped to an "except" clause on
/// <see cref="BecomesCopyEffect"/>/<see cref="MagicAST.AST.Effects.TokenCopy.CopyEffect"/>,
/// recording the counter kind and count the copy gains on top of whatever it would
/// otherwise enter with. Spark Double: "it enters with an additional +1/+1 counter
/// on it if it's a creature" pairs this node with <see cref="ConditionalModification"/>
/// to gate it on the copy's resulting card type.
/// </summary>
[CopyModificationKind("counterAdder")]
public sealed record CounterAdder : CopyModification
{
  /// <summary>
  /// The kind of counter added (lowercase: "+1/+1", "loyalty", etc.).
  /// </summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// How many additional counters are added.
  /// </summary>
  public required Quantity Count { get; init; }
}

/// <summary>
/// "except [modification] if [condition]" — gates another <see cref="CopyModification"/>
/// on a predicate about the copy (CR 707.2 copiable values, applied conditionally;
/// CR 614.12 replacement effect). Spark Double's except-clause names two counter
/// additions that apply only depending on what the copy turns out to be: "it enters
/// with an additional +1/+1 counter on it if it's a creature, [and] an additional
/// loyalty counter on it if it's a planeswalker" → two <see cref="ConditionalModification"/>s,
/// each gating a <see cref="CounterAdder"/> on an <see cref="ObjectHasCardTypeCondition"/>
/// naming the copy's resulting type (Subject: Self — the entering permanent checking
/// its own post-copy type). The composable analogue of
/// <see cref="MagicAST.AST.Effects.Core.ConditionalEffect"/> for the modification axis
/// rather than the effect axis: WHICH modification applies is separate from WHETHER it
/// applies, so <see cref="Condition"/> and <see cref="Modification"/> are distinct fields
/// rather than baking the gate into a modification-specific discriminator.
/// </summary>
[CopyModificationKind("conditionalModification")]
public sealed record ConditionalModification : CopyModification
{
  /// <summary>The condition that must be true for <see cref="Modification"/> to apply.</summary>
  public required Condition Condition { get; init; }

  /// <summary>The modification applied when <see cref="Condition"/> is true.</summary>
  public required CopyModification Modification { get; init; }
}

/// <summary>
/// "except it has [activated ability]" — adds a fully-structured ACTIVATED ability
/// to the copy token (CR 707.2 copiable values). The activated analogue of
/// <see cref="TriggeredAbilityAdder"/>: used when the "except" clause is a quoted
/// activated ability such as <c>"{2}, {T}, Sacrifice this token: You gain 3 life."</c>
/// (Brenard, Ginger Sculptor) — a full "[Cost]: [Effect]" ability (CR 602.1) whose
/// cost list and effect are rules-meaningful and therefore cannot be held as free
/// text in <see cref="AbilityAdder.AbilityText"/>.
/// Rule CR 707.2: "when copying an object, the copy acquires the copiable values
/// of the original object's characteristics … abilities listed in the definition
/// of that object" — an "except it has [ability]" clause overrides the printed
/// abilities the token would otherwise inherit.
/// </summary>
[CopyModificationKind("activatedAbilityAdder")]
public sealed record ActivatedAbilityAdder : CopyModification
{
  /// <summary>
  /// The structured activated ability added to the copy token.
  /// </summary>
  public required ActivatedAbility Ability { get; init; }
}
