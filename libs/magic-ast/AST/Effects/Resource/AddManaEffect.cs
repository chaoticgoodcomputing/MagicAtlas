namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "add [mana]"
/// </summary>
[OracleEffect("addMana")]
public sealed record AddManaEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  public required string Mana { get; init; }

  /// <summary>
  /// For effects like "add one mana of any color"
  /// </summary>
  public bool AnyColor { get; init; }

  /// <summary>
  /// Descriptive capture of a "Spend this mana only to &lt;X&gt;" restriction that
  /// follows the mana-production sentence (e.g. "cast a creature spell of the
  /// chosen type" on Unclaimed Territory). MAST describes; it does not execute —
  /// this holds the restriction text verbatim rather than a structured spend
  /// predicate. Null when the produced mana carries no spend restriction.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? SpendRestriction { get; init; }

  /// <summary>
  /// For variable amounts
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

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
