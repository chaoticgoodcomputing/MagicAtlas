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
  /// Subtypes EXCLUDED by a "non-[subtype]" qualifier — e.g. "non-Human creature"
  /// → <c>CardTypes=["creature"]</c> + <c>ExcludedSubtypes=["Human"]</c> (Mutate's
  /// "target non-Human creature you own"). Parallel negation axis to
  /// <see cref="Subtypes"/>, mirroring <see cref="ExcludedCardTypes"/> over
  /// <see cref="CardTypes"/>: a filter matches only objects with none of these
  /// subtypes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ExcludedSubtypes { get; init; }

  /// <summary>
  /// Supertypes to match: Legendary, Basic, Snow, etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Supertypes { get; init; }

  /// <summary>
  /// Supertypes EXCLUDED by a "non[supertype]" qualifier — e.g. "nonlegendary
  /// creature" → <c>CardTypes=["creature"]</c> + <c>ExcludedSupertypes=["Legendary"]</c>
  /// (Kiki-Jiki's "target nonlegendary creature you control"). Parallel negation
  /// axis to <see cref="Supertypes"/>, mirroring <see cref="ExcludedCardTypes"/>
  /// over <see cref="CardTypes"/> and <see cref="ExcludedSubtypes"/> over
  /// <see cref="Subtypes"/>: a filter matches only objects with none of these
  /// supertypes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ExcludedSupertypes { get; init; }

  /// <summary>
  /// Colors to match. An object passes the filter if it has at least one of
  /// the listed colors. Do NOT use this to encode colorlessness — colorless
  /// is the absence of color (Rule 105.1: "Colorless is not a color"). Use
  /// <see cref="IsColorless"/> instead.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Colors { get; init; }

  /// <summary>
  /// Colors EXCLUDED by a "non[color]" qualifier — e.g. "nonblack creature"
  /// → <c>CardTypes=["creature"]</c> + <c>ExcludedColors=["B"]</c> (Doom Blade),
  /// "nonblue spell" (Frazzle). Parallel negation axis to <see cref="Colors"/>,
  /// mirroring <see cref="ExcludedCardTypes"/> over <see cref="CardTypes"/>: a
  /// filter matches only objects that have none of these colors (CR 105.1).
  /// Distinct from <see cref="IsColorless"/> ("no colors at all"): a nonblack
  /// object may still be colored, just not black.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ExcludedColors { get; init; }

  /// <summary>
  /// Relational color axis: filters to objects that share a color with a referenced
  /// object. Conspire's "two untapped creatures you control that each share a color with
  /// it" (CR 702.78a), where the reference is the spell with conspire
  /// (<c>{Kind: Self}</c>). Distinct from <see cref="Colors"/> (absolute "has any of these
  /// literal colors"): the colors to match are those of the referenced object, resolved by
  /// a consumer, not card-text literals. Parallels the relational <see cref="AttachedTo"/>
  /// and <see cref="ExiledWith"/> axes (an <see cref="ObjectReference"/>-valued predicate).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? SharesColorWith { get; init; }

  /// <summary>
  /// Relational subtype axis: filters to objects that share a creature type with a
  /// referenced object — "shares a creature type with it" (Titan of Littjara: "you may
  /// draw a card for each other creature you control that shares a creature type with
  /// it"). CR 205.3 (creature subtypes are called creature types). Parallels
  /// <see cref="SharesColorWith"/> (the color-family sibling): the creature types to
  /// match are those the referenced object CURRENTLY has, resolved by a consumer, not a
  /// card-text literal — distinct from <see cref="ChosenCharacteristic"/>, which
  /// compares against a single fixed value chosen once (not the referenced object's
  /// live, possibly-multiple, type set).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? SharesCreatureTypeWith { get; init; }

  /// <summary>
  /// Filters to colorless objects (those with no colors at all). Rule 105.1.
  /// Mutually exclusive with <see cref="Colors"/> in practice (a card cannot
  /// be both colorless and have a color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsColorless { get; init; }

  /// <summary>
  /// Filters to historic objects — those that have the legendary supertype, the
  /// artifact card type, or the Saga subtype (CR 700.6: "The term historic refers
  /// to an object that has the legendary supertype, the artifact card type, or the
  /// Saga subtype."). A named game quality, not a supertype or subtype: it cannot
  /// be expressed on the existing type axes without losing semantic precision
  /// (encoding it as a Subtypes entry would assert "Historic" is a printed subtype
  /// of the card, which is false). Parallels <see cref="IsColorless"/> as a boolean
  /// game-quality axis on the filter.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsHistoric { get; init; }

  /// <summary>
  /// Filters to objects in the controlling player's party — CR 700.8: "Some cards
  /// refer to a player's party. A player's party consists of up to one Cleric
  /// creature that player controls, up to one Rogue creature they control, up to
  /// one Warrior creature they control, and up to one Wizard creature they
  /// control." A named CR 700-level game grouping, not a card type or subtype: it
  /// cannot be expressed on the existing type axes without falsely asserting
  /// "Party" is a printed subtype. Parallels <see cref="IsHistoric"/> (CR 700.6)
  /// as a boolean game-quality axis on the filter — the "up to one each of the
  /// four classes, capped at four" counting is engine territory (descriptive-not-
  /// engine doctrine); the AST records only that the filter is scoped to party
  /// membership.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? InParty { get; init; }

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
  /// Who OWNS the objects (CR 108.3) — distinct from <see cref="Controller"/>
  /// (CR 109.4). "a creature you own", Mutate's "target non-Human creature you own".
  /// An object's owner is the player who started the game with it in their deck;
  /// control can differ. Parallel axis to <see cref="Controller"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? Owner { get; init; }

  /// <summary>
  /// "another" — the filter excludes the source object of the ability (CR 109.5).
  /// Champion's "another creature", Soulbond's "another … creature you control".
  /// Distinct from <see cref="ObjectReferenceKind.Another"/> on a reference: this is
  /// the filter-level exclusion, so a filtered set ("another creature you control")
  /// honestly omits self.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ExcludeSelf { get; init; }

  /// <summary>
  /// "this" — the filter is the source object of the ability itself ("this creature", "this
  /// permanent"; CR 109). The dual of <see cref="ExcludeSelf"/>: where ExcludeSelf omits the source,
  /// IsSelf restricts to <em>only</em> the source. Lets "when this creature dies" be distinguished
  /// from "when a creature dies" — the self/any axis the interaction operator gates, since an
  /// arbitrary object is not provably the source. Mirrors <see cref="ObjectReferenceKind.Self"/> at
  /// the filter level.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsSelf { get; init; }

  /// <summary>
  /// "enchanted [type]" — the object this Aura is currently attached to (CR 303.4 /
  /// 702.5). An Aura's "enchanted creature" refers to the permanent it enchants, not
  /// an arbitrary object of that type. Flat boolean axis mirroring <see cref="IsSelf"/>
  /// and <see cref="IsToken"/>: where IsSelf restricts to the source object and
  /// ExcludeSelf omits it, IsEnchanted restricts to the Aura's attached permanent.
  /// Distinct from <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> at the reference
  /// level — this is the filter-level predicate, so a trigger/effect filter
  /// ("whenever enchanted creature attacks") honestly names the attached object rather
  /// than carrying it as a free-text characteristic.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsEnchanted { get; init; }

  /// <summary>
  /// "equipped creature" — the creature this Equipment is currently attached to (CR 301.5 /
  /// 702.6). An Equipment's "equipped creature" refers to the permanent it is attached to,
  /// not an arbitrary creature. Flat boolean axis parallel to <see cref="IsEnchanted"/>:
  /// where IsEnchanted restricts to the Aura's attached permanent, IsEquipped restricts to
  /// the Equipment's attached permanent. Used in trigger filters ("whenever equipped creature
  /// deals combat damage") to distinguish the source Equipment's host from any creature.
  /// Distinct from <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> at the reference
  /// level — this is the filter-level predicate.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsEquipped { get; init; }

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
  /// Zone EXCLUDED by a "from anywhere other than [zone]" qualifier. Parallel
  /// negation axis to <see cref="Zone"/>, mirroring <see cref="ExcludedCardTypes"/>
  /// over <see cref="CardTypes"/>.
  ///
  /// <para>
  /// On a <c>CardTypes=["spell"]</c> filter this names the zone the spell was CAST
  /// FROM rather than its current zone — a spell's current zone is always the stack
  /// (CR 111.6/109.5), so a stated zone other than <see cref="Zone.Stack"/> on a
  /// spell filter is unambiguous shorthand for the pre-cast origin zone (Savvy
  /// Trader: "Spells you cast from anywhere other than your hand cost {1} less to
  /// cast" → <c>CardTypes=["spell"], ExcludedZone=Hand</c>; CR 601.2f cost
  /// reduction). Parallels <see cref="MagicAST.AST.Effects.CardFlow.AlternativeCastEffect.FromZone"/>
  /// (the positive "you may cast this from [zone]" permission's origin-zone axis).
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Zone? ExcludedZone { get; init; }

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
  /// Relational axis: the object must be attached to the referenced object —
  /// Strong Back's "each Aura and Equipment attached to it" (ADR 0003 follow-up
  /// 3, replacing the stringly-typed fake KorSpiritdancer used). The referent is
  /// an <see cref="ObjectReference"/> (e.g. <c>EnchantedOrEquipped</c>, "it").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? AttachedTo { get; init; }

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

  /// <summary>"of the chosen type" where the type is a card type (artifact, creature, enchantment, instant, or sorcery)
  /// chosen as the permanent entered — the structured consumer of a CR 607 linked ability whose producer is a
  /// <see cref="MagicAST.AST.Effects.Keyword.ChooseCardTypeEffect"/> (e.g. Cloud Key).</summary>
  CardType,

  /// <summary>"the basic land type of your choice" — one of the five basic land types (Plains, Island, Swamp,
  /// Mountain, Forest) chosen as an effect resolves (Reef Shaman: "{T}: Target land becomes the basic land type
  /// of your choice until end of turn."). Constrained per CR 305.6 ("If an object uses the words 'basic land
  /// type,' it's referring to one of these subtypes"). Used on <see cref="Effects.Modification.ChangeSubtypeEffect.ChosenSubtype"/>
  /// as the fresh-choice land-type analogue of <see cref="CreatureType"/>; setting a land's subtype to a basic
  /// land type is governed by CR 305.7 (the land loses its old land types and gains the corresponding mana
  /// abilities).</summary>
  BasicLandType,

  /// <summary>"the chosen name" — the card name chosen as the permanent entered (CR 614.12; Declaration of
  /// Naught: "As this enchantment enters, choose a card name." / "{U}: Counter target spell with the chosen
  /// name."). Structured consumer of a <see cref="Effects.Keyword.ChooseCardNameEffect"/> producer, mirroring
  /// how <see cref="CreatureType"/>/<see cref="Color"/>/<see cref="CardType"/> consume their own
  /// "choose a [X]" producers. Used on an <see cref="ObjectFilter"/> whose <see cref="ObjectFilter.CardTypes"/>
  /// already names the object category (e.g. "spell") — the chosen name narrows that category to the single
  /// named card rather than duplicating the literal <see cref="ObjectFilter.Name"/> string field, since the
  /// name itself is a fresh per-game choice, not a fixed value printed on the card.</summary>
  CardName,
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

  /// <summary>
  /// Objects belonging to the player named by the trigger condition — "cards in
  /// [that player's] hand" in an intervening-if on an each-opponent's-upkeep
  /// trigger (Prickle Faeries: "if that player has two or fewer cards in hand").
  /// The controller/owner axis analogue of <see cref="ObjectReferenceKind.ThatPlayer"/>:
  /// the player whose turn/step fired the trigger (CR 109.5 — "that player"), not
  /// the ability's own controller. Used on the <see cref="ObjectFilter.Owner"/>
  /// axis when the count is of cards in that player's hand (hand membership is by
  /// ownership, CR 108.3).
  /// </summary>
  ThatPlayer,

  /// <summary>
  /// Objects controlled by the defending player (CR 508.1b) — "target creature
  /// defending player controls", "any creature defending player controls". The
  /// defending player is the player being attacked (or the controller of a
  /// planeswalker or battle being attacked). Distinct from
  /// <see cref="Opponent"/> (which covers any opponent regardless of combat role)
  /// and from <see cref="Target"/> (which requires a "target" keyword to select
  /// a player). Used in combat-triggered abilities that target permanents
  /// controlled by whoever is defending against the attacking creature.
  /// CR 508.1b: "If the defending player controls any planeswalkers, is the
  /// protector of any battles, or the game allows the active player to attack
  /// multiple other players, the active player announces which player, planeswalker,
  /// or battle each of the chosen creatures is attacking."
  /// </summary>
  DefendingPlayer,

  /// <summary>
  /// Objects belonging to the player selected by a "choose a player" effect
  /// (CR 607 linked ability) — "the chosen player or a permanent they control"
  /// (Sawhorn Nemesis: "If a source would deal damage to the chosen player or a
  /// permanent they control, it deals double that damage instead."). Leaving
  /// <see cref="ObjectFilter.CardTypes"/> unset lets a single filter cover both
  /// the chosen player themselves and any permanent they control, mirroring how
  /// <see cref="Opponent"/> is used bare (no CardTypes) for the structurally
  /// identical "an opponent or a permanent an opponent controls" shape
  /// (<c>NoncombatDamageDoublingReplacementRule</c>, Solphim). Parallels
  /// <see cref="EnchantedPlayer"/> on the controller axis: the player bound by a
  /// fresh "choose a player" declaration rather than an Aura's enchanted object.
  /// </summary>
  ChosenPlayer,
}

