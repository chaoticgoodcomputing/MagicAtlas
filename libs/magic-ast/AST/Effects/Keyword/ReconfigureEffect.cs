namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Reconfigure (Rule 702.173). A keyword ability printed on Equipment creatures
/// that lets a player pay the reconfigure cost to attach the permanent to a
/// creature they control, or to unattach it from a creature, at sorcery speed.
/// Oracle form: "Reconfigure [cost] ([cost]: Attach to target creature you
/// control; or unattach from a creature. Reconfigure only as a sorcery. While
/// attached, this isn't a creature.)"
/// MAST records the keyword's presence and the reconfigure cost; the
/// attach/unattach mechanics, sorcery-speed restriction, and creature-status
/// switching are conventionally inferred from the rules (per the
/// descriptive-not-engine doctrine), mirroring the EquipEffect pattern.
///
/// <para>
/// <see cref="Cost"/> is the polymorphic base type to accommodate future
/// non-mana reconfigure costs without a schema change, mirroring
/// <see cref="EquipEffect"/>.
/// </para>
/// </summary>
[OracleEffect("reconfigure")]
public sealed record ReconfigureEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// The cost paid to attach or unattach this Equipment creature.
  /// Most commonly a <see cref="ManaCost"/>, but the polymorphic <see cref="Cost"/>
  /// base accommodates future non-mana reconfigure costs.
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
