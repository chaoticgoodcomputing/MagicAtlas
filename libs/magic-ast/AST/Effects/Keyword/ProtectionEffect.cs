namespace MagicAST.AST.Effects.Keyword;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// Protection effect: comprehensive immunity from a quality.
/// "This permanent can't be blocked, targeted, dealt damage, enchanted, or equipped by [quality]."
/// Rule 702.16
/// </summary>
[OracleEffect("protection")]
public sealed record ProtectionEffect : Effect
{
  /// <summary>
  /// The qualities this permanent has protection from.
  /// Can be colors ("red"), card types ("Demons"), or other qualities ("everything").
  /// </summary>
  public required IReadOnlyList<ProtectionQuality> From { get; init; }

  /// <summary>
  /// True when the granting Aura says "This effect doesn't remove this Aura" (or
  /// "...doesn't remove all Auras"), per CR 702.16n: the specified Aura(s) aren't put
  /// into their owners' graveyards as a state-based action even though the enchanted
  /// permanent now has protection from the granting Aura's own quality (CR 702.16c).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
  public bool DoesNotRemoveThisAura { get; init; }
}
