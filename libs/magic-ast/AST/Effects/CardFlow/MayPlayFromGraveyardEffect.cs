namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may play [lands/cards] from your graveyard." — a static permission that
/// allows the controller to play cards matching <see cref="Cards"/> from their
/// graveyard (Crucible of Worlds, Ramunap Excavator).
///
/// <para>
/// CR 305.1: "A player who has priority may play a land card from their hand
/// during a main phase of their turn when the stack is empty. Playing a land is
/// a special action; it doesn't use the stack." This ability extends that
/// permission so the eligible land cards may come from the graveyard rather than
/// (or in addition to) the hand. MAST describes the permission; when and whether
/// the player actually plays a land is engine territory (ADR 0003/0004
/// describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("mayPlayFromGraveyard")]
public sealed record MayPlayFromGraveyardEffect : Effect
{
  /// <summary>
  /// Which cards the controller may play — a graveyard-zone filter that narrows
  /// the eligible cards (e.g. lands only: <c>CardTypes=["land"]</c>).
  /// </summary>
  public required ObjectFilter Cards { get; init; }
}
