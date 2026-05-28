namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Multikicker (Rule 702.33c). "You may pay an additional [cost] any number of times
/// as you cast this spell."
/// MAST records the keyword's presence and the multikicker cost; the "for each time
/// it was kicked" scaling on conditional effects is inferred from the rules, mirroring
/// the KickerEffect pattern (descriptive-not-engine doctrine).
///
/// <para>
/// Distinct from <see cref="KickerEffect"/>: Multikicker is paid any number of times
/// (Rule 702.33c) whereas single-cost Kicker is paid at most once (Rule 702.33a).
/// A separate effect type is used so fixture discriminators and parsers can cleanly
/// distinguish the two without an IsMultikicker flag on the shared type.
/// </para>
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Equip, Cycling, Bestow, Echo, Kicker) — most
/// printings use a <see cref="ManaCost"/>, but the base accommodates future variants.
/// </para>
/// </summary>
[OracleEffect("multikicker")]
public sealed record MultikickerEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The multikicker cost paid any number of times as an additional cost when
  /// casting this spell. Most commonly a <see cref="ManaCost"/>, but the
  /// polymorphic <see cref="Cost"/> base accommodates future non-mana variants.
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
