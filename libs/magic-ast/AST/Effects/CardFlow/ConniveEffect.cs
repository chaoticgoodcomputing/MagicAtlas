namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Connive (Rule 701.50a). "Certain spells and abilities instruct a permanent
/// to connive. To do so, that permanent's controller draws a card, then
/// discards a card. If a nonland card is discarded this way, that player puts
/// a +1/+1 counter on the conniving permanent."
///
/// <para>
/// MAST records the keyword action's invocation and the conniving subject; the
/// draw / discard / conditional-+1/+1-counter sequence is engine territory per
/// the descriptive-not-engine doctrine (mirroring the SurveilEffect pattern —
/// Surveil similarly records only the keyword and count, not the look/put-into-
/// graveyard machinery).
/// </para>
///
/// <para>
/// Filed under <c>CardFlow</c> alongside <see cref="SurveilEffect"/> and
/// <see cref="ExploreEffect"/>: the keyword's primary descriptive axis is
/// draw-then-discard, with the +1/+1 counter being a conditional fallback path.
/// The Counter directory hosts effects whose <em>primary</em> shape is counter
/// placement; here that is conditional and secondary.
/// </para>
/// </summary>
[OracleEffect("connive")]
public sealed record ConniveEffect : Effect
{
  /// <summary>
  /// The permanent doing the conniving. In the canonical ETB trigger the
  /// subject is "it" (the entering creature) — represented by
  /// <see cref="ObjectReference.It"/>.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
