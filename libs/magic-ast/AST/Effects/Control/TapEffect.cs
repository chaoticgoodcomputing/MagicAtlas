namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "tap [target]"
/// </summary>
[OracleEffect("tap")]
public sealed record TapEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Multiplicity of the target set. Null = single target (default "tap target X").
  /// Used for variable- or literal-count target sets, e.g. "Tap X target lands"
  /// (Count = VariableQuantity "X") or "Tap two target creatures" (Count = literal 2).
  /// Mirrors <see cref="MagicAST.AST.Effects.ZoneChange.SacrificeEffect.Count"/>.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }

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
