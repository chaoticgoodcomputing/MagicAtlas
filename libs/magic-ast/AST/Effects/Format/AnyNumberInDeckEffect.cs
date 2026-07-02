namespace MagicAST.AST.Effects.Format;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "A deck can have any number of cards named [this]." — the self-referential
/// exception to the four-of deck-construction rule (Relentless Rats, Rat Colony,
/// Shadowborn Apostle, Persistent Petitioners, Dragon's Approach).
///
/// <para>
/// CR 100.2a: "In constructed play …, each deck has a minimum deck size of 60
/// cards. A constructed deck may contain any number of basic land cards and no
/// more than four of any card with a particular English name other than basic
/// land cards." This line lifts that four-of limit for cards sharing this card's
/// name.
/// </para>
///
/// <para>
/// Field-less: the "named [this]" self-reference is implicit (it always names the
/// card the line appears on, mirroring how self-referential continuous abilities
/// elide their subject). MAST records only that the exception exists; the actual
/// deck-building enforcement and the 60-card minimum are engine/format territory.
/// </para>
/// </summary>
[OracleEffect("anyNumberInDeck")]
public sealed record AnyNumberInDeckEffect : Effect;
