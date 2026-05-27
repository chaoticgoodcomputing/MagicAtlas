namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Backup (Rule 702.165). A triggered keyword ability printed as "Backup N".
/// When this creature enters, put N +1/+1 counters on target creature. If that
/// is another creature, it also gains the non-backup abilities of this creature
/// printed below the backup ability until end of turn.
/// MAST records the keyword and its integer value; the counter placement,
/// ability-grant, and "printed below this one" scoping are engine territory
/// per the descriptive-not-engine doctrine.
///
/// <para>
/// Integer-parameterized keyword; mirrors the BushidoEffect and SoulshiftEffect
/// shape — <see cref="Value"/> is the backup number lifted from the printed
/// oracle text.
/// </para>
/// </summary>
[OracleEffect("backup")]
public sealed record BackupEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>The backup value N printed on the card (e.g., "Backup 2" → 2).</summary>
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
