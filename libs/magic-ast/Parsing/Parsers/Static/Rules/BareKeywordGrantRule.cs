namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

[StaticRule(Priority = 967)]
public sealed class BareKeywordGrantRule : IStaticRule
{
  // Arm 1: "(Enchanted|Equipped) creature has <keyword>."
  // No P/T modifier allowed — this is the bare-grant shape only.
  private static readonly Regex _bareAnchorKeywordPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+has\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Arm 4: "All <Subtype> creatures have <keyword>." — global tribal keyword grant.
  // Captures the oracle-capitalised creature subtype (e.g. "Sliver") and the keyword
  // name. No "you control" suffix; no "Other" qualifier; no controller on the filter.
  private static readonly Regex _bareAllKeywordPattern = new(
    @"^\s*All\s+(?<sub>[A-Z][a-z]+)\s+creatures\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  // Arm 3: "Other <filter> [you control] have <keyword>." — "Other" prefix signals
  // the exclusion-of-self qualifier (ExcludeSelf = true). The optional
  // "you control" group <ctrl> is captured separately so it can be stitched back
  // onto <filter> before passing to ParseLordPTFilter (which strips it and sets
  // Controller = ControllerFilter.You). Without "you control" the grant applies to
  // all qualifying permanents regardless of controller (e.g. Zombie Master).
  private static readonly Regex _bareOtherKeywordPattern = new(
    @"^\s*Other\s+(?<filter>[A-Za-z][A-Za-z ]+?)\s+(?<ctrl>you\s+control\s+)?have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  // Arm 2: "<filter> [tokens] you control have <keyword>."
  // Captures the noun-phrase before the optional "tokens" word and the trailing keyword.
  // The "token" group is present only when the literal word "tokens" appears in the phrase,
  // indicating the grant is restricted to token permanents. BuildBareGrantFilterTarget
  // disambiguates the filter noun into three shapes: bare card-type ("Creatures"),
  // "[Subtype] creatures" (subtype + card-type), and bare plural subtype ("Goblins",
  // "Warlocks", etc.) — the last added in batch 33 for the lord-grants-keyword cluster.
  private static readonly Regex _bareFilterKeywordPattern = new(
    @"^\s*(?<filter>[A-Za-z][A-Za-z ]+?)\s+(?<token>tokens?\s+)?you\s+control\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Arm 5: "Each creature you control with a +1/+1 counter on it has <keyword>."
  // Matches the lord-grants-keyword pattern where the subject filter is augmented
  // by the "+1/+1 counter" predicate. The counter constraint is structured as a
  // CounterCharacteristic{"+1/+1"} (CR 122) via Characteristic.FromLabel, consistent with
  // how "tapped"/"untapped" predicates are structured on the lord-PT filter shapes.
  // "Each" (universal quantifier) signals the Each-kinded ObjectReference; the
  // "you control" clause sets Controller = You. The trailing reminder text is stripped
  // before matching so lines like "...has trample. (It can deal excess...)" still match.
  private static readonly Regex _eachCreatureWithCounterKeywordPattern = new(
    @"^\s*Each\s+creatures?\s+you\s+control\s+with\s+a\s+\+1/\+1\s+counter\s+on\s+(?:it|them)\s+ha(?:s|ve)\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Arm 6: "All creatures have <keyword>." — global all-creatures keyword grant with
  // no controller restriction and no subtype qualifier (CR 611.3). The grant applies
  // to EVERY creature on the battlefield regardless of who controls it. Distinguished
  // from Arm 4 ("All <Subtype> creatures have <kw>.") by the absence of a capitalised
  // subtype before "creatures". Distinguished from Arm 2 by the absence of "you control".
  // Mass Hysteria ("All creatures have haste.") is the canonical example.
  private static readonly Regex _bareAllCreaturesKeywordPattern = new(
    @"^\s*All\s+creatures\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
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
    // Strip trailing reminder text before pattern matching so lines like
    // "Creature tokens you control have deathtouch. (Any amount ...)" still match.
    var rawText = StaticRuleHelpers.StripReminderText(clause.RawText);

    // Arm 1: anchor target — "(Enchanted|Equipped) creature has <keyword>."
    var anchorMatch = _bareAnchorKeywordPattern.Match(rawText);
    if (anchorMatch.Success)
    {
      var kw = anchorMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var grantedAbility = StaticRuleHelpers.MapKeywordToStaticAbility(kw);
      if (grantedAbility is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            GainedAbility = grantedAbility,
          }],
        },
      ];
    }

    // Arm 4: "All <Subtype> creatures have <keyword>." — global tribal keyword grant
    // (Rule 613.1c). "All" signals a universal quantifier with no controller restriction:
    // the grant applies to EVERY creature with the named subtype regardless of who controls
    // it. No Characteristics: ["other"] (this isn't an exclusion-of-self shape). No
    // Controller (global, not "you control"). The filter carries only CardTypes + Subtypes.
    // Covers Heart Sliver, Winged Sliver, Spinneret Sliver, etc. ("All Sliver creatures
    // have haste/flying/reach…").
    var allMatch = _bareAllKeywordPattern.Match(rawText);
    if (allMatch.Success)
    {
      var allSubtype = allMatch.Groups["sub"].Value.Trim();
      var allKw = allMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var allGranted = StaticRuleHelpers.MapKeywordToStaticAbility(allKw);
      if (allGranted is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Subtypes = [allSubtype],
              },
            },
            GainedAbility = allGranted,
          }],
        },
      ];
    }

    // Arm 3: "Other <filter> [you control] have <keyword>." — lord-grants-keyword
    // with the "Other" exclusion-of-self prefix (Rule 613.1c). The "Other" qualifier
    // maps to ExcludeSelf = true on the filter, matching the convention used
    // by TribalAnthemModifyPTRule and LordPTBuffRule. Filter parsing is
    // delegated to ParseLordPTFilter (isOther: true) which handles the full range
    // of filter shapes: bare "creatures", "[Subtype] creatures", two-word subtype
    // (e.g. "Goblin Warrior creatures"), bare plural subtype, "[Color] creatures",
    // and the optional "you control" controller suffix.
    var otherMatch = _bareOtherKeywordPattern.Match(rawText);
    if (otherMatch.Success)
    {
      var otherFilterText = otherMatch.Groups["filter"].Value.Trim();
      // Include trailing "you control" in the filter text so ParseLordPTFilter
      // can peel it and set Controller = ControllerFilter.You.
      if (otherMatch.Groups["ctrl"].Success)
      {
        otherFilterText = otherFilterText + " you control";
      }
      var otherKw = otherMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var otherGranted = StaticRuleHelpers.MapKeywordToStaticAbility(otherKw);
      if (otherGranted is null)
      {
        return null;
      }
      var otherFilter = ParseLordPTFilter(otherFilterText, isOther: true);
      if (otherFilter is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = otherFilter,
            },
            GainedAbility = otherGranted,
          }],
        },
      ];
    }

    // Arm 5: "Each creature you control with a +1/+1 counter on it has <keyword>."
    // Lord-grants-keyword where the subject filter carries a counter predicate.
    // Structures the "+1/+1 counter on it" condition as CounterCharacteristic{"+1/+1"}
    // (CR 122) on the filter, consistent with how "tapped"/"untapped" predicates are
    // structured in the lord-PT filter shapes (ParseLordPTFilter). The Each-kinded reference plus
    // Controller = You mirrors the filter-arm shape from TryParseLordPTBuff.
    var counterKeywordMatch = _eachCreatureWithCounterKeywordPattern.Match(rawText);
    if (counterKeywordMatch.Success)
    {
      var counterKw = counterKeywordMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var counterGranted = StaticRuleHelpers.MapKeywordToStaticAbility(counterKw);
      if (counterGranted is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.You,
                Characteristics = [Characteristic.FromLabel("with a +1/+1 counter")],
              },
            },
            GainedAbility = counterGranted,
          }],
        },
      ];
    }

    // Arm 6: "All creatures have <keyword>." — global all-creatures keyword grant
    // (CR 611.3: "A continuous effect may be generated by the static ability of an
    // object. Example: A permanent with the static ability 'All white creatures get
    // +1/+1' generates an effect that continuously gives +1/+1 to each white creature
    // on the battlefield."). The subject is ALL creatures with no controller restriction
    // and no subtype qualifier. CR 702.10a: "Haste is a static ability." No Controller
    // is set on the ObjectFilter — the grant is global, not scoped to "you control".
    var allCreaturesMatch = _bareAllCreaturesKeywordPattern.Match(rawText);
    if (allCreaturesMatch.Success)
    {
      var allCreaturesKw = allCreaturesMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var allCreaturesGranted = StaticRuleHelpers.MapKeywordToStaticAbility(allCreaturesKw);
      if (allCreaturesGranted is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
              },
            },
            GainedAbility = allCreaturesGranted,
          }],
        },
      ];
    }

    // Arm 2: filter target — "<filter> [tokens] you control have <keyword>."
    // Handles: "Creature tokens you control have <kw>.",
    //          "<Subtype> tokens you control have <kw>.",
    //          "Creatures you control have <kw>."
    var filterMatch = _bareFilterKeywordPattern.Match(rawText);
    if (!filterMatch.Success)
    {
      return null;
    }

    var filterText = filterMatch.Groups["filter"].Value.Trim();
    var filterKw = filterMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
    var isTokenFilter = filterMatch.Groups["token"].Success;

    var grantedAbility2 = StaticRuleHelpers.MapKeywordToStaticAbility(filterKw);
    if (grantedAbility2 is null)
    {
      return null;
    }

    var target = BuildBareGrantFilterTarget(filterText, isTokenFilter);
    if (target is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = target,
          GainedAbility = grantedAbility2,
        }],
      },
    ];
  }

  private static ObjectReference? BuildBareGrantFilterTarget(string filterText, bool isToken)
  {
    var lower = filterText.Trim().ToLowerInvariant();

    // "Creature" / "Creatures" — bare card-type filter.
    if (lower is "creature" or "creatures")
    {
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          IsToken = isToken ? true : (bool?)null,
          Controller = ControllerFilter.You,
        },
      };
    }

    // "[Subtype] creature(s)" — subtype + card-type filter (e.g. "Wolf creatures").
    var subtypeCreatureMatch = Regex.Match(filterText, @"^(?<sub>[A-Z][a-z]+)\s+creatures?$");
    if (subtypeCreatureMatch.Success)
    {
      var subtype = subtypeCreatureMatch.Groups["sub"].Value;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          IsToken = isToken ? true : (bool?)null,
          Controller = ControllerFilter.You,
        },
      };
    }

    // Bare plural subtype (e.g. "Goblins", "Warlocks", "Zombies") — no explicit
    // "creatures" word. The capitalised plural noun is the distinguishing feature;
    // the singular form is produced by <see cref="DepluralizeSubtype"/> so known
    // irregular plurals ("Elves" → "Elf") round-trip correctly. No CardTypes is
    // emitted: the subtype constraint on ObjectFilter.Subtypes already scopes the
    // filter to permanents carrying that subtype, regardless of card type (matching
    // the convention established by Sachi's Shamans you control gold).
    var bareSubtypeMatch = Regex.Match(filterText.Trim(), @"^(?<sub>[A-Z][a-z]+)s?$");
    if (bareSubtypeMatch.Success && !isToken)
    {
      var pluralWord = bareSubtypeMatch.Groups[0].Value;
      var subtype = DepluralizeSubtype(pluralWord);
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
        },
      };
    }

    return null;
  }

  private static ObjectFilter? ParseLordPTFilter(string filterText, bool isOther = false)
  {
    var text = filterText.Trim();

    // "Other " qualifier on the oracle line → set the structured ExcludeSelf
    // self-exclusion (CR 109.5 "another"), folded onto every returned filter
    // below. Any co-occurring axis predicates still ride on `characteristics`.
    bool? excludeSelf = isOther ? true : (bool?)null;
    IReadOnlyList<string>? characteristics = null;

    // Peel optional controller suffix — "you control" or "your opponents control".
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

    // --- Shape: "[Color] creatures" ---
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
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Colors = [colorCode],
        Controller = controller,
        ExcludeSelf = excludeSelf,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "artifact creatures" ---
    if (text.Equals("artifact creatures", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("artifact creature", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["artifact", "creature"],
        Controller = controller,
        ExcludeSelf = excludeSelf,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "tapped creatures" / "untapped creatures" / "nontoken creatures" / "attacking creatures" ---
    foreach (var (prefix, characteristic) in new[]
    {
      ("tapped creature", "tapped"),
      ("untapped creature", "untapped"),
      ("nontoken creature", "nontoken"),
      ("attacking creature", "attacking"),
    })
    {
      var pluralPrefix = prefix + "s";
      if (text.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
          text.Equals(pluralPrefix, StringComparison.OrdinalIgnoreCase))
      {
        var chars = characteristics is null
          ? (IReadOnlyList<string>)[characteristic]
          : [..characteristics, characteristic];
        return new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = controller,
          ExcludeSelf = excludeSelf,
          Characteristics = chars?.Select(Characteristic.FromLabel).ToList(),
        };
      }
    }

    // --- Shape: "Creature tokens" ---
    if (text.Equals("creature tokens", StringComparison.OrdinalIgnoreCase) ||
        text.Equals("creature token", StringComparison.OrdinalIgnoreCase))
    {
      var chars = characteristics is null
        ? (IReadOnlyList<string>)["token"]
        : [..characteristics, "token"];
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = controller,
        ExcludeSelf = excludeSelf,
        Characteristics = chars?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "Face-down creatures" ---
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
        ExcludeSelf = excludeSelf,
        Characteristics = chars?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: "[SubtypeA] [SubtypeB] creatures" ---
    var twoWordSubtypeCreatureMatch = Regex.Match(
      text,
      @"^(?<sub1>[A-Z][a-z]+)\s+(?<sub2>[A-Z][a-z]+)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (twoWordSubtypeCreatureMatch.Success)
    {
      var subtype1 = twoWordSubtypeCreatureMatch.Groups["sub1"].Value;
      var subtype2 = twoWordSubtypeCreatureMatch.Groups["sub2"].Value;
      if (!_colorNameToCode.ContainsKey(subtype1) && !_colorNameToCode.ContainsKey(subtype2))
      {
        return new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype1, subtype2],
          Controller = controller,
          ExcludeSelf = excludeSelf,
          Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
        };
      }
    }

    // --- Shape: "[Subtype] creatures" ---
    var subtypeCreatureMatch = Regex.Match(
      text,
      @"^(?<sub>[A-Z][a-z]+)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (subtypeCreatureMatch.Success)
    {
      var subtype = subtypeCreatureMatch.Groups["sub"].Value;
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = controller,
        ExcludeSelf = excludeSelf,
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
        ExcludeSelf = excludeSelf,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    // --- Shape: bare plural subtype ---
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
        ExcludeSelf = excludeSelf,
        Characteristics = characteristics?.Select(Characteristic.FromLabel).ToList(),
      };
    }

    return null;
  }

  private static string DepluralizeSubtype(string plural)
  {
    if (_subtypeIrregularPlurals.TryGetValue(plural, out var singular))
    {
      return singular;
    }
    return plural.EndsWith('s') ? plural[..^1] : plural;
  }
}
