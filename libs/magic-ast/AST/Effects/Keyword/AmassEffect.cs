namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Amass [Subtype] N" keyword action (CR 701.47a).
///
/// <para>
/// CR 701.47a: "Amass [subtype] N means 'Either put N +1/+1 counters on an Army
/// you control, or create a 0/0 black [Subtype] Army creature token, then put N
/// +1/+1 counters on it.'"
/// </para>
///
/// <para>
/// MAST records the keyword-action with its subtype (e.g., "Orcs") and integer
/// value N. The token creation, counter placement, and army selection are engine
/// territory — the node names the action, not the execution.
/// </para>
///
/// <para>
/// Modern Amass printings (War of the Spark and later) specify the army subtype
/// explicitly (e.g., "amass Orcs 1"). The <see cref="ArmySubtype"/> field carries
/// that subtype; it is null for legacy Amass printings that do not specify a subtype
/// (those are untyped Army tokens).
/// </para>
/// </summary>
[OracleEffect("amass")]
public sealed record AmassEffect : Effect
{
  /// <summary>
  /// The N value: number of +1/+1 counters placed on the amassed Army (CR 701.47a).
  /// </summary>
  public required Quantity Count { get; init; }

  /// <summary>
  /// The creature subtype of the Army token created (e.g., "Orcs").
  /// Null for legacy Amass printings that do not specify a subtype.
  /// CR 701.47a: the army token is a "0/0 black [Subtype] Army creature token".
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? ArmySubtype { get; init; }
}
