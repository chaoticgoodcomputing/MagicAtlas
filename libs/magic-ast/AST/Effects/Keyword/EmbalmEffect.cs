namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Costs;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Embalm (Rule 702.88). An activated ability functioning only from the graveyard:
/// "[Cost], Exile this card from your graveyard: Create a token that's a copy of it,
/// except it's a white Zombie [subtype(s)] with no mana cost. Embalm only as a sorcery."
/// MAST records the keyword's presence and the embalm cost; the token-creation
/// mechanics are conventionally inferred from the rules and carried in the Reminder.
/// </summary>
[OracleEffect("embalm")]
public sealed record EmbalmEffect : Effect
{
  /// <summary>
  /// The cost paid to embalm this card. Most commonly a <see cref="ManaCost"/>.
  /// </summary>
  public required Cost Cost { get; init; }
}
