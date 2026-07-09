namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Reveal the top N cards of your library. You may put a [filter] card from among
/// them into your hand. Put the rest into your graveyard." — the Grisly Salvage /
/// Scout the Borders / Commune with the Gods reveal-then-dredge spell family.
///
/// <para>
/// All three sentences are one coupled game action ("from among them" and "the rest"
/// are back-references to the reveal), so this rule matches the whole clause and
/// emits a single <see cref="RevealTopMayPutMatchingRestToGraveyardEffect"/>. The
/// single-effect <see cref="ISpellRule.TryMatch"/> path receives the full clause
/// because the neighbouring sentences do not independently parse, so the generic
/// sentence-bundle splitter falls through to here (mirroring
/// <see cref="LookAtTopPutInHandRule"/>). CR 701.20 (Reveal); CR 404.1/404.3
/// (the remainder goes to the graveyard, owner-arranged).
/// </para>
///
/// <para>
/// The <c>[filter]</c> is a bare card type ("creature") or a two-or-more-way
/// disjunction ("creature or land", "creature or enchantment"). The regex is fully
/// anchored (<c>^…$</c>) and demands the exact "You may put a … card from among them
/// into your hand" phrasing, so it does NOT match the "creature card and/or a land
/// card" (up-to-one-of-each), "any number of …" (unbounded), or "…onto the
/// battlefield" (different destination) siblings, nor the multi-clause cards whose
/// text continues past "graveyard" (Gather the Pack, Malevolent Rumble).
/// </para>
/// </summary>
[SpellRule]
public sealed class RevealTopMayPutMatchingRestToGraveyardRule : ISpellRule
{
  private const string CountTokens =
    @"one|two|three|four|five|six|seven|eight|nine|ten|\d+";

  // "[type]" or "[type] or [type] (or [type])*" — a bare card type or a disjunction.
  private const string FilterTokens = @"[a-z]+(?:\s+or\s+[a-z]+)*";

  private static readonly Regex _pattern = new(
    $@"^Reveal\s+the\s+top\s+(?<count>{CountTokens})\s+cards?\s+of\s+your\s+library\.\s*"
    + $@"You\s+may\s+put\s+a\s+(?<filter>{FilterTokens})\s+card\s+from\s+among\s+them\s+into\s+your\s+hand\.\s*"
    + @"Put\s+the\s+rest\s+into\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase). A filter token matching one of
  // these is a card type; anything else is treated as a subtype (mirrors
  // RevealTopPutMatchingToHandTriggeredRule).
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

    if (!SpellRuleHelpers.TryParseSmallWord(match.Groups["count"].Value, out var count))
    {
      return false;
    }

    List<string>? cardTypes = null;
    List<string>? subtypes = null;
    foreach (var raw in Regex.Split(match.Groups["filter"].Value, @"\s+or\s+", RegexOptions.IgnoreCase))
    {
      var token = raw.Trim();
      if (token.Length == 0)
      {
        continue;
      }
      if (_knownCardTypes.Contains(token))
      {
        (cardTypes ??= new List<string>()).Add(token.ToLowerInvariant());
      }
      else
      {
        var subtype = char.ToUpperInvariant(token[0]) + token[1..].ToLowerInvariant();
        (subtypes ??= new List<string>()).Add(subtype);
      }
    }

    if (cardTypes is null && subtypes is null)
    {
      return false;
    }

    effect = new RevealTopMayPutMatchingRestToGraveyardEffect
    {
      Player = ObjectReference.You(),
      Count = LiteralQuantity.Of(count),
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes,
        Subtypes = subtypes,
      },
    };
    return true;
  }
}
