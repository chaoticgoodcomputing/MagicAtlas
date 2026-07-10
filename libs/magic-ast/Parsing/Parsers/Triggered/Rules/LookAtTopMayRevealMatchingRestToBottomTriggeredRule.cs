namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Look at the top N cards of your library. You may reveal a(n) [filter]
/// card from among them and put it into your hand. Put the rest on the
/// bottom of your library in a random order." — Dragonologist's ETB pattern.
///
/// <para>
/// The whole three-sentence fragment is one coherent game action — "from among
/// them" and "the rest" in the second and third sentences are back-references
/// to the look in the first — so this rule matches all three sentences as a
/// single fragment and emits one
/// <see cref="LookAtTopMayRevealMatchingRestToBottomEffect"/>. CR 701.19a
/// (look); CR 701.20a (reveal); CR 401.4 (the remainder placed on the bottom
/// in a random order).
/// </para>
///
/// <para>
/// The filter clause is either a single card-type/subtype word, an "or"
/// two-way disjunction, or a two-comma three-way disjunction ("an instant,
/// sorcery, or Dragon card") whose members may mix card types (instant,
/// sorcery, …) and a capitalised creature subtype (Dragon) — the mixed
/// type-or-subtype shape
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.CounterTargetTypeOrSubtypeSpellRule"/>
/// already establishes for a two-way disjunction, generalised here to any
/// count of comma/or-joined members.
/// </para>
///
/// <para>
/// Priority 95: must beat the generic multi-sentence bundle splitter
/// (mirroring <see cref="RevealTopMayPutSharesCreatureTypeRestToBottomTriggeredRule"/>),
/// which would otherwise try to resolve each of the three sentences
/// independently and fail.
/// </para>
/// </summary>
[TriggeredRule(Priority = 95)]
public sealed class LookAtTopMayRevealMatchingRestToBottomTriggeredRule : ITriggeredRule
{
  // Matches: "look at the top <count> cards of your library. You may reveal a(n)
  // <filter> card from among them and put it into your hand. Put the rest on the
  // bottom of your library in a random order". Anchored end-to-end (no sibling
  // rule shares this three-sentence surface — the "look" verb and the trailing
  // "and put it into your hand" clause distinguish it from the REVEAL-based
  // siblings, which never re-state "and put it into your hand" inline).
  private static readonly Regex _pattern = new(
    @"^look\s+at\s+the\s+top\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+of\s+your\s+library\.\s*"
    + @"You\s+may\s+reveal\s+an?\s+(?<filter>[A-Za-z][A-Za-z ,]*?)\s+card\s+from\s+among\s+them\s+and\s+put\s+it\s+into\s+your\s+hand\.\s*"
    + @"Put\s+the\s+rest\s+on\s+the\s+bottom\s+of\s+your\s+library\s+in\s+a\s+random\s+order$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase). A disjunction member matching one
  // of these is a card type; anything else (capitalised, per oracle convention for
  // creature subtypes — CR 205.3m) is a subtype.
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var count = TriggeredRuleHelpers.ParseWordOrDigitCount(match.Groups["count"].Value);
    if (count is null)
    {
      return false;
    }

    // Split the filter clause on ", " and/or " or " into its disjunction members
    // — "instant, sorcery, or Dragon" → ["instant", "sorcery", "Dragon"]. The
    // ", or " alternative MUST be tried before the plain "," alternative — a
    // plain-comma-first split leaves the Oxford-comma "or" glued onto the final
    // member ("or Dragon") since the comma's trailing \s* already consumes the
    // separating whitespace before "or" is ever considered.
    var members = Regex
      .Split(match.Groups["filter"].Value.Trim(), @"\s*,\s*or\s+|\s*,\s*|\s+or\s+")
      .Where(m => m.Length > 0)
      .ToList();
    if (members.Count == 0)
    {
      return false;
    }

    var cardTypes = new List<string>();
    var subtypes = new List<string>();
    foreach (var member in members)
    {
      if (_knownCardTypes.Contains(member))
      {
        cardTypes.Add(member.ToLowerInvariant());
      }
      else if (char.IsUpper(member[0]))
      {
        subtypes.Add(member);
      }
      else
      {
        // Neither a known card type nor a capitalised subtype word — decline
        // rather than guess.
        return false;
      }
    }

    effect = new LookAtTopMayRevealMatchingRestToBottomEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count.Value),
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes.Count > 0 ? cardTypes : null,
        Subtypes = subtypes.Count > 0 ? subtypes : null,
      },
    };
    return true;
  }
}
