namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "add [mana]"
/// </summary>
[OracleEffect("addMana")]
public sealed record AddManaEffect : Effect
{
  public required string Mana { get; init; }

  /// <summary>
  /// For effects like "add one mana of any color"
  /// </summary>
  public bool AnyColor { get; init; }

  /// <summary>
  /// Descriptive capture of a "Spend this mana only to &lt;X&gt;" restriction that
  /// follows the mana-production sentence (e.g. "cast a creature spell of the
  /// chosen type" on Unclaimed Territory). MAST describes; it does not execute —
  /// this holds the restriction text verbatim rather than a structured spend
  /// predicate. Null when the produced mana carries no spend restriction.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? SpendRestriction { get; init; }

  /// <summary>
  /// For variable amounts
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }
}
