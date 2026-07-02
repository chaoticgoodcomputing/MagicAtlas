namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You have no maximum hand size." — Rule 402.2. A continuous static effect
/// that removes the controller's maximum hand size restriction for as long as
/// the source permanent is on the battlefield. The subject is always "You"
/// (the controller); no target, duration, or condition field is needed because
/// the effect persists by virtue of the permanent's presence (Rule 604.3).
/// Parameterless, mirroring AscendEffect and TakeInitiativeEffect.
/// </summary>
[OracleEffect("noMaxHandSize")]
public sealed record NoMaxHandSizeEffect : Effect
{
}
