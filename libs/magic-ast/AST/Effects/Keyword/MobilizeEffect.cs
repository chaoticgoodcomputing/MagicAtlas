namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Mobilize N (Rule 702.175, introduced in Tarkir: Dragonstorm). A triggered
/// keyword ability printed as "Mobilize N (Whenever this creature attacks,
/// create N tapped and attacking 1/1 red Warrior creature tokens. Sacrifice
/// them at the beginning of the next end step.)". MAST records the keyword and
/// its integer token-creation count; the attack trigger, token-creation,
/// tapped-and-attacking entry, and delayed-sacrifice semantics are engine
/// territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors the HideawayEffect shape — parameter
/// is a <see cref="Quantity"/> (typically a <see cref="LiteralQuantity"/>).
/// </para>
/// </summary>
[OracleEffect("mobilize")]
public sealed record MobilizeEffect : Effect
{
  /// <summary>
  /// The number of Warrior tokens created when this creature attacks
  /// (N in "Mobilize N").
  /// </summary>
  public required Quantity Amount { get; init; }
}
