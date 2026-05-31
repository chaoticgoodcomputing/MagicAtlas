namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// Describes a filter for selecting objects.
/// e.g., "nontoken creature with flying you control"
/// </summary>
public sealed record ObjectFilter
{
  /// <summary>
  /// Exact card-name constraint, e.g. "a card named Sorin, Vampire Lord".
  /// Rule 201.4 (an object "named" a specific card matches only objects with
  /// that exact name). Distinct from the type/subtype axes: a named-card filter
  /// pins the identity of the matched object rather than a category of objects.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Name { get; init; }

  /// <summary>
  /// Card types to match: Creature, Artifact, Enchantment, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? CardTypes { get; init; }

  /// <summary>
  /// Card types EXCLUDED by a "non-[type]" qualifier — e.g. "a nonland card"
  /// → <c>CardTypes=["card"]</c> + <c>ExcludedCardTypes=["land"]</c>. Parallel
  /// negation axis to <see cref="CardTypes"/>: a filter matches only objects that
  /// have none of these types. Distinct from <see cref="IsToken"/> (a token/nontoken
  /// predicate, not a card type) per CR 110.4 / 111.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ExcludedCardTypes { get; init; }

  /// <summary>
  /// Token predicate: <c>true</c> matches only tokens, <c>false</c> only
  /// nontoken objects ("nontoken creature", CR 111). Null when the oracle text
  /// does not qualify token-ness. Not a card type (CR 111.1: a token is not a
  /// card), so it is a separate boolean axis rather than a <see cref="CardTypes"/>
  /// or <see cref="ExcludedCardTypes"/> entry.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsToken { get; init; }

  /// <summary>
  /// The kind of game entity this filter selects when it is not a card/permanent
  /// object — e.g. "player" in "Enchant player" (CR 702.5) or in a target
  /// restriction. Distinct from <see cref="CardTypes"/> (which categorizes
  /// objects); a player is not an object (CR 109 vs CR 102).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? EntityType { get; init; }

  /// <summary>
  /// Restricts to cards exiled by a linked exile ability (CR 406.6) — "cards
  /// exiled with [object]". The reference identifies the object whose exile
  /// ability produced the cards (Azula: <c>{Kind:"Self"}</c>). This is a
  /// reference, not a runtime binding (ADR 0004 "reference not resolution"): it
  /// names the linking object, not a threaded variable. Used together with
  /// <c>Zone=Exile</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? ExiledWith { get; init; }

  /// <summary>
  /// Subtypes to match: Human, Equipment, Aura, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Supertypes to match: Legendary, Basic, Snow, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Colors to match. An object passes the filter if it has at least one of
  /// the listed colors. Do NOT use this to encode colorlessness — colorless
  /// is the absence of color (Rule 105.1: "Colorless is not a color"). Use
  /// <see cref="IsColorless"/> instead.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Filters to colorless objects (those with no colors at all). Rule 105.1.
  /// Mutually exclusive with <see cref="Colors"/> in practice (a card cannot
  /// be both colorless and have a color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsColorless { get; init; }

  /// <summary>
  /// Filters to multicolored objects (those with two or more colors). Rule 105.5
  /// ("An object is multicolored if it has two or more colors"). Parallel axis to
  /// <see cref="IsColorless"/> — distinct from the "has any of these colors" semantics
  /// of <see cref="Colors"/>, which is satisfied by a single matching color and so
  /// cannot encode the "two or more" constraint.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsMulticolored { get; init; }

  /// <summary>
  /// Filters to monocolored objects (those with exactly one color). Rule 105.3
  /// ("An object is the color or colors of the mana symbols in its mana cost,
  /// regardless of the cost of that mana ... If an object has exactly one of
  /// these five colors, it is monocolored"). Parallel axis to
  /// <see cref="IsMulticolored"/> and <see cref="IsColorless"/> — the "exactly
  /// one color" constraint cannot be expressed by <see cref="Colors"/>, which
  /// encodes "has any of these colors" (satisfied by multicolored objects that
  /// happen to include a listed color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsMonocolored { get; init; }

  /// <summary>
  /// Who controls the objects: You, Opponent, Any.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Controller { get; init; }

  /// <summary>
  /// Additional characteristic constraints beyond the structured axes above —
  /// a keyword-ability requirement (<see cref="KeywordCharacteristic"/>) or the
  /// typed residual (<see cref="OtherCharacteristic"/>) for shapes not yet
  /// structured. Replaces the former bare-string list (see ADR 0001).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Characteristic>? Characteristics { get; init; }

