namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Recover [cost] (Rule 702.59). A triggered ability that functions only while the
/// card with recover is in a player's graveyard: "When a creature is put into your
/// graveyard from the battlefield, you may pay [cost]. If you do, return this card
/// from your graveyard to your hand. Otherwise, exile this card." MAST records the
/// keyword's presence and the recover cost; the trigger, conditional return, and
/// exile clause are all reminder-text territory.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic <see cref="Cost"/> base type so future
/// variants with non-mana costs can plug in without a schema change, mirroring
/// the <see cref="CyclingEffect"/> pattern.
/// </para>
/// </summary>
[OracleEffect("recover")]
public sealed record RecoverEffect : Effect
{
  /// <summary>
  /// The cost paid to recover this card. Most commonly a <see cref="ManaCost"/>.
  /// </summary>
  public required Cost Cost { get; init; }
}
