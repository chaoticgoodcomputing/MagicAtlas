namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "return [target] to its owner's hand"
/// </summary>
[OracleEffect("returnToHand")]
public sealed record ReturnToHandEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Optional SET budget on the total mana value of the cards returned — "return any number of cards
  /// with total mana value X or less … where X is [a quantity]" (Pair o' Dice Lost: X is the die-roll
  /// total). This is a knapsack-style cap on the SUM of the selected cards' mana values (CR 107.3 mana
  /// value), distinct from a per-card filter. Null when there is no such budget (the common case).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? TotalManaValueBudget { get; init; }
}
