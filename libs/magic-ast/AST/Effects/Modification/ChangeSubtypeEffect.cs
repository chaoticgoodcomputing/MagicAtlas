namespace MagicAST.AST.Effects.Modification;

using System.Text.Json.Serialization;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "Enchanted [type] is a(n) [Subtype]." / "This creature becomes the creature type
/// of your choice [until end of turn]." — a layer-4 (CR 613.1d) subtype-changing
/// continuous effect. Describes the oracle-text declaration that the target's
/// subtypes from the appropriate set are <em>set</em> to a value (CR 205.1a: "when
/// an effect sets one or more of an object's subtypes, the new subtype(s) replaces
/// any existing subtypes from the appropriate set"). The new value is either a
/// literal list (<see cref="Subtypes"/>) or a value the controller picks on
/// resolution (<see cref="ChosenSubtype"/>), or a back-reference to a creature type
/// chosen earlier in the same ability (<see cref="ChosenSubtypeReference"/>) — exactly
/// one of the three is populated.
///
/// <para>
/// The static Aura form ("Enchanted land is an Island") carries no
/// <see cref="ContinuousEffect.Duration"/>: it persists while the Aura remains
/// attached (CR 702.5 / 613.1d). The Mistform activated form is bounded —
/// "until end of turn" lands in the inherited <see cref="ContinuousEffect.Duration"/>.
/// </para>
///
/// <para>
/// Examples:
/// <list type="bullet">
///   <item>Spreading Seas — "Enchanted land is an Island." → Subtypes: ["Island"]</item>
///   <item>Convincing Mirage — "Enchanted land is a Plains." → Subtypes: ["Plains"]</item>
///   <item>Phantasmal Terrain — various basic land type shapes.</item>
///   <item>Mistform Stalker / Mistform Dreamer — "This creature becomes the creature
///   type of your choice until end of turn." → ChosenSubtype: CreatureType,
///   Duration: until end of turn.</item>
/// </list>
/// </para>
///
/// <para>
/// MAST is descriptive: this node records what the oracle line says. The rules
/// engine is responsible for how layer-4 subtype changes interact with other
/// continuous effects (CR 613.7), the implicit basic land mana ability grant
/// (CR 305.6), and landwalk evasion (CR 702.14). This stays a subtype-set effect,
/// not a <c>BecomesCreatureEffect</c>: the Mistform card remains a creature and only
/// its creature type is replaced (CR 205.1a) — no card type is added.
/// </para>
/// </summary>
[OracleEffect("changeSubtype")]
public sealed record ChangeSubtypeEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent whose subtype is being changed.
  /// Typically <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> for Aura lines,
  /// <see cref="ObjectReferenceKind.Self"/> for the Mistform "this creature" form.
  /// </summary>
  public required ObjectReference Target { get; init; }

  /// <summary>
  /// The literal subtype(s) the target is set to. For the Spreading Seas pattern this
  /// is a single element: <c>["Island"]</c>. Multiple subtypes are possible for
  /// effects that set a permanent to several subtypes simultaneously. Null (and
  /// omitted) when the subtype is player-chosen — see <see cref="ChosenSubtype"/>;
  /// exactly one of the two is populated.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? Subtypes { get; init; }

  /// <summary>
  /// Set instead of <see cref="Subtypes"/> when the new subtype is one the controller
  /// chooses as the effect resolves — "the creature type of your choice" (Mistform
  /// Stalker / Dreamer). <see cref="ChosenCharacteristicKind.CreatureType"/> records
  /// that the picked value is a creature type, so the choice is constrained per CR
  /// 205.3 ("'Merfolk' or 'Wizard' is acceptable, but 'Merfolk Wizard' is not …
  /// 'artifact', 'opponent', 'Swamp', or 'truck' can't be chosen because they aren't
  /// creature types"). This is a <em>fresh</em> on-resolution choice, distinct from
  /// <see cref="ObjectFilter.ChosenCharacteristic"/>, which back-references a value
  /// chosen earlier (the CR 607 linked-ability consumer side).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ChosenCharacteristicKind? ChosenSubtype { get; init; }

  /// <summary>
  /// Set instead of <see cref="Subtypes"/> / <see cref="ChosenSubtype"/> when the new
  /// subtype BACK-REFERENCES a characteristic chosen earlier in the SAME ability rather
  /// than being a literal value or a fresh pick on this effect's resolution —
  /// "Target creature becomes <em>that</em> type until end of turn" after
  /// "Choose a creature type other than Wall" (Imagecrafter). The demonstrative
  /// "that type" points back to the producer <see cref="MagicAST.AST.Effects.Keyword.ChooseCreatureTypeEffect"/>
  /// sitting earlier in the ability's effect list, so this is the CR 607.1 linked-ability
  /// consumer side ("two abilities [here, two effects] … one causes … the other one
  /// directly refers to those actions"). It parallels
  /// <see cref="ObjectFilter.ChosenCharacteristic"/> (the filter-side back-reference,
  /// "creatures of the chosen type"): both record a reference to an already-chosen value,
  /// distinct from <see cref="ChosenSubtype"/>, which is the fresh "the creature type of
  /// your choice" pick. <see cref="ChosenCharacteristicKind.CreatureType"/> records that
  /// the referenced value is a creature type, constrained per CR 205.3.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public ChosenCharacteristicKind? ChosenSubtypeReference { get; init; }
}
