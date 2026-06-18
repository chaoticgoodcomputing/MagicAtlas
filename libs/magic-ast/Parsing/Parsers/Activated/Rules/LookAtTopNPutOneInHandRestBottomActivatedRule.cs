namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;

/// <summary>
/// "Look at the top N cards of your library. Put one of them into your hand
/// and the other on the bottom of your library." — or "the rest on the bottom
/// of your library [in any order]." The activated-ability Impulse family for
/// both two-card and multi-card variants.
///
/// <para>
/// Two surface variants share the same <see cref="ImpulseEffect"/> representation:
/// <list type="bullet">
///   <item>"the other on the bottom of your library" — singular "other" (Teferi,
///   Temporal Archmage +1; only 2 cards so there is exactly one unchosen card).</item>
///   <item>"the rest on the bottom of your library in any order" — plural "rest"
///   (Anticipate, Impulse). Semantically identical for the AST.</item>
/// </list>
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> — the two-sentence oracle
/// text cannot be split independently ("them" in the second sentence back-references
/// the look set of the first). <see cref="IActivatedEffectRule.TryMatch"/> always
/// returns null; only the multi-rule path is served.
/// </para>
///
/// CR 701.12 (look at cards); CR 400.4 (order at bottom). Rule 606.3 (loyalty
/// ability activation timing) — this rule is used in both loyalty and non-loyalty
/// activated-ability contexts.
/// </summary>
[ActivatedEffectRule(Priority = 956)]
public sealed class LookAtTopNPutOneInHandRestBottomActivatedRule
  : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // Matches the full two-sentence oracle text:
  //   "Look at the top N cards of your library. Put one of them into your hand
  //    and the [other|rest] on the bottom of your library [in any order]."
  // ANCHORED (^…$) to prevent mislabeling longer multi-sentence effects.
  private static readonly Regex _pattern = new(
    $@"^Look\s+at\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\."
    + @"\s+Put\s+one\s+of\s+them\s+into\s+your\s+hand\s+and\s+the\s+(?:other|rest)\s+on\s+the\s+bottom\s+of\s+your\s+library(?:\s+in\s+any\s+order)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — served exclusively via <see cref="TryMatchMulti"/>.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return false;
    }

    if (!TryParseCount(m.Groups["count"].Value, out var count))
    {
      return false;
    }

    effects = new List<Effect>
    {
      new ImpulseEffect
      {
        Count = LiteralQuantity.Of(count),
        RestDestination = ImpulseRestDestination.BottomOfLibrary,
      },
    };
    return true;
  }

  private static bool TryParseCount(string raw, out int count)
  {
    count = 0;
    switch (raw.ToLowerInvariant())
    {
      case "a":
      case "one":
        count = 1;
        return true;
      case "two":
        count = 2;
        return true;
      case "three":
        count = 3;
        return true;
      case "four":
        count = 4;
        return true;
      case "five":
        count = 5;
        return true;
      case "six":
        count = 6;
        return true;
      case "seven":
        count = 7;
        return true;
      case "eight":
        count = 8;
        return true;
      case "nine":
        count = 9;
        return true;
      case "ten":
        count = 10;
        return true;
      default:
        return int.TryParse(raw, out count);
    }
  }
}
