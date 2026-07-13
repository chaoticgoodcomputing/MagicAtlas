namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// Recognises the Sleight of Hand "the other" variant of the Impulse/Anticipate
/// look-and-keep-one family:
/// "Look at the top N cards of your library. Put one of them into your hand and
///  the other on the bottom of your library."
///
/// <para>
/// This is the singular-remainder spelling of the same single atomic action modelled
/// by <see cref="ImpulseEffect"/> with
/// <see cref="ImpulseRestDestination.BottomOfLibrary"/>: the controller looks at the
/// top N cards, keeps one in hand, and the remaining card(s) go to the bottom of the
/// library. It differs from the sibling <c>LookAtTopPutInHandRule</c> only in surface
/// form — "the other" (rather than "the rest … in any order"), the wording a card uses
/// when exactly one card remains after keeping one of two, so no ordering clause is
/// printed. The semantics are identical, so it reuses the same <see cref="ImpulseEffect"/>
/// node; no new discriminator is introduced.
/// </para>
///
/// <para>
/// The two-sentence oracle text is a single game action — "the other" in the second
/// sentence back-references the cards looked at by the first. Matching the whole clause
/// here (an anchored <see cref="ISpellRule.TryMatch"/>) keeps it as one coupled
/// <see cref="ImpulseEffect"/> rather than a decomposed [lookAtCards, …] list.
/// </para>
///
/// Example:
/// <list type="bullet">
///   <item>"Look at the top two cards of your library. Put one of them into your hand and the other on the bottom of your library." — Sleight of Hand</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class LookAtTopTwoPutOneInHandOtherBottomRule : ISpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // "Look at the top N cards of your library. Put one of them into your hand and
  //  the other on the bottom of your library" — anchored end-to-end so the "the other"
  //  tail cannot be matched as a substring of any more-specific sibling clause.
  private static readonly Regex _pattern = new(
    $@"^Look\s+at\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\."
    + @"\s+Put\s+one\s+of\s+them\s+into\s+your\s+hand\s+and\s+the\s+other\s+on\s+the\s+bottom\s+of\s+your\s+library$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var m = _pattern.Match(trimmed);
    if (m.Success && SpellRuleHelpers.TryParseSmallWord(m.Groups["count"].Value, out var count))
    {
      effect = new ImpulseEffect
      {
        Count = LiteralQuantity.Of(count),
        RestDestination = ImpulseRestDestination.BottomOfLibrary,
      };
      return true;
    }

    return false;
  }
}
