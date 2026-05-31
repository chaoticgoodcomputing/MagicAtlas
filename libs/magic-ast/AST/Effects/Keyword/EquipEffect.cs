namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Equip (Rule 702.6). A keyword ability that lets a player attach an Equipment
/// to a creature they control. The full activated form is
/// "[cost]: Attach to target creature you control. Activate only as a sorcery."
/// MAST records the keyword's presence and the equip cost; the attach mechanics
/// and sorcery-speed restriction are conventionally inferred from the rules
/// (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type so future variants (e.g.,
/// "Equip — Sacrifice a creature") can plug in without a schema change, mirroring
/// the <see cref="CyclingEffect"/> pattern.
/// </para>
/// </summary>
[OracleEffect("equip")]
public sealed record EquipEffect : Effect
{
  /// <summary>
  /// The cost paid to equip this Equipment to a creature you control.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana equip costs.
  /// </summary>
  public required Cost Cost { get; init; }
}
