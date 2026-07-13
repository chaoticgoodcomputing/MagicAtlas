namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// "Enchanted [type] is a [P/T] [colors] [subtype] creature with [keyword(s)]. It's
/// still a [type]." — the same Zendikon-cycle "animate the enchanted permanent"
/// template as <see cref="EnchantedIsCreatureStillTypeRule"/>, extended with an
/// inline "with [keyword list]" clause naming one or more keyword abilities the
/// animated creature also has (Crackling Emergence: "...red Spirit creature with
/// haste.").
///
/// <para>
/// Kept as a SEPARATE file rather than editing
/// <see cref="EnchantedIsCreatureStillTypeRule"/> in place: the two patterns are
/// mutually exclusive (one requires the literal "with [keywords]" clause directly
/// before the period, the other requires the period immediately after "creature"),
/// so either rule can run in isolation without touching the other's regex or
/// risking the existing Vastwood Zendikon fixture. Both emit the identical
/// <see cref="BecomesCreatureEffect"/> shape; this rule's only addition is a
/// non-empty <see cref="BecomesCreatureEffect.GainedAbilities"/> populated via
/// <see cref="StaticRuleHelpers.MapKeywordToStaticAbility"/> (CR 113.6 — granted
/// keyword abilities are full-fledged abilities of the gainer).
/// </para>
///
/// <para>
/// <b>"It's still a [type]" retention (CR 205.1b)</b> and the CR 305.7 land-type
/// retention reasoning are identical to the sibling rule; see that rule's doc
/// comment for the full citation.
/// </para>
///
/// <para>
/// Canonical card: Crackling Emergence — "Enchanted land is a 3/3 red Spirit
/// creature with haste. It's still a land." → Power/Toughness 3/3, Colors ["R"],
/// AddedSubtypes ["Spirit"], CardTypes ["land","creature"], GainedAbilities
/// [haste].
/// </para>
///
/// <para>
/// Anchored (^…$) to the exact "Enchanted [type] is a P/T [spec] creature with
/// [keywords]. It's still a [type]" shape so it cannot collide with
/// <see cref="EnchantedIsCreatureStillTypeRule"/> (no "with [keywords]" clause),
/// <see cref="EnchantedIsCreatureWithBasePTRule"/> (requires the literal "with
/// base power and toughness ... in addition to its other types" wording), or
/// <see cref="EnchantedLandIsSubtypeRule"/> (a bare "Enchanted land is a(n)
/// [BasicLandType]" subtype declaration, no P/T box).
/// </para>
///
/// Rule 613 (Layer System / continuous effects); Rule 205.1b, 205.2 (card types);
/// Rule 208.3 (power/toughness); Rule 105 (color); Rule 113.6 (granted keyword
/// abilities); Rule 305.7 (animated lands retain land types); Rule 303.4c / 702.5
/// ("enchanted [type]" refers to the attached permanent).
/// </summary>
[StaticRule(Priority = 972)]
public sealed class EnchantedIsCreatureWithKeywordsStillTypeRule : IStaticRule
{
  // "Enchanted land is a 3/3 red Spirit creature with haste. It's still a land."
  // <subj> is the retained card type; <spec> is colors + subtype words between the
  // P/T box and the literal "creature"; <keywords> is the comma/"and"-separated
  // keyword list after "with"; <retain> is the trailing confirmation sentence's
  // type word (consumed, not separately emitted).
  private static readonly Regex _pattern = new(
    @"^\s*Enchanted\s+(?<subj>artifact|land|creature|permanent|enchantment|planeswalker)\s+is\s+a\s+(?<p>\d+|X)/(?<t>\d+|X)\s+(?<spec>.+?)\s+creature\s+with\s+(?<keywords>[A-Za-z][A-Za-z ,]*?)\.\s*It['’]s\s+still\s+a\s+(?<retain>\w+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _colorCodes =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"]     = "W",
      ["blue"]      = "U",
      ["black"]     = "B",
      ["red"]       = "R",
      ["green"]     = "G",
      ["colorless"] = "C",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var retainedType = match.Groups["subj"].Value.ToLowerInvariant();

    var colors = new List<string>();
    var subtypes = new List<string>();

    // Walk the spec words, classifying each as a color, a connective ("and"), or a
    // creature subtype (anything else — oracle text capitalizes subtypes, CR 205.3m).
    foreach (var rawWord in match.Groups["spec"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
    {
      var word = rawWord.Trim();
      if (word.Length == 0 || string.Equals(word, "and", StringComparison.OrdinalIgnoreCase))
      {
        continue;
      }

      if (_colorCodes.TryGetValue(word, out var code))
      {
        colors.Add(code);
      }
      else
      {
        subtypes.Add(char.ToUpperInvariant(word[0]) + word[1..]);
      }
    }

    // Split the "with [keywords]" clause on commas and "and" into individual
    // keyword tokens, mapping each through the shared keyword table. Bail (return
    // null) on any unrecognised token so the fallback dispatch chain still has a
    // chance to handle the clause faithfully rather than silently dropping a keyword.
    var gainedAbilities = new List<Ability>();
    var keywordTokens = Regex.Split(match.Groups["keywords"].Value, @",\s*|\s+and\s+", RegexOptions.IgnoreCase);
    foreach (var rawKeyword in keywordTokens)
    {
      var keyword = rawKeyword.Trim();
      if (keyword.Length == 0)
      {
        continue;
      }

      var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(keyword);
      if (grantedAbility is null)
      {
        return null;
      }

      gainedAbilities.Add(grantedAbility);
    }

    if (gainedAbilities.Count == 0)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new BecomesCreatureEffect
          {
            Subject = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            Power = ParsePT(match.Groups["p"].Value),
            Toughness = ParsePT(match.Groups["t"].Value),
            Colors = colors,
            CardTypes = [retainedType, "creature"],
            AddedSubtypes = subtypes,
            GainedAbilities = gainedAbilities,
          },
        ],
      },
    ];
  }

  // Animate P/T is a fixed literal ("3/3") or a variable ("X/X").
  private static Quantity ParsePT(string token) =>
    string.Equals(token, "X", StringComparison.OrdinalIgnoreCase)
      ? new VariableQuantity { Name = "X" }
      : LiteralQuantity.Of(int.Parse(token));
}
