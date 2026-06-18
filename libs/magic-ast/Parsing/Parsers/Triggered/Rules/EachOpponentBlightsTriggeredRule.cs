namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "each opponent blights N" — triggered effect where each opponent performs the
/// blight keyword action (CR 701.68). Emits a <see cref="BlightEffect"/> with
/// <c>Player = EachOpponent</c>.
///
/// <para>
/// CR 701.68a (verbatim): "To 'blight N' means to put N -1/-1 counters on a
/// creature you control." When an opponent blights, they put the counters on a
/// creature THEY control (not the active player's creature).
/// CR 701.68b: A player who controls no creatures cannot blight.
/// </para>
///
/// <para>
/// Pattern: "^each opponent blights \d+$" — anchored so "each opponent blights 1"
/// cannot match inside a broader clause such as hypothetical multi-clause text.
/// Reminder text "(They each put a -1/-1 counter on a creature they control.)"
/// is stripped by the triggered-ability parser before this rule is called.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class EachOpponentBlightsTriggeredRule : ITriggeredRule
{
  // Anchored: prevent matching as a substring of a longer triggered clause.
  // Matches numeric digits AND common word forms to be robust; oracle text uses digits.
  private static readonly Regex _pattern = new(
    @"^each\s+opponent\s+blights?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one"   => 1,
      "two"   => 2,
      "three" => 3,
      "four"  => 4,
      "five"  => 5,
      "six"   => 6,
      "seven" => 7,
      "eight" => 8,
      "nine"  => 9,
      "ten"   => 10,
      _       => int.Parse(raw),
    };

    effect = new BlightEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      Amount = LiteralQuantity.Of(amount),
    };
    return true;
  }
}
