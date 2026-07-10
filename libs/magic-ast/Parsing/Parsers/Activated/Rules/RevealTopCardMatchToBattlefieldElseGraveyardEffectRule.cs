namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;

/// <summary>
/// Recognises the Call of the Wild reveal-top-card-and-partition effect:
/// "Reveal the top card of your library. If it's a creature card, put it onto the
///  battlefield. Otherwise, put it into your graveyard."
///
/// <para>
/// The three-sentence fragment is one atomic action — the "it" in the second and
/// third sentences back-references the single card revealed in the first — and is
/// emitted as a single <see cref="RevealTopCardMatchToBattlefieldElseGraveyardEffect"/>.
/// Splitting the sentences would dangle the "it" reference, so this rule matches the
/// whole clause (the activated-effect single-rule path receives the full effect half
/// because the second/third sentences do not independently parse, so the multi-sentence
/// pre-pass falls through). CR 701.20 (Reveal); CR 400.7 (onto the battlefield — the
/// card becomes a new object on zone change); CR 404.1 (a graveyard is the discard pile).
/// </para>
///
/// <para>
/// The regex is fully anchored (<c>^…$</c>) and demands the exact "Reveal the top card
/// of your library. If it's a … card, put it onto the battlefield. Otherwise, put it
/// into your graveyard" phrasing, so it does NOT match the fixed-count "top N cards"
/// reveal-and-partition siblings, the "look at the top" siblings, or any variant with a
/// different disposition. The <c>[filter]</c> is a bare card type ("creature").
/// </para>
///
/// <para>Priority 96: must beat generic reveal/look sentence dispatch.</para>
/// </summary>
[ActivatedEffectRule(Priority = 96)]
public sealed class RevealTopCardMatchToBattlefieldElseGraveyardEffectRule : IActivatedEffectRule
{
  // "['’]" tolerates a straight or curly apostrophe in "it's".
  // "[a-z]+(?:\s+[a-z]+)*" captures the card qualifier before " card,".
  private static readonly Regex _pattern = new(
    @"^Reveal\s+the\s+top\s+card\s+of\s+your\s+library\.\s+"
    + @"If\s+it['’]s\s+a\s+(?<filter>[a-z]+(?:\s+[a-z]+)*)\s+card,\s+"
    + @"put\s+it\s+onto\s+the\s+battlefield\.\s+"
    + @"Otherwise,\s+put\s+it\s+into\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Known MTG card types (singular, lowercase). A filter token matching one of
  // these is a card type; anything else is treated as a subtype (mirrors
  // RevealTopMayPutMatchingRestToGraveyardRule).
  private static readonly HashSet<string> _knownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "artifact", "enchantment", "instant", "sorcery", "land",
    "planeswalker", "battle", "permanent",
  };

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
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
      return null;
    }

    return new RevealTopCardMatchToBattlefieldElseGraveyardEffect
    {
      Player = ObjectReference.You(),
      Filter = new ObjectFilter
      {
        CardTypes = cardTypes,
        Subtypes = subtypes,
      },
    };
  }
}
