namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Cost increase effect: spells matching the containing ability's
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/> filter
/// cost more to cast (Rule 601.2f / Rule 117.6 — total cost modification).
///
/// <para>
/// Two shapes share this node:
/// <list type="bullet">
///   <item>"Noncreature spells cost {1} more to cast." (Thorn of Amethyst / Sphere of
///         Resistance) — the filter sits on the enclosing StaticAbility.AffectedObjects;
///         this effect carries only the Amount.</item>
///   <item>"Spells your opponents cast that target this creature cost {N} more to cast."
///         (pre-Ward pattern) — a targeting condition with CasterFilter and TargetedObject.</item>
/// </list>
/// </para>
/// </summary>
[OracleEffect("costIncrease")]
public sealed record CostIncreaseEffect : Effect
{
  /// <summary>
  /// The amount of the increase.
  /// </summary>
  public required Quantity Amount { get; init; }

  /// <summary>
  /// The object that affected spells must target for the increase to apply.
  /// Typically <see cref="ObjectReferenceKind.Self"/> ("this creature").
  /// Null when the increase is unconditional (Thorn of Amethyst shape).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? TargetedObject { get; init; }

  /// <summary>
  /// Filter on whose spells are affected. Typically
  /// <see cref="ControllerFilter.Opponent"/> ("your opponents cast").
  /// When null, all spells are affected regardless of who casts them.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ControllerFilter? CasterFilter { get; init; }
}
