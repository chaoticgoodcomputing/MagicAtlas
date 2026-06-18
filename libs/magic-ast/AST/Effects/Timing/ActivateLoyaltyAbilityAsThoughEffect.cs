namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "For each planeswalker you control, you may activate one of its loyalty
/// abilities once this turn as though none of its loyalty abilities have been
/// activated this turn."
///
/// <para>
/// CR 606.3: "A player may activate a loyalty ability of a permanent they
/// control any time they have priority and the stack is empty during a main
/// phase of their turn, but only if no player has previously activated a
/// loyalty ability of that permanent that turn."
/// </para>
///
/// <para>
/// This effect grants the controller permission to activate one loyalty
/// ability of each planeswalker identified by <see cref="Target"/> once
/// this turn, bypassing the once-per-turn restriction of CR 606.3 — modelled
/// as the "as though none of its loyalty abilities have been activated this
/// turn" clause. The permission is bounded to one activation per planeswalker
/// per resolution of this ability (the "once this turn" qualifier).
/// </para>
///
/// <para>
/// Cluster axis: The Chain Veil / Carth the Lion / Teferi temporal-reset
/// family. Any card that resets or grants extra loyalty-ability uses lands
/// on this node.
/// </para>
///
/// <para>
/// CR 606.3 (verbatim): "A player may activate a loyalty ability of a
/// permanent they control any time they have priority and the stack is empty
/// during a main phase of their turn, but only if no player has previously
/// activated a loyalty ability of that permanent that turn."
/// </para>
/// </summary>
[OracleEffect("activateLoyaltyAbilityAsThough")]
public sealed record ActivateLoyaltyAbilityAsThoughEffect : Effect
{
  /// <summary>
  /// The planeswalker(s) whose loyalty ability the controller may activate.
  /// Typically "each planeswalker you control" (ForEach / You-controlled filter).
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// How many loyalty-ability activations are granted per planeswalker per
  /// resolution. The oracle text "once this turn" on The Chain Veil encodes
  /// this as 1.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? Count { get; init; }
}
