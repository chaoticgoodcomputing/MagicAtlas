namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Amplify N (Rule 702.37). "As this creature enters, put a +1/+1 counter on
/// it for each [creature type] card you reveal in your hand." An
/// enters-the-battlefield keyword from Legions. MAST records the keyword's
/// presence and its integer value N; the creature-type-reveal and
/// counter-placement semantics are engine territory (per the
/// descriptive-not-engine doctrine).
///
/// <para>
/// The creature type associated with the amplify trigger is defined by the
/// card's type line and is not encoded as a field — MAST captures the N
/// multiplier only, mirroring how <see cref="BushidoEffect"/> and
/// <see cref="ModularEffect"/> omit flavour-coupling details that the rules
/// engine derives from context.
/// </para>
///
/// <para>
/// Integer-parameterized keyword; mirrors the <see cref="BushidoEffect"/> shape.
/// </para>
/// </summary>
[OracleEffect("amplify")]
public sealed record AmplifyEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The amplify value N printed on the card (e.g., "Amplify 1" → 1).</summary>
  public required int Value { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
