namespace MagicAST.AST.Effects;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
// Import all effect subdirectories
using MagicAST.AST.Effects.Battle;
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
public abstract record Effect
{
  /// <summary>
  /// The span in the card's oracle text this effect was parsed from — for a
  /// triggered/activated ability, the EFFECT-half region (after the trigger comma /
  /// the cost colon). All the effects an ability produces share this half-granular
  /// span (clause-accurate provenance — upstream-atlas-data-plan §4); MAST does not
  /// thread per-individual-effect offsets. <c>null</c> when a parser cannot attribute
  /// a span; never fabricated.
  /// <para>
  /// Serialized when non-null (the global <c>WhenWritingNull</c> policy), matching
  /// <see cref="MagicAST.AST.Abilities.Ability.SourceSpan"/>. The <see cref="Core.UnparsedEffect"/>
  /// and <see cref="Core.UnstructuredEffect"/> spans ride on this single base property —
  /// there is no separate serialized copy.
  /// </para>
  /// </summary>
  public TextSpan? SourceSpan { get; init; }
}

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
