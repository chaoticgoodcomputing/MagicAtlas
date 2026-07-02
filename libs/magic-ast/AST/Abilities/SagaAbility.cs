namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents the chapter-triggered structure on a Saga card (Rule 715).
/// Each <see cref="SagaChapter"/> is an implicit triggered ability that fires
/// when the Nth lore counter is added to the saga; the SagaAbility itself is
/// the container that holds them in order.
/// </summary>
/// <remarks>
/// MAST is descriptive, not an engine — the implicit "when the Nth lore
/// counter is added" trigger isn't encoded as a <see cref="TriggerCondition"/>
/// here. Consumers that need runtime trigger semantics derive them from the
/// <see cref="SagaChapter.Numbers"/> field.
/// </remarks>
[OracleAbility("saga")]
public sealed record SagaAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Saga;

  /// <summary>The chapter bodies, in oracle-text order.</summary>
  public required IReadOnlyList<SagaChapter> Chapters { get; init; }
}

/// <summary>
/// One chapter of a Saga. The chapter is fired when the saga has accumulated
/// the indicated number of lore counters.
/// </summary>
/// <remarks>
/// Most chapters fire on exactly one count (e.g. <c>"I — Effect"</c> → Numbers = [1]).
/// Grouped chapters (e.g. <c>"I, II — Effect"</c>) share a body; the Numbers
/// list captures all the counts that fire it.
/// </remarks>
public sealed record SagaChapter
{
  /// <summary>The lore-counter counts that fire this chapter.</summary>
  public required IReadOnlyList<int> Numbers { get; init; }

  /// <summary>
  /// The ability that fires when this chapter's counter is added.
  /// The orchestrator dispatches the body text through the registry, so
  /// individual chapters land as <see cref="SpellAbility"/>,
  /// <see cref="StaticAbility"/>, or whatever the body's classification
  /// resolves to.
  /// </summary>
  public required Ability Body { get; init; }
}
