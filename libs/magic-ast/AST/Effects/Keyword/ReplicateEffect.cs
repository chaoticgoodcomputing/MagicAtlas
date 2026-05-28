namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Replicate [cost] (Rule 702.57). "When you cast this spell, copy it for each
/// time you paid its replicate cost. You may choose new targets for the copies."
/// A triggered keyword ability from Ravnica block (Guildpact, Dissension) and
/// later sets. MAST records the keyword's presence and the replicate cost;
/// the per-payment copy-creation and new-target-selection are engine territory
/// (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// cost-bearing keyword effects (Kicker, Buyback, Conspire). All known
/// printings use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("replicate")]
public sealed record ReplicateEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The replicate cost paid each time the player wants an additional copy.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic
  /// <see cref="Cost"/> base accommodates future non-mana variants.
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
