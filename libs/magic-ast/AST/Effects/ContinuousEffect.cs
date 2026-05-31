namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;

/// <summary>
/// Base for effects that establish a continuous effect (CR 611) — modify P/T,
/// grant an ability, restrict combat, etc. These persist, so they may carry a
/// <see cref="Duration"/>; one-shot action effects (CR 608) extend
/// <see cref="Effect"/> directly and cannot. The former IDurativeEffect.Duration
/// lives here now, defined once, only where it can be borne (ADR 0005). Abstract,
/// so the polymorphic converter skips it and discovers concrete continuous effects
/// (each with its own [OracleEffect] discriminator) under the Effect base.
/// </summary>
public abstract record ContinuousEffect : Effect
{
  /// <summary>How long the continuous effect lasts; null means no stated duration.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }
}
