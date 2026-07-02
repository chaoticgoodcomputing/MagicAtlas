namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Toxic (Rule 702.164). A static ability printed as "Toxic N" where N is a
/// positive integer. Whenever a creature with toxic deals combat damage to a
/// player, that player gets N poison counters (in addition to the damage).
/// MAST records the keyword and its integer value; the poison-counter placement
/// and interaction with combat damage are engine territory per the
/// descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect shape —
/// <see cref="Value"/> is the toxic number lifted from the printed oracle text.
/// </para>
///
/// <para>
/// Multiple toxic instances on one creature add their N values (702.164b);
/// MAST records each instance separately.
/// </para>
/// </summary>
[OracleEffect("toxic")]
public sealed record ToxicEffect : Effect
{
  /// <summary>The toxic value N printed on the card (e.g., "Toxic 2" → 2).</summary>
  public required int Value { get; init; }
}
