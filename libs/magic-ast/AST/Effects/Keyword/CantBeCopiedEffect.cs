namespace MagicAST.AST.Effects.Keyword;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "This spell can't be copied." — a static ability that applies to the spell
/// itself while it is on the stack, preventing it from being copied.
///
/// <para>
/// CR 707 (copying spells and abilities): to copy a spell means to put a copy of
/// it onto the stack; a spell with this ability can't be the source of such a copy.
/// The self-spell sibling of <see cref="CantBeCounteredEffect"/> — both are
/// negative on-the-stack statics scoped to the spell that carries them (they have
/// no target and no fields), differing only in the game action they forbid.
/// </para>
/// </summary>
[OracleEffect("cantBeCopied")]
public sealed record CantBeCopiedEffect : Effect
{
  // This effect makes the spell (or ability) uncopyable. No fields: the subject is
  // the spell itself, exactly as with CantBeCounteredEffect.
}
