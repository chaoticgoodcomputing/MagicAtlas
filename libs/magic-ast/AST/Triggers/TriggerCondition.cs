namespace MagicAST.AST.Triggers;

using System.Text.Json.Serialization;
using MagicAST.AST.References;

/// <summary>
/// Represents the trigger condition for a triggered ability.
/// Rule 603
/// </summary>
public sealed record TriggerCondition
{
  /// <summary>
  /// The timing word: When, Whenever, or At.
  /// </summary>
  public required TriggerTiming Timing { get; init; }

  /// <summary>
  /// The event that causes this to trigger.
  /// </summary>
  public required TriggerOccurrence Event { get; init; }

  /// <summary>
  /// Optional filter for objects involved in the trigger.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// The mana symbol produced when this trigger fires — used for "whenever you tap a permanent
  /// for {C}" triggers (CR 605.1a) where the trigger event is <see cref="TriggerEvent.TapsForMana"/>
  /// and the produced mana constrains which tapping events match. Null for all other trigger events.
  /// Follows the same mana-symbol-string convention as <see cref="MagicAST.AST.Effects.Resource.AddManaEffect.Mana"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ProducedMana { get; init; }

  /// <summary>
  /// Optional ordinal qualifier selecting which occurrence of the trigger event
  /// (counted within the window described by <see cref="PerTurn"/>) actually fires
  /// the ability. e.g. "you draw your <i>second</i> card each turn" → <c>Ordinal = 2</c>.
  /// When null, every matching occurrence triggers (the default for most triggers).
  /// </summary>
  /// <remarks>
  /// This is a descriptive datum, not a runtime counter: MAST records <i>which</i>
  /// occurrence the oracle text names, leaving the per-turn tally to the engine.
  /// Rule 603.2 — the event-match is the trigger; the ordinal narrows which match counts.
  /// </remarks>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Ordinal { get; init; }

  /// <summary>
  /// True when the trigger's <see cref="Ordinal"/> is counted on a per-turn basis
  /// (the "each turn" qualifier — e.g. "your second card <i>each turn</i>"), so the
  /// occurrence count conceptually resets at turn boundaries. Descriptive only;
  /// the reset mechanics are engine territory.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? PerTurn { get; init; }

  /// <summary>
  /// True when this draw trigger excludes the first card drawn in each of the
  /// triggering player's draw steps — the "except the first one they draw in each
  /// of their draw steps" qualifier on Orcish Bowmasters (CR 121.1: "a player draws
  /// a card"; CR 603.2: each matching draw event fires the ability, but this
  /// qualifier narrows the match to non-first-draw-step draws). Descriptive only;
  /// the per-step first-draw exclusion is engine territory.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ExceptFirstDrawStep { get; init; }

  /// <summary>
  /// The specific counter type that must be placed for a <see cref="TriggerEvent.CounterPlaced"/>
  /// or <see cref="TriggerEvent.CounterRemoved"/> event to match — e.g. <c>"-1/-1"</c>
  /// for Nest of Scarabs ("whenever you put one or more -1/-1 counters on a creature").
  /// Null when the trigger fires on any counter type.
  ///
  /// <para>
  /// CR 122.1: "A counter is a marker placed on an object or player that modifies its
  /// characteristics and/or interacts with a rule or effect." The counter type is a named
  /// quality of the counter ("+1/+1", "-1/-1", "charge", "loyalty", etc.). This field
  /// narrows the event match so that only counter-placement events involving the stated type
  /// trigger the ability — a <see cref="TriggerEvent.CounterPlaced"/> trigger without this
  /// field fires on any counter type (e.g. "+1/+1 or charge counters").
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? CounterType { get; init; }

  /// <summary>
  /// The minimum number of counters that must be placed in the triggering event for
  /// a <see cref="TriggerEvent.CounterPlaced"/> (or <see cref="TriggerEvent.CounterRemoved"/>)
  /// event to match — e.g. <c>1</c> for Nest of Scarabs ("one or more -1/-1 counters").
  /// Null when no minimum-quantity qualifier appears in the oracle text (which typically means
  /// any single counter placement triggers).
  ///
  /// <para>
  /// "One or more" in oracle text is a CR 122.1 quantity constraint on the event: the ability
  /// fires whenever the triggering action places at least this many counters in a single
  /// event. Descriptive only; the per-event count is engine territory.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? MinimumCount { get; init; }
}

/// <summary>
/// The timing word that starts a triggered ability.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerTiming
{
  /// <summary>"When" - triggers once</summary>
  When,

  /// <summary>"Whenever" - triggers each time</summary>
  Whenever,

  /// <summary>"At" - triggers at a specific time</summary>
  At,
}

