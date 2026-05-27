namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Traits;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Its owner shuffles [target] into their library."
/// A zone-change effect that moves the target from its current zone into its
/// owner's library, then the owner shuffles their library. Distinct from
/// <see cref="PutOnTopOfLibraryEffect"/> (which places on top without shuffling)
/// and from <see cref="ShuffleEffect"/> (which shuffles a player's library
/// without moving a target object into it).
///
/// <para>
/// Most commonly appears as the resolution of the Unravel the Aether pattern —
/// a two-sentence oracle form "Choose target artifact or enchantment. Its owner
/// shuffles it into their library." Rule 701.20 governs zone-change actions;
/// the shuffle is an inherent part of the zone-change (the owner shuffles
/// immediately after the card is placed in the library). Rule 701.19 governs shuffle.
/// </para>
/// </summary>
[OracleEffect("shuffleIntoLibrary")]
public sealed record ShuffleIntoLibraryEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  public required ObjectReference Target { get; init; }

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
