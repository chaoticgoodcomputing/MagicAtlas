namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you may mill N cards." — the optional variant of the CR 701.17a mill keyword
/// action on the triggered side (Daggerfang Duo's ETB, CR 603.6a).
///
/// <para>
/// The "you may" is a structured <see cref="OptionalEffect"/> (the codebase's
/// convention, matching <see cref="ExileThenReturnFlickerTriggeredRule"/>) rather
/// than a boolean flag: wrapper presence alone encodes the optionality. Mill is a
/// one-shot action effect, so it composes directly under the wrapper with no
/// further decomposition.
/// </para>
///
/// <para>
/// Anchored (^you may mill … cards$) so it never collides with
/// <see cref="MillTriggeredRule"/>'s <c>^mill …$</c> pattern (which has no "you
/// may" prefix) — the two patterns are mutually exclusive by construction, so
/// rule priority is irrelevant.
/// </para>
///
/// CR 701.17a (Mill): "For a player to mill a number of cards, that player puts
/// that many cards from the top of their library into their graveyard."
/// </summary>
[TriggeredRule]
public sealed class OptionalMillTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+may\s+mill\s+(a|an|one|two|three|four|five|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');

    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    effect = new OptionalEffect
    {
      Inner = new MillEffect
      {
        Count = LiteralQuantity.Of(ParseCount(match.Groups[1].Value)),
        Player = ObjectReference.You(),
      },
    };
    return true;
  }

  private static int ParseCount(string token) =>
    token.ToLowerInvariant() switch
    {
      "a" or "an" or "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.TryParse(token, out var n) ? n : 1,
    };
}
