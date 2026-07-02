namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may look at the top card of your library any time." A continuous static
/// permission that lets the controller inspect the top card
/// of their library whenever they choose, rather than only at times when they
/// could cast or play something. This is a persistent visibility grant, not a
/// one-shot look or a triggered look.
///
/// <para>
/// MAST records this as a static ability (the permission persists as long as the
/// source is on the battlefield — Rule 604.2). The "You may" preamble makes the
/// effect permissive (<see cref="IsOptional"/> = <c>true</c>); the controller is
/// never forced to look. There are no parameters: the subject is always "You" (the
/// controller), the zone is always the top of the library, and the timing grant is
/// "any time" — all of which are captured by the discriminator alone.
/// </para>
/// </summary>
[OracleEffect("lookAtTopCardAnyTime")]
public sealed record LookAtTopCardAnyTimeEffect : Effect
{
}
