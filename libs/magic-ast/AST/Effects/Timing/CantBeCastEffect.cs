namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A continuous static effect that prevents a class of spells from being cast.
/// e.g. "Noncreature spells with mana value 4 or greater can't be cast."
///
/// The affected spells are described by the surrounding
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/> filter on the
/// containing ability — this effect carries no filter or payload of its own.
///
/// Rule 601.5 — Legality of casting a spell. A spell that's been put on the stack
/// in violation of this kind of restriction is removed as an illegal action;
/// see also Rule 601.2 (proposing to cast a spell).
/// </summary>
[OracleEffect("cantBeCast")]
public sealed record CantBeCastEffect : Effect
{
}
