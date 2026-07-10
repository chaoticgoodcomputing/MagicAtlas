namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Death event: "would die" (CR 700.4 — "dies" means "is put into a graveyard
/// from the battlefield").
/// </summary>
[OracleReplacementEvent("death")]
public sealed record DeathEvent : ReplacementEvent
{
  /// <summary>
  /// The single specific object whose death the replacement watches, when that
  /// object is named by an anaphoric back-reference — "If <em>that creature</em>
  /// would die this turn, exile it instead" (Ob Nixilis's Cruelty). "That creature"
  /// here refers to the object established earlier in the SAME spell's resolution
  /// (the "target creature" the spell already modified), so per the anaphoric-pronoun
  /// convention it maps to <see cref="ObjectReferenceKind.It"/> — the identical
  /// treatment <c>UntapThatCreatureRule</c> gives the "that creature" back-reference.
  ///
  /// <para>
  /// Distinct from the inherited kind-scoped <see cref="ReplacementEvent.AffectedObjects"/>
  /// filter (Incendiary Flow's "a creature dealt damage this way" — a category of objects
  /// selected by a filter/history predicate): this axis pins ONE already-identified object
  /// by reference, which a filter cannot express. A linked reference, not a threaded binding
  /// (ADR 0004 reference-not-resolution). Null (omitted) for filter-scoped death events.
  /// </para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? DyingObject { get; init; }
}
