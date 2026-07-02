namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Look at the top N cards of your library. You may put a [filter] card from among them onto
/// the battlefield. Put the rest on the bottom of your library in a random order." —
/// the Kinnan, Bonder Prodigy activated ability family.
///
/// <para>
/// This is a single atomic action: the controller looks at the top N cards, optionally chooses
/// one matching <see cref="CardFilter"/> to put onto the battlefield, and then the remaining cards
/// go to the bottom of the library in a random order. The "from among them" back-references the
/// same looked-at pile; the action must not be decomposed into separate look + zone-change effects.
/// </para>
///
/// <para>
/// Structural position: similar to <see cref="ImpulseEffect"/> (look top N, keep one, rest
/// elsewhere) but with distinct semantics:
/// <list type="bullet">
///   <item>The kept card goes to the battlefield (not hand).</item>
///   <item>The kept card must match <see cref="CardFilter"/> (e.g. non-Human creature).</item>
///   <item>The choice is optional ("You may").</item>
///   <item>The rest go to the bottom in a random order (not any order).</item>
/// </list>
/// </para>
///
/// <para>
/// CR 701.12 (look); CR 400.7 (putting a card onto the battlefield); CR 400.4 (random order).
/// </para>
/// </summary>
[OracleEffect("topLookPutOntoBattlefield")]
public sealed record TopLookPutOntoBattlefieldEffect : Effect
{
  /// <summary>
  /// How many cards from the top of the library the player looks at.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// Whose library to look at — typically <c>You</c> (the controller).
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// The filter the chosen card must match — e.g. non-Human creature card.
  /// Only cards that match this filter may be put onto the battlefield.
  /// </summary>
  public required ObjectFilter CardFilter { get; init; }

  /// <summary>
  /// True when the choice is optional ("You may put…"). When the controller
  /// declines, all looked-at cards go to the bottom in a random order.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool Optional { get; init; }
}
