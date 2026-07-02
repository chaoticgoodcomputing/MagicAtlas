namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may cast [filter] spells from the top of your library." — a static
/// permission that allows the controller to cast matching spells from the top
/// of their library (Mystic Forge: "artifact spells and colorless spells";
/// Bolas's Citadel: "nonland cards").
///
/// <para>
/// This is a continuous static ability (CR 604.2): the permission persists as
/// long as the source permanent is on the battlefield. MAST models what the
/// oracle text says — which cards may be cast and from which zone — not the
/// execution (when the player actually casts, the engine handles priority, timing
/// restrictions, etc.). The <see cref="Cards"/> filter identifies which subset of
/// the library's top card qualifies; Zone=Library is implied by the effect type.
/// </para>
///
/// <para>
/// CR 604.2: "Static abilities create continuous effects, some of which are
/// prevention effects or replacement effects. These effects are active as long as
/// the permanent with the ability remains on the battlefield and has the ability,
/// or as long as the object with the ability remains in the appropriate zone …"
/// </para>
/// </summary>
[OracleEffect("mayPlayFromLibrary")]
public sealed record MayPlayFromLibraryEffect : Effect
{
  /// <summary>
  /// Which cards the controller may cast — a library-zone filter that narrows
  /// the eligible cards (e.g. artifacts only: <c>CardTypes=["artifact"]</c>;
  /// colorless only: <c>IsColorless=true</c>).
  /// </summary>
  public required ObjectFilter Cards { get; init; }
}
