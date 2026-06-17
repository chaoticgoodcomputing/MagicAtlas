namespace MagicAST.AST.Effects.CardFlow;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Look at the top N cards of your library. Put up to one of them on top of your library
/// and the rest on the bottom of your library in a random order." — the Thassa's Oracle /
/// oracle-look family.
///
/// <para>
/// Semantics: the controller looks at N cards from the top of their library, then chooses
/// up to one card to remain on top, and the remaining cards are placed on the bottom of the
/// library in a random (unordered) arrangement. CR 701.12 (look); CR 701.14 (in a random
/// order, the owner randomizes the placement).
/// </para>
///
/// <para>
/// This is a single atomic action — the "put up to one on top" clause and the "rest on the
/// bottom in a random order" clause back-reference the same looked-at pile. It must not be
/// decomposed into separate look + move effects; the coupling between the two dispositions
/// (the chosen one to top, the unchosen to random-bottom) is part of the action's meaning.
/// </para>
/// </summary>
[OracleEffect("oracleTopLook")]
public sealed record OracleTopLookEffect : Effect
{
  /// <summary>
  /// How many cards from the top of the library the player looks at.
  /// Typically a <see cref="DevotionQuantity"/> (Thassa's Oracle) but may be any
  /// <see cref="Quantity"/> in future card families using this shape.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// Whose library to look at — typically <c>You</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }
}
