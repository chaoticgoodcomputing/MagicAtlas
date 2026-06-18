namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Untap up to N target permanents." — the Teferi, Temporal Archmage −1 loyalty
/// ability shape. Emits an <see cref="UntapEffect"/> whose target has an
/// <see cref="UpToQuantity"/> and a permanent card-type filter.
///
/// <para>
/// CR 701.26b: "To untap a permanent, rotate it back to the upright position from
/// a sideways position. Only tapped permanents can be untapped." The engine
/// applies this; MAST records that up to N target permanents are untapped.
/// </para>
///
/// <para>
/// CR 115.1 (target keyword) — "target" makes these targeted choices, with the
/// number of targets constrained to 0–N by the "up to N" phrasing (CR 107.3).
/// "Permanent" is a CR card type covering all permanents on the battlefield
/// (CR 110.1). The filter records <c>CardTypes: ["permanent"]</c>.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): "Untap up to" appears as a substring of more-specific
/// sibling untap phrases only if followed by a different qualifier (e.g. "up to one
/// target creature") — those would not match this rule's "permanents" suffix.
/// Anchoring is the defensive convention regardless.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 995)]
public sealed class UntapUpToNTargetPermanentsRule : IActivatedEffectRule
{
  private const string CountTokens =
    @"one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // ANCHORED to avoid matching inside a longer phrase.
  // Matches: "Untap up to N target permanents."
  private static readonly Regex _pattern = new(
    $@"^Untap\s+up\s+to\s+(?<count>{CountTokens})\s+target\s+permanents?\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    if (!TryParseCount(m.Groups["count"].Value, out var maximum))
    {
      return null;
    }

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = new UpToQuantity { Maximum = maximum, Minimum = 0 },
        Filter = new ObjectFilter { CardTypes = ["permanent"] },
      },
    };
  }

  private static bool TryParseCount(string raw, out int count)
  {
    count = 0;
    switch (raw.ToLowerInvariant())
    {
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
