namespace MagicAST.AST.Effects.Control;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "untap [target]"
/// </summary>
[OracleEffect(
  "untap",
  NearDuplicateOf = new[] { "tap" },
  Reason = "Opposite operations, distinct as both Cost and Effect: 'tap' taps a permanent, 'untap' untaps it (CR 701.21 / 701.22). The shared root is the verb; the 'un-' prefix inverts it. Not sprawl."
)]
public sealed record UntapEffect : Effect
{
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// Multiplicity of the target set. Null = single target (default "untap target X").
  /// Used for variable- or literal-count target sets, e.g. "Untap X target lands"
  /// (Count = VariableQuantity "X") or "Untap two target creatures" (Count = literal 2).
  /// Mirrors <see cref="MagicAST.AST.Effects.Control.TapEffect.Count"/>; distinct from
  /// <see cref="ObjectReference.Quantity"/>, which is reserved for the "up to N target"
  /// phrasing (see <see cref="MagicAST.Parsing.Parsers.Activated.Rules.UntapUpToNTargetPermanentsRule"/>).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Count { get; init; }
}
