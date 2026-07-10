namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents an activated ability: "[Cost]: [Effect.] [Instructions]"
/// Rule 113.3b, Rule 602
/// </summary>
[OracleAbility("activated")]
public sealed record ActivatedAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Activated;

  /// <summary>
  /// The costs that must be paid to activate this ability.
  /// Everything before the colon.
  /// </summary>
  public required IReadOnlyList<Cost> Costs { get; init; }

  /// <summary>
  /// The effects that occur when this ability resolves.
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Restrictions on when/how this ability can be activated.
  /// e.g., "Activate only as a sorcery", "Activate only once each turn"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ActivationRestriction>? Restrictions { get; init; }

  /// <summary>
  /// Who is permitted to activate this ability. Null/omitted means the default:
  /// only the object's controller (or owner, if it lacks a controller) may activate
  /// it — CR 602.2. Set to <see cref="ActivationPermission.AnyPlayer"/> when the
  /// object "specifically says otherwise" (CR 602.2), e.g. "Any player may activate
  /// this ability." This is a permission BROADENING, distinct from
  /// <see cref="ActivationRestriction"/> (which narrows who/when may activate).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ActivationPermission? WhoMayActivate { get; init; }

  /// <summary>
  /// A structured game-state predicate that must be true to activate this ability.
  /// "Activate only if [condition]" — e.g., "Activate only if you control three or
  /// more artifacts" (Mox Opal / Metalcraft). Uses the same <see cref="Condition"/>
  /// union as <see cref="MagicAST.AST.Abilities.StaticAbility.Condition"/> so the
  /// count-gate is structured, not free-texted into <see cref="Restrictions"/>.
  /// CR 602.5c (activation restrictions in oracle text).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? ActivationCondition { get; init; }

  /// <summary>
  /// True if this is a mana ability (doesn't use the stack).
  /// Rule 605
  /// </summary>
  public bool IsManaAbility { get; init; }

  /// <summary>
  /// For loyalty abilities, the loyalty cost (+N, -N, or 0).
  /// Null for non-loyalty abilities.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? LoyaltyCost { get; init; }
}

/// <summary>
/// Restrictions on when an activated ability can be used.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivationRestriction
{
  /// <summary>"Activate only as a sorcery"</summary>
  OnlyAsSorcery,

  /// <summary>"Activate only as an instant" — CR 602.5a: you can activate an ability any time you
  /// could cast an instant unless the ability's text states otherwise. "Activate only as an instant"
  /// is the inverse of "Activate only as a sorcery": the ability can be activated at instant speed
  /// (during any player's turn, in response to spells/abilities). CR numbers per orchestrator brief.</summary>
  OnlyAsInstant,

  /// <summary>"Activate only during your turn"</summary>
  OnlyDuringYourTurn,

  /// <summary>"Activate only during your upkeep." — a step-scoped timing restriction
  /// (CR 602.5: "A player can't begin to activate an ability that's prohibited from
  /// being activated."). Strictly narrower than <see cref="OnlyDuringYourTurn"/>: the
  /// ability may be activated only during the upkeep step of the controller's turn
  /// (Magus of the Mirror).</summary>
  OnlyDuringYourUpkeep,

  /// <summary>"Activate only once each turn"</summary>
  OnlyOnceEachTurn,

  /// <summary>"Activate only if you control no untapped lands"</summary>
  OnlyIfNoUntappedLands,

  /// <summary>"Activate only during combat." — a phase-scoped timing restriction (CR 602.5:
  /// "A player can't begin to activate an ability that's prohibited from being activated.").
  /// The ability may be activated only while a combat phase is in progress (CR 506 — the
  /// combat phase), regardless of whose turn it is (Najeela, the Blade-Blossom). Narrower than
  /// <see cref="OnlyDuringYourTurn"/> (a turn-scoped restriction) along the phase axis and
  /// orthogonal to the player axis.</summary>
  OnlyDuringCombat,

  /// <summary>"Activate only once." — the Exhaust keyword constraint (CR 702.177a:
  /// "'Exhaust — [Cost]: [Effect]' means '[Cost]: [Effect]. Activate only once.'"
  /// Unlike <see cref="OnlyOnceEachTurn"/> (which resets each turn), an ability with
  /// this restriction can never be activated again once used.</summary>
  OnlyOnce,

  /// <summary>Other restriction captured as raw text</summary>
  Other,
}

/// <summary>
/// Who may activate an activated ability. CR 602.2: "Only an object's controller
/// (or its owner, if it doesn't have a controller) can activate its activated
/// ability unless the object specifically says otherwise." CR 602.1: activated
/// abilities are written as "[Cost]: [Effect.] [Activation instructions (if any).]" —
/// this enum models the "Activation instructions" slot for permission-broadening
/// text like "Any player may activate this ability."
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivationPermission
{
  /// <summary>Default: only the controller (or owner) may activate — CR 602.2.</summary>
  Controller,

  /// <summary>"Any player may activate this ability." — CR 602.2's "unless the
  /// object specifically says otherwise" branch.</summary>
  AnyPlayer,

  /// <summary>"Only your opponents may activate this ability." — CR 602.2's
  /// "unless the object specifically says otherwise" branch, narrowed to the
  /// controller's opponents rather than broadened to any player (Detention
  /// Vortex).</summary>
  Opponent,
}
