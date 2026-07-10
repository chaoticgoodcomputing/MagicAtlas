namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may [Inner]. If you do, [IfYouDo] / if you don't, [IfYouDoNot]." (CR 118.12 —
/// "[A player] may [do something]. If [that player] [does, doesn't, or can't], [effect]").
/// Wrapper presence is the "you may" — no bool. One-shot action effects only (ADR 0005).
///
/// <para>
/// The chooser defaults to the ability's controller ("you"), so <see cref="Chooser"/> is
/// <c>null</c> for the overwhelming majority of "you may …" cards and is omitted from their
/// JSON. It is set only when the oracle names a DIFFERENT decision-maker — "any opponent may
/// have it deal 5 damage to them" (Longhorn Firebeast): the OPPONENT, not the controller,
/// decides whether <see cref="Inner"/> happens. This is load-bearing (CR 118.12 keys the
/// "If [that player] does" branch on the choosing player), so who chooses cannot be inferred
/// from <see cref="Inner"/>'s references when the inner action's source/target are other
/// objects (here the source is "it"/the creature and the recipient is the opponent).
/// </para>
/// </summary>
[OracleEffect("optional")]
public sealed record OptionalEffect : Effect
{
  /// <summary>The effect the controller may choose to perform.</summary>
  public required Effect Inner { get; init; }

  /// <summary>
  /// The player who decides whether to perform <see cref="Inner"/>, when it is NOT the
  /// ability's controller (CR 118.12 "[A player] may …"). Null ≡ the controller ("you may").
  /// Mirrors the separate-chooser concept on <see cref="MagicAST.AST.Effects.CardFlow.DiscardCardsEffect.Chooser"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Chooser { get; init; }

  /// <summary>Effect performed if the controller chooses to perform <see cref="Inner"/>.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Effect performed if the controller chooses not to (CR 117.7 per-player fork).</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }
}
