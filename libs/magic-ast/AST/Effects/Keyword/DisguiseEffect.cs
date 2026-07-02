namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Disguise (Rule 702.168). An alternative casting mode that puts the card onto the
/// battlefield face down as a 2/2 creature with ward {2}. MAST records the keyword's
/// presence and the disguise cost; the face-down/ward and turn-face-up semantics are
/// conventionally described in the Reminder parenthetical.
/// </summary>
[OracleEffect("disguise")]
public sealed record DisguiseEffect : Effect
{
  /// <summary>
  /// The cost paid to turn this card face up from disguise.
  /// </summary>
  public required Cost Cost { get; init; }
}
