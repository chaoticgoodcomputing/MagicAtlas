namespace MagicAST.AST.Effects.Counter;

using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// Blight keyword action (CR 701.68). "To blight N means to put N -1/-1 counters
/// on a creature you control."
///
/// <para>
/// CR 701.68a (verbatim): "To 'blight N' means to put N -1/-1 counters on a
/// creature you control."
/// CR 701.68b: "If a player is given the choice to blight but is unable to put
/// N -1/-1 counters on a creature they control (usually because they control no
/// creatures), they can't choose to blight."
/// </para>
///
/// <para>
/// MAST records who blights and how many, matching the oracle phrase "[player(s)]
/// blight(s) N". The choice of which creature receives the counters — and whether
/// the player is able to blight at all (CR 701.68b) — is engine territory.
/// </para>
/// </summary>
[OracleEffect(
  "blight",
  NearDuplicateOf = new[] { "fight" },
  Reason = "Distinct effects: 'blight' (High Perfect Morcant) puts -1/-1 counters / affliction on a target; 'fight' (CR 701.12) makes two creatures deal damage equal to power to each other. Edit distance 1 (b/f) is coincidental; no semantic overlap."
)]
public sealed record BlightEffect : Effect
{
  /// <summary>
  /// The player or players who perform the blight action.
  /// e.g. <c>EachOpponent</c> for "each opponent blights N".
  /// </summary>
  public required ObjectReference Player { get; init; }

  /// <summary>
  /// How many -1/-1 counters are placed. CR 701.68a: "N" in "blight N".
  /// </summary>
  public required Quantity Amount { get; init; }
}
