namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Madness (Rule 702.35). A keyword ability that lets a player cast a card
/// they discard for its madness cost rather than putting it into the
/// graveyard. Oracle form: "Madness [cost]". MAST records the keyword's
/// presence and the madness cost; the discard-into-exile-and-cast machinery
/// is engine territory.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type because madness is
/// typically a mana cost but the rarer "Madness—Pay six {2}." form carries
/// a composite cost; both reduce to <see cref="Cost"/>.
/// </para>
/// </summary>
[OracleEffect("madness")]
public sealed record MadnessEffect : Effect
{
  /// <summary>The alternative cost paid to cast the discarded card.</summary>
  public required Cost Cost { get; init; }
}
