namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Renown (Rule 702.112). A triggered keyword ability printed as "Renown N".
/// When this creature deals combat damage to a player, if it isn't renowned,
/// put N +1/+1 counters on it and it becomes renowned.
/// MAST records the keyword and its integer value; the trigger, renowned
/// designation, and counter-placement are engine territory.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect and ModularEffect
/// shape — <see cref="Value"/> is the renown number lifted from the printed
/// oracle text.
/// </para>
/// </summary>
[OracleEffect("renown")]
public sealed record RenownEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The renown value N printed on the card (e.g., "Renown 2" → 2).</summary>
  public required int Value { get; init; }

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
