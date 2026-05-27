namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Look at target player's hand." — the Peek pattern (Rule 701.12).
///
/// The controller looks at all cards in the named player's hand.
/// "Their hand" means every card present, modelled as
/// <see cref="DerivedKind.CardsInHand"/> to mirror the
/// <see cref="DiscardHandSpellRule"/> convention for "their hand" phrasing.
///
/// <list type="bullet">
///   <item>"Look at target player's hand." — Peek (INV)</item>
///   <item>"Look at target opponent's hand." — Ostracize variant</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class LookAtHandRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Look\s+at\s+target\s+(?<subject>player|opponent)'s\s+hand\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var isOpponent = m.Groups["subject"].Value.Equals("opponent", StringComparison.OrdinalIgnoreCase);

    var player = isOpponent
      ? new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["opponent"] },
        }
      : ObjectReference.Target(ObjectFilter.Player());

    effect = new LookAtCardsEffect
    {
      Player = player,
      Count = new DerivedQuantity { DerivedFrom = DerivedKind.CardsInHand },
      Zone = Zone.Hand,
    };
    return true;
  }
}
