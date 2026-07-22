namespace MagicAST.AST.Abilities;

using MagicAST.AST.References;
using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "you reveal a [TypeA] or [TypeB] card from your hand" — a reveal gate that holds when
/// the named player reveals a card matching <see cref="Card"/> from the stated zone. The
/// "Snarl" reveal-lands' entry predicate (Furycalm Snarl: "As this land enters, you may
/// reveal a Mountain or Plains card from your hand. If you don't, this land enters
/// tapped."; Necroblossom Snarl: "… a Swamp or Forest card …") — the land enters untapped
/// exactly when this reveal happens, and tapped otherwise.
///
/// <para>
/// The "you may reveal" choice is recorded as a descriptive predicate, not a game action
/// (descriptive-not-engine doctrine): MAST records that the entry gate is a reveal of a
/// qualifying card, the engine performs the reveal and evaluates whether it happened. The
/// revealed-card criteria live on <see cref="Card"/> — the two land types ride the
/// disjunctive <see cref="ObjectFilter.Subtypes"/> list ("Mountain or Plains" →
/// <c>Subtypes=["Mountain","Plains"]</c>, an OR exactly as a multi-valued
/// <see cref="ObjectFilter.CardTypes"/> list is), with the reveal zone and ownership on the
/// filter (<c>Zone=Hand, Owner=You</c> for "from your hand"). Structured to this dedicated
/// <see cref="Condition"/> arm rather than left as a free-text
/// <see cref="OtherCondition"/> residual.
/// </para>
///
/// <para>
/// Reference-not-resolution (ADR 0004): MAST records the printed reveal gate; the engine
/// reads whether a matching card was actually revealed, MAST does not pre-evaluate it.
/// </para>
///
/// CR 701.16a (verbatim): "To reveal a card, show that card to all players for a brief time."
/// </summary>
[ConditionKind("revealCard")]
public sealed record RevealCardCondition : Condition
{
  /// <summary>The player who reveals the card — "you" → <see cref="ControllerFilter.You"/>.</summary>
  public required ControllerFilter Revealer { get; init; }

  /// <summary>
  /// The card that must be revealed — its type/subtype criteria plus the reveal zone and
  /// ownership ("a Mountain or Plains card from your hand" →
  /// <c>{CardTypes:["card"], Subtypes:["Mountain","Plains"], Zone:Hand, Owner:You}</c>).
  /// </summary>
  public required ObjectFilter Card { get; init; }
}
