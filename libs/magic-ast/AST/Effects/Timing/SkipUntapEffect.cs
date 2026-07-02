namespace MagicAST.AST.Effects.Timing;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Untap-step opt-out: oracle text states "You may choose not to untap this
/// {permanent} during your untap step." Rule 302.6 (untap step — active player
/// determines which of their tapped permanents untap; replacement effects and
/// "doesn't untap" / "may choose not to untap" instructions modify that set).
/// See also Rule 116 (player actions) for the general framework of optional
/// player choices during turn-based actions.
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records that
/// the card's oracle line grants its controller the option to skip the
/// untap-step untap for the named object; it does not model the player's
/// runtime choice during the untap step itself.
///
/// <para>
/// No parameters — this is a descriptive marker. The subject of the opt-out is
/// the static ability's controlling object (the card the ability is printed
/// on). The "may choose" language belongs to the rules-engine semantics of
/// turn-based actions, not to <see cref="IOptionalEffect.IsOptional"/>: the
/// IsOptional trait models a "You may" PREFIX on the effect's resolution, not
/// an opt-out attached to a separate turn-based action. Fixtures should set
/// <c>IsOptional = false</c>.
/// </para>
/// <para>
/// Full-sentence form on oracle text, not a printed keyword — no
/// <c>KeywordSource</c> is recorded on the containing ability. Contrast with
/// <c>VigilanceEffect</c> (Rule 702.20), where the static "doesn't tap when
/// attacking" behaviour IS expressed as a keyword.
/// </para>
/// </remarks>
[OracleEffect("skipUntap")]
public sealed record SkipUntapEffect : Effect
{
}
