namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Return X target creatures to their owners' hands." — the multi-target bounce
/// whose target set has a variable (or literal) cardinality, e.g. Alexi, Zephyr Mage:
/// "{X}{U}, {T}, Discard two cards: Return X target creatures to their owners' hands."
/// Handles a variable count (X/Y/Z) or a literal count ("two", "3") over a PLURAL
/// permanent type ("creatures", "artifacts", …) returned to their owners' hands
/// (plural possessive, because there are multiple returned permanents with potentially
/// different owners).
///
/// <para>
/// The cardinality rides on <see cref="ObjectReference.Quantity"/> — the documented
/// home for "N target" / "up to N target" phrasings — rather than a bespoke Count
/// field on <see cref="ReturnToHandEffect"/>. This mirrors the sibling
/// <see cref="ReturnUpToOneTargetTypeDisjunctionToHandEffectRule"/> (which uses
/// <see cref="UpToQuantity"/> on the reference) and Frost Breath's "up to two target
/// creatures" tap. A variable X becomes <see cref="VariableQuantity"/>; the ability's
/// {X} in its activation cost defines that value (CR 107.3 — "Many objects use the
/// letter X as a placeholder for a number that needs to be determined").
/// </para>
///
/// <para>
/// CR 400.3: "If an object would go to any library, graveyard, or hand other than its
/// owner's, it goes to its owner's corresponding zone." CR 402.1: the hand is a zone;
/// returning an object to hand is a zone change stated directly (no keyword action).
/// CR 108.3: "The owner of a card in the game is the player who started the game with
/// it in their deck." CR 115.1: "target" declares the affected objects.
/// </para>
///
/// <para>
/// ANCHOR: the pattern is anchored (^…$) on the full clause and is disjoint from the
/// single-target sibling <see cref="ReturnTargetToHandEffectRule"/> (which matches
/// "Return target <type> … its owner's hand", singular) and from
/// <see cref="ReturnUpToOneTargetTypeDisjunctionToHandEffectRule"/> ("up to one
/// target … its owner's hand", singular hand). This rule requires an explicit count
/// token, a PLURAL type, and the PLURAL "their owners' hands", so no sibling's phrase
/// is a substring of, or subsumes, this one.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class ReturnCountTargetPermanentsToOwnersHandsEffectRule : IActivatedEffectRule
{
  // "Return <count> target <plural-type> to their owners' hands."
  //   count      : X/Y/Z (variable), a run of digits, or a number word (two..ten)
  //   plural-type: creatures | artifacts | enchantments | lands | permanents | planeswalkers
  private static readonly Regex _pattern = new(
    @"^Return\s+(?<count>[XYZ]|\d+|two|three|four|five|six|seven|eight|nine|ten)\s+target\s+"
      + @"(?<type>creatures|artifacts|enchantments|lands|permanents|planeswalkers)\s+"
      + @"to\s+their\s+owners['’]?\s+hands\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var text = effectText.Trim();
    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return null;
    }

    var countToken = m.Groups["count"].Value;
    // Plural type -> singular CardTypes entry (all six drop a trailing "s").
    var cardType = m.Groups["type"].Value.ToLowerInvariant().TrimEnd('s');

    return new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = ParseCount(countToken),
        Filter = new ObjectFilter
        {
          CardTypes = [cardType],
        },
      },
    };
  }

  private static Quantity ParseCount(string token)
  {
    // Variable placeholder (X/Y/Z) — value is defined by the ability's {X} cost.
    if (token.Length == 1 && char.IsLetter(token[0]))
    {
      return new VariableQuantity { Name = token.ToUpperInvariant() };
    }

    // Digits.
    if (int.TryParse(token, out var digits))
    {
      return LiteralQuantity.Of(digits);
    }

    // Number word (two..ten).
    var word = ActivatedRuleHelpers.ParseNumberWord(token);
    return LiteralQuantity.Of(word ?? 1);
  }
}
