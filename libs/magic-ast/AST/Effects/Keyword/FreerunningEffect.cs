namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Freerunning (Rule 702.166). "You may cast this spell for its freerunning cost
/// if you dealt combat damage to a player this turn with an Assassin or commander."
/// MAST records the keyword and its alternative mana cost; the combat-damage
/// condition and the alternative-cast semantics are conventionally inferred from
/// the rules (per the descriptive-not-engine doctrine).
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type for parity with other
/// cost-bearing keyword effects (Cycling, Bestow, Blitz, etc.). Most printings
/// use a <see cref="ManaCost"/>.
/// </para>
/// </summary>
[OracleEffect("freerunning")]
public sealed record FreerunningEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The freerunning cost paid to cast this card via the freerunning condition.
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
