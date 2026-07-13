namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "[target] loses [ability]"
/// </summary>
[OracleEffect("loseAbility")]
public sealed record LoseAbilityEffect : ContinuousEffect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The single keyword ability that is lost, when the lost ability is a named
  /// keyword ("loses flying"). Structured value — preferred over <see cref="AbilityText"/>
  /// whenever the removed ability is a keyword expressible by the enum. Exactly one of
  /// <see cref="Keyword"/> / <see cref="AbilityText"/> is set.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public KeywordAbility? Keyword { get; init; }

  /// <summary>
  /// The ability text that is lost when it is NOT a single structurable keyword —
  /// an ability SCOPE ("all abilities", "all other abilities") or an ability described
  /// by prose the keyword enum can't capture. A bare keyword must use <see cref="Keyword"/>.
  /// </summary>
  [FreeTextField]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? AbilityText { get; init; }
}
