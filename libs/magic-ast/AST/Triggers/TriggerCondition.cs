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

  /// <summary>Unrecognized trigger event</summary>
  Other,
}
