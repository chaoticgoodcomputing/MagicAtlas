namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "sacrifice [target]"
/// </summary>
[OracleEffect("sacrifice")]
public sealed record SacrificeEffect : Effect
{
  public required ObjectReference Target { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }

  /// <summary>
  /// "sacrifice [target] unless [condition]" — a game-state reprieve: the sacrifice happens
  /// only if this condition is NOT met (CR 608.2). Chorale of the Void's Void end-step
  /// ("sacrifice this Aura unless a nonland permanent left the battlefield this turn or a spell
  /// was warped this turn") sets it to a <see cref="MagicAST.AST.Abilities.VoidCondition"/>.
  /// Null for an unconditional sacrifice. A structured-condition adjunct mirroring
  /// <see cref="MagicAST.AST.Effects.Control.CounterSpellEffect.UnlessCost"/> (the "unless [player]
  /// pays" reprieve on a counter) — here the reprieve is a satisfied game condition rather than a
  /// paid cost. Reference-not-resolution (ADR 0004): MAST records the printed unless-gate; the
  /// engine evaluates it and skips the sacrifice when it holds.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? UnlessCondition { get; init; }
}
