namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Outlast {cost} (Rule 702.107). A keyword ability that allows a creature
/// to grow larger over time. Oracle form: "Outlast {cost} ({cost}, {T}: Put
/// a +1/+1 counter on this creature. Outlast only as a sorcery.)".
///
/// <para>
/// MAST records the keyword's presence and its mana cost parameter; the
/// activated-ability structure ({cost}, tap, sorcery-speed restriction) and
/// the counter-placement are engine territory.
/// </para>
///
/// <para>
/// Mana-cost-parameterized keyword; mirrors <see cref="FlashbackEffect"/>,
/// <see cref="MadnessEffect"/>, and the Kicker/Echo/Bestow family.
/// <see cref="Cost"/> is typed as the polymorphic <see cref="Cost"/> base to
/// mirror the existing mana-cost keyword pattern.
/// </para>
/// </summary>
[OracleEffect("outlast")]
public sealed record OutlastEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The mana cost paid to activate Outlast (e.g., "Outlast {W}" → {W}).</summary>
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
