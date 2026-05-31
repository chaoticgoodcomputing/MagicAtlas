namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may play an additional land on each of your turns." — Rule 305.2
/// (land-play limit). A continuous static effect that grants the controller
/// permission to play one more land per turn than the normal limit of one.
/// The subject is always "You" (the controller); no target, duration, or
/// condition field is needed because the effect persists by virtue of the
/// permanent's presence on the battlefield (Rule 604.3).
/// </summary>
/// <remarks>
/// MAST describes what the oracle text says, not what the rules engine
/// enforces. The presence of this effect on a <c>StaticAbility</c> records
/// that the card's oracle line grants its controller the option to play one
/// additional land per turn; it does not model runtime enforcement of the
/// increased limit.
///
/// <para>
/// The "You may" preamble means <see cref="IsOptional"/> is <c>true</c>:
/// the controller is not required to play the extra land — the grant is
/// purely permissive. Contrast with unconditional replacement effects
/// (e.g., <see cref="BlockAdditionalEffect"/>) which use <c>IsOptional = false</c>.
/// </para>
/// <para>
/// Full-sentence form on oracle text, not a printed keyword — no
/// <c>KeywordSource</c> is recorded on the containing ability. Covers
/// Exploration, Oracle of Mul Daya, Summer Bloom (triggered shape),
/// and similar land-drop expanders.
/// </para>
/// </remarks>
[OracleEffect("playAdditionalLand")]
public sealed record PlayAdditionalLandEffect : Effect
{
}
