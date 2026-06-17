namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 969)]
public sealed class LordPTBuffRule : IStaticRule
{
  // Pattern: optional "Other " or "All " prefix, then a filter noun-phrase,
  // then "get"/"gets", then [+-]N/[+-]N. Anchored; does not match mid-sentence.
  // The named group "other" fires when "Other " is present; used to populate
  // ObjectFilter.Characteristics: ["other"] on the resulting filter.
  private static readonly Regex _lordPTBuffPattern = new(
    @"^\s*(?:(?<other>Other)\s+|All\s+)?(?<filter>\S.+?)\s+gets?\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Color-name → single-letter code map (WUBRG order, all five colours).
  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["White"] = "W",
      ["Blue"] = "U",
      ["Black"] = "B",
      ["Red"] = "R",
      ["Green"] = "G",
    };

  // Known irregular plural → singular mappings for MTG creature subtypes.
  // Checked before the fallback simple strip-s in DepluralizeSubtype.
  private static readonly IReadOnlyDictionary<string, string> _subtypeIrregularPlurals =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Elves"] = "Elf",
      ["Mice"] = "Mouse",
      ["Loci"] = "Locus",
      ["Dwarves"] = "Dwarf",
      ["Wolves"] = "Wolf",
      ["Djinn"] = "Djinn",
    };

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var match = _lordPTBuffPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Don't steal from AsLongAs — that parser peels the suffix itself.
    // If the raw text has " as long as" anywhere, skip here.
    if (clause.RawText.Contains(" as long as", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var isOther = match.Groups["other"].Success;
    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;

    var filter = ParseLordPTFilter(filterText, isOther);
    if (filter is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = filter,
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }

  private ObjectFilter? ParseLordPTFilter(string filterText, bool isOther = false)
  {
    var text = filterText.Trim();

    // "Other " qualifier on the oracle line → record as a Characteristics
    // entry so the AST preserves the exclusion-of-self semantics.
    IReadOnlyList<string>? characteristics = isOther ? ["other"] : null;

    // Peel optional controller suffix — "you control" or "your opponents control".
    // The suffix determines whether the filter applies to the active player's
    // permanents (Controller.You) or to all opponents' permanents (Controller.Opponent).
    ControllerFilter? controller = null;
    if (text.EndsWith(" you control", StringComparison.OrdinalIgnoreCase))
    {
      controller = ControllerFilter.You;
      text = text[..^" you control".Length].Trim();
    }
    else if (text.EndsWith(" your opponents control", StringComparison.OrdinalIgnoreCase))
    {
      controller = ControllerFilter.Opponent;
      text = text[..^" your opponents control".Length].Trim();
    }

    // --- Shape: "[Color] creatures" (e.g. "White creatures", "Black creatures") ---
    // Must be checked BEFORE the generic "[Subtype] creatures" branch because
    // colour adjectives like "White" also match the capitalised-subtype pattern.
    // Oracle colour adjectives are capitalised at the start of a clause.
    var colorCreatureMatch = Regex.Match(
      text,
      @"^(?<color>White|Blue|Black|Red|Green)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (colorCreatureMatch.Success)
    {
      var colorName = colorCreatureMatch.Groups["color"].Value;
      if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
      {
        return null;
      }
      return QualifierAxisMapper.Apply(
        new ObjectFilter
        {
          CardTypes = ["creature"],
          Colors = [colorCode],
          Controller = controller,
        },
        characteristics
      );
    }

    // --- Shape: "artifact creatures" — both card types (Rule 205.2) ---
    // Must be checked BEFORE the generic "[Subtype] creatures" branch because
    // "artifact" is a card type (not a creature subtype) and would otherwise be
    // misclassified as a Subtypes entry. The filter records both card types so
    // the AST accurately represents "permanents that are artifacts and creatures".
    if (text.Equals("artifact creatures", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("artifact creature", StringComparison.OrdinalIgnoreCase))
    {
      return QualifierAxisMapper.Apply(
        new ObjectFilter
        {
          CardTypes = ["artifact", "creature"],
          Controller = controller,
        },
        characteristics
      );
    }

    // --- Shape: "tapped creatures" / "untapped creatures" / "nontoken creatures" /
    //            "attacking creatures" ---
    // State-based or token-status modifier immediately before "creatures". These are
    // game-state predicates (Rule 109.3 for tapped/untapped, Rule 111 for token,
    // Rule 508 for attacking), not subtypes, so they ride on Characteristics rather
    // than Subtypes or CardTypes. The modifier word is appended to any existing
    // characteristics (e.g. the "other" characteristic set by isOther) so the
    // combined filter is accurate.
    foreach (var (prefix, characteristic) in new[]
    {
      ("tapped creature", "tapped"),
      ("untapped creature", "untapped"),
      ("nontoken creature", "nontoken"),
      ("attacking creature", "attacking"),
    })
    {
      // Accept both singular and plural: "tapped creature" and "tapped creatures".
      var pluralPrefix = prefix + "s";
      if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
          text.Equals(pluralPrefix, StringComparison.OrdinalIgnoreCase))
      {
        var chars = characteristics is null
          ? (IReadOnlyList<string>)[characteristic]
          : [..characteristics, characteristic];
        return QualifierAxisMapper.Apply(
          new ObjectFilter
          {
            CardTypes = ["creature"],
            Controller = controller,
          },
          chars
        );
      }
    }

    // --- Shape: "Creature tokens" — global token P/T modifier (Rule 111 + 613.1c) ---
    // "Creature tokens" has no subtype and no controller: the modifier applies to ALL
    // creature tokens on the battlefield regardless of controller (e.g. Illness in the
    // Ranks, Leyline of the Meek, Virulent Plague). The token predicate rides on
    // Characteristics: ["token"] consistent with the convention used by
    // TryParseBareKeywordGrant / BuildBareGrantFilterTarget for the same subject.
    if (text.Equals("creature tokens", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("creature token", StringComparison.OrdinalIgnoreCase))
    {
      var chars = characteristics is null
        ? (IReadOnlyList<string>)["token"]
        : [..characteristics, "token"];
      return QualifierAxisMapper.Apply(
        new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = controller,
        },
        chars
      );
    }

    // --- Shape: "Face-down creatures" — characteristic filter (Rule 707, face-down permanents) ---
    // "Face-down creatures" is a game-state predicate, not a subtype (Rule 707.2):
    // the creature's subtype is hidden while it is face-down. The predicate lives on
    // Characteristics: ["face-down"] so the filter accurately encodes the oracle
    // intent (e.g. Ixidor, Reality Sculptor).
    if (text.Equals("face-down creatures", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("face-down creature", StringComparison.OrdinalIgnoreCase))
    {
      var chars = characteristics is null
        ? (IReadOnlyList<string>)["face-down"]
        : [..characteristics, "face-down"];
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = controller,
        Characteristics = chars?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "[SubtypeA] [SubtypeB] creatures" (e.g. "Goblin Warrior creatures") ---
    // Two-word creature subtype immediately before "creatures". Both words are
    // oracle-capitalised creature subtypes (Rule 205.3m). Must be checked BEFORE
    // the single-word "[Subtype] creatures" branch so that "Goblin Warrior creatures"
    // doesn't get partially matched as "Goblin" (leaving " Warrior creatures" unhandled).
    var twoWordSubtypeCreatureMatch = Regex.Match(
      text,
      @"^(?<sub1>[A-Z][a-z]+)\s+(?<sub2>[A-Z][a-z]+)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (twoWordSubtypeCreatureMatch.Success)
    {
      var subtype1 = twoWordSubtypeCreatureMatch.Groups["sub1"].Value;
      var subtype2 = twoWordSubtypeCreatureMatch.Groups["sub2"].Value;
      // Exclude colour adjectives — "White Green creatures" should not be treated
      // as two-word subtypes (those would route through the colour branch above).
      if (!_colorNameToCode.ContainsKey(subtype1) && !_colorNameToCode.ContainsKey(subtype2))
      {
        return new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype1, subtype2],
          Controller = controller,
          Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
        };
      }
    }

    // --- Shape: "[Subtype] creatures" (e.g. "Dragon creatures", "Bird creatures") ---
    // Capitalised subtype immediately before the lower-case "creatures" noun.
    // Checked after the colour branch so that colour adjectives ("White") are
    // not misclassified as subtypes.
    var subtypeCreatureMatch = Regex.Match(
      text,
      @"^(?<sub>[A-Z][a-z]+)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (subtypeCreatureMatch.Success)
    {
      var subtype = subtypeCreatureMatch.Groups["sub"].Value;
      // Normalise to singular oracle-canonical capitalised form.
      // Oracle capitalises creature subtypes; the matched group already has
      // its original capitalisation (e.g. "Dragon", "Bird").
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = controller,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "creatures" / "Creatures" (bare card-type) ---
    if (text.Equals("creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = controller,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: bare plural subtype (e.g. "Elves", "Goblins", "Saprolings") ---
    // Capitalised plural noun, no "creatures" word. Depluralise to the
    // singular canonical oracle-capitalised form. Irregular plurals (e.g.
    // "Elves" → "Elf") are handled via a lookup before the fallback simple
    // strip-s path.
    // When the "Other " prefix is present the filter describes a tribal-lord
    // shape ("Other Cats you control get …") where the subtype is a
    // creature-only subtype. Include CardTypes: ["creature"] so the filter
    // aligns with the "[Subtype] creatures" branch — both describe creatures
    // of that subtype. Without the "Other" qualifier the subtype noun may span
    // non-creature permanents (e.g. "Elves" in some enchantment anthems) so
    // CardTypes is omitted for backward compatibility.
    var bareSubtypeMatch = Regex.Match(text, @"^(?<sub>[A-Z][a-z]+)s$");
    if (bareSubtypeMatch.Success)
    {
      var pluralWord = bareSubtypeMatch.Groups["sub"].Value + "s";
      var subtype = DepluralizeSubtype(pluralWord);
      return new ObjectFilter
      {
        CardTypes = isOther ? (IReadOnlyList<string>?)["creature"] : null,
        Subtypes = [subtype],
        Controller = controller,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // Unrecognised filter shape — fall through to the fallback parser.
    return null;
  }

  /// <summary>
  /// Returns the oracle-canonical singular form of a plural subtype word.
  /// Handles known irregular plurals first; falls back to stripping a
  /// trailing "s" for regular "-s" plurals.
  /// </summary>
  private static string DepluralizeSubtype(string plural)
  {
    if (_subtypeIrregularPlurals.TryGetValue(plural, out var singular))
    {
      return singular;
    }
    // Simple regular plural: strip trailing "s".
    return plural.EndsWith('s') ? plural[..^1] : plural;
  }
}
