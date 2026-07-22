namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Spell-resolution rule for the Knowledge Exploitation pattern:
/// "Search target opponent's library for an instant or sorcery card. You may cast that
/// card without paying its mana cost. Then that player shuffles."
///
/// <para>
/// Three ordered clauses that read as a single spell resolution and so are grouped in one
/// <see cref="CompositeEffect"/> (the whole line is not a "you may" — only the middle cast
/// clause is optional):
/// </para>
/// <list type="number">
///   <item><see cref="SearchLibraryEffect"/> over the TARGET OPPONENT's library
///   (<c>Player = {Kind: Opponent}</c>, CR 701.20) for a card matching the type filter.
///   The search only FINDS the card — it is not relocated to a zone, so
///   <c>Destination = <see cref="SearchDestination.Retained"/></c> (the sentinel for
///   "not relocated"; a Hand/Battlefield/… value would misstate the rules).</item>
///   <item><see cref="OptionalEffect"/> ("You may …") wrapping a
///   <see cref="CastWithoutPayingEffect"/> whose <c>Target</c> is the found card
///   (<c>{Kind: It}</c> — the previously mentioned object; CR 601 cast-from-library).</item>
///   <item><see cref="ShuffleEffect"/> — "Then that player shuffles"
///   (<c>Player = {Kind: ThatPlayer}</c>, the searched opponent).</item>
/// </list>
///
/// <para>
/// The three-sentence line is split by <see cref="SpellAbilityParser"/>'s sentence-bundle
/// pass first, but the opening "Search target opponent's library …" clause matches no
/// existing per-sentence rule (the search rules anchor on "your library"), so the bundle
/// fails as a unit and dispatch reaches this whole-line single-effect rule. Fully anchored
/// (^…$) — no substring collision with any sibling.
/// </para>
/// </summary>
[SpellRule(Priority = 50)]
public sealed class SearchOpponentLibraryCastWithoutPayingThenShuffleRule : ISpellRule
{
  // Whole-line match. The type filter ("instant or sorcery") is captured so the rule
  // generalizes across the "search opponent's library, may cast that card, then that
  // player shuffles" shape without hard-coding a single card's types.
  private static readonly Regex _pattern = new(
    @"^Search\s+target\s+opponent's\s+library\s+for\s+an?\s+(?<type1>[A-Za-z]+)\s+or\s+(?<type2>[A-Za-z]+)\s+card\.\s*"
      + @"You\s+may\s+cast\s+that\s+card\s+without\s+paying\s+its\s+mana\s+cost\.\s*"
      + @"Then\s+that\s+player\s+shuffles$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Canonical card types recognised in the filter position.
  private static readonly HashSet<string> _cardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "creature", "land", "artifact", "enchantment", "instant", "sorcery",
    "planeswalker", "battle", "tribal",
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

    var type1 = match.Groups["type1"].Value.ToLowerInvariant();
    var type2 = match.Groups["type2"].Value.ToLowerInvariant();

    // Both tokens must be known card types for this rule to own the match; anything else
    // falls through so it isn't silently mislabelled.
    if (!_cardTypes.Contains(type1) || !_cardTypes.Contains(type2))
    {
      return false;
    }

    effect = new CompositeEffect
    {
      Effects =
      [
        new SearchLibraryEffect
        {
          Filter = new ObjectFilter { CardTypes = [type1, type2] },
          Count = LiteralQuantity.Of(1),
          Player = new ObjectReference { Kind = ObjectReferenceKind.Opponent },
          Destination = SearchDestination.Retained,
          Revealed = false,
        },
        new OptionalEffect
        {
          Inner = new CastWithoutPayingEffect { Target = ObjectReference.It() },
        },
        new ShuffleEffect
        {
          Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        },
      ],
    };
    return true;
  }
}
