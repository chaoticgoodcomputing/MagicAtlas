namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Entwine effect: a modal spell's "Entwine [cost]" keyword. Paying the entwine
/// cost in addition to the spell's mana cost lets the controller choose all
/// modes instead of the usual subset. Rule 702.41. MAST records only the
/// keyword's presence and its cost parameter; the mode-selection override is
/// engine territory, not described by the oracle line itself.
/// </summary>
[OracleEffect("entwine")]
public sealed record EntwineEffect : Effect
{
  /// <summary>
  /// The additional cost paid to choose all modes of the modal spell.
  /// </summary>
  public required Cost Cost { get; init; }
}
