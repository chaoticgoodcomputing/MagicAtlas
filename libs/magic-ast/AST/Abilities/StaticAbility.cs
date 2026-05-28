namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Replacement-timing qualifier for a static ability — the "when" half of the
/// composite "At &lt;When&gt;, do &lt;Effect&gt;". Kept deliberately small: timing is a
/// separate axis from the effect, so an effect node never bakes its own timing in.
/// </summary>
public enum StaticTimingKind
{
  /// <summary>
  /// "As [this] enters", "[This] enters tapped/with/as …" — a self-replacement
  /// effect that applies as the permanent enters the battlefield (CR 603.6d, 614.1c).
  /// </summary>
  AsThisEnters,
}

/// <summary>
/// Represents a static ability: a statement that is simply true.
/// Rule 113.3d, Rule 604
/// </summary>
[OracleAbility("static")]
public sealed record StaticAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Static;

  /// <summary>
  /// The continuous effects this static ability creates.
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Optional replacement-timing qualifier — the "when" half of the composite
  /// "At &lt;When&gt;, do &lt;Effect&gt;". For the "As [this] enters …" / "[This]
  /// enters tapped/with/as …" replacement family this is <see cref="StaticTimingKind.AsThisEnters"/>
  /// (CR 603.6d, 614.1c). Null for an ordinary always-on static ability. Timing
  /// lives here, never baked into an effect discriminator.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public StaticTimingKind? When { get; init; }

  /// <summary>
  /// Optional condition for when this static ability applies.
  /// e.g., "as long as you control a Forest"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }

  /// <summary>
  /// Which objects this static ability affects.
  /// Null if it affects the object itself or the game in general.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? AffectedObjects { get; init; }
}
