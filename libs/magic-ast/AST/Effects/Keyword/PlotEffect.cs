namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Plot (Rule 702.170). "You may pay [cost] and exile this card from your hand.
/// Cast it as a sorcery on a later turn without paying its mana cost. Plot only
/// as a sorcery."
/// MAST records the keyword's presence and the plot cost; the exile-from-hand,
/// deferred-cast, and sorcery-speed restrictions are conventionally inferred
/// from the rules (per the descriptive-not-engine doctrine), mirroring the
/// KickerEffect, UnearthEffect, and BestowEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow, Echo, Kicker, Unearth)
/// — all known Plot printings use a <see cref="ManaCost"/>, but the base
/// accommodates future non-mana variants.
/// </para>
///
/// <para>
/// Scope: single-cost plot only (Rule 702.170a). The "cast it as a sorcery on a
/// later turn without paying its mana cost" clause is engine territory — MAST
/// does not model the deferred-cast mechanic.
/// </para>
/// </summary>
[OracleEffect("plot")]
public sealed record PlotEffect : Effect
{
  /// <summary>
  /// The plot cost paid to exile this card from hand for later casting. Most
  /// commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana variants.
  /// </summary>
  public required Cost Cost { get; init; }
}
