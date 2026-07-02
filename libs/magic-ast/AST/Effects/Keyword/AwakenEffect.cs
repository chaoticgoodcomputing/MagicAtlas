namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Awaken (Rule 702.113). Printed as "Awaken N—[cost]" on an instant or sorcery.
/// It establishes an alternative cost: if the spell is cast for its awaken cost,
/// the spell does its normal thing and additionally puts N +1/+1 counters on a
/// target land you control and turns that land into a 0/0 Elemental creature
/// that's still a land. MAST records the keyword's presence and the N + cost
/// parameters; the counters-on-land and land-becomes-creature semantics are
/// conventionally inferred from the rules (and echoed in reminder text).
/// </summary>
[OracleEffect("awaken")]
public sealed record AwakenEffect : Effect
{
  /// <summary>
  /// "Awaken N—[cost]" — the number of +1/+1 counters placed on the target land
  /// when the spell is cast for its awaken cost.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? N { get; init; }

  /// <summary>
  /// "Awaken N—[cost]" — the alternative cost paid to cast the spell with awaken.
  /// </summary>
  public required Cost Cost { get; init; }
}
