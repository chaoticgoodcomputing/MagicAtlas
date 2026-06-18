namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Records that a source assigns no combat damage this combat:
/// "[Source] assigns no combat damage this combat."
///
/// <para>
/// Rule 510.1: "Each attacking creature and each blocking creature assigns combat
/// damage." This effect records that the named source is explicitly excluded from
/// assigning combat damage for the remainder of the current combat, overriding
/// the default rule. Descriptive only — MAST records what the oracle text says;
/// the zero-damage assignment is engine territory.
/// </para>
///
/// <para>
/// Canonical use: Master of Cruelties — after triggering its life-total-setting
/// ability, the clause "This creature assigns no combat damage this combat" prevents
/// the creature from also dealing normal combat damage that turn. This effect
/// accompanies the <see cref="SetLifeTotalEffect"/> in the same triggered ability.
/// </para>
/// </summary>
[OracleEffect("noCombatDamage")]
public sealed record NoCombatDamageEffect : Effect
{
  /// <summary>
  /// The source that assigns no combat damage. Typically
  /// <see cref="ObjectReferenceKind.Self"/> for a creature's own triggered ability.
  /// </summary>
  public required ObjectReference Source { get; init; }
}
