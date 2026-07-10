namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Return X target nonland permanents to their owners' hands." — the spell-side
/// multi-target bounce whose target set has a variable (or literal) cardinality over a
/// PLURAL permanent type, e.g. Distorting Wake: "Return X target nonland permanents to
/// their owners' hands." Handles a variable count (X/Y/Z), a literal count ("two",
/// "3"), or the unbounded "any number of" choice (Kiora's Dismissal: "Return any
/// number of target enchantments to their owners' hands.", CR 107.3) over a PLURAL
/// card type ("permanents", "creatures", …), with an optional "non&lt;x&gt;" or colour
/// modifier before the type ("nonland", "green"), returned to their owners' hands
/// (plural possessive, because there are multiple returned permanents with
/// potentially different owners).
///
/// <para>
/// Spell-side sibling of the activated-ability
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.ReturnCountTargetPermanentsToOwnersHandsEffectRule"/>
/// (Alexi, Zephyr Mage): identical shape, different dispatch surface (a sorcery/instant
/// spell effect rather than an activated-ability effect fragment). The cardinality
/// rides on <see cref="ObjectReference.Quantity"/> — the documented home for "N target"
/// / "up to N target" phrasings — rather than a bespoke Count field on
/// <see cref="ReturnToHandEffect"/>. A variable X becomes <see cref="VariableQuantity"/>;
/// the spell's own {X} in its mana cost defines that value (CR 107.3 — "Many objects
/// use the letter X as a placeholder for a number that needs to be determined").
/// </para>
///
/// <para>
/// The optional modifier is folded onto the filter via <see cref="QualifierAxisMapper"/>:
/// a "non&lt;x&gt;" token routes to <see cref="ObjectFilter.ExcludedCardTypes"/> /
/// <see cref="ObjectFilter.ExcludedColors"/> (e.g. "nonland" →
/// <c>ExcludedCardTypes:["land"]</c>); a bare colour word routes to
/// <see cref="ObjectFilter.Colors"/>. Mirrors the modifier handling in the sibling
/// <see cref="ReturnTargetToHandRule"/> (the "Return all [mod] &lt;type&gt;" branch).
/// </para>
///
/// <para>
/// CR 400.3: "If an object would go to any library, graveyard, or hand other than its
/// owner's, it goes to its owner's corresponding zone." CR 402.1: the hand is a zone;
/// returning an object to hand is a zone change stated directly (no keyword action).
/// CR 115.1: targets are declared as the spell is put on the stack.
/// </para>
///
/// <para>
/// ANCHOR: the pattern is anchored (^…$) on the full clause and requires an explicit
/// count token (bounded or "any number of"), a PLURAL type, and the PLURAL "their
/// owners' hands", so it is disjoint from the singular-target sibling
/// <see cref="ReturnTargetToHandRule"/> (which matches "Return target &lt;type&gt; …
/// its owner's hand", with no count token before "target", and whose patterns do not
/// accept an "any number of" prefix) and cannot match as a substring of, or subsume,
/// any other Return* sibling.
/// </para>
/// </summary>
[SpellRule]
public sealed class ReturnCountTargetPermanentsToOwnersHandsSpellRule : ISpellRule
{
  // "Return <count> target [mod] <plural-type> to their owners' hands."
  //   count      : X/Y/Z (variable), a run of digits, a number word (two..ten), or the
  //                unbounded "any number of" choice (CR 107.3)
  //   mod        : an optional "non<x>" negation or a bare colour word
  //   plural-type: creatures | artifacts | enchantments | lands | permanents | planeswalkers
  private static readonly Regex _pattern = new(
    @"^Return\s+(?<count>[XYZ]|\d+|two|three|four|five|six|seven|eight|nine|ten|any\s+number\s+of)\s+target\s+"
      + @"(?:(?<mod>non\w+|white|blue|black|red|green)\s+)?"
      + @"(?<type>creatures|artifacts|enchantments|lands|permanents|planeswalkers)\s+"
      + @"to\s+their\s+owners['’]?\s+hands$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> ColorCode =
    new(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var countToken = m.Groups["count"].Value;
    // Plural type -> singular CardTypes entry (all six drop a trailing "s").
    var cardType = m.Groups["type"].Value.ToLowerInvariant().TrimEnd('s');
    var modWord = m.Groups["mod"].Success ? m.Groups["mod"].Value.ToLowerInvariant() : null;

    IReadOnlyList<string>? colors = null;
    List<string>? characteristics = null;
    if (!string.IsNullOrEmpty(modWord))
    {
      if (ColorCode.TryGetValue(modWord, out var code))
      {
        colors = [code];
      }
      else
      {
        // "non<x>" negation (e.g. "nonland") or another qualifier; routed through
        // QualifierAxisMapper below to its structured axis.
        characteristics = [modWord];
      }
    }

    effect = new ReturnToHandEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Quantity = ParseCount(countToken),
        Filter = QualifierAxisMapper.Apply(
          new ObjectFilter
          {
            CardTypes = [cardType],
            Colors = colors,
          },
          characteristics
        ),
      },
    };
    return true;
  }

  private static Quantity ParseCount(string token)
  {
    // Unbounded "any number of" choice (CR 107.3) — an upper-unbounded player choice,
    // distinct from a bounded literal/variable count. Normalise internal whitespace
    // (the regex tolerates runs of \s) before comparing.
    if (Regex.IsMatch(token, @"^any\s+number\s+of$", RegexOptions.IgnoreCase))
    {
      return new AnyAmountQuantity();
    }

    // Variable placeholder (X/Y/Z) — value is defined by the spell's own {X} mana cost.
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
    return LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(token));
  }
}
