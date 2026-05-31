namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Saddle (Rule 702.171). An activated keyword ability printed as
/// "Saddle N (Tap any number of other untapped creatures you control with
/// total power N or greater: This permanent becomes saddled until end of
/// turn. Activate only as a sorcery.)". Although Saddle is mechanically an
/// activated ability, MAST records it as a keyword marker — same approach as
/// Crew, Bushido, Soulshift — and treats the canonical activation / saddled
/// designation as engine territory.
/// </summary>
/// <remarks>
/// Integer-parameterized keyword effect; <see cref="Value"/> is the saddle
/// threshold N lifted from the printed oracle text (e.g., "Saddle 2" → 2).
/// Mirrors the BushidoEffect and SoulshiftEffect shape: literal printed
/// numeral, not a <c>Quantity</c> wrapper — the value is fixed at
/// oracle-print time, not derived at runtime.
///
/// <para>
/// The containing <c>StaticAbility</c> records <c>KeywordSource = "Saddle"</c>
/// so the keyword's identity survives normalization.
/// </para>
///
/// <para>
/// Saddle is the OTJ Mount mechanic (Outlaws of Thunder Junction, 2024).
/// It is structurally identical to Crew (Rule 702.122) but applies to Mounts
/// rather than Vehicles, and the result is a "saddled" designation rather
/// than an artifact-creature transformation. MAST does not distinguish between
/// the two at the keyword-marker level.
/// </para>
/// </remarks>
[OracleEffect("saddle")]
public sealed record SaddleEffect : Effect
{
  /// <summary>The saddle value N printed on the card (e.g., "Saddle 2" → 2).</summary>
  public required int Value { get; init; }
}
