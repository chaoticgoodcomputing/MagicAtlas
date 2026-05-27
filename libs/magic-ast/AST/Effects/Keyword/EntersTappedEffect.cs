namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This [permanent] enters tapped." A static-ability-attached effect that
/// records the oracle-level property. Rules-engine treatment (Rule 614,
/// replacement effects) — that the permanent enters tapped instead of
/// untapped — is derived from this descriptive declaration; MAST does not
/// model the replacement-event machinery itself.
/// </summary>
[OracleEffect("entersTapped")]
public sealed record EntersTappedEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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

  /// <summary>
  /// Board-state condition under which this land enters untapped instead.
  /// Captures the fastland / checkland oracle shape: "This land enters tapped
  /// unless [condition]." Rule 614.1c (as-enters replacement effect).
  /// Distinct from <see cref="UnlessClause"/> which represents the
  /// "unless [player] pays [cost]" cost-prevention pattern.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? EntryCondition { get; init; }
}
