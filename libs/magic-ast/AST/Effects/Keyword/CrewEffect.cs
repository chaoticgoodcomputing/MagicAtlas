namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Crew effect: this Vehicle becomes an artifact creature until end of turn
/// if a total power of N or more of other creatures is tapped to crew it.
/// "Crew N" — Rule 702.122. The oracle keyword reads "Crew N" and expands to
/// an activated ability whose cost is tapping any number of other untapped
/// creatures with total power N or more. MAST records only the keyword's
/// presence and its parameter; the cost-and-resolution machinery is engine
/// territory, not described by the oracle line itself.
/// </summary>
[OracleEffect("crew")]
public sealed record CrewEffect : Effect
{
  /// <summary>
  /// The total power required from creatures tapped to crew this Vehicle.
  /// Typically a literal quantity (Crew 1, Crew 3, Crew 8...).
  /// </summary>
  public required Quantity Power { get; init; }
}
