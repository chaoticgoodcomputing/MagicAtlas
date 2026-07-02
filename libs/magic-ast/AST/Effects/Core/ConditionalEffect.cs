namespace MagicAST.AST.Effects.Core;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "If [condition], [effect]." — an in-ability conditional gate where a condition
/// is checked mid-ability-resolution and an effect fires only if it is true.
///
/// <para>
/// Distinct from <see cref="MagicAST.AST.Abilities.TriggeredAbility.InterveningIf"/> (CR 603.4),
/// which gates whether a TRIGGERED ABILITY fires at all: <c>ConditionalEffect</c> is an
/// effect-level gate that fires AFTER the preceding effects in the same ability have already
/// resolved, e.g. "If X is greater than or equal to the number of cards in your library,
/// you win the game." after the look on Thassa's Oracle.
/// </para>
///
/// <para>
/// MAST describes, does not execute (ADR 0004): the engine evaluates <see cref="Condition"/>
/// and conditionally applies <see cref="Then"/>; MAST simply records that the oracle text
/// has an if-then structure.
/// </para>
/// </summary>
[OracleEffect("conditional")]
public sealed record ConditionalEffect : Effect
{
  /// <summary>The condition that must be true for <see cref="Then"/> to apply.</summary>
  public required Condition Condition { get; init; }

  /// <summary>The effect that fires when <see cref="Condition"/> is true.</summary>
  public required Effect Then { get; init; }

  /// <summary>Optional effect that fires when <see cref="Condition"/> is false ("if not, …").</summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Effect? Else { get; init; }
}
