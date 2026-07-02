namespace MagicAST.AST.Effects.Battle;

using MagicAST.AST.Effects;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Marker for the Siege mechanic (the March-of-the-Machine "Battle — Siege"
/// subtype). Recorded on a <c>StaticAbility</c> whose <c>Reminder</c> carries the
/// verbatim standalone Siege type-reminder parenthetical
/// ("(As a Siege enters, choose an opponent to protect it. You and others can
/// attack it. When it's defeated, exile it, then cast it transformed.)").
/// </summary>
/// <remarks>
/// Field-less by design — same keyword-marker doctrine as Crew/Bushido/Saddle.
/// Its sole job is to structurally anchor the Siege-ness of the card's rules text
/// (a query anchor) while honoring no-silent-drop: the reminder is preserved via
/// the existing <c>Reminder</c> mechanism rather than discarded.
///
/// <para>
/// MAST describes, it does not execute. The Siege life-cycle — choose a protector,
/// be attacked, and on defeat (defense reaches 0) exile + cast transformed
/// (CR 310.7) — is engine territory, summarized in the captured reminder, not
/// modeled here. The italic type-reminder itself has no game function (CR 207.2).
/// </para>
///
/// <para>
/// A Siege's <em>defense</em> value (Battles' loyalty-like number) is absent from
/// the MAST <c>Input</c> model, so it is out of scope; MAST cannot model an input
/// it does not receive.
/// </para>
/// </remarks>
[OracleEffect("siege")]
public sealed record SiegeEffect : Effect
{
  // Field-less marker. Semantics are fixed by the Siege subtype rules
  // (CR 310, CR 310.7); the engine handles the defeat → exile → cast-transformed
  // life cycle, which MAST does not execute.
}
