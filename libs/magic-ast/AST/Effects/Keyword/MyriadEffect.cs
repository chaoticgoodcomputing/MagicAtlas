namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Myriad (Rule 702.116). A triggered keyword ability on a creature: whenever this
/// creature attacks, for each opponent other than defending player, you may create a
/// token that's a copy of this creature tapped and attacking that player or a
/// planeswalker they control; if one or more tokens are created this way, exile
/// them at end of combat. MAST records the keyword's presence; the per-opponent
/// copy-creation, tapped-and-attacking, and delayed-exile semantics are engine
/// territory per the descriptive-not-engine doctrine. Mirrors EvolveEffect,
/// FlankingEffect, and MentorEffect exactly: parameterless, four trait interfaces.
/// </summary>
[OracleEffect("myriad")]
public sealed record MyriadEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
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
}
