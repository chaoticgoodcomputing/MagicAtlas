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
  /// Relational card-type axis: filters to objects that share at least one PERMANENT type
  /// (CR 110.4: artifact, battle, creature, enchantment, land, planeswalker) with a referenced
  /// object — Cloudstone Curio: "return another permanent you control that shares a permanent
  /// type with it". Parallels <see cref="SharesCreatureTypeWith"/> (the narrower creature-SUBTYPE
  /// sibling, CR 205.3m) and <see cref="SharesColorWith"/> (the color sibling): the permanent
  /// types to match are those the referenced object CURRENTLY has, resolved by a consumer, not a
  /// card-text literal. Distinct from a plain <see cref="CardTypes"/> entry, which pins one or more
  /// FIXED literal types rather than comparing against another object's live type set.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? SharesPermanentTypeWith { get; init; }

  /// <summary>
  /// Relational name axis: filters to objects that have the SAME NAME as a referenced object —
  /// "other creature you control with the same name as that creature" (Mirror Box: "Each nontoken
  /// creature you control gets +1/+1 for each other creature you control with the same name as that
  /// creature."). CR 201.2 (two objects have the same name if the English versions of their names
  /// are identical). Parallels <see cref="SharesCreatureTypeWith"/> (the creature-type sibling) and
  /// <see cref="SharesColorWith"/> (the color sibling): the name to match is the one the referenced
  /// object CURRENTLY has, resolved by a consumer, not a card-text literal — distinct from the
  /// absolute <see cref="Name"/> axis (CR 201.4), which pins a single fixed printed name. The
  /// referent is an <see cref="ObjectReference"/> ("that creature", the anaphoric back-reference to
  /// the per-object subject the anthem is currently modifying — <c>{Kind:It}</c> per Rule 109.2).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? SharesNameWith { get; init; }

  /// <summary>
  /// Relational name-EXCLUSION axis: the negation sibling of <see cref="SharesNameWith"/> —
  /// filters to objects that do NOT have the same name as a referenced object. "creatures you
  /// control that don't have the same name as this creature" (Marvin, Murderous Mimic: "Marvin
  /// has all activated abilities of creatures you control that don't have the same name as this
  /// creature."). CR 201.2 (two objects have the same name if the English versions of their names
  /// are identical) applied under negation. Parallels how <see cref="ExcludedCardTypes"/>/
  /// <see cref="ExcludedColors"/> pair as the negation of their positive axes: where
  /// <see cref="SharesNameWith"/> keeps name-sharing objects, this keeps name-DIFFERING ones. The
  /// name to compare against is the one the referenced object CURRENTLY has, resolved by a consumer
  /// — the referent is an <see cref="ObjectReference"/> (Marvin's "this creature" =
  /// <c>{Kind:Self}</c>, CR 109). The type-honest home the free-text whitelist named for the
  /// "ExcludesSameNameAsSelf" residual.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? ExcludesNameOf { get; init; }

  /// <summary>
  /// Relational card-type axis: filters to objects that share a card type with a referenced
  /// object — "Spells you cast that share a card type with the exiled card cost {2} less to
  /// cast" (Semblance Anvil, CR 118.7 cost reduction). CR 110.4 (card types: artifact,
  /// creature, enchantment, instant, land, planeswalker, sorcery, etc.). Parallels
  /// <see cref="SharesCreatureTypeWith"/> (the creature-subtype sibling, CR 205.3) and
  /// <see cref="SharesColorWith"/> (the color sibling): the card types to match are those the
  /// referenced object CURRENTLY has, resolved by a consumer, not a literal
  /// <see cref="CardTypes"/> list. The referent is typically the card exiled by a linked
  /// Imprint ability (CR 702.38) — an <see cref="ObjectReference"/> with Zone.Exile +
  /// ExiledWith: Self (ADR 0004 "reference not resolution").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? SharesCardTypeWith { get; init; }

  /// <summary>
  /// Filters to colorless objects (those with no colors at all). Rule 105.1.
  /// Mutually exclusive with <see cref="Colors"/> in practice (a card cannot
  /// be both colorless and have a color).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsColorless { get; init; }

  /// <summary>
  /// Filters to colored objects — those that have one or more colors (CR 105.1:
  /// "There are five colors in the Magic game … Colorless is not a color."). The exact
  /// complement of <see cref="IsColorless"/>: an object passes iff it has at least one of
  /// the five colors. Ugin, the Ineffable: "Destroy target permanent that's one or more
  /// colors." → <c>CardTypes=["permanent"], IsColored=true</c>. Distinct from
  /// <see cref="Colors"/> ("has any of THESE literal colors"): "one or more colors" names
  /// no specific color, only the presence of colour, so it cannot be expressed as a
  /// <see cref="Colors"/> list. Mirrors <see cref="IsColorless"/> as a boolean colour-quality
  /// axis; the two are mutually exclusive.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsColored { get; init; }

  /// <summary>
  /// Filters to a set whose members all have DIFFERENT names from one another — "seven or
  /// more lands with different names" (The Necrobloom: "If you control seven or more lands
  /// with different names, …"). CR 201.2 (two objects have the same name if the English
  /// versions of their names are identical): the "with different names" qualifier constrains
  /// the COUNTED set to distinct-named members, so a <see cref="MagicAST.AST.Abilities.CountCondition"/>
  /// over such a filter counts distinct names rather than raw objects. A set-level uniqueness
  /// predicate, not a per-object characteristic — distinct from the relational
  /// <see cref="SharesNameWith"/>/<see cref="ExcludesNameOf"/> axes (which compare one object
  /// against a specific referent): this asserts pairwise-distinctness across the whole matched
  /// population, which no per-object or relational axis can express.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? DifferentNames { get; init; }

  /// <summary>
  /// A cross-axis DISJUNCTION — the object matches this filter if it matches ANY of the
  /// listed sub-filters (in addition to the base axes on this filter, which are ANDed as
  /// usual). The structured home for an "[A] or [B]" phrase whose alternatives live on
  /// DIFFERENT axes and so cannot be folded into a single multi-valued axis: "creature or
  /// Vehicle" is a card-type (<see cref="CardTypes"/>) OR a subtype (<see cref="Subtypes"/>)
  /// — <c>AnyOf=[{CardTypes:["creature"]}, {Subtypes:["Vehicle"]}]</c> (Silken Strength,
  /// Swift Reconfiguration, Broodheart Engine's "creature or Vehicle card"). Distinct from a
  /// multi-valued <see cref="CardTypes"/> list (which is itself a disjunction, but only WITHIN
  /// the card-type axis — "artifact or creature"): a sub-filter here may set any axis, so the
  /// disjuncts can straddle the card-type/subtype/colour boundary. Each disjunct is a full
  /// <see cref="ObjectFilter"/> (recursive), evaluated independently; the outer match is the
  /// logical OR over them.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ObjectFilter>? AnyOf { get; init; }

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
  /// Filters to face-down objects — CR 708.1: "Some cards allow spells and permanents
  /// to be face down." A face-down spell or permanent's copiable characteristics are
  /// replaced (typically the 2/2 no-name/no-text default of CR 708.2a) regardless of
  /// its printed card. A named game-state axis, not a card type or subtype: it cannot
  /// be expressed on the existing type axes without falsely asserting "face-down" is a
  /// printed characteristic. Parallels <see cref="IsColorless"/> / <see cref="IsHistoric"/>
  /// as a boolean game-quality axis on the filter (Obscuring Aether: "Face-down creature
  /// spells you cast cost {1} less to cast." → <c>CardTypes=["spell","creature"],
  /// IsFaceDown=true, Controller=You</c>).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsFaceDown { get; init; }

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
  /// Filters to the player's commander — CR 903.3: a designated legendary creature
  /// card is that player's commander. "Your commander" (Road of Return: "Put your
  /// commander into your hand from the command zone."). A named, format-defined game
  /// quality (CR 903, the Commander variant), not a card type, subtype, or supertype:
  /// it cannot be expressed on the existing type axes without falsely asserting
  /// "Commander" is a printed characteristic (a legendary-creature card gains the
  /// status by designation, not by printing). Parallels <see cref="IsHistoric"/>
  /// (CR 700.6) and <see cref="InParty"/> (CR 700.8) as a boolean CR 700/900-level
  /// game-quality axis on the filter; used with <see cref="Owner"/>/<see cref="Zone"/>
  /// ("your commander … from the command zone" → <c>IsCommander=true, Owner=You,
  /// Zone=CommandZone</c>).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsCommander { get; init; }

  /// <summary>
  /// Mana-cost symbols the object's mana cost must CONTAIN — a contains-any set over
  /// the printed mana-cost symbols (CR 107.3 / 202.1). Gaddock Teeg: "Noncreature
  /// spells with {X} in their mana costs can't be cast." → <c>ManaCostSymbols=["X"]</c>.
  /// Each entry is a symbol as printed inside the braces — <c>"X"</c> for the {X}
  /// variable placeholder (CR 107.3), a colour letter for a coloured symbol (CR 107.4),
  /// etc. A filter matches an object whose mana cost includes at least one of the listed
  /// symbols. Distinct from <see cref="ManaValueComparison"/> (which constrains the numeric
  /// mana VALUE, CR 202.3, not the presence of a particular symbol) and from
  /// <see cref="Colors"/> (colour is derived from the cost but is a separate
  /// characteristic, CR 105.2): a {X} symbol contributes 0 to mana value and no colour,
  /// so its presence is expressible on neither of those axes.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? ManaCostSymbols { get; init; }

  /// <summary>
  /// Ordered-zone positional restriction — "the top card of [a] library", "the top six
  /// cards of your library" (CR 401.1: the cards in a library are a single ordered pile a
  /// player can't reorder). Carries which end of the ordered zone the cards are taken from
  /// (<see cref="ZonePosition.Top"/>/<see cref="ZonePosition.Bottom"/>) and, optionally,
  /// how many from that end (<see cref="LibraryPosition.Count"/> for "the top SIX cards").
  /// Used with <see cref="Zone.Library"/>. A first-class ordering axis — position is a
  /// property of the ordered pile, not of the card, so "the top card" cannot be expressed
  /// on any of the flat characteristic axes: the same "top" residual appeared on Mystic
  /// Forge, Fathom Feeder, and Demonic Consultation, and the axis generalizes to the
  /// scry/surveil/impulse-draw tail ("look at the top N cards of your library").
  /// Descriptive-only: MAST records the positional designation, not the runtime library
  /// order.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public LibraryPosition? LibraryPosition { get; init; }

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
  /// "that's attacking you" — the combat-defender axis: filters to attacking creatures
  /// whose declared defending player is the ability's controller (Giant Trap Door Spider:
  /// "target creature without flying that's attacking you"). CR 508.1a: each attacking
  /// creature is declared as attacking a player, planeswalker, or battle; "attacking you"
  /// is the subset whose declared defender is this controller (CR 508.1b, defending
  /// player). A named combat-state predicate on the object, not a card characteristic: it
  /// cannot be expressed on the type/keyword axes (an attacker is any creature; the axis
  /// asserts WHOM it is attacking). Flat boolean axis mirroring
  /// <see cref="IsEquipped"/> / <see cref="IsEnchanted"/> / <see cref="IsFaceDown"/> — the
  /// first-class home for the "attackingYou" combat-defender residual (ADR 0001), keyed to
  /// the controller as the standard "you" defender; a different defender ("attacking a
  /// player", "attacking one of your opponents") would earn its own axis. Descriptive-only:
  /// MAST records the declared-defender predicate, not the runtime attack assignment.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsAttackingYou { get; init; }

  /// <summary>
  /// Tapped-status axis: <c>true</c> matches only tapped objects ("a tapped creature",
  /// CR 110.6a), <c>false</c> only untapped ones. Null when the oracle text does not
  /// qualify tapped-ness. A permanent's tapped status (CR 110.6: "A permanent is either
  /// tapped or untapped.") is a game state, not a card characteristic, so it is a
  /// separate boolean axis mirroring <see cref="IsFaceDown"/> / <see cref="IsAttackingYou"/>
  /// rather than a type/keyword entry — the <see cref="ObjectFilter"/>-side analogue of
  /// the <see cref="MagicAST.AST.Abilities.ObjectStatusCondition"/> status predicate.
  /// Quicksand Whirlpool: "this spell costs {2} less to cast if it targets a tapped
  /// creature" → a <see cref="MagicAST.AST.Abilities.TargetsFilterCondition"/> whose
  /// filter is <c>{CardTypes:["creature"], IsTapped:true}</c>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsTapped { get; init; }

  /// <summary>
  /// Additional characteristic constraints beyond the structured axes above —
  /// a keyword-ability requirement (<see cref="KeywordCharacteristic"/>) or the
  /// typed residual (<see cref="OtherCharacteristic"/>) for shapes not yet
  /// structured. Replaces the former bare-string list (see ADR 0001).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<Characteristic>? Characteristics { get; init; }

  /// <summary>
  /// Keyword abilities the filtered object must NOT have — "creatures without
  /// flying" (Moat: "Creatures without flying can't attack.", CR 702.9 flying).
  /// The negation axis parallel to the positive "has [keyword]" predicate carried
  /// by <see cref="KeywordCharacteristic"/> inside <see cref="Characteristics"/>,
  /// mirroring how <see cref="ExcludedCardTypes"/>/<see cref="ExcludedSubtypes"/>/
  /// <see cref="ExcludedColors"/>/<see cref="ExcludedSupertypes"/> each pair with
  /// their positive counterpart axis. Distinct from the typed residual
  /// <see cref="OtherCharacteristic"/> ("withoutFlying" free-text description)
  /// some existing rules emit as a deliberate scope deferral (ADR 0001) — this is
  /// the first-class structured predicate for the "without [keyword]" shape.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<KeywordAbility>? LacksKeywords { get; init; }

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
  /// Constraint on how many targets a spell/ability on the stack has — "target spell
  /// with a single target" (Divert: <c>TargetCountComparison = Equal 1</c>; CR 115).
  /// A spell "with a single target" is one that has exactly one target (CR 115.1a: a
  /// spell's targets are chosen as it is cast). Mirrors <see cref="PowerComparison"/>/
  /// <see cref="ManaValueComparison"/> as a <see cref="Comparison"/>-valued axis, but
  /// ranges over the object's target COUNT rather than a printed characteristic — the
  /// count of objects the spell is targeting, a stack-time property of the object,
  /// recorded descriptively (the engine tracks the actual targets). Distinct from
  /// <see cref="MagicAST.AST.References.ObjectReference.Quantity"/> (how many objects
  /// THIS reference selects): this constrains the target count of the referenced spell,
  /// not the cardinality of the reference to it.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? TargetCountComparison { get; init; }

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
  /// Relational axis: the object (a spell/ability on the stack) must TARGET the
  /// referenced object — Heroic's "a spell that targets this creature" (Triton
  /// Fortune Hunter: <c>Targets = Self</c>; CR 115, CR 702.85). The referent is an
  /// <see cref="ObjectReference"/> (Heroic's "this creature" = <c>Self</c>). Mirrors
  /// <see cref="AttachedTo"/> (an <see cref="ObjectReference"/>-valued relational
  /// predicate): where AttachedTo relates an object to what it is attached to, this
  /// relates a spell to what it targets. Descriptive-only per ADR 0004
  /// reference-not-resolution — MAST records that the spell's target set includes the
  /// referent, not the runtime target choice. Distinct from a target-COUNT constraint
  /// (<see cref="TargetCountComparison"/>): this names WHICH object is targeted, that
  /// counts HOW MANY.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Targets { get; init; }

  /// <summary>
  /// Relational exclusion axis: the filtered set EXCLUDES the referenced object —
  /// "each other opponent" (Grenzo's Ruffians: it deals combat damage to an opponent,
  /// then "deals that much damage to each other opponent", <c>Excludes = ThatPlayer</c>;
  /// CR 109.5, "other" means other than the named object). The referent is an
  /// <see cref="ObjectReference"/> — here <c>ThatPlayer</c>, the opponent just dealt
  /// combat damage (a runtime-bound referent established earlier in the ability), NOT
  /// the ability's own source. Generalizes the boolean <see cref="ExcludeSelf"/> (whose
  /// implicit referent is fixed to the source object) to an arbitrary referent:
  /// where <see cref="ExcludeSelf"/> omits <em>self</em>, this omits whatever object the
  /// <see cref="ObjectReference"/> names. An <see cref="ObjectReference"/>-valued predicate,
  /// mirroring <see cref="AttachedTo"/>/<see cref="Targets"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Excludes { get; init; }

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
/// A positional restriction within an ordered zone — "the top card of your library",
/// "the top six cards of your library" (CR 401.1: a library is a single ordered pile).
/// Carries which <see cref="ZonePosition"/> end the cards are taken from and, optionally,
/// how many from that end. The structured home for the "top"/"bottom" positional residual
/// (used with <see cref="ObjectFilter.LibraryPosition"/> and <see cref="Zone.Library"/>).
/// </summary>
public sealed record LibraryPosition
{
  /// <summary>Which end of the ordered zone — <see cref="ZonePosition.Top"/> or <see cref="ZonePosition.Bottom"/>.</summary>
  public required ZonePosition Position { get; init; }

  /// <summary>
  /// How many cards from that end ("the top SIX cards" → <c>Count = 6</c>). Null for the
  /// singular "the top card" (a single card at that end). Descriptive of the positional
  /// block named by the oracle text; distinct from an effect's own quantity, which counts
  /// how many objects the effect acts on.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Count { get; init; }
}

/// <summary>
/// Which end of an ordered zone a <see cref="LibraryPosition"/> designates (CR 401.1).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ZonePosition
{
  /// <summary>"the top card of [a] library" — the card(s) at the top of the ordered pile.</summary>
  Top,

  /// <summary>"the bottom card of [a] library" — the card(s) at the bottom of the ordered pile.</summary>
  Bottom,
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
