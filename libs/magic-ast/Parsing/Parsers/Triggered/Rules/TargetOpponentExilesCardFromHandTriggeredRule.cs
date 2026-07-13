namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "target opponent exiles a card from their hand" as the effect clause of a
/// triggered ability — the Skullcap Snail / Unscrupulous Agent ETB shape ("When
/// this creature enters, target opponent exiles a card from their hand") and the
/// cast-trigger sibling (Kyoki, Sanity's Eclipse: "Whenever you cast a Spirit or
/// Arcane spell, target opponent exiles a card from their hand").
///
/// <para>
/// The targeted opponent moves one card from their own hand to exile. Because the
/// hand is a hidden zone (CR 108.3 — hand membership is by ownership), the card is
/// not itself a legal target: the "target" keyword (CR 115.1) creates a targeting
/// requirement on the OPPONENT, and that opponent — the card's owner — chooses which
/// card. This is the exile analogue of a self-chosen discard, but the card is moved
/// to exile (CR 701.13a — "To exile an object, move it to the exile zone from
/// wherever it is") rather than the graveyard, so it is an <see cref="ExileEffect"/>
/// and not a <c>DiscardCardsEffect</c>.
/// </para>
///
/// <para>
/// Following the convention already committed for "target opponent exiles [an
/// indefinite object]" (the Azula, Cunning Usurper composite in
/// <c>TriggeredAbilityParser.TryParseOpponentExileCreatureThenExileGraveyardCard</c>
/// and the graveyard-hate <see cref="ExileTargetCardFromOpponentGraveyardTriggeredRule"/>),
/// the exiled card is folded into <see cref="ExileEffect.Target"/> with
/// <see cref="ObjectReferenceKind.Target"/> (carrying the targeting requirement) and
/// its filter scoped by <see cref="ObjectFilter.Owner"/> = <see cref="ControllerFilter.Opponent"/>
/// (whose card) + <see cref="Zone.Hand"/> (the source zone). No separate actor axis is
/// added — the owner-scope + hidden zone already imply the opponent is the one who
/// chooses and moves the card.
/// </para>
///
/// <para>
/// Anchored (^…$) so it fires only when the entire effect clause is this single-card
/// hand exile, never as a substring of a longer composite a more-specific sibling
/// should own: the "each opponent exiles a card from their hand …" broadcast forms
/// (Yarok's Fenlurker, Lightstall Inquisitor — different subject and trailing
/// permission clauses), the "at random" / "face down" variants, and the two-card
/// "Witness the End" form all fail the anchor.
/// </para>
///
/// CR 701.13 (Exile) + CR 115.1 (target) + CR 108.3 (ownership).
/// </summary>
[TriggeredRule]
public sealed class TargetOpponentExilesCardFromHandTriggeredRule : ITriggeredRule
{
  // The terminal period is stripped by the dispatcher before TryMatch is called.
  private static readonly Regex Pattern = new(
    @"^target\s+opponent\s+exiles\s+a\s+card\s+from\s+their\s+hand\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["card"],
          Owner = ControllerFilter.Opponent,
          Zone = Zone.Hand,
        },
      },
    };
    return true;
  }
}
