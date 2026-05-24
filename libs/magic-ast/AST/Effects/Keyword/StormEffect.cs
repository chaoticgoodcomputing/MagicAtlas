namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Storm effect: when this spell is cast, a copy is created for each other spell
/// cast before it this turn. You may choose new targets for each copy.
/// "When you cast this spell, copy it for each other spell that was cast before it this turn.
/// You may choose new targets for the copies."
/// Rule 702.40
/// </summary>
/// <remarks>
/// Storm is rules-defined (Rule 702.40) — its expansion is fixed.
/// Modeled as a static-keyword-effect (per the codebase convention of attaching
/// rules-defined keywords to a <see cref="MagicAST.AST.Abilities.StaticAbility"/>
/// with KeywordSource set), the runtime semantics live in the rules, not the AST.
/// </remarks>
[OracleEffect("storm")]
public sealed record StormEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
