namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// A continuous static effect that prevents a class of spells from being cast.
/// e.g. "Noncreature spells with mana value 4 or greater can't be cast."
///
/// The affected spells are described by the surrounding
/// <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/> filter on the
/// containing ability — this effect carries no filter of its own, beyond the
/// optional <see cref="Caster"/> scope below.
///
/// Rule 601.5 — Legality of casting a spell. A spell that's been put on the stack
/// in violation of this kind of restriction is removed as an illegal action;
/// see also Rule 601.2 (proposing to cast a spell).
/// </summary>
/// <remarks>
/// <para>
/// CR 601.3a: "If an effect prohibits a player from casting a spell with certain
/// qualities..." — the restriction can be either UNSCOPED (applies to every
/// player, e.g. Gaddock Teeg's "Noncreature spells with mana value 4 or greater
/// can't be cast.", where <see cref="Caster"/> is null) or CASTER-SCOPED (applies
/// to one named player only, e.g. Steel Golem's "You can't cast creature
/// spells.", where <see cref="Caster"/> is <see cref="ObjectReferenceKind.You"/>
/// — the controller of this static ability). Both forms share the same
/// discriminator because they are the same restriction (a class of spells can't
/// be cast) differing only in WHO the restriction binds; the spell-class filter
/// lives on the containing ability's <c>AffectedObjects</c> either way.
/// </para>
/// </remarks>
[OracleEffect("cantBeCast")]
public sealed record CantBeCastEffect : Effect
{
  /// <summary>
  /// Which player is restricted from casting the filtered spell class. Null
  /// means the restriction is unscoped — it applies to every player (the
  /// original Gaddock Teeg form). When set (e.g. <see cref="ObjectReferenceKind.You"/>
  /// for "You can't cast..."), only the named player is restricted; other
  /// players remain free to cast the filtered spells.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ObjectReference? Caster { get; init; }
}
