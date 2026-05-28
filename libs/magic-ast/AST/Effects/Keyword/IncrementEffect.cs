namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Increment (CR 702.191a). A triggered keyword ability printed as a bare
/// keyword token. Per CR 702.191a: "Increment is a triggered ability.
/// 'Increment' means 'Whenever you cast a spell, if this permanent is a
/// creature and the amount of mana spent to cast that spell is greater than
/// this creature's power or this creature's toughness, put a +1/+1 counter
/// on this creature.'"
///
/// <para>
/// Although Increment is a triggered ability under the rules, it is printed
/// as a bare keyword token (no cost, no reminder text inline). MAST records
/// the keyword's presence (descriptive-not-engine doctrine); the trigger
/// condition, mana comparison, and counter placement are engine territory.
/// </para>
///
/// <para>
/// Parameterless keyword marker; mirrors the <see cref="StartYourEnginesEffect"/>
/// and <see cref="TrampleEffect"/> shape.
/// </para>
/// </summary>
[OracleEffect("increment")]
public sealed record IncrementEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
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
