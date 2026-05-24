namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Represents an activated ability: "[Cost]: [Effect.] [Instructions]"
/// Rule 113.3b, Rule 602
/// </summary>
[OracleAbility("activated")]
public sealed record ActivatedAbility : Ability
{
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Activated;

  /// <summary>
  /// The costs that must be paid to activate this ability.
  /// Everything before the colon.
  /// </summary>
  public required IReadOnlyList<Cost> Costs { get; init; }

  /// <summary>
  /// The effects that occur when this ability resolves.
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }

  /// <summary>
  /// Restrictions on when/how this ability can be activated.
  /// e.g., "Activate only as a sorcery", "Activate only once each turn"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<ActivationRestriction>? Restrictions { get; init; }

  /// <summary>
  /// True if this is a mana ability (doesn't use the stack).
  /// Rule 605
  /// </summary>
  public bool IsManaAbility { get; init; }

  /// <summary>
  /// For loyalty abilities, the loyalty cost (+N, -N, or 0).
  /// Null for non-loyalty abilities.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public int? LoyaltyCost { get; init; }
}

/// <summary>
/// Restrictions on when an activated ability can be used.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActivationRestriction
{
  /// <summary>"Activate only as a sorcery"</summary>
  OnlyAsSorcery,

  /// <summary>"Activate only during your turn"</summary>
  OnlyDuringYourTurn,

  /// <summary>"Activate only once each turn"</summary>
  OnlyOnceEachTurn,

  /// <summary>"Activate only if you control no untapped lands"</summary>
  OnlyIfNoUntappedLands,

  /// <summary>Other restriction captured as raw text</summary>
  Other,
}
