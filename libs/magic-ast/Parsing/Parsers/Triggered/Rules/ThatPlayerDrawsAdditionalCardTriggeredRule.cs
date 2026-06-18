namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "that player draws an additional card" — a draw-step trigger effect that makes
/// the player whose draw step is occurring draw one extra card. "That player" is
/// the pronoun for the trigger's named player (CR 603.2b: "at the beginning of"
/// fires for the active player; "that player" back-references whoever owns that
/// step). The "additional" qualifier is oracle flavour; mechanically this is
/// identical to drawing one card (CR 121.1: "A player draws a card by putting the
/// top card of their library into their hand.").
///
/// <para>
/// Anchored (^…$) to prevent substring matches inside longer clauses.
/// Distinct from <see cref="DrawCardsTriggeredRule"/> (which handles "draw a card"
/// directed at the controller "you", not "that player") and
/// <see cref="EachPlayerDrawsTriggeredRule"/> (which handles "each player draws a
/// card").
/// </para>
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class ThatPlayerDrawsAdditionalCardTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player\s+draws\s+an?\s+additional\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(1),
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
    };
    return true;
  }
}
