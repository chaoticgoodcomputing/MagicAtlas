namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Vanishing N (Rule 702.63). A keyword ability that limits how long a permanent
/// remains on the battlefield. Oracle form: "Vanishing N (This creature enters
/// with N time counters on it. At the beginning of your upkeep, remove a time
/// counter from it. When the last is removed, sacrifice it.)".
///
/// <para>
/// MAST records the keyword's presence and its integer value (the number of
/// time counters the permanent enters with); the counter-removal upkeep trigger
/// and sacrifice-when-empty mechanics are engine territory.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and
/// <see cref="FadingEffect"/>. Vanishing differs from Fading in using time
/// counters and in triggering sacrifice when the last counter is removed
/// (rather than when you can't remove one). MAST records the value only;
/// the counter-type distinction is rules-territory.
/// </para>
/// </summary>
[OracleEffect("vanishing")]
public sealed record VanishingEffect : Effect
{
  /// <summary>The vanishing value N printed on the card (e.g., "Vanishing 3" → 3).</summary>
  public required int Value { get; init; }
}
