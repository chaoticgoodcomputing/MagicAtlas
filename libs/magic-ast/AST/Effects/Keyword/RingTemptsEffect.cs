namespace MagicAST.AST.Effects.Keyword;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "the Ring tempts you" — the keyword action from Rule 701.54. A one-shot
/// instruction that advances the Ring's temptation track and designates a
/// Ring-bearer. MAST records only the instruction; the Ring-bearer choice,
/// the temptation-track level, and the resulting Ring-bearer designation are
/// resolved by the engine and intentionally not modeled here.
///
/// CR 701.54a: "Certain spells and abilities have the text 'the Ring tempts
/// you.' Each time the Ring tempts you, choose a creature you control. That
/// creature becomes your Ring-bearer until another creature becomes your
/// Ring-bearer or another player gains control of it."
/// </summary>
[OracleEffect("ringTemptsYou")]
public sealed record RingTemptsEffect : Effect;
