namespace MagicAST.AST.Effects.ZoneChange;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "transform this creature" / "transform [target]"
///
/// Describes the transform keyword action (Rule 701.28): a double-faced permanent
/// transforms by rotating to its other face. MAST records the instructed action and
/// the subject; the rules-engine mechanics of which face becomes visible and any
/// triggered "whenever this creature transforms" abilities are engine territory per
/// the descriptive-not-engine doctrine.
///
/// Distinct from the Daybound/Nightbound day/night system (<see cref="DayNightEffect"/>):
/// classic Innistrad werewolves carry explicit triggered abilities that instruct
/// "transform this creature"; Daybound/Nightbound cards transform as part of the
/// day/night state-based system and carry no explicit transform instruction in
/// their oracle text.
/// </summary>
[OracleEffect("transform")]
public sealed record TransformEffect : Effect
{
  /// <summary>
  /// The permanent being transformed. Typically <see cref="ObjectReference.Self()"/>
  /// for "transform this creature", but may reference a target for effects that
  /// transform another permanent.
  /// </summary>
  public required ObjectReference Target { get; init; }
}
