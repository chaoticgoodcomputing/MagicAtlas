namespace MagicAST.AST.Effects.Keyword;

using MagicAST.AST.Effects.Traits;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "Choose Island or Swamp." — the basic-land-type-choice declaration (CR 614.12's
/// as-enters chosen-value binding; CR 305.6 constrains the value to one of Plains,
/// Island, Swamp, Mountain, or Forest — "If an object uses the words 'basic land
/// type,' it's referring to one of these subtypes"). The oracle line records that
/// the controller selects a basic land type from the printed disjunction; subsequent
/// abilities that reference "the chosen type" (via
/// <see cref="MagicAST.AST.References.ChosenCharacteristicKind.BasicLandType"/>) are
/// downstream consumers of this choice (CR 607 linked abilities). MAST models only
/// the choice declaration itself, not the producer/consumer link.
///
/// <para>Timing is a separate axis: when this choice happens as the permanent enters
/// ("As this enchantment enters, choose Island or Swamp." — Roots of Life, the CR
/// 614.12 shape), the enclosing <see cref="MagicAST.AST.Abilities.StaticAbility"/>
/// carries <see cref="MagicAST.AST.Abilities.StaticTimingKind.AsThisEnters"/>; the
/// effect itself stays plain. Timing and effect are composable, never baked into the
/// effect discriminator.</para>
///
/// <para>Sibling of <see cref="ChooseCardTypeEffect"/>, but NOT structured the same
/// way: <see cref="ChooseCardTypeEffect"/> omits an explicit options list because its
/// printed disjunction is the exhaustive five-of-five card-type set (no information is
/// lost by treating the enumeration as reminder context). Here the printed disjunction
/// is frequently a real SUBSET of the five basic land types (Roots of Life offers only
/// Island or Swamp, not all five) — dropping the specific options would erase a genuine
/// game-affecting restriction, so <see cref="Options"/> records the literal printed
/// basic land type names, mirroring the existing <c>Subtypes</c> convention used for
/// basic-land-type disjunctions elsewhere (e.g. search-library filters).</para>
/// </summary>
[OracleEffect("chooseBasicLandType")]
public sealed record ChooseBasicLandTypeEffect : Effect
{
  /// <summary>
  /// The basic land type names offered as the choice, in printed order (e.g.
  /// <c>["Island", "Swamp"]</c> for Roots of Life). Each entry is one of the five
  /// basic land types per CR 305.6.
  /// </summary>
  public required IReadOnlyList<string> Options { get; init; }
}
