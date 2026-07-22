namespace MagicAST.AST.Abilities;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "during your turn" — a turn-scope timing gate that holds while the ability's
/// controller is the active player (CR 102.1: "The player whose turn it is … is the
/// active player."). The recurring "During your turn, …" / "… during your turn"
/// timing qualifier that scopes a granted keyword (Daggersail Aeronaut / Fresh-Faced
/// Recruit / Pouncing Lynx / Razorkin Needlehead: "this creature has [flying|first
/// strike] during your turn"), a cast-prohibition (Dragonlord Dromoka: "Your opponents
/// can't cast spells during your turn."; Myrel, Shield of Argive), or a cost reduction
/// (Mental Modulation: "This spell costs {1} less to cast during your turn.") to the
/// controller's own turn.
///
/// <para>
/// A field-less marker: "your" is always the ability's controller (self-referential,
/// like <see cref="PairedCondition"/> / <see cref="VoidCondition"/>), and the qualifier
/// "during your turn" is fixed and invariant across the family, so there is nothing to
/// parameterise. The turn-phase timing scope carries no quantity and no referent that
/// varies. A different turn scope ("during an opponent's turn", "during each other
/// player's turn") is a distinct phrase and would earn its own arm; this node encodes
/// only the fixed "your turn" idiom.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed timing gate; the engine
/// reads whether it is currently the controller's turn (whether the controller is the
/// active player, CR 102.1 / 500.1), MAST does not pre-evaluate it. Structured to this
/// dedicated <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 102.1 (verbatim): "A player who is participating in the game is one of the game's
/// players. … The player whose turn it is, is the active player."
/// CR 500.1 (excerpt): "A turn consists of five phases, in this order: beginning, precombat
/// main, combat, postcombat main, and ending."
/// </summary>
[ConditionKind("duringYourTurn")]
public sealed record DuringYourTurnCondition : Condition;
