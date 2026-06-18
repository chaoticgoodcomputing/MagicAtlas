namespace MagicAST.AST.Effects.Combat;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may exert [this creature] as it attacks." — the optional cost to attack
/// that defines the Exert mechanic (CR 701.43d).
///
/// <para>
/// CR 701.43d (verbatim): "'You may exert [this creature] as it attacks' is an
/// optional cost to attack (see rule 508.1g). Some objects with this static ability
/// have a triggered ability that triggers 'when you do' printed in the same
/// paragraph. These abilities are linked. (See rule 607.2h.)"
/// </para>
///
/// <para>
/// CR 701.43a: "To exert a permanent, you choose to have it not untap during
/// your next untap step." The untap consequence is engine-side; MAST records the
/// oracle text's mechanic, not the enforcement.
/// </para>
///
/// <para>
/// This is a parameterless marker — the subject is always the source creature
/// itself (Self). It sits on a <c>StaticAbility</c> whose <c>KeywordSource</c> is
/// "Exert" and pairs with a linked <c>TriggeredAbility</c> (Event: Exerted) that
/// fires "When you do" — i.e., when the controller pays the optional exert cost.
/// </para>
/// </summary>
[OracleEffect("exert")]
public sealed record ExertEffect : Effect
{
}
