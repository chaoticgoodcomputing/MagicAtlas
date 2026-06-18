namespace MagicAST.AST.Effects.Combat;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Permission for creatures to attack as though they did not have the Defender
/// keyword ability.
/// Rule 702.3b (verbatim): "A creature with defender can't attack."
/// This effect records the static override that removes the can't-attack restriction
/// normally imposed by Defender — the High Alert / Assault Formation family.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine enforces.
/// The presence of this effect on a <c>StaticAbility</c> records that the card's
/// oracle line grants the named set of creatures permission to attack even if they
/// have the Defender keyword; it does not model the declare-attackers legality
/// check itself.
///
/// <para>
/// Distinct from simply removing the Defender keyword: the oracle text is
/// "can attack as though they didn't have defender," not "lose defender." The
/// creatures still <em>have</em> Defender (it does not leave their rules text);
/// the restriction it imposes is suppressed. The structured "as though" form is
/// intentional — it matches the oracle phrasing precisely and clusters with other
/// "as though" permission grants in the codebase.
/// </para>
///
/// <para>
/// When <see cref="AppliesTo"/> is null, the effect applies to the static
/// ability's controlling object (the card itself). When set, it describes the
/// set of permanents that receive the permission — e.g., "creatures you control"
/// — via an <see cref="ObjectReference"/> whose <see cref="ObjectReference.Filter"/>
/// carries the card-type and controller constraints.
/// </para>
/// </remarks>
[OracleEffect("canAttackIgnoringDefender")]
public sealed record CanAttackIgnoringDefenderEffect : ContinuousEffect
{
  /// <summary>
  /// The set of creatures that receive permission to attack as though they did
  /// not have Defender. Null means the static ability's controlling object;
  /// set when the oracle line specifies a broader subject ("creatures you control").
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? AppliesTo { get; init; }
}
