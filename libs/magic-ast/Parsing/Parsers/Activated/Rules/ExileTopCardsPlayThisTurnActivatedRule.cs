namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Exile the top N cards of your library. You may play those cards this turn." —
/// the current-turn-bounded impulse in an activated-ability context
/// (Harnfel, Horn of Bounty).
///
/// <para>
/// The two oracle sentences are one semantic unit — "those cards" back-references
/// the exiled pile from the first sentence — so they collapse to a single
/// <see cref="ImpulseEffect"/> with <see cref="ImpulseRestDestination.RemainExiled"/>
/// and <see cref="UntilTimeDuration.EndOfTurn"/>, matching the spell-context sibling
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ExileTopCardsPlayThisTurnRule"/>.
/// ADR 0004 (reference, not resolution): MAST records the exile and the play window;
/// the engine tracks which specific cards were exiled and enforces the deadline.
/// </para>
///
/// <para>
/// CR 406 (exile zone); CR 701.13 (exile); CR 701.18 ("play").
/// </para>
///
/// <para>
/// ANCHORED (^…$): "Exile the top … cards of your library. You may play those cards
/// this turn." is the exact Harnfel shape. Non-anchored matching risks absorbing
/// "Exile the top N … put them into …" shapes that should be plain exile effects.
/// Priority 975 — above mid-range, below ultra-specific multi-sentence composite
/// rules, so the two-sentence split path (TryParseMultiEffectSentences) can't claim
/// either sentence independently.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 975)]
public sealed class ExileTopCardsPlayThisTurnActivatedRule : IActivatedEffectRule
{
  private const string CountTokens =
    @"a|one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  private static readonly Regex _pattern = new(
    $@"^Exile\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\.\s+You\s+may\s+play\s+those\s+cards\s+this\s+turn\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    if (!TryParseCount(m.Groups["count"].Value, out var count))
    {
      return null;
    }

    return new ImpulseEffect
    {
      Count = LiteralQuantity.Of(count),
      RestDestination = ImpulseRestDestination.RemainExiled,
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }

  private static bool TryParseCount(string raw, out int count)
  {
    count = raw.ToLowerInvariant() switch
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
      _ => -1,
    };
    return count > 0;
  }
}
