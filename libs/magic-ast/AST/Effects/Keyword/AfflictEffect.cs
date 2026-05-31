namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Afflict N (Rule 702.130). A triggered keyword ability: whenever this creature
/// becomes blocked, the defending player loses N life. MAST records the keyword
/// and its integer value; the becomes-blocked trigger and life-loss are engine
/// territory per the descriptive-not-engine doctrine. Integer-parameterized
/// keyword; mirrors BushidoEffect.
/// </summary>
[OracleEffect("afflict")]
public sealed record AfflictEffect : Effect
{
  /// <summary>The afflict value N printed on the card (e.g., "Afflict 2" → 2).</summary>
  public required int Value { get; init; }
}
