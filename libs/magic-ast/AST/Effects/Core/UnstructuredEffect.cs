namespace MagicAST.AST.Effects.Core;

using MagicAST.AST;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// L1 residual — an effect POSITION the parser recognised (it sits inside a
/// classified ability shell, i.e. an already-parsed trigger/cost) but whose
/// interior it has not yet structured.
///
/// <para>
/// This is the keystone of the fidelity ladder. Unlike <see cref="UnparsedEffect"/>
/// — an <see cref="IUnparsed"/> hole that fails triage and is banned from gold
/// fixtures — <c>UnstructuredEffect</c> is an <see cref="IResidual"/> under the
/// free-text doctrine (ADR 0001): the loss is <b>accounted for</b> (its
/// <see cref="Text"/> is counted in residual debt, and it is permitted in golds
/// via the free-text whitelist), not a silent drop. A clause that would otherwise
/// collapse to a whole-ability <c>UnparsedAbility</c> (fidelity L0) instead lands
/// as its real shell — a <c>TriggeredAbility</c> with its parsed trigger, an
/// <c>ActivatedAbility</c> with its parsed cost — carrying this node for the
/// unstructured interior, which is fidelity L1: structurally anchored, interior
/// deferred, zero silent loss.
/// </para>
/// </summary>
[OracleEffect("unstructured")]
public sealed record UnstructuredEffect : Effect, IResidual
{
  /// <summary>The raw effect text held verbatim at the frontier (the deferred interior).</summary>
  [FreeTextField]
  public required string Text { get; init; }

  /// <summary>
  /// Location of this residual in the original oracle text — used to attribute it
  /// back to its oracle line(s) and to account its character mass against the
  /// card's total (the residual-mass fidelity metric).
  /// </summary>
  public required TextSpan SourceSpan { get; init; }
}
