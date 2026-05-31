namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Bloodthirst (Rule 702.54). A static ability printed as "Bloodthirst N".
/// If an opponent was dealt damage this turn, this creature enters the
/// battlefield with N +1/+1 counters on it.
/// MAST records the keyword and its integer value; the condition check and
/// counter-placement on entry are engine territory.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect and ModularEffect
/// shape — <see cref="Value"/> is the bloodthirst number lifted from the
/// printed oracle text.
/// </para>
/// </summary>
[OracleEffect("bloodthirst")]
public sealed record BloodthirstEffect : Effect
{
  /// <summary>The bloodthirst value N printed on the card (e.g., "Bloodthirst 3" → 3).</summary>
  public required int Value { get; init; }
}
