namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "counter [target spell/ability]"
/// </summary>
[OracleEffect("counterSpell")]
public sealed record CounterSpellEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// "unless its controller pays [cost]"
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? UnlessCost { get; init; }

  /// <summary>
  /// "exile it instead of putting it into its owner's graveyard" — the countered
  /// spell goes to exile rather than the graveyard (CR 406.6 linked-exile setup;
  /// Transcendent Dragon). A replacement on the counter's own zone-change, modeled
  /// on the counter effect because the two are a single sentence whose follow-up
  /// ("then you may cast it") references the exiled card.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public bool? ExileInsteadOfGraveyard { get; init; }
}
