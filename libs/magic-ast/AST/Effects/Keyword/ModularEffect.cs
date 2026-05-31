namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Modular (Rule 702.43). A static ability and triggered ability printed as
/// "Modular N". The permanent enters with N +1/+1 counters on it. When it is
/// put into a graveyard from the battlefield, you may put a +1/+1 counter on
/// target artifact creature for each +1/+1 counter on this permanent.
/// MAST records the keyword and its integer value; the counter placement,
/// death trigger, and optional transfer are engine territory.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect and SoulshiftEffect
/// shape — <see cref="Value"/> is the modular number lifted from the printed
/// oracle text. Referenced explicitly in BushidoEffect.cs remarks as a future
/// peer in the integer-parameterized keyword family.
/// </para>
/// </summary>
[OracleEffect("modular")]
public sealed record ModularEffect : Effect
{
  /// <summary>The modular value N printed on the card (e.g., "Modular 2" → 2).</summary>
  public required int Value { get; init; }
}
