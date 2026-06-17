namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "enchanted player mills X cards, where X is the number of cards put into
/// their graveyard from anywhere this turn." — the Fraying Sanity pattern
/// (CR 701.17: "To mill N cards, a player puts the top N cards of their library
/// into their graveyard."; CR 702.5: "Enchant player" — the enchanted player is
/// the Aura's enchanted object).
///
/// <para>
/// X is a <see cref="CountQuantity"/> over cards owned by the enchanted player
/// with a <see cref="PutIntoGraveyardThisTurnPredicate"/> history predicate,
/// encoding the backward-looking "cards put into their graveyard from anywhere
/// this turn" phrase faithfully per ADR 0004 (reference-not-resolution). The
/// milling subject is the <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>
/// reference (the enchanted player). The <c>FromZone</c> field on the predicate
/// is set to <see cref="Zone.Anywhere"/> for the explicit "from anywhere" qualifier.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EnchantedPlayerMillsXCardsWhereXIsGraveyardCountRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^enchanted\s+player\s+mills\s+X\s+cards?,\s*where\s+X\s+is\s+the\s+number\s+of\s+cards\s+put\s+into\s+their\s+graveyard\s+from\s+anywhere\s+this\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    // Count = the number of cards put into the enchanted player's graveyard
    // from anywhere this turn. Owner = EnchantedPlayer (their graveyard);
    // History = PutIntoGraveyardThisTurnPredicate(FromZone = Anywhere).
    var countQuantity = new CountQuantity
    {
      CountOf = new ObjectFilter
      {
        CardTypes = ["card"],
        Owner = ControllerFilter.EnchantedPlayer,
        History = new PutIntoGraveyardThisTurnPredicate
        {
          FromZone = Zone.Anywhere,
        },
      },
    };

    effect = new MillEffect
    {
      Count = countQuantity,
      Player = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
    };
    return true;
  }
}
