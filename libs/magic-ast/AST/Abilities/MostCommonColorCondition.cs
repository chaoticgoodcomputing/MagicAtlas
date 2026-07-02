namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "[color] is the most common color among all [objects]" (optionally "or is tied
/// for most common") — a prevalence predicate that is true when the named color is
/// the DOMINANT color across a filtered set of objects. Paradigm card: Halam Djinn —
/// "This creature gets -2/-2 as long as red is the most common color among all
/// permanents or is tied for most common."
///
/// <para>
/// Distinct from <see cref="CountCondition"/>: a <c>CountCondition</c> counts the
/// NUMBER of objects matching a filter and compares it to a threshold; this condition
/// takes a MAX-BY-COLOR over the filtered set and asks whether a specific color wins
/// (or, when <see cref="IncludeTies"/> is set, wins or draws) that tally. A board with
/// five red permanents but six white permanents fails this predicate for red, yet
/// either color would satisfy any purely numeric count. The counted quantity is a
/// per-color histogram, not an object count — so it earns its own node rather than a
/// reshaping of <see cref="CardTypeDiversityCondition"/> (which counts distinct card
/// TYPES, not the maximum tally among colors).
/// </para>
///
/// <para>
/// There is NO CR rule defining "most common color": it is a card-defined,
/// engine-evaluated characteristic tally, not a keyword or named game quality. MAST
/// records it as written — reference-not-resolution (ADR 0004): the engine counts the
/// colors of the objects in <see cref="Among"/> and evaluates the max; MAST does not
/// pre-evaluate the phrase into a boolean.
/// </para>
///
/// <para>
/// CR 105.1: the five colors are white, blue, black, red, and green — recorded here on
/// the WUBRG code axis (<see cref="Color"/> = "R" for red) to match
/// <see cref="ObjectFilter.Colors"/> and the mana-symbol vocabulary, not the English
/// color word.
/// </para>
/// </summary>
[ConditionKind("mostCommonColor")]
public sealed record MostCommonColorCondition : Condition
{
  /// <summary>
  /// The color whose prevalence is tested, as a WUBRG code — "R" for red — matching
  /// <see cref="ObjectFilter.Colors"/> and the mana-symbol vocabulary (NOT the word
  /// "red"). CR 105.1.
  /// </summary>
  public required string Color { get; init; }

  /// <summary>
  /// <c>true</c> for the "or is tied for most common" form (the color wins OR draws
  /// the color tally); <c>false</c> when a strict maximum is required.
  /// </summary>
  public required bool IncludeTies { get; init; }

  /// <summary>
  /// The set of objects the color tally is taken over — <c>{ CardTypes = ["permanent"] }</c>
  /// for "among all permanents".
  /// </summary>
  public required ObjectFilter Among { get; init; }
}
