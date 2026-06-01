namespace MagicAST.AST.Effects.TokenCopy;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "copy [target]" or "create a copy of [target]"
/// </summary>
[OracleEffect("copy")]
public sealed record CopyEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// How many copies to create. Null means a single copy (the default, e.g. Conspire's
  /// "copy it"). A <see cref="MagicAST.AST.Quantities.KeywordCostPaidCountQuantity"/> for
  /// Replicate — "copy it for each time its replicate cost was paid" (CR 702.56a).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }

  /// <summary>
  /// "Except"-clauses applied to the copy — power/toughness overrides,
  /// type additions, ability additions.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<CopyModification>? Modifications { get; init; }
}
