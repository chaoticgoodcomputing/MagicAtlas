namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Cumulative upkeep (Rule 702.24). "At the beginning of your upkeep, put an age
/// counter on this permanent, then sacrifice it unless you pay its upkeep cost for
/// each age counter on it." MAST records the keyword's presence and the cumulative
/// upkeep cost; the upkeep-trigger, age-counter-scaling, and sacrifice-unless-pay
/// semantics are conventionally inferred from the rules (per the
/// descriptive-not-engine doctrine), mirroring the EchoEffect, BestowEffect, and
/// KickerEffect patterns.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with the other
/// cost-bearing keyword effects (Echo, Kicker, Bestow, Unearth, Plot). All known
/// printings of cumulative upkeep use a <see cref="ManaCost"/>, but the
/// polymorphic base accommodates future non-mana variants.
/// </para>
/// </summary>
[OracleEffect("cumulativeUpkeep")]
public sealed record CumulativeUpkeepEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The per-age-counter upkeep cost paid each upkeep to avoid sacrificing this permanent.
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
