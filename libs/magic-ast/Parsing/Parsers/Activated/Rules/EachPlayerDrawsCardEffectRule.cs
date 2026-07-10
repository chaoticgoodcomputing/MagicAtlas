namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Each player draws a card." — draw effect applied to every player
/// simultaneously (CR 121.1: "A player draws a card by putting the top card of
/// their library into their hand.").
///
/// <para>
/// The generic <see cref="DrawCardsEffectRule"/> only special-cases "each other
/// player" and "each opponent"; a bare "each player" falls through to its "you"
/// default, silently dropping the broadcast scope. Anchored (^ … $) and given a
/// higher priority than <see cref="DrawCardsEffectRule"/> so this shape is
/// claimed before the generic rule's "you" fallback can misfire — mirrors
/// <see cref="EachPlayerMillsNCardsEffectRule"/>'s equivalent guard for mill.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 999)]
public sealed class EachPlayerDrawsCardEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^Each\s+player\s+draws\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    return new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(ParseCount(m.Groups["count"].Value)),
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer },
    };
  }

  private static int ParseCount(string token) =>
    token.ToLowerInvariant() switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => int.TryParse(token, out var n) ? n : 1,
    };
}
