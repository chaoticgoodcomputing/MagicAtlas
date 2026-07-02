namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Modifier for replacement effects that scale the original event.
/// </summary>
public sealed record ReplacementModifier
{
  /// <summary>
  /// The type of modification: "double", "triple", "plusOne", "plusX", etc.
  ///
  /// <para>
  /// "advantage" is the dice-roll variant (Pixie Guide, Wyll, Barbarian Class —
  /// the "Grant an Advantage" ability word): the replaced <see cref="DiceRollEvent"/>
  /// of N dice becomes a roll of N+1 dice with the lowest result ignored (CR 706.6 —
  /// an ignored roll is treated as never having happened). It is a single atomic
  /// template, not decomposable into a plain count modifier, so it is named in full.
  /// </para>
  /// </summary>
  public required string Type { get; init; }

  /// <summary>
  /// For variable modifiers, the amount.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

  /// <summary>
  /// When true, the controller may choose new targets for the additional copy produced
  /// by this modifier (e.g. Twinning Staff's "You may choose new targets for the
  /// additional copy."). Applies only when the modifier adds extra copies (plusOne, etc.)
  /// and mirrors the <see cref="MagicAST.AST.Effects.TokenCopy.CopyEffect.MayChooseNewTargets"/>
  /// flag on the extra copy.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? MayChooseNewTargets { get; init; }
}
