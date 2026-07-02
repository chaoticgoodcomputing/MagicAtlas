namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Attach [this permanent] to target [filter]." — the oracle-text instruction to
/// move an Equipment, Aura, or Fortification onto a qualifying object or player.
/// Rule 701.3: "To take an Aura, Equipment, or Fortification from where it currently
/// is and put it onto a specified object or player."
///
/// <para>
/// MAST records what oracle text says (the attachment instruction and its target);
/// the rules-engine mechanics of legality checks, zone-change effects, and
/// continuous effects from the attachment are engine territory
/// (per the descriptive-not-engine doctrine).
/// </para>
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.Effects.Keyword.EquipEffect"/>, which
/// models the Equip keyword ability (an activated ability defined by Rule 702.6).
/// This effect models explicit oracle-text "attach" instructions, typically inside
/// triggered abilities on Equipment that auto-attach on entry.
/// </para>
/// </summary>
[OracleEffect("attach")]
public sealed record AttachEffect : Effect
{
  /// <summary>
  /// The object to attach this permanent to.
  /// e.g., "target creature you control" →
  ///   <c>{ Kind = Target, Filter = { CardTypes = ["creature"], Controller = You } }</c>
  /// </summary>
  public required ObjectReference Target { get; init; }
}
