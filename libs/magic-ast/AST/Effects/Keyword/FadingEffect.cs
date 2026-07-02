namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Fading N (Rule 702.32). A keyword ability that limits how long a permanent
/// remains on the battlefield. Oracle form: "Fading N (This creature enters
/// with N fade counters on it. At the beginning of your upkeep, remove a fade
/// counter from it. If you can't, sacrifice it.)".
///
/// <para>
/// MAST records the keyword's presence and its integer value (the number of
/// fade counters the permanent enters with); the counter-removal upkeep trigger
/// and sacrifice-unless-counter mechanics are engine territory.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and the
/// Modular/Backup family. <see cref="Value"/> is the fading number lifted from
/// the printed oracle text.
/// </para>
/// </summary>
[OracleEffect("fading")]
public sealed record FadingEffect : Effect
{
  /// <summary>The fading value N printed on the card (e.g., "Fading 3" → 3).</summary>
  public required int Value { get; init; }
}
