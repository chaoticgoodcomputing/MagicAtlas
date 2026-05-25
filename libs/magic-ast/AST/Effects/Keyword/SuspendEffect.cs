namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.Quantities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Suspend (Rule 702.62). A keyword that represents three abilities — a static
/// "Suspend N—[cost]" ability that functions in hand, a triggered upkeep
/// ability that removes a time counter, and a triggered ability that plays
/// the card when the last time counter is removed. MAST records the keyword's
/// presence and (when present in oracle text) the N + cost parameters; the
/// three sub-abilities are conventionally inferred from the rules.
///
/// When granted by another effect ("It gains suspend"), the gained keyword
/// carries no parameters of its own — the parameters are filled in from the
/// granting context (e.g. the time counters already placed by the same
/// resolving effect, and the card's own mana cost). In that case <see cref="N"/>
/// and <see cref="Cost"/> are both null.
/// </summary>
[OracleEffect("suspend")]
public sealed record SuspendEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// "Suspend N—[cost]" — the number of time counters placed when the card
  /// is suspended. Null when the keyword is granted without parameters.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? N { get; init; }

  /// <summary>
  /// "Suspend N—[cost]" — the alternative cost paid to suspend the card.
  /// Null when the keyword is granted without parameters.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Cost? Cost { get; init; }

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
