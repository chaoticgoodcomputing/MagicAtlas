namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Flashback (Rule 702.34). A keyword ability that lets a player cast a card
/// from their graveyard for its flashback cost; the spell is then exiled
/// rather than going to the graveyard. Oracle form: "Flashback [cost]".
/// MAST records the keyword's presence and the flashback cost; the
/// cast-from-graveyard-then-exile machinery is engine territory.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type because flashback can be
/// a mana cost (most common) or a non-mana cost on "Flashback—[cost]" lines
/// (sacrifice a creature, tap untapped creatures, etc.).
/// </para>
/// </summary>
[OracleEffect("flashback")]
public sealed record FlashbackEffect : Effect
{
  /// <summary>The alternative cost paid to cast this card from the graveyard.</summary>
  public required Cost Cost { get; init; }
}
