namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.References;
using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Soulshift (Rule 702.46). A triggered keyword ability printed as
/// "Soulshift N (When this creature dies, you may return target Spirit card
/// with mana value N or less from your graveyard to your hand.)". Although
/// Soulshift is mechanically a triggered ability with an embedded targeted
/// return, MAST records it as a keyword marker — same approach as Bushido,
/// Prowess, Exalted — and treats the canonical trigger-and-return expansion
/// as engine territory.
/// </summary>
/// <remarks>
/// Integer-parameterized keyword effect; <see cref="Value"/> is the soulshift
/// number lifted from the printed oracle text. Mirrors the BushidoEffect
/// shape: literal printed numeral, not a <c>Quantity</c> wrapper — the value
/// is fixed at oracle-print time, not derived at runtime.
///
/// <para>
/// The containing <c>StaticAbility</c> records <c>KeywordSource = KeywordAbility.Soulshift</c>
/// so the keyword's identity survives normalization.
/// </para>
/// </remarks>
[OracleEffect("soulshift")]
public sealed record SoulshiftEffect : Effect
{
  /// <summary>The soulshift value N printed on the card (e.g., "Soulshift 5" → 5).</summary>
  public required int Value { get; init; }
}
