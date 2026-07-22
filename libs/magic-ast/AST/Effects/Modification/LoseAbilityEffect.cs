namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[target] loses [ability]"
/// </summary>
[OracleEffect("loseAbility")]
public sealed record LoseAbilityEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The single keyword ability that is lost, when the lost ability is a named
  /// keyword ("loses flying"). Structured value — preferred over <see cref="AbilityText"/>
  /// whenever the removed ability is a keyword expressible by the enum. Exactly one of
  /// <see cref="Keyword"/> / <see cref="AbilityText"/> is set.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public KeywordAbility? Keyword { get; init; }

  /// <summary>
  /// The unbounded ability-loss SCOPE, when the lost abilities aren't a single named
  /// ability but a determiner over the whole set — "loses all abilities"
  /// (<see cref="AbilityScope.All"/>) or "loses all other ... abilities"
  /// (<see cref="AbilityScope.AllOther"/>, used when a preceding effect in the same
  /// sentence grants a new ability that must survive the strip, e.g. Vraska, Betrayal's
  /// Sting's "becomes a Treasure artifact with '…' and loses all other card types and
  /// abilities"). Structured value — preferred over <see cref="AbilityText"/> whenever
  /// the loss is a scope determiner rather than a specific named ability. Exactly one of
  /// <see cref="Keyword"/> / <see cref="Scope"/> / <see cref="AbilityText"/> is set.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public AbilityScope? Scope { get; init; }

  /// <summary>
  /// The ability text that is lost when it is NOT a single structurable keyword and
  /// NOT an unbounded scope — a SPECIFIC ability named verbatim (Animate Dead: "it
  /// loses 'enchant creature card in a graveyard'", naming its own printed enchant
  /// ability by text) or an ability described by prose the keyword enum can't
  /// capture. A bare keyword must use <see cref="Keyword"/>; an unbounded "all"/"all
  /// other" scope must use <see cref="Scope"/>.
  /// </summary>
  [FreeTextField]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? AbilityText { get; init; }
}

/// <summary>
/// The unbounded determiner over a target's whole ability set that
/// <see cref="LoseAbilityEffect.Scope"/> records — CR 613.1f (a continuous effect
/// removing abilities). Recorded as written (reference-not-resolution, ADR 0004):
/// "all other abilities" is its own value, not a negation or exclusion list layered
/// on <see cref="AbilityScope.All"/>.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AbilityScope
{
  /// <summary>"loses all abilities" — every ability, with no exception.</summary>
  All,

  /// <summary>"loses all other abilities" / "loses all other card types and abilities" —
  /// every ability except one granted by a sibling effect earlier in the same sentence.</summary>
  AllOther,
}
