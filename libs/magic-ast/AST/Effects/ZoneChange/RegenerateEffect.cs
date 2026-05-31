namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "regenerate [target]" — keyword action (Rule 701.19). Creates a regeneration
/// shield on the target permanent: the next time it would be destroyed this turn,
/// instead remove all damage from it, tap it, and if it's in combat remove it
/// from combat. MAST records the effect's presence and target; the shield /
/// destruction-replacement semantics are conventionally inferred from the rules
/// (per the descriptive-not-engine doctrine), mirroring the DestroyEffect /
/// PreventDamageEffect patterns.
/// </summary>
[OracleEffect("regenerate")]
public sealed record RegenerateEffect : Effect
{
  /// <summary>The permanent that gains the regeneration shield.</summary>
  public required ObjectReference Target { get; init; }
}
