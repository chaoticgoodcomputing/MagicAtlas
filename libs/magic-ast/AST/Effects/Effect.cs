namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
// Import all effect subdirectories
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Replacement;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.Serialization;

/// <summary>
/// Base type for all effects in Magic.
/// Effects are what happens when spells and abilities resolve.
/// </summary>
[PolymorphicBase("effectType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Effect>))]
public abstract record Effect
{
  /// <summary>
  /// Duration of this effect, if temporary.
  /// </summary>
  [JsonPropertyName("duration")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>
  /// Whether this effect is optional ("you may...").
  /// </summary>
  [JsonPropertyName("isOptional")]
  public bool IsOptional { get; init; }

  /// <summary>
  /// Secondary effect that happens "if you do" perform the main effect.
  /// </summary>
  [JsonPropertyName("ifYouDo")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>
  /// "Unless [player] pays [cost]" clause that can prevent this effect.
  /// Common in cards like Rhystic Study, Mystic Remora, Ward.
  /// </summary>
  [JsonPropertyName("unlessClause")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}

/// <summary>
/// Represents an "unless [player] pays [cost]" clause.
/// </summary>
public sealed record UnlessClause
{
  /// <summary>
  /// The player who can pay to prevent the effect.
  /// </summary>
  [JsonPropertyName("player")]
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The cost that can be paid to prevent the effect.
  /// </summary>
  [JsonPropertyName("cost")]
  public required Cost Cost { get; init; }
}
