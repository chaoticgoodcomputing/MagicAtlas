namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Tribute N (Rule 702.102). "As this creature enters, an opponent of your
/// choice may put N +1/+1 counters on it." An enters-the-battlefield keyword
/// from Born of the Gods. If the opponent does not pay tribute, an ETB
/// triggered ability typically fires.
///
/// <para>
/// MAST records the keyword's presence and its integer value N; the
/// opponent-choice, counter-placement, and conditional ETB semantics are
/// engine territory (per the descriptive-not-engine doctrine).
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors the <see cref="BushidoEffect"/> shape.
/// </para>
/// </summary>
[OracleEffect("tribute")]
public sealed record TributeEffect : Effect
{
  /// <summary>The tribute value N printed on the card (e.g., "Tribute 3" → 3).</summary>
  public required int Value { get; init; }
}
