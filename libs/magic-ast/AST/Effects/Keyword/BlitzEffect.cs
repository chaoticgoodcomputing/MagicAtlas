namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Blitz (Rule 702.152). An alternative-cost keyword ability: if you cast this
/// spell for its blitz cost, the creature gains haste, gains "When this creature
/// dies, draw a card.", and is sacrificed at the beginning of the next end step.
/// MAST records the keyword's presence and the blitz cost; the granted haste,
/// death-draw trigger, and end-step sacrifice are conventionally inferred from
/// the rules (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// alternative-cost keyword effects (Bestow, Cycling) — most printings use a
/// <see cref="ManaCost"/>, but the base accommodates future non-mana variants.
/// </para>
/// </summary>
[OracleEffect("blitz")]
public sealed record BlitzEffect : Effect
{
  /// <summary>
  /// The blitz cost paid as an alternative casting cost.
  /// </summary>
  public required Cost Cost { get; init; }
}