/// <summary>
/// Categories of trigger events.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TriggerEvent
{
  // Zone change triggers
  /// <summary>A permanent enters the battlefield</summary>
  Enters,

  /// <summary>A permanent dies (creature goes to graveyard from battlefield)</summary>
  Dies,

  /// <summary>A permanent leaves the battlefield</summary>
  LeavesTheBattlefield,

  /// <summary>A card is put into a graveyard</summary>
  PutIntoGraveyard,

  /// <summary>A card is exiled</summary>
  Exiled,

  // Combat triggers
  /// <summary>A creature attacks</summary>
  Attacks,

  /// <summary>
  /// A creature attacks an opponent specifically (not a planeswalker or battle).
  /// CR 508.1b: if the game allows attacking multiple players, the active player
  /// announces which player each creature is attacking. "Whenever [creature]
  /// attacks an opponent" fires only when the declared attack target is a player
  /// who is an opponent (CR 102.2), not a planeswalker or battle they control.
  /// Distinct from <see cref="Attacks"/> (generic attack, any legal target) — the
  /// opponent-specificity is a named part of the oracle text and a cluster axis
  /// (e.g. Kaalia of the Vast). CR 508 (Declare Attackers Step).
  /// </summary>
  AttacksAnOpponent,

  /// <summary>
  /// A creature is exerted by its controller (CR 701.43 — Exert keyword action).
  /// "When you do" after "you may exert [this creature] as it attacks" fires on this
  /// event. The linked ability (CR 607.2h) uses this trigger event to match the
  /// specific exert action that triggered it.
  /// </summary>
  Exerted,

  /// <summary>A creature blocks</summary>
  Blocks,

  /// <summary>A creature attacks or blocks (combined trigger — Rule 508/509)</summary>
  AttacksOrBlocks,

  /// <summary>A creature becomes blocked</summary>
  BecomesBlocked,

  /// <summary>
  /// A creature blocks or becomes blocked (combined trigger — Rule 702.45 Bushido).
  /// "Whenever this creature blocks or becomes blocked" fires both when the creature
  /// is declared as a blocker (Rule 509) AND when the creature is blocked by a blocker
  /// (the attacking creature's perspective). Distinct from <see cref="AttacksOrBlocks"/>
  /// (which covers the attacker or blocker declaring phase) and from the two individual
  /// events <see cref="Blocks"/> and <see cref="BecomesBlocked"/>.
  /// </summary>
  BlocksOrBecomesBlocked,

  /// <summary>A creature deals combat damage</summary>
  DealsCombatDamage,

  /// <summary>A creature deals combat damage to a player</summary>
  DealsCombatDamageToPlayer,

  /// <summary>
  /// A creature deals combat damage to a player or planeswalker — the broader
  /// "deals combat damage to a player or planeswalker" trigger condition used on
  /// Equipment and creatures (The Reaver Cleaver, Sword of Sinew and Steel).
  /// Rule 510 (Combat Damage Step): combat damage may be assigned to both players
  /// and planeswalkers. Rule 603.2 (triggered abilities): the event fires whenever
  /// either recipient class takes combat damage from the triggering source.
  /// Distinct from <see cref="DealsCombatDamageToPlayer"/> (player only) and
  /// <see cref="DealsCombatDamage"/> (recipient unspecified).
  /// </summary>
  DealsCombatDamageToPlayerOrPlaneswalker,

  /// <summary>
  /// A creature deals combat damage to a creature — Rule 510.1 (combat damage
  /// assignment) / Rule 603.2 (the event-match is the trigger). The recipient
  /// class is a creature rather than a player; the Filter carries the subject
  /// (what is dealing the damage). Distinct from
  /// <see cref="DealsCombatDamageToPlayer"/> (player recipient) and
  /// <see cref="DealsCombatDamage"/> (recipient unspecified).
  /// </summary>
  DealsCombatDamageToCreature,

  /// <summary>A source deals damage (any damage, not only combat) — Rule 120</summary>
  DealsDamage,

  /// <summary>
  /// A creature deals any damage (combat or non-combat) to an opponent specifically
  /// (not to any player, creature, or planeswalker). Rule 120 (dealing damage);
  /// Rule 102.2 (opponent = player not on controller's team). Distinct from
  /// <see cref="DealsCombatDamageToPlayer"/> (combat-only) and <see cref="DealsDamage"/>
  /// (any damage, unspecified recipient). Used for oracle text such as
  /// "Whenever this creature deals damage to an opponent, [effect]."
  /// </summary>
  DealsDamageToOpponent,

  // Damage triggers
  /// <summary>Damage is dealt</summary>
  DamageDealt,

  /// <summary>Noncombat damage is dealt</summary>
  NoncombatDamageDealt,

  /// <summary>A player is dealt damage</summary>
  PlayerDealtDamage,

  /// <summary>A creature is dealt damage</summary>
  CreatureDealtDamage,

  // Life triggers
  /// <summary>A player gains life</summary>
  GainsLife,

  /// <summary>A player loses life</summary>
  LosesLife,

  // Spell/ability triggers
  /// <summary>A spell is cast</summary>
  SpellCast,

  /// <summary>An ability is activated</summary>
  AbilityActivated,

  /// <summary>An ability triggers</summary>
  AbilityTriggers,

  /// <summary>A spell or ability targets something</summary>
  BecomesTarget,

  // Phase/step triggers — the clock points moved to GameTime (ADR 0002); a
  // time-trigger is now TriggerCondition.Event = TimeOccurrence(GameTime{...}),
  // e.g. "at the beginning of your upkeep" → { Part: Upkeep, Edge: Beginning, Whose: You }.

  // State change triggers
  /// <summary>A permanent becomes tapped</summary>
  BecomesTapped,

  /// <summary>A permanent becomes untapped</summary>
  BecomesUntapped,

  /// <summary>A Vehicle becomes crewed (Rule 702.122)</summary>
  BecomesCrewed,

  /// <summary>A permanent is turned face up (Rule 702.37 — Morph/Megamorph flip)</summary>
  TurnedFaceUp,

  /// <summary>A permanent transforms</summary>
  Transforms,

  /// <summary>
  /// A creature mutates (Rule 702.140 Mutate). Fires when a permanent
  /// is successfully placed over or under a non-Human creature the
  /// controller owns via a mutate cost payment.
  /// </summary>
  Mutates,

  // Counter triggers
  /// <summary>A counter is placed on a permanent</summary>
  CounterPlaced,

  /// <summary>A counter is removed from a permanent</summary>
  CounterRemoved,

  // Card draw triggers
  /// <summary>A player draws a card</summary>
  DrawsCard,

  /// <summary>A player discards a card</summary>
  DiscardsCard,

  // Other
  /// <summary>A player sacrifices a permanent</summary>
  Sacrifices,

  /// <summary>A token is created</summary>
  TokenCreated,

  /// <summary>A player searches their library</summary>
  SearchesLibrary,

  /// <summary>A player scries</summary>
  Scries,

  /// <summary>A player surveils</summary>
  Surveils,

  /// <summary>A player scries or surveils (combined trigger)</summary>
  ScryOrSurveil,

  /// <summary>
  /// A permanent enters the battlefield or dies (combined trigger).
  /// Rule 603: "When this creature enters or dies, [effect]."
  /// Both zone-change events (battlefield entry and battlefield→graveyard) share a
  /// single triggered ability; the ability triggers on whichever event occurs first
  /// on each occurrence.
  /// </summary>
  EntersOrDies,

  /// <summary>
  /// The controlling player transitions to controlling no lands of a given basic-land subtype
  /// (e.g., "When you control no Islands"). Rule 603 triggered ability; the Filter carries the
  /// land subtype (Subtypes=["Island"]) and controller (You).
  /// </summary>
  ControlNoLandType,

  /// <summary>
  /// Compound Haunt trigger: fires both when this creature enters the battlefield
  /// and when the creature it haunts dies (Rule 702.55). Unique to the Haunt
  /// mechanic — oracle text: "When this creature enters or the creature it haunts
  /// dies, [effect]." Descriptive record of the compound trigger condition; the
  /// dual-event semantics are engine territory.
  /// </summary>
  EntersOrHauntedCreatureDies,

  /// <summary>
  /// A player taps a permanent as part of activating a mana ability that produces a specific mana
  /// type (e.g. "Whenever you tap a permanent for {C}"). CR 605.1a: an activated ability is a mana
  /// ability if it could add mana to a player's mana pool when it resolves and doesn't require a
  /// target. The companion field <see cref="TriggerCondition.ProducedMana"/> carries the mana symbol
  /// ({C} for colorless mana) so the trigger can be distinguished from tapping for any other type.
  /// Distinct from <see cref="BecomesTapped"/> (which fires on any tapping, regardless of mana context).
  /// </summary>
  TapsForMana,

  /// <summary>
  /// A player commits a crime — casts a spell, activates an ability, or puts a triggered
  /// ability on the stack that targets at least one opponent, a permanent/spell/ability an
  /// opponent controls, and/or a card in an opponent's graveyard (CR 700.13).
  /// Oracle text: "Whenever you commit a crime, …"
  /// </summary>
  CommitsACrime,

  /// <summary>Unrecognized trigger event</summary>
  Other,
}
