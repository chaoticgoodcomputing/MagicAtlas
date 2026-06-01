namespace MagicAST.AST.Effects.ZoneChange;

using MagicAST.Serialization.DiscriminatorAttributes;

/// <summary>
/// "open an Attraction" — keyword action (Rule 701.51). Puts the top card of the
/// controller's Attraction deck onto the battlefield.
///
/// CR 701.51b: "To open an Attraction, move the top card of your Attraction deck
/// off the Attraction deck, turn it face up, and put it onto the battlefield under
/// your control."
/// CR 701.51a: "A player may open an Attraction only during a game in which that
/// player is playing with an Attraction deck (see rule 717, 'Attraction Cards')."
///
/// MAST records the keyword action descriptively; moving the card off the
/// Attraction deck, turning it face up, and the Attraction-deck legality check are
/// engine territory per the descriptive-not-engine doctrine (mirroring the
/// <see cref="MagicAST.AST.Effects.TokenCopy.InvestigateEffect"/> minimal
/// keyword-action node). No card text in this family opens more than one
/// Attraction, so the node carries no count field.
/// </summary>
[OracleEffect("openAttraction")]
public sealed record OpenAttractionEffect : Effect;
