namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// Recognises the Impulse/Strategic Planning pattern:
/// "Look at the top N cards of your library. Put one of them into your hand
///  and the rest into your graveyard."
/// or the bottom-of-library variant:
/// "Look at the top N cards of your library. Put one of them into your hand
///  and the rest on the bottom of your library in any order."
///
/// Both sentences are part of a single <see cref="ImpulseEffect"/>. The rule
/// matches the full two-sentence text before the sentence-bundle path splits it,
/// via the single-effect <see cref="ISpellRule.TryMatch"/> path which receives
/// the whole clause text.
///
/// Examples:
/// <list type="bullet">
///   <item>"Look at the top three cards of your library. Put one of them into your hand and the rest into your graveyard." — Strategic Planning</item>
///   <item>"Look at the top four cards of your library. Put one of them into your hand and the rest on the bottom of your library in any order." — Impulse</item>
/// </list>
/// </summary>
[SpellRule]
public sealed class LookAtTopPutInHandRule : ISpellRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // Graveyard variant: "…and the rest into your graveyard"
  private static readonly Regex _graveyardPattern = new(
    $@"^Look\s+at\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\."
    + @"\s+Put\s+one\s+of\s+them\s+into\s+your\s+hand\s+and\s+the\s+rest\s+into\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Bottom-of-library variant: "…and the rest on the bottom of your library in any order"
  private static readonly Regex _bottomPattern = new(
    $@"^Look\s+at\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\."
    + @"\s+Put\s+one\s+of\s+them\s+into\s+your\s+hand\s+and\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+any\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var gm = _graveyardPattern.Match(trimmed);
    if (gm.Success && SpellRuleHelpers.TryParseSmallWord(gm.Groups["count"].Value, out var gc))
    {
      effect = new ImpulseEffect
      {
        Count = LiteralQuantity.Of(gc),
        RestDestination = ImpulseRestDestination.Graveyard,
      };
      return true;
    }

    var bm = _bottomPattern.Match(trimmed);
    if (bm.Success && SpellRuleHelpers.TryParseSmallWord(bm.Groups["count"].Value, out var bc))
    {
      effect = new ImpulseEffect
      {
        Count = LiteralQuantity.Of(bc),
        RestDestination = ImpulseRestDestination.BottomOfLibrary,
      };
      return true;
    }

    return false;
  }
}
