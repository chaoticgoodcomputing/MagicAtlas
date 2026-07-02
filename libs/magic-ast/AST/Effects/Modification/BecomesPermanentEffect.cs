namespace MagicAST.AST.Effects.Modification;

using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[This permanent] becomes a [card types/subtypes] permanent (not necessarily a
/// creature) with [P/T], gaining [abilities], for [duration]." — the general
/// "animate" template, GENERALISED to the noncreature case: a single continuous
/// effect (CR 611) that, for a duration, sets a chosen permanent's characteristics
/// to a fully-specified box that need NOT include the creature card type.
///
/// <para>
/// The motivating case is Captain Rex Nebula: "Until end of turn, it becomes a
/// Vehicle artifact with base power and toughness each equal to its mana value, and
/// it gains crew 2 and …". A Vehicle is an artifact <b>subtype</b> and is NOT a
/// creature unless and until it is crewed (CR 301.7: "A Vehicle isn't a creature
/// unless an effect or ability says it is. … A Vehicle that's become a creature can
/// be affected by anything that affects a creature."). So this transformation makes
/// the object an <i>artifact</i> (a noncreature permanent) that happens to carry a
/// power/toughness box and the crew keyword — exactly the Karn "noncreature artifact"
/// shape, but with the creature type DELIBERATELY absent.
/// </para>
///
/// <para>
/// <b>Why a distinct node rather than reusing <see cref="BecomesCreatureEffect"/>.</b>
/// <see cref="BecomesCreatureEffect"/> asserts, by its very discriminator, "becomes a
/// creature" — its <c>CardTypes</c> always include "creature" (CR 205). Reusing it to
/// model "becomes a Vehicle artifact" would be a fidelity error a rules judge would
/// reject: the object does not become a creature (CR 301.7). This node is the general
/// continuous-animate that names whatever card types/subtypes the text states, leaving
/// "creature" out when the text leaves it out. Field set mirrors
/// <see cref="BecomesCreatureEffect"/> so the two are interchangeable wherever only the
/// type assertion differs.
/// </para>
///
/// <para>
/// Layer/timestamp ordering (CR 613) — how this continuous effect interacts with
/// others, which layer each characteristic change lands in — is ENGINE territory and
/// is deliberately not modeled here (descriptive-not-executive doctrine, ADR 0004).
/// </para>
///
/// <para>
/// Example — Captain Rex Nebula (the chosen permanent): CardTypes ["artifact"],
/// AddedSubtypes ["Vehicle"], Power/Toughness = DerivedQuantity(ManaValue), Colors [],
/// GainedAbilities = [Crew 2 (static), Crash Land (triggered)], Duration: untilTime
/// (end of turn). Note the absence of "creature" from CardTypes — the hallmark of the
/// noncreature case.
/// </para>
/// </summary>
[OracleEffect("becomesPermanent")]
public sealed record BecomesPermanentEffect : ContinuousEffect
{
  /// <summary>
  /// The permanent that becomes the stated permanent. For Captain Rex Nebula this is
  /// a <see cref="ObjectReferenceKind.Target"/> reference to "target nonland permanent
  /// you control".
  /// </summary>
  public required ObjectReference Subject { get; init; }

  /// <summary>
  /// The power the permanent's box is set to (CR 208). A <see cref="Quantity"/> so
  /// variable animates ("base power … equal to its mana value") are expressible.
  /// </summary>
  public required Quantity Power { get; init; }

  /// <summary>
  /// The toughness the permanent's box is set to (CR 208).
  /// </summary>
  public required Quantity Toughness { get; init; }

  /// <summary>
  /// The full color set the permanent becomes (CR 105), as WUBRG codes. Empty when
  /// the text states no color change (Captain Rex Nebula states none).
  /// </summary>
  public required IReadOnlyList<string> Colors { get; init; }

  /// <summary>
  /// The card types the permanent has for the duration (CR 205.2), lowercase to match
  /// the <see cref="ObjectFilter.CardTypes"/> convention — e.g. ["artifact"] for
  /// "becomes a Vehicle artifact". DELIBERATELY may omit "creature" (CR 301.7: a
  /// Vehicle isn't a creature), which is the distinction from
  /// <see cref="BecomesCreatureEffect"/>.
  /// </summary>
  public required IReadOnlyList<string> CardTypes { get; init; }

  /// <summary>
  /// The subtypes added (CR 205.3), title-cased to match the
  /// <see cref="ObjectFilter.Subtypes"/> convention — e.g. ["Vehicle"].
  /// </summary>
  public required IReadOnlyList<string> AddedSubtypes { get; init; }

  /// <summary>
  /// The abilities granted as part of the transformation (CR 113.6). Each is a full
  /// structured <see cref="Ability"/> — for Captain Rex Nebula, the Crew 2 static
  /// keyword ability and the nested "Crash Land" triggered ability. Empty when the
  /// animate spec grants no abilities.
  /// </summary>
  public required IReadOnlyList<Ability> GainedAbilities { get; init; }
}
