namespace MagicAST.AST.Effects.Resource;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;
using MagicAST.AST.Effects.Traits;

/// <summary>
/// "add [mana]"
/// </summary>
[OracleEffect("addMana")]
public sealed record AddManaEffect : Effect
{
  public required string Mana { get; init; }

  /// <summary>
  /// For effects like "add one mana of any color"
  /// </summary>
  public bool AnyColor { get; init; }

  /// <summary>
  /// For "add one mana of any type that permanent produced" (Kinnan, Bonder Prodigy) —
  /// the produced mana's type is determined by the mana type the triggering permanent just
  /// produced. Distinct from <see cref="AnyColor"/> (freely chosen from the five colors):
  /// AnyType means the mana mirrors the specific type actually produced by the tap event.
  /// Includes colorless ({C}), making it strictly broader than <see cref="AnyColor"/>.
  /// CR 106.1: "Mana is the primary resource in the game. Players spend mana to pay costs,
  /// usually when casting spells and activating abilities." The mana types are W, U, B, R,
  /// G, and C (colorless).
  /// </summary>
  public bool AnyType { get; init; }

  /// <summary>
  /// For "add one mana of the chosen color" (Coldsteel Heart, Shimmerdrift Vale,
  /// Thriving lands) — the produced mana's color is the color CHOSEN as this
  /// permanent entered, the consumer side of a CR 607 linked ability whose producer
  /// is the "As this enters, choose a color" <see cref="MagicAST.AST.Effects.Keyword.ChooseColorEffect"/>.
  ///
  /// <para>Structural marker parallel to <see cref="AnyColor"/>: both record that
  /// the produced mana's color is determined at resolution rather than fixed in
  /// <see cref="Mana"/> (any of five colors vs. the one prior choice), so the chosen
  /// reference is never free-texted into the <see cref="Mana"/> scalar. A bool rather
  /// than a kind-enum because the only chosen characteristic that can determine a
  /// mana color is a chosen color — a chosen creature type cannot — so the enum
  /// would carry exactly one meaningful value. Mirrors
  /// <see cref="ChooseColorEffect"/>'s doctrine: MAST records only the reference to
  /// the choice, not the producer→consumer link (that is engine territory).</para>
  ///
  /// <para>Under CR 605.1a the enclosing activated ability is still a mana ability —
  /// it doesn't require a target, it could add mana when it resolves, and it's not a
  /// loyalty ability — so the ability carries <c>IsManaAbility = true</c>.</para>
  /// </summary>
  public bool OfChosenColor { get; init; }

  /// <summary>
  /// Descriptive capture of a "Spend this mana only to &lt;X&gt;" restriction that
  /// follows the mana-production sentence (e.g. "cast a creature spell of the
  /// chosen type" on Unclaimed Territory). MAST describes; it does not execute —
  /// this holds the restriction text verbatim rather than a structured spend
  /// predicate. Null when the produced mana carries no spend restriction.
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public string? SpendRestriction { get; init; }

  /// <summary>
  /// For variable amounts
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Quantity? Amount { get; init; }

  /// <summary>
  /// For "Add X mana <b>in any combination of</b> {W} and/or {U}" (Calciform
  /// Pools) — the produced mana is <see cref="Amount"/> units (here
  /// <c>VariableQuantity.X</c>), each freely chosen from this colour set:
  /// <c>["W","U"]</c> for the restricted set, <c>["W","U","B","R","G"]</c> for
  /// "…in any combination of colors". Per CR 106.4 — "When an effect instructs a
  /// player to add mana, that mana goes into a player's mana pool" — the colours
  /// are the produced mana, chosen at resolution.
  ///
  /// <para>Pairs with <see cref="Amount"/> (the count of units) and parallels the
  /// <see cref="AnyColor"/> branch: the colour is the choice, so
  /// <see cref="Mana"/> is <c>""</c> in this shape rather than free-texting the
  /// "in any combination of …" phrase into the <see cref="Mana"/> scalar. Differs
  /// from <see cref="AnyColor"/> in carrying a count and a possibly-restricted
  /// colour set rather than a single mana of all five colours.</para>
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public IReadOnlyList<string>? AnyCombinationOf { get; init; }
}
