namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Typecycling (Rule 702.32; variant subrule 702.32f): a parameterized
/// keyword printed as "[Type]cycling [cost]" — e.g., Plainscycling {2},
/// Forestcycling {1}, Swampcycling {2}. An activated ability functioning only
/// in hand: "[Cost], Discard this card: Search your library for a [Type] card,
/// reveal it, put it into your hand, then shuffle."
///
/// <para>
/// Distinct from the plain <see cref="CyclingEffect"/> (Rule 702.32 base):
/// plain Cycling's inner effect is "Draw a card", whereas Typecycling's inner
/// effect is "Search your library for a [Type] card, reveal it, put it into
/// your hand, then shuffle." The Comprehensive Rules treat these as variants
/// of the same keyword, but the inner effects differ enough that MAST models
/// them as separate effect types (per the existing pattern of one effect type
/// per distinct oracle-template shape).
/// </para>
///
/// <para>
/// MAST records the keyword's presence, the land type, and the typecycling
/// cost; the inner search/reveal/shuffle structure is conventionally inferred
/// from the rules and not re-modeled per fixture.
/// </para>
/// </summary>
[OracleEffect("typecycling")]
public sealed record TypecyclingEffect : Effect
{
  /// <summary>
  /// The basic land type to search for. The five printed variants use
  /// "Plains", "Island", "Swamp", "Mountain", "Forest" — but the field is a
  /// string to accommodate any printed sub-variant (typecycling has not been
  /// printed for non-basic types, but the schema does not foreclose it).
  /// </summary>
  public required string Type { get; init; }

  /// <summary>
  /// The cost paid to typecycle this card. Most commonly a
  /// <see cref="ManaCost"/>, mirroring <see cref="CyclingEffect.Cost"/>.
  /// </summary>
  public required Cost Cost { get; init; }
}
