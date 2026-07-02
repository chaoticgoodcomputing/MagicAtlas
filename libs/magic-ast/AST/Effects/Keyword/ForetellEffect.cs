namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Foretell (Rule 702.143). A keyword ability that lets a player pay {2}
/// during their turn to exile a card from their hand face down, then cast it
/// on a later turn for its foretell cost. Oracle form: "Foretell [cost]".
/// MAST records the keyword's presence and the foretell cost; the
/// exile-and-deferred-cast machinery (along with the {2} foretell activation
/// cost) is engine territory.
/// </summary>
[OracleEffect("foretell")]
public sealed record ForetellEffect : Effect
{
  /// <summary>The alternative cost paid to cast a foretold card on a later turn.</summary>
  public required Cost Cost { get; init; }
}
