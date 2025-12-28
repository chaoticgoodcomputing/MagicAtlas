namespace MagicAST.AST.Effects.Damage;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Lifelink effect: damage dealt by this source causes its controller to gain that much life.
/// Applies to all damage (not just combat damage) dealt from any zone.
/// The source's controller (or owner if no controller) gains the life.
/// Rule 702.15
/// </summary>
public sealed record LifelinkEffect : Effect
{
  // Lifelink is a static ability with fixed semantics defined by rule 702.15.
  // The interpreter handles: any damage → source's controller gains that much life.
}
