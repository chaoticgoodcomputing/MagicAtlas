namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Eternalize (Rule 702.91). An activated ability functioning only in a graveyard:
/// "[Cost], Exile this card from your graveyard: Create a token that's a copy of it,
/// except it's a 4/4 black Zombie [subtype] with no mana cost. Eternalize only as a sorcery."
/// MAST records the keyword's presence and the eternalize cost; the token-copy and
/// graveyard-exile mechanics are conventionally inferred from the rules.
/// </summary>
[OracleEffect("eternalize")]
public sealed record EternalizeEffect : Effect
{
  /// <summary>
  /// The cost paid to eternalize this card. Always a <see cref="ManaCost"/> in printed
  /// oracle text, but typed as the polymorphic <see cref="Cost"/> base for consistency
  /// with other keyword-cost effects.
  /// </summary>
  public required Cost Cost { get; init; }
}
