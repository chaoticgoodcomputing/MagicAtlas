namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Enchant [quality]" - defines what an Aura can legally target and attach to.
/// Rule 702.5
/// </summary>
[OracleEffect("enchantRestriction")]
public sealed record EnchantRestrictionEffect : Effect
{
  /// <summary>
  /// The filter defining what permanents this Aura can enchant.
  /// </summary>
  public required ObjectFilter LegalTargets { get; init; }
}
