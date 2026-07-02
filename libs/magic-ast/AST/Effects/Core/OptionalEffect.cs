namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may [Inner]. If you do, [IfYouDo] / if you don't, [IfYouDoNot]." (CR 117.7).
/// Wrapper presence is the "you may" — no bool. One-shot action effects only (ADR 0005).
/// </summary>
[OracleEffect("optional")]
public sealed record OptionalEffect : Effect
{
  /// <summary>The effect the controller may choose to perform.</summary>
  public required Effect Inner { get; init; }

  /// <summary>Effect performed if the controller chooses to perform <see cref="Inner"/>.</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Effect performed if the controller chooses not to (CR 117.7 per-player fork).</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }
}
