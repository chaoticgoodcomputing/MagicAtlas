namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Firebending N (Avatar: The Last Airbender). A triggered keyword ability
/// printed as "Firebending N (Whenever this creature attacks, add N {R}.
/// This mana lasts until end of combat.)". MAST records the keyword and its
/// integer value; the attack trigger, mana-addition, and end-of-combat duration
/// are engine territory per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors <see cref="BushidoEffect"/> and
/// <see cref="MobilizeEffect"/> in shape. The <see cref="Value"/> is the
/// printed integer N (the number of {R} mana added when attacking).
/// Variable-value printings ("Firebending X, where X is ...") are out of scope
/// for this batch.
/// </para>
/// </summary>
[OracleEffect("firebending")]
public sealed record FirebendingEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The amount of {R} mana added whenever this creature attacks
  /// (N in "Firebending N").
  /// </summary>
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
