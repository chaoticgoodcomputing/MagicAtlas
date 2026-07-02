namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each opponent may discard a card. If they don't, they lose N life.
/// Repeat this process M more times." — the Professor Onyx −8 loyalty ability shape.
///
/// <para>
/// Three-sentence structure:
/// <list type="number">
///   <item>The base action: each opponent may discard a card ("each opponent may"
///   means each opponent individually decides whether to discard).</item>
///   <item>The consequence: any opponent who chose not to discard loses N life.</item>
///   <item>A repeat instruction: the entire base action + consequence sequence is
///   performed M additional times (total = M + 1).</item>
/// </list>
/// </para>
///
/// <para>
/// Modelled as a <see cref="RepeatEffect"/> wrapping an <see cref="OptionalEffect"/>:
/// <list type="bullet">
///   <item><see cref="RepeatEffect.Inner"/> — the <see cref="OptionalEffect"/> for one
///   iteration (discard or lose life).</item>
///   <item><see cref="RepeatEffect.AdditionalTimes"/> — how many times to repeat
///   after the initial pass (the oracle phrase "six more times" → 6).</item>
///   <item><see cref="OptionalEffect.Inner"/> — <see cref="DiscardCardsEffect"/>
///   targeting each opponent.</item>
///   <item><see cref="OptionalEffect.IfYouDoNot"/> — <see cref="LoseLifeEffect"/>
///   targeting each opponent who declined to discard.</item>
/// </list>
/// CR 701.9a (discard); CR 119.3 (lose life); CR 608.2 (effects resolve, then the
/// repeat instruction directs additional resolutions).
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): this multi-sentence pattern is highly specific; anchoring
/// is the defensive convention. Priority 999 — above <see cref="DiscardCardsEffectRule"/>
/// (Priority 997) because that rule greedily matches any text containing "discard"
/// with no anchoring, so this more-specific three-sentence shape must be tried first.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class EachOpponentMayDiscardOtherwiseLoseLifeRepeatEffectRule : IActivatedEffectRule
{
  // Anchored three-sentence pattern for the Professor Onyx -8:
  //   "Each opponent may discard a card. If they don't, they lose N life.
  //    Repeat this process M more times."
  private static readonly Regex _pattern = new(
    @"^Each\s+opponent\s+may\s+discard\s+a\s+card\.\s+"
    + @"If\s+they\s+don't,\s+they\s+lose\s+(?<lose>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.\s+"
    + @"Repeat\s+this\s+process\s+(?<extra>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+more\s+times$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var loseAmount = ParseCount(m.Groups["lose"].Value);
    var extraTimes = ParseCount(m.Groups["extra"].Value);
    if (loseAmount is null || extraTimes is null)
    {
      return null;
    }

    var eachOpponent = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent };

    var baseAction = new OptionalEffect
    {
      Inner = new DiscardCardsEffect
      {
        Count = LiteralQuantity.Of(1),
        Player = eachOpponent,
      },
      IfYouDoNot = new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseAmount.Value),
        Player = eachOpponent,
      },
    };

    return new RepeatEffect
    {
      Inner = baseAction,
      AdditionalTimes = extraTimes.Value,
    };
  }

  private static int? ParseCount(string raw) =>
    raw.ToLowerInvariant() switch
    {
      "one" => 1,
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
