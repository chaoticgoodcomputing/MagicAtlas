namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "You may cast [target] without paying its mana cost"
/// </summary>
[OracleEffect("castWithoutPaying")]
public sealed record CastWithoutPayingEffect : Effect
{
  /// <summary>
  /// What can be cast.
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Optional filter for what can be cast.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectFilter? Filter { get; init; }

  /// <summary>
  /// Timing restriction: "this turn", "until end of turn", etc.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? TimingRestriction { get; init; }
}
