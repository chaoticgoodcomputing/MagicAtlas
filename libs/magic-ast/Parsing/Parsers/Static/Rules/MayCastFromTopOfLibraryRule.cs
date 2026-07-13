namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Recognises "You may cast [X] spells and [Y] spells from the top of your
/// library." and produces a <see cref="StaticAbility"/> with two
/// <see cref="MayPlayFromLibraryEffect"/> instances — one per named eligibility
/// class.
///
/// <para>
/// The oracle line is a single sentence but names two disjoint eligibility
/// classes. Each eligibility word maps to an <see cref="ObjectFilter"/> axis:
/// the literal "colorless" → <c>IsColorless = true</c> (CR 105.1 — colorless is
/// the absence of any color, so it is a boolean axis rather than a Colors
/// entry); a lowercase card-type word (e.g. "artifact") →
/// <c>CardTypes = [word]</c>; a capitalised subtype word (e.g. "Angel",
/// "Human") → <c>Subtypes = [word]</c> (CR 205.3m). Mystic Forge ("artifact
/// spells and colorless spells") and Sigarda, Font of Blessings ("Angel spells
/// and Human spells") are both instances of this shape. MAST models these as
/// two sibling <see cref="MayPlayFromLibraryEffect"/> nodes under one
/// <see cref="StaticAbility"/>: the pair expresses "you have permission to cast
/// from the top if the card matches filter A OR filter B". The effects sit on
/// the same ability rather than two separate abilities because the oracle
/// sentence is one continuous permission grant, not two discrete abilities
/// (CR 604.2 — topology not annotation).
/// </para>
///
/// <para>
/// CR 604.2: "Static abilities create continuous effects … These effects are
/// active as long as the permanent with the ability remains on the battlefield."
/// </para>
/// </summary>
[StaticRule(Priority = 940)]
public sealed class MayCastFromTopOfLibraryRule : IStaticRule
{
  // Matches "You may cast <a> spells and <b> spells from the top of your
  // library." <a>/<b> are single words — a lowercase card-type word or the
  // literal "colorless", or a capitalised subtype word. Anchored (^…$) to
  // prevent substring collisions with sibling permission grants
  // (MayPlayFromTopOfLibraryRule, MayPlayLandsFromTopOfLibraryRule).
  private static readonly Regex _pattern = new(
    @"^\s*You\s+may\s+cast\s+(?<a>[A-Za-z]+)\s+spells\s+and\s+(?<b>[A-Za-z]+)\s+spells\s+from\s+the\s+top\s+of\s+your\s+library\.?\s*$",
    RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _pattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterA = BuildSpellFilter(match.Groups["a"].Value);
    var filterB = BuildSpellFilter(match.Groups["b"].Value);
    if (filterA is null || filterB is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new MayPlayFromLibraryEffect { Cards = filterA },
          new MayPlayFromLibraryEffect { Cards = filterB },
        ],
      },
    ];
  }

  // The printed card types a "[type] spells" eligibility class can name (CR 300.1 /
  // 205.2a). Used to (a) validate a bare lowercase word is a real type before
  // emitting CardTypes=[word], and (b) validate the remainder of a "non<type>" word
  // before negating it — so a non-type word (or a bogus "noncreature"-as-a-type)
  // declines the match rather than fabricating a nonexistent card type.
  private static readonly HashSet<string> KnownCardTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "artifact",
    "creature",
    "enchantment",
    "instant",
    "sorcery",
    "planeswalker",
    "battle",
    "land",
    "kindred",
    "tribal",
  };

  private static bool IsCardType(string word) => KnownCardTypes.Contains(word);

  private static ObjectFilter? BuildSpellFilter(string word)
  {
    // "Colorless" is the absence of any color (CR 105.1): IsColorless=true
    // encodes this without adding a color restriction entry (which would
    // misrepresent it as "has the color colorless").
    if (word.Equals("colorless", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        IsColorless = true,
        Zone = Zone.Library,
        Controller = ControllerFilter.You,
      };
    }

    // A lowercase "non<type>" word (e.g. "noncreature spells") is a NEGATED card
    // type, not a card type named "noncreature" (CR 205.2a lists no such type).
    // The codebase convention is CardTypes=["card"] + ExcludedCardTypes=[type]:
    // "any card that is not a <type>" (Madame Web, Clairvoyant — "Spider spells and
    // noncreature spells"). Only strip "non" when the remainder is a real card
    // type; otherwise fall through and let an unknown word decline the match.
    if (word.Length > 3
        && word.StartsWith("non", StringComparison.OrdinalIgnoreCase)
        && char.IsLower(word, 0))
    {
      var excluded = word.Substring(3).ToLowerInvariant();
      if (IsCardType(excluded))
      {
        return new ObjectFilter
        {
          CardTypes = ["card"],
          ExcludedCardTypes = [excluded],
          Zone = Zone.Library,
          Controller = ControllerFilter.You,
        };
      }
    }

    // A lowercase word is a printed card type (e.g. "artifact spells").
    if (char.IsLower(word, 0))
    {
      if (!IsCardType(word.ToLowerInvariant()))
      {
        return null;
      }
      return new ObjectFilter
      {
        CardTypes = [word.ToLowerInvariant()],
        Zone = Zone.Library,
        Controller = ControllerFilter.You,
      };
    }

    // A capitalised word is a creature subtype (CR 205.3m — e.g. "Angel
    // spells", "Human spells").
    return new ObjectFilter
    {
      Subtypes = [word],
      Zone = Zone.Library,
      Controller = ControllerFilter.You,
    };
  }
}
