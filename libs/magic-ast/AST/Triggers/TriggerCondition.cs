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
  /// The span in the card's oracle text that produced this trigger condition — the
  /// TRIGGER-half region of a triggered ability (before the resolution comma).
  /// Clause-accurate provenance (upstream-atlas-data-plan §4): the consume ports the
  /// port graph projects from this trigger trace back to exactly this substring.
  /// <c>null</c> when the parser cannot attribute a boundary (e.g. no comma split);
  /// never fabricated. Serialized when non-null (the global <c>WhenWritingNull</c>
  /// policy), matching <see cref="MagicAST.AST.Abilities.Ability.SourceSpan"/>.
  /// </summary>
  public MagicAST.AST.TextSpan? SourceSpan { get; init; }

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
  /// Result threshold for a die-roll trigger (<see cref="TriggerEvent.DiceRolled"/>): the minimum roll
  /// result that fires the ability — "whenever you roll a 4 or higher" → <c>DieResultThreshold = 4</c>
  /// (Mr. House, President and CEO). Null means the trigger fires on any roll ("whenever you roll one or
  /// more dice"). Descriptive only; the runtime compares the actual result. CR 706.3.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? DieResultThreshold { get; init; }

  /// <summary>
  /// Exact die-result value(s) that fire a die-roll trigger (<see cref="TriggerEvent.DiceRolled"/>):
  /// the ability fires only when the roll's result equals one of these values — "whenever you roll a 1"
  /// → <c>DieResultValues = [1]</c> (Complaints Clerk); "whenever you roll a 1 or 2"
  /// → <c>DieResultValues = [1, 2]</c> (Atomwheel Acrobats). Distinct from <see cref="DieResultThreshold"/>
  /// (a <i>minimum</i>, "a 4 or higher") — this is an exact match against an enumerated set, not a lower
  /// bound. Null means the trigger fires on any roll, or uses the threshold form instead.
  ///
  /// <para>
  /// CR 706.2: "the final number is the result of the die roll." A specific-value trigger compares that
  /// result to the named value(s) (CR 706.7 references comparing "the results of that roll … to a given
  /// number"). Descriptive only; the runtime compares the actual result. Mutually exclusive with
  /// <see cref="DieResultThreshold"/> — a roll trigger carries at most one result qualifier.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<int>? DieResultValues { get; init; }

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

  /// <summary>
  /// True when the trigger event only counts if it occurs during the
  /// ability's controller's own turn — "during your turn" (Thran Vigil:
  /// "Whenever one or more artifact and/or creature cards leave your
  /// graveyard <i>during your turn</i>, ..."). This narrows the WINDOW in
  /// which the event must occur for the ability to trigger.
  ///
  /// <para>
  /// Distinct from <see cref="PerTurn"/> (which scopes an <see cref="Ordinal"/>
  /// reset, not the event window) and from
  /// <see cref="MagicAST.AST.Abilities.TriggeredAbilityRestriction.OnlyDuringYourTurn"/>
  /// (a restriction carried by a trailing standalone sentence AFTER the
  /// resolution clause — CR 603.2h, "Do this only during your turn.") — here
  /// the qualifier is grammatically part of the trigger condition clause
  /// itself, so it lives on the condition rather than on the ability.
  /// </para>
  ///
  /// <para>
  /// CR 603.2: "Whenever a game event or game state matches a triggered
  /// ability's trigger event, that ability automatically triggers." This
  /// field narrows which occurrences of the event match. Null when no turn
  /// qualifier is present (the default: any occurrence of the event
  /// triggers, regardless of whose turn it is).
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? DuringYourTurn { get; init; }
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

  /// <summary>
  /// One or more creatures of a specific subtype deal any damage (combat or non-combat)
  /// to multiple opponents simultaneously (the plural "your opponents" form).
  /// Rule 120 (dealing damage); Rule 102.2 (opponent = player not on controller's team).
  /// Distinct from <see cref="DealsDamageToOpponent"/> (singular "an opponent" / single source).
  /// Used for oracle text such as "Whenever one or more Pirates you control deal damage
  /// to your opponents, [effect]." The Filter carries the subtype group (e.g. Pirate) and
  /// controller (You).
  /// Rule 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
  /// players. This is generally detrimental to the object or player that receives that
  /// damage. An object that deals damage is the source of that damage."
  /// Rule 603.2: "Whenever a game event or game state matches a triggered ability's
  /// trigger event, that ability automatically triggers."
  /// </summary>
  DealsDamageToOpponents,

  // Damage triggers
  /// <summary>Damage is dealt</summary>
  DamageDealt,

  /// <summary>Noncombat damage is dealt</summary>
  NoncombatDamageDealt,

  /// <summary>A player is dealt damage</summary>
  PlayerDealtDamage,

  /// <summary>A creature is dealt damage</summary>
  CreatureDealtDamage,

  /// <summary>
  /// A creature or planeswalker is dealt excess noncombat damage — i.e., noncombat
  /// damage that exceeds the amount needed to destroy or reduce the target to 0 loyalty
  /// (CR 120.10). Fires on the triggering permanent controlled by the specified controller
  /// (Filter carries the card types and Controller). Toralf, God of Fury is the paradigm
  /// card: "Whenever a creature or planeswalker an opponent controls is dealt excess
  /// noncombat damage, …" (KHM). CR 120.10 (verbatim): "Some triggered abilities check
  /// whether a permanent has been dealt excess damage. These abilities check after the
  /// permanent has been dealt damage by one or more sources. If those sources together
  /// dealt an amount of damage to a creature greater than lethal damage, excess damage
  /// equal to the difference was dealt to that creature."
  /// </summary>
  ExcessNoncombatDamageDealt,

  // Life triggers
  /// <summary>A player gains life</summary>
  GainsLife,

  /// <summary>A player loses life</summary>
  LosesLife,

  // Spell/ability triggers
  /// <summary>A spell is cast</summary>
  SpellCast,

  /// <summary>
  /// A spell is cast OR copied (the Magecraft ability-word trigger — CR 207.2c lists
  /// "magecraft" as an ability word). CR 707.10: "a copy of a spell isn't cast" — so
  /// the union event "cast or copy" is distinct from <see cref="SpellCast"/> alone.
  /// Oracle text: "Whenever you cast or copy an instant or sorcery spell, …"
  /// The <see cref="TriggerCondition.Filter"/> carries the type qualifier
  /// (CardTypes = ["spell", "instant", "sorcery"]) and controller restriction.
  /// </summary>
  CastOrCopySpell,

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

  /// <summary>
  /// A player mills one or more cards (CR 701.17a: "For a player to mill a number of
  /// cards, that player puts that many cards from the top of their library into their
  /// graveyard."). Parallels the other card-flow player-action events
  /// (<see cref="DrawsCard"/>, <see cref="DiscardsCard"/>): the <see cref="TriggerCondition.Filter"/>
  /// carries the milling player (Controller = You for "when you mill"). Primary use is
  /// the reflexive "mill N cards. When you do, …" shape (CR 603.12), where the milling
  /// action is the reflexive trigger's event.
  /// </summary>
  Mills,

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
  /// A creature enters the battlefield or attacks (combined trigger).
  /// Oracle form: "Whenever this creature enters or attacks, [effect]."
  /// Both the ETB zone-change and the attack declaration (CR 508) share a single
  /// triggered ability; the ability triggers on whichever event occurs.
  /// Rule 603 (triggered abilities): the event fires on either matching game event.
  /// </summary>
  EntersOrAttacks,

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

  /// <summary>
  /// A card is cycled by its controller — "When you cycle this card" (CR 702.29c:
  /// "'When you cycle this card' means 'When you discard this card to pay an
  /// activation cost of a cycling ability.' These abilities trigger from whatever
  /// zone the card winds up in after it's cycled.").
  /// CR 702.29 (verbatim): "Cycling is an activated ability that functions only
  /// while the card with cycling is in a player's hand."
  /// The Filter carries IsSelf=true to identify that this card is the one being cycled.
  /// </summary>
  Cycled,

  /// <summary>
  /// A card leaves a player's graveyard — "a creature card leaves your graveyard"
  /// (Syr Konrad, the Grim). Covers any zone change that removes a card from the
  /// graveyard (cast from graveyard, returned to hand, exiled, etc.).
  /// CR 603.2: "Whenever a game event … matches a triggered ability's trigger event,
  /// that ability automatically triggers." The Filter carries card-type and
  /// Controller (You = your graveyard) restrictions.
  /// Distinct from <see cref="LeavesTheBattlefield"/> (which fires when a permanent
  /// moves away from the battlefield) — here the origin zone is the graveyard.
  /// </summary>
  LeavesGraveyard,

  /// <summary>
  /// A player fully unlocks a Room permanent — "whenever you fully unlock a Room"
  /// (CR 709.5i). A Room permanent has a shared type line and two halves (doors);
  /// it is "fully unlocked" when the second of its two unlocked designations is
  /// assigned — either when both halves are cast as spells simultaneously or when
  /// a player pays the unlock cost of the remaining locked half. The Filter carries
  /// the controller restriction (Controller = You).
  /// CR 709.5i: "Some abilities trigger when a player 'fully unlocks' a permanent
  /// with a shared type line. Such an ability triggers when that permanent has one
  /// of the two unlocked designations and gets the other, or when it has neither
  /// designation and gains both."
  /// </summary>
  FullyUnlockRoom,

  /// <summary>
  /// A player rolls one or more dice — "whenever you roll one or more dice" (CR 706, the dice-rolling
  /// rules). A player-action trigger (no object on the battlefield), so the <see cref="TriggerCondition.Filter"/>
  /// is typically null; the controller restriction ("you") is implicit. A result threshold ("roll a 4 or
  /// higher" — Mr. House) is carried as an intervening-if on the triggered ability, not on this event.
  /// This is the dice CONSUMER side: it is what a <c>RollDieEffect</c> (the emitter) satisfies, so the
  /// interaction engine can close a dice loop (roll → this trigger → effect → … → roll again).
  /// </summary>
  DiceRolled,

  /// <summary>
  /// An Aura, Equipment, Fortification, or other attached permanent becomes unattached
  /// from the object or player it was attached to — "Whenever this Equipment becomes
  /// unattached from a permanent, …" (Stitcher's Graft). CR 701.3d (verbatim): "To
  /// 'unattach' an Equipment from a creature means to move it away from that creature
  /// so the Equipment is on the battlefield but is not equipping anything. … If an Aura,
  /// Equipment, or Fortification that was attached to an object or player ceases to be
  /// attached to it, that counts as 'becoming unattached [from that object or player]';
  /// this includes if that Aura, Equipment, or Fortification leaves the battlefield, the
  /// object leaves the zone it was in, or that player leaves the game." The
  /// <see cref="TriggerCondition.Filter"/> carries the object it was attached to (the
  /// "from a permanent" complement), so the effect can resolve the back-reference "that
  /// permanent" (<see cref="MagicAST.AST.References.ObjectReferenceKind.ThatPermanent"/>).
  /// Distinct from the parameterless <see cref="MagicAST.AST.Effects.Modification.UnattachEffect"/>
  /// (an explicit unattach INSTRUCTION) — this is the CONSUMER side, the event a trigger watches for.
  /// </summary>
  BecomesUnattached,

  /// <summary>Unrecognized trigger event</summary>
  Other,
}
