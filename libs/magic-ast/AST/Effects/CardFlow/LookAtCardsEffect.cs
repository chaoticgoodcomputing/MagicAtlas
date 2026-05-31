namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Look at the top N cards of your library" or "Look at target player's hand"
/// </summary>
[OracleEffect("lookAtCards")]
public sealed record LookAtCardsEffect : Effect
{
  /// <summary>
  /// Whose cards to look at.
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many cards to look at.
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// Where to look: Library, Hand, etc.
  /// </summary>
  public required Zone Zone { get; init; }

  /// <summary>
  /// Where in the zone: Top, Bottom, Random, All.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? Location { get; init; }

  /// <summary>
  /// True when oracle text specifies "then put them back in any order" — the
  /// controller reorders all looked-at cards and returns them to the same zone.
  /// Distinct from scry (top/bottom choice) and surveil (graveyard option).
  /// Rule 701.12 (look) does not define a default disposition; the clause must
  /// be explicit in oracle text.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool PutBackInAnyOrder { get; init; }
}