  /// <summary>
  /// Restricts the filter to objects matching the characteristic value CHOSEN as
  /// this permanent entered — the structured consumer side of a CR 607 linked ability
  /// (the producer is a "choose a [creature type|color]" effect under
  /// <c>StaticAbility.When = AsThisEnters</c>, CR 614.1c). Implicit and kind-based:
  /// a permanent has at most one chosen creature type and one chosen color, so no
  /// explicit variable name is needed. Replaces free-text
  /// <c>Characteristics: ["of the chosen type"]</c>.
  /// e.g. "creatures you control of the chosen type" → <c>Controller=You</c> +
  /// <c>ChosenCharacteristic=CreatureType</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ChosenCharacteristicKind? ChosenCharacteristic { get; init; }

  /// <summary>
  /// Zone restriction: Battlefield, Graveyard, Hand, Library, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? Zone { get; init; }

  /// <summary>
  /// Power comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? PowerComparison { get; init; }

  /// <summary>
  /// Toughness comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? ToughnessComparison { get; init; }

  /// <summary>
  /// Mana value comparison.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? ManaValueComparison { get; init; }

  /// <summary>
  /// Backward-looking lifecycle predicate restricting the filter.
  /// e.g., "a creature dealt damage by Zurgo this turn".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public HistoryPredicate? History { get; init; }

  /// <summary>
  /// Location in source text. Only present for unparsed or partially-parsed nodes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public TextSpan? SourceSpan { get; init; }

  // Factory methods
  public static ObjectFilter Creature(TextSpan? span = null) =>
    new() { CardTypes = ["creature"], SourceSpan = span };

  public static ObjectFilter Permanent(TextSpan? span = null) =>
    new() { CardTypes = ["permanent"], SourceSpan = span };

  public static ObjectFilter Card(TextSpan? span = null) =>
    new() { CardTypes = ["card"], SourceSpan = span };

  public static ObjectFilter Player(TextSpan? span = null) =>
    new() { CardTypes = ["player"], SourceSpan = span };
}

/// <summary>
/// Which "chosen" characteristic an <see cref="ObjectFilter.ChosenCharacteristic"/>
/// reference points at — the value selected by a "choose a [creature type|color]"
/// effect as this permanent entered (CR 607 linked abilities). Kind-based and
/// implicit: a permanent has at most one chosen creature type and one chosen color.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ChosenCharacteristicKind
{
  /// <summary>"of the chosen type" — the creature type chosen (Etchings of the Chosen, Unclaimed Territory).</summary>
  CreatureType,

  /// <summary>"of the chosen color" — the color chosen (Paradise Plume, Thriving lands).</summary>
  Color,
}

/// <summary>
/// Filter for who controls an object.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ControllerFilter
{
  You,
  Opponent,
  Any,

  /// <summary>
  /// Objects controlled by a targeted player or opponent — "creatures target player controls",
  /// "each creature target opponent controls". The targeting requirement is expressed by the
  /// parent <see cref="ObjectReference.Kind"/> being <see cref="ObjectReferenceKind.Target"/>
  /// or by a separate targeting reference on the enclosing effect; this value records only
  /// that the controller axis is a runtime-chosen target rather than the spell's own
  /// controller (You) or any opponent (Opponent).
  /// </summary>
  Target,

  /// <summary>
  /// Objects controlled by the player enchanted by this Aura — "a planeswalker
  /// that player controls" on a player-enchanting Aura (Curse of the Pierced
  /// Heart). Parallels <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> on
  /// the controller axis: the enchanted player (CR 702.5, player Aura) rather than
  /// the ability's own controller.
  /// </summary>
  EnchantedPlayer,
}

/// <summary>
/// A numeric comparison.
/// </summary>
public sealed record Comparison
{
  public required ComparisonOperator Operator { get; init; }

  public required int Value { get; init; }
}

/// <summary>
/// Comparison operators.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ComparisonOperator
{
  LessThan,
  LessThanOrEqual,
  Equal,
  GreaterThanOrEqual,
  GreaterThan,
  NotEqual,
}

/// <summary>
/// Game zones where cards can exist.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum Zone
{
  Battlefield,
  Graveyard,
  Hand,
  Library,
  Exile,
  Stack,
  CommandZone,
  Anywhere,
}
