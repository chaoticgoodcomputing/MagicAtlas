namespace MagicAST.Parsing.Parsers.Triggered;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.References;
using MagicAST.Parsing;

/// <summary>
/// Shared utilities used across <see cref="ITriggeredRule"/> implementations.
/// </summary>
internal static class TriggeredRuleHelpers
{
  public static ManaCost? TryBuildManaCost(string manaText)
  {
    try
    {
      var parsed = new ManaCostParser().Parse(manaText);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      return new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }
  }

  public static int? ParseWordOrDigitCount(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("two")) return 2;
    if (lower.Contains("three")) return 3;
    if (lower.Contains("four")) return 4;
    if (lower.Contains("five")) return 5;
    if (lower.Contains("six")) return 6;
    if (lower.Contains("seven")) return 7;
    if (lower.Contains("eight")) return 8;
    if (lower.Contains("nine")) return 9;
    if (lower.Contains("ten")) return 10;
    var m = Regex.Match(lower, @"\b(\d+)\b");
    if (m.Success) return int.Parse(m.Groups[1].Value);
    if (Regex.IsMatch(lower, @"\b(a|an|one)\b")) return 1;
    return null;
  }

  public static (string Article, int Count) ParseArticle(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("two ")) return ("two", 2);
    if (lower.Contains("three ")) return ("three", 3);
    if (lower.Contains("four ")) return ("four", 4);
    if (lower.Contains("an ")) return ("an", 1);
    if (lower.Contains("a ")) return ("a", 1);
    return ("", 1);
  }

  public static (string Power, string Toughness)? ParsePowerToughness(string text)
  {
    var match = Regex.Match(text, @"(\d+|X)/(\d+|X)");
    if (!match.Success)
    {
      return null;
    }
    return (match.Groups[1].Value, match.Groups[2].Value);
  }

  public static List<string> ParseColors(string text)
  {
    var colors = new List<string>();
    var lower = text.ToLowerInvariant();
    var colorMappings = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMappings)
    {
      if (lower.Contains(name))
      {
        colors.Add(code);
      }
    }
    if (lower.Contains("colorless"))
    {
      colors.Clear();
      colors.Add("C");
    }
    return colors;
  }

  // Captures the subtype word(s) between a color/colorless word and "creature token" in
  // the canonical "P/T color [Subtype] [artifact] creature token(s)" oracle pattern.
  // Handles optional "you may" prefix, trailing "with [ability]" suffixes, and the
  // optional "artifact" super-type that precedes "creature" for multi-type tokens
  // such as Thopters and Golems (Rule 205.3m — subtypes follow the type line).
  //
  // Intentionally NOT using IgnoreCase so that proper-noun subtypes (capitalised)
  // are distinguished from lowercase type words like "artifact" — this prevents
  // "artifact" from being mis-captured as a two-word subtype's second word.
  // The color/colorless alternatives and the "creature" / "token" anchors use
  // explicit casing that matches oracle text conventions (lowercase).
  private static readonly Regex _creatureTokenSubtypePattern = new(
    @"\d+/\d+\s+(?:(?:white|blue|black|red|green|colorless)(?:\s+and\s+(?:white|blue|black|red|green|colorless))*)\s+(?<sub1>[A-Z][a-z]+)(?:\s+(?<sub2>[A-Z][a-z]+))?\s+(?:artifact\s+)?creature\s+tokens?",
    RegexOptions.Compiled
  );

  /// <summary>
  /// Extracts creature subtypes from oracle token-creation text.
  /// Uses the structural position (word(s) between color/colorless and
  /// "[artifact] creature token(s)") so arbitrary MTG creature subtypes are
  /// handled without a closed enumeration.
  /// Rule 205.3m — creature subtypes are listed after the card's types.
  /// Handles both coloured tokens ("1/1 red Goblin creature token") and
  /// colorless tokens ("1/1 colorless Thopter artifact creature tokens").
  /// </summary>
  public static List<string> ParseCreatureSubtypes(string text)
  {
    var subtypes = new List<string>();
    var match = _creatureTokenSubtypePattern.Match(text);
    if (!match.Success)
    {
      return subtypes;
    }

    // sub1 is always present on a successful match; capitalize first letter.
    var raw1 = match.Groups["sub1"].Value;
    subtypes.Add(char.ToUpperInvariant(raw1[0]) + raw1[1..]);

    // sub2 is present only for two-word subtypes (e.g. "Phyrexian Germ").
    if (match.Groups["sub2"].Success)
    {
      var raw2 = match.Groups["sub2"].Value;
      subtypes.Add(char.ToUpperInvariant(raw2[0]) + raw2[1..]);
    }

    return subtypes;
  }

  /// <summary>
  /// Returns the card types for the token described in oracle text.
  /// Multi-type tokens such as "artifact creature token" produce
  /// <c>["artifact", "creature"]</c>; plain "creature token" produces
  /// <c>["creature"]</c>. Rule 205.2 — a token's type line lists all of
  /// its types in the standard order.
  /// </summary>
  public static List<string> ParseTokenTypes(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("artifact creature token") || lower.Contains("artifact creature tokens"))
    {
      return ["artifact", "creature"];
    }
    return ["creature"];
  }

  public static List<string> ParseTokenAbilities(string text)
  {
    var abilities = new List<string>();
    var lower = text.ToLowerInvariant();
    if (lower.Contains("with flying")) abilities.Add("flying");
    if (lower.Contains("with lifelink")) abilities.Add("lifelink");
    if (lower.Contains("with vigilance")) abilities.Add("vigilance");
    if (lower.Contains("with deathtouch")) abilities.Add("deathtouch");
    if (lower.Contains("with haste")) abilities.Add("haste");
    if (lower.Contains("with trample")) abilities.Add("trample");
    return abilities;
  }

  public static StaticAbility? BuildKeywordStaticAbility(string keywordRaw)
  {
    var lower = keywordRaw.ToLowerInvariant().Trim();
    Effect? effect = lower switch
    {
      "flying" => new MagicAST.AST.Effects.Keyword.EvasionEffect
      {
        CanBeBlockedBy = new MagicAST.AST.References.ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
        },
      },
      "vigilance" => new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance },
      "trample" => new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample },
      "haste" => new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste },
      "reach" => new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach },
      "lifelink" => new MagicAST.AST.Effects.Damage.LifelinkEffect(),
      "indestructible" => new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible },
      "deathtouch" => null,
      _ => null,
    };
    if (effect is null)
    {
      return null;
    }
    KeywordAbility? keywordSource = lower switch
    {
      "flying" => KeywordAbility.Flying,
      "vigilance" => KeywordAbility.Vigilance,
      "trample" => KeywordAbility.Trample,
      "haste" => KeywordAbility.Haste,
      "reach" => KeywordAbility.Reach,
      "lifelink" => KeywordAbility.Lifelink,
      "indestructible" => KeywordAbility.Indestructible,
      _ => null,
    };
    return new StaticAbility { Effects = [effect], KeywordSource = keywordSource };
  }

  /// <summary>
  /// Parses object filters from trigger text.
  /// Compositional component used by multiple trigger types.
  /// </summary>
  public static ObjectFilter? ParseObjectFilter(string text)
  {
    var lower = text.ToLowerInvariant();

    // Possessive cue: any "you control" / "under your control" / "an opponent controls" qualifier
    // lands on the filter's Controller axis. Applies on top of card-type
    // matching below.
    // "under your control" is the older oracle phrasing for what modern oracle writes as
    // "you control" (Rule 109.5 — an object is under a player's control if they own it
    // or have been given control of it). Both phrasings describe the same relationship.
    ControllerFilter? controller = null;
    if (Regex.IsMatch(lower, @"\byou\s+control\b") || Regex.IsMatch(lower, @"\bunder\s+your\s+control\b"))
    {
      controller = ControllerFilter.You;
    }
    else if (
      Regex.IsMatch(lower, @"\ban\s+opponent\s+controls\b")
      || Regex.IsMatch(lower, @"\bunder\s+an\s+opponent'?s\s+control\b")
    )
    {
      controller = ControllerFilter.Opponent;
    }

    // Self-reference by card type: "this [type]" — Oracle text uses the
    // card's own type word to refer to itself ("this creature", "this land",
    // "this artifact"...). MAST resolves the self-reference to a filter on
    // the named type. Order before the "a [type]" path so "this creature"
    // doesn't fall through to "a creature".
    foreach (
      var selfType in new[]
      {
        "creature",
        "land",
        "artifact",
        "enchantment",
        "planeswalker",
        "permanent",
        "battle",
        // Subtype self-reference: oracle text uses the subtype word when the
        // card's subtype is the most precise type reference available.
        // Rule 205.3 (Subtypes) — artifact subtypes include Equipment,
        // Vehicle, Spacecraft; enchantment subtypes include Aura; battle
        // subtypes include Siege (Rule 310, CR 207.2). Each is treated
        // descriptively as a CardTypes singleton; MAST records the word the
        // text used, not the resolved card-type hierarchy (i.e. "this Siege"
        // stays "siege", not resolved up to "battle"). This single entry
        // unblocks all 39 "When this Siege enters, [effect]" ETB triggers
        // (the March-of-the-Machine Invasions); their effects already parse.
        "aura",
        "equipment",
        "vehicle",
        "spacecraft",
        "siege",
      }
    )
    {
      if (Regex.IsMatch(lower, $@"\bthis\s+{selfType}\b"))
      {
        // "this [type]" is the source object itself (CR 109) — mark IsSelf so a self-event trigger
        // ("when this creature dies") is distinguishable from "a [type]" ("when a creature dies").
        // This is the §6 self/any axis the interaction operator gates (an arbitrary object is not
        // provably the source); without it a cross-card sac false-bridges to a self-death trigger.
        // Self-ONLY: a disjunction like "this creature or another creature" means ANY creature, so it
        // must NOT be marked IsSelf (Blood Artist) — guard on "other"/"another" in the subject text.
        var selfOnly = !lower.Contains("other");
        return new ObjectFilter
        {
          CardTypes = [selfType],
          Controller = controller,
          IsSelf = selfOnly ? true : null,
        };
      }
    }

    // "enchanted creature" — the creature to which this Aura is currently attached.
    // Rule 303.4c: an Aura's "enchanted [type]" refers to the permanent it's attached to.
    // The "enchanted" characteristic is recorded as a Characteristics entry on the filter;
    // the card type remains "creature" per the oracle text. This is descriptive — MAST
    // records the oracle word, not a resolved attachment reference.
    if (Regex.IsMatch(lower, @"\bthe\s+enchanted\s+creature\b") || Regex.IsMatch(lower, @"\benchanted\s+creature\b"))
    {
      return new ObjectFilter { CardTypes = ["creature"], Characteristics = [Characteristic.Other("enchanted")] };
    }

    if (lower.Contains("a creature") || lower.Contains("another creature"))
    {
      return new ObjectFilter { CardTypes = ["creature"], Controller = controller };
    }

    if (lower.Contains("a land") || lower.Contains("another land"))
    {
      return new ObjectFilter { CardTypes = ["land"], Controller = controller };
    }

    // Constellation ability-word (Rule 702.110): "an enchantment you control enters" —
    // Rule 207.2c ability-word prefix peeled before this call; the trigger body is
    // "Whenever an enchantment you control enters, ...". Model the subject as a plain
    // enchantment filter with a You controller.
    if (lower.Contains("an enchantment") || lower.Contains("another enchantment"))
    {
      return new ObjectFilter { CardTypes = ["enchantment"], Controller = controller };
    }

    // Self-by-name pattern, e.g. "When Denethor enters" / "Whenever Barrin dies".
    // Oracle text uses the card's own short name to refer to itself; treat this
    // as a self-reference resolved to a creature filter (matches the convention
    // already used for "this creature" — MAST describes what the text says).
    if (IsSelfByNameTrigger(text))
    {
      // CR 201.4: a card naming itself refers to THAT object — a self-reference, exactly like "this
      // creature" (§6). Mark IsSelf so "When Elenda dies" → ltb:creature:to-graveyard:self, not the
      // generic ltb:creature; without it a cross-card sacrifice false-bridges to the self-death
      // trigger (the interaction judge harness caught this on the Elenda loops). Self-ONLY, guarded
      // against an "... or another ..." disjunction, mirroring the "this [type]" path above.
      var selfOnly = !lower.Contains("other");
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = controller,
        IsSelf = selfOnly ? true : null,
      };
    }

    return null;
  }

  /// <summary>
  /// Detects the "[Self-name] enters/dies/attacks" shape, where the card refers
  /// to itself by its own name rather than by "this creature". The heuristic:
  /// after stripping the leading trigger timing keyword, the remaining trigger
  /// text begins with one or more name-words and ends with a recognized event
  /// verb. Name-words are either capitalised content words (e.g. "Goblin",
  /// "Chieftain") or lowercase function words that legally appear in MTG card
  /// names ("of", "the", "a", "an", "from", "for", "to", "in", "at", "with").
  /// An optional trailing comma on any word is allowed to accommodate legendary
  /// card names with epithets (e.g. "Kari Zev, Skyship Raider attacks").
  /// The parser does not have access to the card name at this point, so this is
  /// a structural match, not a name-equality check.
  /// </summary>
  public static bool IsSelfByNameTrigger(string triggerText)
  {
    // Strip the leading trigger timing keyword if present.
    var stripped = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );

    // Name word: capitalised content word OR lowercase function word that can
    // legally appear in a card name (prepositions / articles / conjunctions).
    // First word MUST be capitalised (card names begin with a capital letter).
    // Subsequent words may be function words ("Hag of Noxious Nightmares").
    // Each word token may optionally end with a comma to handle legendary card
    // names with epithets, e.g. "Kari Zev, Skyship Raider attacks".
    const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";
    return Regex.IsMatch(
      stripped,
      @"^[A-Z][A-Za-z'\-]*,?(?:\s+(?:[A-Z][A-Za-z'\-]*|" + FunctionWords + @"),?)*\s+(enters\s+or\s+dies|enters|dies|attacks|blocks)\b",
      RegexOptions.CultureInvariant
    );
  }
}
