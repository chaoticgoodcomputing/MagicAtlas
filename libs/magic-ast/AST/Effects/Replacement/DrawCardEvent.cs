namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Draw-a-card event: "would draw [N] card(s)". Rule 121 (drawing a card),
/// Rule 614 (replacement effects).
/// </summary>
[OracleReplacementEvent("drawCard")]
public sealed record DrawCardEvent : ReplacementEvent
{
  /// <summary>
  /// The player whose draw is being replaced. Defaults to the controller of the
  /// replacement effect ("If you would draw...") when omitted.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Player { get; init; }

  /// <summary>
  /// Number of cards the original draw would have been. Defaults to one when omitted
  /// ("If you would draw a card...").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }
}
