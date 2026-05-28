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
public sealed record EternalizeEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The cost paid to eternalize this card. Always a <see cref="ManaCost"/> in printed
  /// oracle text, but typed as the polymorphic <see cref="Cost"/> base for consistency
  /// with other keyword-cost effects.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>Whether this effect carries a "You may" prefix in oracle text. (IOptionalEffect)</summary>
  public bool IsOptional { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing to perform this one. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDo { get; init; }

  /// <summary>Optional follow-up effect contingent on the controller choosing NOT to perform this one. Rule 117.7. (IOptionalEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? IfYouDoNot { get; init; }

  /// <summary>Duration clause attached to this effect, if any. (IDurativeEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Duration? Duration { get; init; }

  /// <summary>"Unless [player] pays [cost]" preventable clause, if any. (IPreventableEffect)</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public UnlessClause? UnlessClause { get; init; }
}
