namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Sneak (Rule 702.173). An alternative-cost keyword found on spells:
/// "Sneak [Cost] (You may cast this spell for [Cost] if you also return an
/// unblocked attacker you control to its owner's hand during the declare
/// blockers step.)"
///
/// <para>
/// MAST records the keyword and its alternative mana cost only. The
/// "return an unblocked attacker" condition is part of the reminder text
/// and is inferred from the rules; it is not represented in the AST.
/// </para>
/// </summary>
[OracleEffect("sneak")]
public sealed record SneakEffect : Effect
{
  /// <summary>
  /// The alternative mana cost paid to cast this spell via Sneak.
  /// </summary>
  public required Cost Cost { get; init; }
}
