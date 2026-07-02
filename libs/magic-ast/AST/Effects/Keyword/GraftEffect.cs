namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Graft N (Rule 702.58). A keyword ability that has a permanent enter with
/// +1/+1 counters on it and can move those counters to other creatures.
/// Oracle form: "Graft N (This creature enters with N +1/+1 counters on it.
/// Whenever another creature enters, you may move a +1/+1 counter from this
/// creature onto it.)".
///
/// <para>
/// MAST records the keyword's presence and its integer value (the number of
/// +1/+1 counters the permanent enters with); the enters-with-counters and
/// optional-counter-move triggered ability are engine territory.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and the
/// Modular/Backup family. <see cref="Value"/> is the graft number lifted from
/// the printed oracle text.
/// </para>
/// </summary>
[OracleEffect("graft")]
public sealed record GraftEffect : Effect
{
  /// <summary>The graft value N printed on the card (e.g., "Graft 2" → 2).</summary>
  public required int Value { get; init; }
}
