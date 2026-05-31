namespace MagicAST.Parsing;

using System.Text.RegularExpressions;

/// <summary>
/// Derives a card's color identity from its PRINTED text, per CR 903.4: "the color
/// or colors of any mana symbols in that card's mana cost or rules text, plus any
/// colors defined by its characteristic-defining abilities (604.3) or color
/// indicator (204)." Reminder text is ignored (CR 903.4c); every face is included
/// (CR 903.4d).
///
/// <para>
/// Color identity is a property of the PRINTED CARD, not of the decomposed AST.
/// We deliberately read the printed strings (mana cost + reminder-stripped rules
/// text) rather than walking parsed nodes, because the two diverge for keywords
/// whose mana is definitional. A keyword's PRINTED cost symbol counts — "Cycling
/// {U}" makes the card blue — and survives as a structured cost in the AST. But a
/// keyword's DEFINITIONAL mana does NOT count — Firebending's "add {R}{R}" prints
/// {R} only in reminder text (CR 702.189a), so Azula is {U,B}, not {U,B,R}. Once
/// Firebending is honestly decomposed into a triggered ability with an
/// <c>AddManaEffect{Mana:"{R}{R}"}</c>, an AST walk could no longer tell that red
/// apart from a printed "{T}: Add {R}" (which DOES count). Reading printed text,
/// reminder stripped, gets both right by construction and is immune to how richly
/// we decompose keywords.
/// </para>
///
/// <para>
/// Not yet handled (no source data ingested): color indicators (CR 204) and
/// characteristic-defining abilities (CR 604.3). Add those axes if/when the input
/// carries them.
/// </para>
/// </summary>
public static partial class ColorIdentityDeriver
{
  private static readonly string[] Wubrg = ["W", "U", "B", "R", "G"];

  /// <summary>
  /// Derives the WUBRG-ordered color identity from a card's printed mana cost and
  /// rules text, across all faces (CR 903.4d), ignoring reminder text (CR 903.4c).
  /// </summary>
  public static List<string> Derive(CardInputDTO input)
  {
    var colors = new HashSet<string>();

    CollectFromCost(input.ManaCost, colors);
    CollectFromRulesText(input.OracleText, colors);

    if (input.CardFaces is not null)
    {
      foreach (var face in input.CardFaces)
      {
        CollectFromCost(face.ManaCost, colors);
        CollectFromRulesText(face.OracleText, colors);
      }
    }

    return Wubrg.Where(colors.Contains).ToList();
  }

  // The mana cost carries no reminder text — scan its symbols directly.
  private static void CollectFromCost(string? manaCost, HashSet<string> colors)
  {
    if (!string.IsNullOrEmpty(manaCost))
    {
      CollectManaSymbolColors(manaCost, colors);
    }
  }

  // Rules text: strip reminder (parenthetical) spans FIRST (CR 903.4c), then scan
  // the remaining printed mana symbols.
  private static void CollectFromRulesText(string? oracleText, HashSet<string> colors)
  {
    if (!string.IsNullOrEmpty(oracleText))
    {
      var withoutReminder = ReminderTextPattern().Replace(oracleText, string.Empty);
      CollectManaSymbolColors(withoutReminder, colors);
    }
  }

  // Each {...} token is a mana/loyalty/tap symbol; only colored, hybrid, and
  // Phyrexian mana symbols contain a W/U/B/R/G letter, and each such letter is a
  // color the symbol contributes to identity (hybrid {W/U} → both; {G/P} → green).
  private static void CollectManaSymbolColors(string text, HashSet<string> colors)
  {
    foreach (Match match in ManaSymbolPattern().Matches(text))
    {
      foreach (var ch in match.Groups[1].Value)
      {
        switch (char.ToUpperInvariant(ch))
        {
          case 'W':
            colors.Add("W");
            break;
          case 'U':
            colors.Add("U");
            break;
          case 'B':
            colors.Add("B");
            break;
          case 'R':
            colors.Add("R");
            break;
          case 'G':
            colors.Add("G");
            break;
        }
      }
    }
  }

  [GeneratedRegex(@"\(([^)]*)\)")]
  private static partial Regex ReminderTextPattern();

  [GeneratedRegex(@"\{([^}]+)\}")]
  private static partial Regex ManaSymbolPattern();
}
