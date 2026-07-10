namespace MagicAST.AST.Abilities;

using System.Text.Json.Serialization;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// One selectable mode of a <b>Spree</b> spell — a pairing of an additional cost
/// with an effect, printed as "+ [cost] — [effect]".
///
/// <para>
/// Spree is a static ability of some modal spells: CR 702.172a — "Spree is a static
/// ability found on some modal spells (see rule 700.2). It represents two abilities.
/// The first is a spell ability. 'Spree' means 'Choose one or more additional costs.'"
/// A Spree spell lists two or more modes; the caster chooses one or more of them and
/// pays each chosen mode's additional cost as an additional cost to cast the spell.
/// This is distinct from an ordinary <see cref="ModalAbility"/> ("Choose one —",
/// "Choose two —") where the modes carry no per-mode cost: each Spree mode couples its
/// OWN additional cost to its effect. MAST models each printed "+ [cost] — [effect]"
/// line as one <c>SpreeModeAbility</c>; the "choose one or more" selection is recorded
/// by the sibling Spree <see cref="StaticAbility"/> keyword ability on the same card.
/// </para>
/// </summary>
[OracleAbility("spreeMode")]
public sealed record SpreeModeAbility : Ability
{
  /// <inheritdoc/>
  [JsonIgnore]
  public override AbilityKind AbilityKind => AbilityKind.Spell;

  /// <summary>
  /// The additional cost paid to choose this mode (the "+ [cost]" half). Almost
  /// always a <see cref="ManaCost"/>, e.g. "+ {1}".
  /// </summary>
  public required Cost AdditionalCost { get; init; }

  /// <summary>
  /// The effect(s) this mode produces when chosen (the "— [effect]" half).
  /// </summary>
  public required IReadOnlyList<Effect> Effects { get; init; }
}
