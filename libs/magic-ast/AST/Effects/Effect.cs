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
using MagicAST.AST.Effects.Format;
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
[PolymorphicBase("EffectType")]
[JsonConverter(typeof(PolymorphicReflectionConverter<Effect>))]
public abstract record Effect;

/// <summary>
/// Represents an "unless [player] pays [cost]" clause.
/// </summary>
public sealed record UnlessClause
{
  /// <summary>
  /// The player who can pay to prevent the effect.
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The cost that can be paid to prevent the effect.
  /// </summary>
  public required Cost Cost { get; init; }
}
