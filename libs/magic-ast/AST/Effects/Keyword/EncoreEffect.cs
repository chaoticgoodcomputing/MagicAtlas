namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Encore (Rule 702.142). An activated ability functioning only from the graveyard:
/// "[Cost], Exile this card from your graveyard: For each opponent, create a token
/// copy of this card that attacks that opponent this turn if able. Those tokens gain
/// haste. Sacrifice those tokens at the beginning of the next end step."
///
/// <para>
/// MAST records only the keyword and its associated mana cost. The token-copy-per-opponent
/// structure is conventional from the rules and is carried in reminder text.
/// </para>
/// </summary>
[OracleEffect("encore")]
public sealed record EncoreEffect : Effect
{
  /// <summary>
  /// The cost paid to activate encore. Typically a mana cost.
  /// </summary>
  public required Cost Cost { get; init; }
}
