namespace MagicAST.AST.Effects.Replacement;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "The 'legend rule' doesn't apply." / "The 'legend rule' doesn't apply to
/// creatures you control." A meta-rule suppression that disables the
/// state-based action described by Rule 704.5j.
///
/// Rule 704.5j: "If a player controls two or more legendary permanents with
/// the same name, that player chooses one of them, and the rest are put into
/// their owners' graveyards. This is called the 'legend rule.'"
///
/// Mirror Gallery (MRD) is the canonical unscoped card. Council of Reeds
/// (Bloomburrow) prints the scoped variant, narrowing the suppression to a
/// filtered set of permanents ("creatures you control") rather than every
/// legendary permanent controlled by any player.
/// </summary>
/// <remarks>
/// <para>
/// Placed under <c>Replacement/</c> because suppressing a state-based action
/// is structurally a replacement-shaped intervention on the rules check; it
/// neither carries a duration phrase nor produces a continuous-effect output
/// the way a P/T-modifying or ability-granting effect does.
/// </para>
/// <para>
/// MAST is descriptive: this effect records what the oracle text says
/// ("legend rule doesn't apply[, to Filter]"), not the runtime SBA-loop
/// suppression machinery that a rules engine would implement.
/// </para>
/// </remarks>
[OracleEffect("legendRuleSuppression")]
public sealed record LegendRuleSuppressionEffect : Effect
{
  /// <summary>
  /// Optional scope narrowing which permanents the suppression applies to —
  /// e.g. "creatures you control" (Council of Reeds:
  /// <c>Kind=Each, Filter={CardTypes:["creature"], Controller:You}</c>,
  /// reusing the same "creatures you control" shape as
  /// <see cref="MagicAST.AST.Effects.Modification.GainAbilityEffect"/>'s
  /// anthem targets). Null means the suppression is unscoped and applies to
  /// the legend rule broadly, exactly as printed on Mirror Gallery — the
  /// original, parameter-free form of this effect — so that existing gold
  /// serializes unchanged.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Target { get; init; }
}
