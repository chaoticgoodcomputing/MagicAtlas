namespace MagicAST.AST.Effects.Core;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[Inner] unless [player] pays [cost]." (Mana Leak, Rhystic Study). A distinct
/// clause shape from <see cref="OptionalEffect"/>; nests with it in the rare
/// co-occurrence. One-shot action effects only (ADR 0005).
/// </summary>
[OracleEffect("preventable")]
public sealed record PreventableEffect : Effect
{
  /// <summary>The effect that happens unless the cost is paid.</summary>
  public required Effect Inner { get; init; }

  /// <summary>The "unless [player] pays [cost]" clause that prevents <see cref="Inner"/>.</summary>
  public required UnlessClause Unless { get; init; }
}
