namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "copy [target]" or "create a copy of [target]"
/// </summary>
[OracleEffect("copy")]
public sealed record CopyEffect : Effect
{
  [JsonPropertyName("target")]
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Modifications to the copy.
  /// </summary>
  [JsonPropertyName("modifications")]
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Modifications { get; init; }
}
