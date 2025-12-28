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
  [JsonPropertyName("timing")]
  public required TriggerTiming Timing { get; init; }

  /// <summary>
  /// The event that causes this to trigger.
  /// </summary>
  [JsonPropertyName("event")]
  public required TriggerEvent Event { get; init; }

  /// <summary>
  /// Optional filter for objects involved in the trigger.
  /// </summary>
  [JsonPropertyName("filter")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }
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

  /// <summary>A creature becomes blocked</summary>
  BecomesBlocked,

  /// <summary>A creature deals combat damage</summary>
  DealsCombatDamage,

  /// <summary>A creature deals combat damage to a player</summary>
  DealsCombatDamageToPlayer,

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

  // Phase/step triggers
  /// <summary>Beginning of upkeep</summary>
  BeginningOfUpkeep,

  /// <summary>Beginning of draw step</summary>
  BeginningOfDrawStep,

  /// <summary>Beginning of precombat main phase (first main phase)</summary>
  BeginningOfPreCombatMainPhase,

  /// <summary>Beginning of postcombat main phase (second main phase)</summary>
  BeginningOfPostCombatMainPhase,

  /// <summary>Beginning of combat</summary>
  BeginningOfCombat,

  /// <summary>Beginning of end step</summary>
  BeginningOfEndStep,

  /// <summary>End of turn</summary>
  EndOfTurn,

  // State change triggers
  /// <summary>A permanent becomes tapped</summary>
  BecomesTapped,

  /// <summary>A permanent becomes untapped</summary>
  BecomesUntapped,

  /// <summary>A permanent transforms</summary>
  Transforms,

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

  /// <summary>Unrecognized trigger event</summary>
  Other,
}
