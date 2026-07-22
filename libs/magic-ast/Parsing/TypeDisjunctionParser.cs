namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST.References;

/// <summary>
/// Parses a cross-axis type-disjunction phrase — "[A] or [B]" whose alternatives live on
/// DIFFERENT <see cref="ObjectFilter"/> axes (a card type on one side, a subtype on the other)
/// — into a list of disjunct sub-filters for <see cref="ObjectFilter.AnyOf"/>.
///
/// <para>
/// "creature or Vehicle" → <c>[{CardTypes:["creature"]}, {Subtypes:["Vehicle"]}]</c> (Silken
/// Strength / Swift Reconfiguration's Enchant restriction, Broodheart Engine's "creature or
/// Vehicle card"). Vehicle is an artifact subtype (CR 301), so the two halves cannot share a
/// single multi-valued axis — hence the disjunctive <see cref="ObjectFilter.AnyOf"/> home rather
/// than a <see cref="ObjectFilter.CardTypes"/> list (which handles the SAME-axis "artifact or
/// creature" disjunction). A trailing "card"/"cards" noun that applies distributively to both
/// halves ("creature or Vehicle card") is stripped before splitting.
/// </para>
///
/// <para>
/// Returns <c>null</c> when the phrase is not a two-or-more-way "or" disjunction, or when EVERY
/// half is a recognised card-type noun (a same-axis disjunction the caller should fold into a
/// <see cref="ObjectFilter.CardTypes"/> list, not <see cref="ObjectFilter.AnyOf"/>). At least one
/// half must be a subtype for the cross-axis representation to apply.
/// </para>
/// </summary>
public static class TypeDisjunctionParser
{
  private static readonly IReadOnlySet<string> CardTypeNouns = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
  {
    "card", "creature", "artifact", "enchantment", "land", "planeswalker",
    "instant", "sorcery", "permanent", "spell", "battle", "tribal",
  };

  private static readonly Regex OrSplit = new(@"\s+or\s+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// Attempts to parse <paramref name="phrase"/> as a cross-axis type disjunction. Returns the
  /// disjunct sub-filters for <see cref="ObjectFilter.AnyOf"/>, or null when the shape does not
  /// apply (see class remarks).
  /// </summary>
  public static IReadOnlyList<ObjectFilter>? TryParse(string phrase)
  {
    var body = phrase.Trim();
    // Strip a trailing distributive "card"/"cards" noun ("creature or Vehicle card").
    body = Regex.Replace(body, @"\s+cards?$", "", RegexOptions.IgnoreCase).Trim();

    var parts = OrSplit.Split(body).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
    if (parts.Count < 2)
    {
      return null;
    }

    var disjuncts = new List<ObjectFilter>(parts.Count);
    var sawSubtype = false;
    foreach (var part in parts)
    {
      // A single word per half is the recognised shape; anything else (a multi-word
      // qualified phrase) is out of scope for the structured disjunction.
      if (part.Contains(' '))
      {
        return null;
      }

      if (CardTypeNouns.Contains(part))
      {
        disjuncts.Add(new ObjectFilter { CardTypes = [part.ToLowerInvariant()] });
      }
      else
      {
        sawSubtype = true;
        disjuncts.Add(new ObjectFilter { Subtypes = [NormalizeSubtype(part)] });
      }
    }

    // Pure card-type disjunctions belong on the CardTypes list, not AnyOf.
    return sawSubtype ? disjuncts : null;
  }

  private static string NormalizeSubtype(string raw) =>
    raw.Length == 0 ? raw : char.ToUpperInvariant(raw[0]) + raw[1..];
}
