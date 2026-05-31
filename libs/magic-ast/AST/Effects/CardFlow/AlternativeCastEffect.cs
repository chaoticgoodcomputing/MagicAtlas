namespace MagicAST.AST.Effects.CardFlow;

using System.Text.Json.Serialization;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "You may cast this card from [zone] by paying [cost] rather than its mana cost" — the
/// shared permission a CR-static alternative-cast keyword grants: Flashback (CR 702.34,
/// from graveyard), Escape (702.139), Aftermath, Jump-start, Retrace; Madness (702.35,
/// from exile); and the cast-from-hand conditional family Surge, Spectacle, Freerunning.
///
/// <para>
/// These keywords are static abilities (per their CR), so the keyword combinator emits a
/// <c>StaticAbility</c> carrying this effect — not a card-level attribute — which keeps
/// keyword identity on a correctly-categorized ability and is testable through the
/// keyword-expansion harness.
/// </para>
///
/// <para>
/// The alternative <see cref="Cost"/> is the shared polymorphic <see cref="Costs.Cost"/>
/// primitive, so a keyword that pays by exiling or discarding filtered cards reuses the
/// existing cost vocabulary: Escape's "exile N other cards from your graveyard" is a
/// <see cref="CompositeCost"/> bundling the mana cost with an
/// <see cref="ExileCost"/>{Filter, FromZone, Quantity}; Jump-start's "discard a card" a
/// <see cref="DiscardCost"/>. The card itself is the implicit subject ("this card"); the
/// zone it is cast from is <see cref="FromZone"/>. The post-cast exile (Flashback) and
/// stack/zone bookkeeping are engine territory (ADR 0003/0004 describe-not-execute).
/// </para>
/// </summary>
[OracleEffect("alternativeCast")]
public sealed record AlternativeCastEffect : Effect
{
  /// <summary>
  /// The zone this card may be cast from: Graveyard (Flashback/Escape/Aftermath/
  /// Jump-start/Retrace), Exile (Madness/Foretell), or Hand (Surge/Spectacle).
  /// </summary>
  public required Zone FromZone { get; init; }

  /// <summary>
  /// The alternative cost paid instead of the mana cost. A <see cref="ManaCost"/>, or a
  /// <see cref="CompositeCost"/> bundling mana with an <see cref="ExileCost"/> /
  /// <see cref="DiscardCost"/> for keywords whose alternative cost includes paying with
  /// filtered cards.
  /// </summary>
  public required Cost Cost { get; init; }

  /// <summary>
  /// Condition gating the permission, when the keyword adds one — Surge ("if you or a
  /// teammate has cast another spell this turn"), Spectacle ("if an opponent lost life
  /// this turn"). Null for unconditional zone recursion (Flashback, Escape).
  /// </summary>
  [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
  public Condition? Condition { get; init; }
}
