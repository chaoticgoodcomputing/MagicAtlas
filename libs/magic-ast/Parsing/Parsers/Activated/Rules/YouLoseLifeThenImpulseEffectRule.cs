namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "You lose N life. Look at the top M cards of your library. Put one of them into
/// your hand and the rest into your graveyard." — the Professor Onyx +1 loyalty
/// ability shape. Two coupled effects in three sentences: a controller life-loss
/// followed by an impulse-draw (look at top M, keep one, rest to graveyard).
///
/// <para>
/// The three sentences cannot be dispatched independently because the second and
/// third sentences ("Look at the top M cards … Put one of them …") form a single
/// coupled <see cref="ImpulseEffect"/> — "them" in the third sentence is a back-
/// reference to the cards revealed by the second sentence. This rule captures the
/// full three-sentence text as a flat two-element sibling list:
/// <list type="bullet">
///   <item><see cref="LoseLifeEffect"/> — the controller loses N life.</item>
///   <item><see cref="ImpulseEffect"/> — look at top M, put one in hand, rest to graveyard.</item>
/// </list>
/// CR 119.3 (lose life); CR 701.12 (look); CR 701.9 (discard/graveyard).
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit as a
/// flat sibling pair on <c>Effects</c> — not wrapped in a <c>CompositeEffect</c>.
/// <see cref="IActivatedEffectRule.TryMatch"/> always returns <c>null</c> so the
/// single-effect path never claims the text.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): this three-sentence combination is distinctive enough that
/// a non-anchored match would not produce false positives, but anchoring is the
/// defensive convention for all rules that could match a sibling as a substring.
/// Priority 997 — ties with <see cref="DiscardCardsEffectRule"/> which is the most
/// greedy existing rule. The name-based tiebreak puts this rule (YouLoseLife...)
/// after DiscardCardsEffectRule alphabetically, so use 997 + 1 = 998 is occupied
/// by the sacrifice rule. Use 997 to tie-break with alphabetical ordering placing
/// this rule AFTER DiscardCardsEffectRule within the same band — but the anchored
/// pattern will not match when DiscardCardsEffectRule's unanchored check fires first.
/// Since DiscardCardsEffectRule fires on any "discard" text, this rule must be
/// Priority 997 with TryMatch returning null (the single-effect path) so the
/// multi-rule path is tried on the full 3-sentence text BEFORE sentence splitting.
/// Actually: TryParseMultiRuleEffects is called AFTER TryParseMultiEffectSentences,
/// which will fail (individual sentences don't parse). The multi-rule path then
/// gets the full 3-sentence text and tries IMultiActivatedEffectRule implementations
/// in priority order. DiscardCardsEffectRule only implements IActivatedEffectRule,
/// not IMultiActivatedEffectRule, so the multi-rule loop only sees this rule.
/// Priority 957 is safe for the multi-rule path.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 957)]
public sealed class YouLoseLifeThenImpulseEffectRule : IActivatedEffectRule, IMultiActivatedEffectRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // Anchored three-sentence pattern:
  //   "You lose N life. Look at the top M cards of your library. Put one of them into
  //    your hand and the rest into your graveyard."
  private static readonly Regex _pattern = new(
    $@"^You\s+lose\s+(?<lose>{CountTokens})\s+life\.\s+"
    + $@"Look\s+at\s+the\s+top\s+(?<look>{CountTokens})\s+cards?\s+of\s+your\s+library\.\s+"
    + @"Put\s+one\s+of\s+them\s+into\s+your\s+hand\s+and\s+the\s+rest\s+into\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces two sibling effects.</remarks>
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

    var loseCount = ParseCount(m.Groups["lose"].Value);
    var lookCount = ParseCount(m.Groups["look"].Value);
    if (loseCount is null || lookCount is null)
    {
      return false;
    }

    effects = new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseCount.Value),
        Player = ObjectReference.You(),
      },
      new ImpulseEffect
      {
        Count = LiteralQuantity.Of(lookCount.Value),
        RestDestination = ImpulseRestDestination.Graveyard,
      },
    };
    return true;
  }

  private static int? ParseCount(string raw) =>
    raw.ToLowerInvariant() switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ when int.TryParse(raw, out var n) => n,
      _ => null,
    };
}
