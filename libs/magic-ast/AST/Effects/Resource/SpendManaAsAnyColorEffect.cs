namespace MagicAST.AST.Effects.Resource;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Players may spend mana as though it were mana of any color." —
/// a continuous static permission (CR 609.4b) that lets the named players pay
/// mana costs using any mana in their pool as though it were mana of any color.
///
/// <para>
/// CR 609.4b (verbatim): "If an effect allows a player to spend mana 'as though
/// it were mana of any [type or color],' this affects only how the player may pay
/// a cost. It doesn't change that cost, and it doesn't change what mana was
/// actually spent to pay that cost."
/// </para>
///
/// <para>
/// This node captures the global Mycosynth Lattice / Training Grounds family of
/// static permissions that relax mana-color restrictions for a specified group of
/// players. Unlike <see cref="AddManaEffect.AnyColor"/> (which determines the
/// COLOR of mana ADDED to the pool), this models an existing mana pool's
/// spending flexibility — the mana is already there, but may be treated as any
/// color when paying costs.
/// </para>
///
/// <para>
/// MAST is descriptive: this node records the oracle declaration. The rules
/// engine applies the permission when a cost is paid (CR 609.4b / 601.2f).
/// </para>
/// </summary>
[OracleEffect("spendManaAsAnyColor")]
public sealed record SpendManaAsAnyColorEffect : ContinuousEffect
{
  /// <summary>
  /// Which players receive the permission to spend mana as though it were any
  /// color. Encoded as a string matching the oracle-text phrasing:
  /// <list type="bullet">
  ///   <item><c>"Players"</c> — all players (Mycosynth Lattice's global grant).</item>
  ///   <item><c>"You"</c> — the controller of the ability only.</item>
  ///   <item><c>"Each player"</c> — equivalent to "Players" but from an
  ///   explicit "each player" phrasing.</item>
  /// </list>
  /// A string is used (rather than a structured PlayerReference) because the
  /// current player-reference schema does not have a dedicated "all players"
  /// value separate from "each opponent" — "Players" is the oracle keyword for
  /// this global grant, and capturing it literally keeps the node round-trip
  /// stable until a richer player-reference type is available.
  /// </summary>
  public required string Beneficiary { get; init; }
}
