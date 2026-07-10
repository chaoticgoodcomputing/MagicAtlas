namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Reveal any number of green cards in your hand." — the disclosure sentence of the
/// Scent of Ivy family (the green sibling of Scent of Nightshade's black reveal, parsed
/// by <see cref="RevealAnyNumberBlackCardsFromHandRule"/>). The revealed count feeds a
/// later "where X is the number of cards revealed this way" clause parsed by its sibling
/// rule; the two sentences are decomposed into sibling effects by the sentence-bundle
/// dispatch, linked textually via <see cref="CardsRevealedThisWayQuantity"/>
/// (ADR 0004 reference-not-resolution).
///
/// <para>
/// Emits a <see cref="RevealCardsEffect"/>: the revealer is "you"
/// (<see cref="ObjectReferenceKind.You"/>), the count is unbounded
/// (<see cref="AnyAmountQuantity"/>), the zone is <see cref="Zone.Hand"/>, and the
/// filter is "green cards" (<c>CardTypes=["card"]</c> + <c>Colors=["G"]</c>).
/// </para>
///
/// <para>
/// CR 701.20a (verbatim): "To reveal a card, show that card to all players for a brief
/// time. If an effect causes a card to be revealed, it remains revealed for as long as
/// necessary to complete the parts of the effect that card is relevant to."
/// </para>
/// </summary>
[SpellRule]
public sealed class RevealAnyNumberGreenCardsFromHandRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Reveal\s+any\s+number\s+of\s+green\s+cards\s+in\s+your\s+hand$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new RevealCardsEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.You },
      Count = new AnyAmountQuantity(),
      Zone = Zone.Hand,
      Filter = new ObjectFilter
      {
        CardTypes = ["card"],
        Colors = ["G"],
      },
    };
    return true;
  }
}
