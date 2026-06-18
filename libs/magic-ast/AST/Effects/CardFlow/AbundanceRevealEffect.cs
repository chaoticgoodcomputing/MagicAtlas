namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Abundance's "choose land or nonland and reveal cards from the top of your library
/// until you reveal a card of the chosen kind. Put that card into your hand and put
/// all other cards revealed this way on the bottom of your library in any order."
///
/// <para>
/// This is a single atomic action specific to the Abundance draw-replacement family:
/// the controller first declares whether they want a land or nonland card, then reveals
/// cards from the top of their library one at a time until the first card of the chosen
/// kind is found. That card goes to the controller's hand; all other revealed cards go
/// to the bottom of the library in player-chosen order (any order, not a random order).
/// </para>
///
/// <para>
/// "In any order" (player-chosen ordering of the remainder) distinguishes this from
/// <see cref="RevealUntilEffect"/> (which always places the remainder on the bottom in
/// a random order, per CR 400.4). The two shapes are distinct oracle templates and must
/// not be collapsed.
/// </para>
///
/// <para>
/// The "choose land or nonland" step is baked into this effect's discriminator: the binary
/// land/nonland choice is the only dimension of the filter, and it is made by the controller
/// at resolution — unlike <see cref="RevealUntilEffect"/>, where the filter is a static
/// <see cref="ObjectFilter"/> printed on the card. MAST records the instruction to choose,
/// not the chosen value (descriptive-not-engine, ADR 0004).
/// </para>
///
/// <para>
/// The two-sentence oracle text is one coupled action — "a card of the chosen kind" and
/// "all other cards revealed this way" in the second sentence are back-references to the
/// first. This MUST NOT be decomposed into separate effects.
/// </para>
///
/// <para>
/// CR 614.11 (draw replacement effects); CR 701.12 (reveal); CR 400.4 (random order),
/// compared with the Abundance template which says "in any order" (player-chosen).
/// </para>
/// </summary>
[OracleEffect("abundanceReveal")]
public sealed record AbundanceRevealEffect : Effect
{
  /// <summary>
  /// The player performing the reveal — typically <c>{ Kind: "You" }</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
