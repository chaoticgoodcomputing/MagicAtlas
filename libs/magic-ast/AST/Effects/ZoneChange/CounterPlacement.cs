namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.AST.Quantities;

/// <summary>
/// Counters placed as part of a zone-change action. Used when exile (or other
/// movement) carries an attached counter-placement clause, e.g. "exile it with
/// three time counters on it".
/// </summary>
public sealed record CounterPlacement
{
  /// <summary>
  /// The kind of counter placed (lowercase: "time", "loyalty", "+1/+1", etc).
  /// </summary>
  public required string CounterType { get; init; }

  /// <summary>
  /// How many counters to place.
  /// </summary>
  public required Quantity Count { get; init; }
}
