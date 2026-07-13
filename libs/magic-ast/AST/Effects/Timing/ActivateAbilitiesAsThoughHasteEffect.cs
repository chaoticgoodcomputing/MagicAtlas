namespace MagicAST.AST.Effects.Timing;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may activate abilities of creatures you control as though those
/// creatures had haste." — Thousand-Year Elixir.
///
/// <para>
/// CR 302.6: a creature's activated ability with {T}/{Q} can't be activated
/// unless it's been under control continuously since your most recent turn
/// began (the activation restriction imposed by "summoning sickness"). CR
/// 702.10a: "Haste is a static ability." Haste is what removes that
/// restriction for a creature's own controller; this effect grants that same
/// removal to the referenced abilities without granting haste itself.
/// </para>
///
/// <para>
/// MAST describes what the oracle text says: a static permission to
/// activate the referenced class of activated abilities as though their
/// source creatures had haste. It records only the permission, not a
/// simulated activation-legality check. This is distinct from a keyword-haste
/// grant (<c>GainAbilityEffect</c> with <c>HasteKeyword</c>), which would
/// also lift the CR 508.1a "controlled continuously since the turn began"
/// attack restriction — this effect says nothing about attacking, only about
/// ability activation, so it must not be modelled as a full haste grant.
/// </para>
/// </summary>
[OracleEffect("activateAbilitiesAsThoughHaste")]
public sealed record ActivateAbilitiesAsThoughHasteEffect : Effect
{
  /// <summary>
  /// The class of activated abilities this permission applies to — Thousand-Year
  /// Elixir's "abilities of creatures you control" is an
  /// <see cref="ObjectActivatedAbilityReference"/> whose
  /// <see cref="ObjectActivatedAbilityReference.PermanentFilter"/> is creatures
  /// you control (CR 602.1c: "An activated ability is the only kind of ability
  /// that can be activated.").
  /// </summary>
  public required AbilityReference AppliesTo { get; init; }
}
