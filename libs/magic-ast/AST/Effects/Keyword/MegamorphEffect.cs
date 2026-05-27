namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Megamorph (Rule 702.37b). A variant of Morph: the player may cast this card
/// face down as a 2/2 colorless creature for {3}, and may turn it face up by
/// paying its megamorph cost; when turned face up via the megamorph cost, a
/// +1/+1 counter is placed on the permanent. MAST records the keyword's
/// presence and the megamorph cost; the cast-face-down rules, turn-face-up
/// mechanics, and counter-placement are engine territory (per the
/// descriptive-not-engine doctrine).
///
/// <para>
/// Direct structural mirror of <see cref="MorphEffect"/> — same
/// mana-cost-parameterized keyword shape. The counter-placement on face-up
/// flip is the sole mechanical distinction and is engine territory, so no
/// additional AST field is needed beyond <see cref="Cost"/>.
/// </para>
/// </summary>
[OracleEffect("megamorph")]
public sealed record MegamorphEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The cost paid to turn this card face up (as its megamorph cost).
  /// Always a <see cref="ManaCost"/> in all known printings; the polymorphic
  /// <see cref="Cost"/> base mirrors <see cref="MorphEffect"/> for consistency.
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