/// <summary>
/// A numeric comparison. The right-hand side is either a printed literal
/// (<see cref="Value"/>) or a relative reference to another object's characteristic
/// (<see cref="RelativeTo"/> + <see cref="RelativeCharacteristic"/>) — e.g.
/// "power less than this creature's power" (Mentor, CR 702.134) compares against the
/// source object's power, not a static int. Exactly one side is populated: literal
/// consumers set <see cref="Value"/> and leave the relative axes null (serialized
/// byte-identically — the relative fields are omitted via WhenWritingNull); relative
/// consumers set <see cref="RelativeTo"/>/<see cref="RelativeCharacteristic"/> and
/// leave <see cref="Value"/> null.
/// </summary>
public sealed record Comparison
{
  public required ComparisonOperator Operator { get; init; }

  /// <summary>
  /// The printed integer right-hand side ("power 4 or greater"). Null when the
  /// comparison is relative to another object's characteristic — see
  /// <see cref="RelativeTo"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Value { get; init; }

  /// <summary>
  /// The object whose characteristic the comparison is made against, when the
  /// right-hand side is relative rather than a literal — "power less than this
  /// creature's power" → <c>RelativeTo = ObjectReference.Self()</c> (CR 702.134
  /// Mentor; the relative-power evasion convention). Null for literal-int
  /// comparisons (the common case), so they serialize unchanged.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? RelativeTo { get; init; }

  /// <summary>
  /// Which characteristic of <see cref="RelativeTo"/> the comparison reads —
  /// e.g. <see cref="RelativeCharacteristic.Power"/> for "power less than this
  /// creature's power". Null for literal-int comparisons.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public RelativeCharacteristic? RelativeCharacteristic { get; init; }
}

/// <summary>
/// Which characteristic of a referenced object a relative <see cref="Comparison"/>
/// reads — the "[object]'s power" / "[object]'s toughness" axis a relative threshold
/// compares against (CR 702.134 Mentor compares the target's power to the source's power).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RelativeCharacteristic
{
  /// <summary>The referenced object's power ("power less than this creature's power").</summary>
  Power,

  /// <summary>The referenced object's toughness.</summary>
  Toughness,

  /// <summary>The referenced object's mana value.</summary>
  ManaValue,
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
