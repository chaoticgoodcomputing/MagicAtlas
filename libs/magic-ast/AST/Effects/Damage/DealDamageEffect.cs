namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "deals N damage to [target]"
/// </summary>
[OracleEffect("dealDamage")]
public sealed record DealDamageEffect : Effect
{
  public required Quantity Amount { get; init; }

  public required ObjectReference Target { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Source { get; init; }

  /// <summary>
  /// Whether the damage this effect deals is <b>combat</b> damage (CR 510) rather than the default
  /// non-combat damage (CR 120). An explicit "deals N damage to [target]" effect deals <em>non-combat</em>
  /// damage in almost every case (a burn spell, an ability), so this is <c>null</c> (≡ non-combat) for the
  /// overwhelming majority of cards and is omitted from their JSON. It is set <c>true</c> only for the rare
  /// effect whose oracle text marks the dealt damage as combat damage (e.g. "as though it were combat
  /// damage" / an assigned-combat-damage redirection).
  ///
  /// <para>The distinction is <b>load-bearing for the interaction layer</b> (combat-damage-modeling): a
  /// combat-specific trigger — "whenever ~ deals <em>combat</em> damage …" (CR 510, <c>DealsCombatDamage*</c>)
  /// — fires ONLY on combat damage, so a non-combat damage emit must NOT feed it (the false-positive the
  /// engine's damage arm avoids; adding-a-flow-arm anti-pattern 3). A bare "whenever ~ deals damage" trigger
  /// (CR 120, <c>DealsDamage</c>) fires on damage of <em>either</em> kind, so a non-combat emit DOES feed
  /// it (Captain Rex Nebula's Crash Land). MAST describes; the engine reads this facet to tier the arm.</para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? IsCombat { get; init; }
}
