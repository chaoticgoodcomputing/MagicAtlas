namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Handles two "untap another/N other target" effect shapes that require both
/// self-exclusion (<c>ExcludeSelf</c>) and optional cardinality on the target reference:
///
/// <list type="bullet">
///   <item>
///     "Untap another target permanent." — CR 701.26b (untap); "another"
///     excludes the source permanent. Produces a single-target <see cref="UntapEffect"/>
///     with <c>ExcludeSelf = true</c> on the permanent card-type filter.
///   </item>
///   <item>
///     "Untap [N] other target [Supertype] [CardType]s." — e.g. "Untap two other
///     target legendary creatures." Produces a multi-target <see cref="UntapEffect"/>
///     where the <see cref="ObjectReference.Quantity"/> carries the literal count and
///     <see cref="ObjectFilter.ExcludeSelf"/> = true excludes the source. The
///     "another/other" marker encodes the self-exclusion axis (ExcludeSelf self-exclusion), separate
///     from the counted target set size.
///   </item>
/// </list>
///
/// Runs at Priority 995 — above <see cref="UntapTargetCardTypeActivatedEffectRule"/>
/// (994) and <see cref="UntapEffectRule"/> (993) — so these more-specific "another /
/// N other" prefixes are dispatched here and never reach the generic single-target
/// rules. The patterns are fully anchored (<c>^...$</c>) to avoid substring matching
/// inside more-specific siblings.
///
/// CR 701.26b: "To untap a permanent, rotate it back to the upright position from a
/// sideways position. Only tapped permanents can be untapped."
/// "Another"/"other" excludes the source object — modeled on the structured
/// <see cref="ObjectFilter.ExcludeSelf"/> axis (no dedicated lettered subrule in the bundled CR data).
/// </summary>
[ActivatedEffectRule(Priority = 995)]
public sealed class UntapAnotherOrCountTargetActivatedEffectRule : IActivatedEffectRule
{
  // "Untap another target <card-type>." — self-excluding single-target untap.
  // Anchored: must match the full effect text. Card types recognised here are the
  // CR 205.2a permanent card types; subtypes (Forest, Island) are handled by
  // UntapEffectRule (Priority 993) which already defers to Subtypes.
  // Optional "you control" suffix sets Controller = You on the ObjectFilter
  // (e.g. "Untap another target permanent you control." — Kelpie Guide).
  private static readonly Regex _anotherPattern = new(
    @"^Untap\s+another\s+target\s+(?<type>permanent|creature|artifact|enchantment|land|planeswalker)(?:\s+(?<ctrl>you\s+control))?\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "Untap [count-word] other target [Supertype] [card-type]s." — multi-target
  // self-excluding untap with an optional supertype qualifier.
  // Anchored. Supertypes: Legendary, Snow, Basic (CR 205.4a).
  private static readonly Regex _countOtherPattern = new(
    @"^Untap\s+(?<count>two|three|four|five|six|seven|eight|nine|ten|\d+)\s+other\s+target\s+(?:(?<supertype>legendary|snow|basic)\s+)?(?<type>creature|artifact|enchantment|land|planeswalker|permanent)s?\s*\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();

    // --- Shape 1: "Untap another target <card-type>[you control]." ---
    var m1 = _anotherPattern.Match(text);
    if (m1.Success)
    {
      var cardType = m1.Groups["type"].Value.ToLowerInvariant();
      var hasController = m1.Groups["ctrl"].Success;
      return new UntapEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = [cardType],
            ExcludeSelf = true,
            Controller = hasController ? ControllerFilter.You : null,
          },
        },
      };
    }

    // --- Shape 2: "Untap [N] other target [Supertype] <card-type>s." ---
    var m2 = _countOtherPattern.Match(text);
    if (m2.Success)
    {
      var count = ParseCount(m2.Groups["count"].Value);
      var cardType = m2.Groups["type"].Value.ToLowerInvariant();

      // Optional supertype qualifier (e.g. "legendary", "snow").
      IReadOnlyList<string>? supertypes = null;
      if (m2.Groups["supertype"].Success)
      {
        var st = m2.Groups["supertype"].Value;
        // Title-case to match the MAST convention (Legendary, Snow, Basic).
        supertypes = [char.ToUpperInvariant(st[0]) + st[1..].ToLowerInvariant()];
      }

      return new UntapEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Quantity = LiteralQuantity.Of(count),
          Filter = new ObjectFilter
          {
            CardTypes = [cardType],
            Supertypes = supertypes,
            ExcludeSelf = true,
          },
        },
      };
    }

    return null;
  }

  private static int ParseCount(string word) => word.ToLowerInvariant() switch
  {
    "two" => 2,
    "three" => 3,
    "four" => 4,
    "five" => 5,
    "six" => 6,
    "seven" => 7,
    "eight" => 8,
    "nine" => 9,
    "ten" => 10,
    _ => int.TryParse(word, out var n) ? n : 2,
  };
}
