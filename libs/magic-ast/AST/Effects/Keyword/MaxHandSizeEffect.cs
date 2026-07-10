namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Your maximum hand size is ten." / "Each player's maximum hand size is
/// seven." — Rule 402.2. A continuous static effect that SETS the maximum hand
/// size restriction for the scoped player(s) to a fixed number, for as long as
/// the source permanent is on the battlefield (no duration/condition field is
/// needed — the effect persists by virtue of the permanent's presence, Rule
/// 604.3). The numeric-SET sibling of <see cref="NoMaxHandSizeEffect"/> (which
/// removes the maximum hand size entirely rather than setting it to a number).
/// </summary>
/// <remarks>
/// Mirrors <see cref="NoMaxHandSizeEffect"/>'s <see cref="Player"/> field
/// exactly (required <see cref="ObjectReference"/> scoping "you"/"players"),
/// adding the one axis the "no maximum" variant doesn't need: the numeric
/// <see cref="Value"/> the hand size is set to (The Ten Rings: "Your maximum
/// hand size is ten." → <see cref="ObjectReferenceKind.You"/>, Value = 10).
/// </remarks>
[OracleEffect("maxHandSize")]
public sealed record MaxHandSizeEffect : Effect
{
  /// <summary>
  /// Whose maximum hand size is set — the scope of the effect.
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The fixed maximum hand size the scoped player(s) now have.
  /// </summary>
  public required int Value { get; init; }
}
