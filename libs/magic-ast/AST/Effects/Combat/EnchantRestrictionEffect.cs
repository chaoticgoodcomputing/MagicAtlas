namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Enchant [quality]" - defines what an Aura can legally target and attach to.
/// Rule 702.5
/// </summary>
public sealed record EnchantRestrictionEffect : Effect
{
  /// <summary>
  /// The filter defining what permanents this Aura can enchant.
  /// </summary>
  [JsonPropertyName("legalTargets")]
  public required ObjectFilter LegalTargets { get; init; }
}
