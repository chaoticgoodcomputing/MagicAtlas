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
    CollectFromLandTypes(input.TypeLine, colors);
    CollectFromColorIndicator(input.ColorIndicator, colors);

    if (input.CardFaces is not null)
    {
      foreach (var face in input.CardFaces)
      {
        CollectFromCost(face.ManaCost, colors);
        CollectFromRulesText(face.OracleText, colors);
        CollectFromLandTypes(face.TypeLine, colors);
        CollectFromColorIndicator(face.ColorIndicator, colors);
      }
    }

    // NOTE: reminder text is uniformly ignored (CR 903.4c) — there is no per-keyword
    // exception. Extort's reminder "{W/B}" does NOT contribute color identity (Syndic
    // of Tithes is [W], not [W,B]); Scryfall's [W,B] is a legacy Gatecrash-era ruling
    // that predates the 903.4c clarification. This is a deliberate, CR-authoritative
    // divergence from the source database — the parser deriving the more-correct value
    // is exactly the point of deriving rather than echoing.

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

  // Color indicator (CR 204): the colored dot beside the type line. Scryfall supplies
  // it as already-normalized W/U/B/R/G codes; it contributes directly to color identity
  // (CR 903.4) and is the ONLY source for cards colored without a mana symbol (Kobolds,
  // many DFC backs). See the CR 903.4 Civilized Scholar / Homicidal Brute example.
  private static void CollectFromColorIndicator(IReadOnlyList<string>? indicator, HashSet<string> colors)
  {
    if (indicator is null)
    {
      return;
    }

    foreach (var code in indicator)
    {
      if (code is "W" or "U" or "B" or "R" or "G")
      {
        colors.Add(code);
      }
    }
  }

  // Basic land types grant intrinsic mana abilities (CR 305.6) producing colored
  // mana; those colors are part of color identity (CR 903.5d ties a basic-land-type
  // card's legality to "each color of mana it could produce"). The "({T}: Add …)"
  // reminder that spells the ability out is ignored (903.4c) — the color comes from
  // the TYPE, not the reminder. Sacred Foundry ("Land — Mountain Plains") → [R,W].
  private static void CollectFromLandTypes(string? typeLine, HashSet<string> colors)
  {
    if (string.IsNullOrEmpty(typeLine))
    {
      return;
    }

    if (typeLine.Contains("Plains", StringComparison.Ordinal))
    {
      colors.Add("W");
    }
    if (typeLine.Contains("Island", StringComparison.Ordinal))
    {
      colors.Add("U");
    }
    if (typeLine.Contains("Swamp", StringComparison.Ordinal))
    {
      colors.Add("B");
    }
    if (typeLine.Contains("Mountain", StringComparison.Ordinal))
    {
      colors.Add("R");
    }
    if (typeLine.Contains("Forest", StringComparison.Ordinal))
    {
      colors.Add("G");
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
