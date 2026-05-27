namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "draw [count] cards"
/// </summary>
[OracleEffect("drawCards")]
public sealed record DrawCardsEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  public required Quantity Count { get; init; }

  public required ObjectReference Player { get; init; }

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
  /// Game-state condition that must hold for this draw to occur.
  /// Used for "If [condition], draw a card" patterns such as "If this spell was kicked".
  /// Mirrors the <see cref="Condition"/> used by <see cref="TriggeredAbility.InterveningIf"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
