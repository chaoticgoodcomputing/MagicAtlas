namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// An effect that wraps a modal choice inside another ability's resolution.
/// Used when oracle text inserts a "choose one —" preamble inside a triggered
/// or activated ability's effect stream, e.g.:
/// <code>
///   When [self] dies, choose one —
///   • [option A]
///   • [option B]
/// </code>
/// The ability's <c>Effects</c> list carries a single <see cref="ModalEffect"/>
/// whose <see cref="Modes"/> mirror the spell-level <see cref="ModalAbility"/>
/// shape: each option is its own <see cref="Ability"/> (typically a
/// <see cref="SpellAbility"/>) wrapped in a <see cref="ModalOption"/>.
///
/// Rule 700.2 — modal preamble.
/// </summary>
/// <remarks>
/// This is the <see cref="Effect"/>-typed sibling of <see cref="ModalAbility"/>.
/// They mirror each other intentionally:
/// <list type="bullet">
///   <item><see cref="ModalAbility"/> models a modal SPELL ("Choose one — ..."),
///         where the modal preamble is the whole ability.</item>
///   <item><see cref="ModalEffect"/> models a modal EFFECT — the modal choice is
///         one step inside another ability's resolution sequence
///         ("When X happens, choose one — ...").</item>
/// </list>
/// Effects, not abilities, are what trigger/activated/spell bodies carry, so the
/// nested case requires an Effect-typed wrapper.
/// </remarks>
[OracleEffect("modal")]
public sealed record ModalEffect : Effect, IOptionalEffect, IDurativeEffect, IPreventableEffect
{
  /// <summary>
  /// How many modes must/can be chosen. Mirrors <see cref="ModalAbility.ModeSelection"/>.
  /// </summary>
  public required ModeSelection ModeSelection { get; init; }

  /// <summary>
  /// The available modes to choose from. Mirrors <see cref="ModalAbility.Modes"/>.
  /// </summary>
  public required IReadOnlyList<ModalOption> Modes { get; init; }

  /// <summary>
  /// Whether the same mode can be chosen more than once.
  /// Mirrors <see cref="ModalAbility.AllowDuplicates"/>.
  /// </summary>
  public bool AllowDuplicates { get; init; }

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
