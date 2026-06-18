namespace MagicAST.AST.Effects.Timing;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You can't cast additional spells this turn." — a duration-bounded restriction,
/// created as a consequence of an action taken this turn, that prohibits the
/// controller from casting any further spells during the current turn.
///
/// <para>
/// This effect differs from <see cref="CantCastMoreThanNSpellsEffect"/> (a
/// continuous static effect that caps every turn) in two ways: (1) it is a
/// one-shot restriction created at ability resolution rather than a persistent
/// continuous effect, and (2) "additional" means relative to the spell just
/// cast — the controller cannot cast any more spells for the remainder of the
/// current turn.
/// </para>
///
/// <para>
/// CR 602.5d: "Activated abilities that read 'Activate only as a sorcery' mean the
/// player must follow the timing rules for casting a sorcery spell." The restriction
/// is reinforced by that activation gate; see also CR 601.2 (casting a spell).
/// MAST records the restriction as written; the engine enforces it against
/// subsequent cast events (ADR 0003/0004 describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("cantCastAdditionalSpellsThisTurn")]
public sealed record CantCastAdditionalSpellsThisTurnEffect : Effect;
