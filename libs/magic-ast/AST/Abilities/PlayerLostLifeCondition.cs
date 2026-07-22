namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "an opponent lost life this turn" / "an opponent lost 2 or more life this turn" — a
/// backward-looking, turn-scoped gate on whether a player LOST LIFE during the current
/// turn, optionally above a threshold (CR 119.3: "If an effect causes a player to gain
/// life or lose life, that player's life total is adjusted accordingly."). The bare form
/// is Spectacle's cast precondition (CR 702.137a — a spell may be cast for its spectacle
/// cost "if an opponent lost life this turn"; the same surface also gates Spikewheel
/// Acrobat's Raid-style enters-with-a-counter clause); the thresholded form is Bloodchief
/// Ascension's end-step intervening-if ("if an opponent lost 2 or more life this turn, you
/// may put a quest counter on this enchantment").
///
/// <para>
/// <see cref="Player"/> names whose life loss is checked (<see cref="ControllerFilter.Opponent"/>
/// for "an opponent"), composing the same controller axis <see cref="CountCondition"/> uses
/// rather than a new one. <see cref="Amount"/> carries the optional threshold on how much
/// life was lost: null for the bare existence check ("lost life" = lost one or more), a
/// <see cref="Comparison"/> for the explicit bound ("2 or more life" →
/// <see cref="ComparisonOperator.GreaterThanOrEqual"/> 2). This is a threshold on the
/// AMOUNT OF LIFE LOST, not a count of players — reference-not-resolution (ADR 0004): the
/// engine reads the actual per-turn life-loss history and evaluates it; MAST records the
/// printed gate.
/// </para>
///
/// <para>
/// The marker-only counting sibling <see cref="MagicAST.AST.References.LostLifeThisTurnPredicate"/>
/// restricts an <see cref="ObjectFilter"/> (Gev's "each opponent who lost life this turn");
/// THIS is the boolean/threshold gate in <see cref="Condition"/> position, carrying the
/// player scope and amount directly because there is no enclosing filter to hang them on.
/// Structured to this dedicated arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// CR 119.3 (verbatim): "If an effect causes a player to gain life or lose life, that
/// player's life total is adjusted accordingly."
/// CR 702.137a (excerpt, Spectacle): a player may cast a spell for its spectacle cost "if
/// an opponent lost life this turn."
/// </summary>
[ConditionKind("lifeLost")]
public sealed record PlayerLostLifeCondition : Condition
{
  /// <summary>Whose life loss is checked — <see cref="ControllerFilter.Opponent"/> for "an opponent lost life".</summary>
  public required ControllerFilter Player { get; init; }

  /// <summary>
  /// The optional threshold on how much life was lost this turn. Null for the bare
  /// existence form ("an opponent lost life this turn" — one or more); a
  /// <see cref="Comparison"/> for the explicit bound ("2 or more life" →
  /// <see cref="ComparisonOperator.GreaterThanOrEqual"/> 2).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Comparison? Amount { get; init; }
}
