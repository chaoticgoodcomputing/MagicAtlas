namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

/// <summary>
/// Shared utilities used across multiple <see cref="IStaticRule"/> implementations.
/// Lives outside the dispatcher so rules can be added without taking a dependency
/// on <see cref="StaticAbilityParser"/> internals.
/// </summary>
/// <remarks>
/// Phase 3 Stage A: these members are COPIED verbatim from the
/// <see cref="StaticAbilityParser"/> monolith (accessibility widened to
/// <c>internal</c>). The monolith retains its own private copies for its legacy
/// dispatch chain; a later Stage C deletes the monolith's copies once every legacy
/// rule has been extracted into a per-rule file under <c>Parsers/Static/</c>.
/// </remarks>
internal static class StaticRuleHelpers
{
  /// <summary>
  /// Strips trailing reminder text — a parenthetical clause at the end of the
  /// oracle line — before matching patterns that use end-of-string anchors.
  /// Reminder text is purely explanatory (Rule 207.2); stripping it before pattern
  /// matching is safe because the gold AST does not carry the parenthetical on these
  /// bare-grant shapes.
  /// </summary>
  internal static string StripReminderText(string text)
  {
    return Regex.Replace(text, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
  }

  /// <summary>
  /// Maps a small-count token (digit or number-word "one".."ten") onto an
  /// integer. Returns false for anything outside that vocabulary so callers
  /// can fall through to the fallback path.
  /// </summary>
  internal static bool TryParseSmallCount(string token, out int value)
  {
    if (int.TryParse(token, out value))
    {
      return true;
    }
    value = token switch
    {
      "a" => 1,
      "an" => 1,
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => 0,
    };
    return value > 0;
  }

  /// <summary>
  /// Classifies two filter nouns captured from a conjunctive oracle phrase
  /// and builds an <see cref="ObjectFilter"/> that covers spells of either
  /// type. Returns <see langword="null"/> for unrecognised or mixed-tier
  /// noun pairs (triggers an honest fallback failure rather than silently
  /// emitting a wrong filter).
  /// </summary>
  internal static ObjectFilter? BuildConjunctiveTypeSpellFilter(string nounA, string nounB)
  {
    var aIsCardType   = _spellFilterCardTypes.Contains(nounA);
    var bIsCardType   = _spellFilterCardTypes.Contains(nounB);
    var aIsColor      = _colorNameToCode.ContainsKey(nounA);
    var bIsColor      = _colorNameToCode.ContainsKey(nounB);
    var aIsSupertype  = _spellFilterSupertypes.Contains(nounA);
    var bIsSupertype  = _spellFilterSupertypes.Contains(nounB);

    // Both card types (Rule 205.2) — e.g. "Artifact and enchantment spells",
    // "Instant and sorcery spells". Emit as CardTypes disjunction; omit the
    // "spell" root because instant/sorcery cards that are on the stack are
    // always spells, and adding it would create an unreachable intersection
    // with types like Instant that cannot also be a "spell" card type.
    if (aIsCardType && bIsCardType)
    {
      return new ObjectFilter
      {
        CardTypes = [nounA.ToLowerInvariant(), nounB.ToLowerInvariant()],
        Controller = ControllerFilter.You,
      };
    }

    // Both colour names (Rule 105) — e.g. "Red spells and white spells"
    // (Familiar cycle from Planeshift). Emit as Colors disjunction.
    if (aIsColor && bIsColor)
    {
      _ = _colorNameToCode.TryGetValue(nounA, out var codeA);
      _ = _colorNameToCode.TryGetValue(nounB, out var codeB);
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Colors = [codeA!, codeB!],
        Controller = ControllerFilter.You,
      };
    }

    // One colour, one card type — not yet in corpus; fall through to null.
    // Both supertypes — not yet in corpus; fall through to null.

    // Otherwise treat both as creature/permanent subtypes (Rule 205.3) —
    // e.g. "Kithkin spells and Soldier spells" (Banneret cycle from
    // Morningtide). Root the filter at "spell" and carry both subtypes.
    if (!aIsCardType && !bIsCardType && !aIsColor && !bIsColor
        && !aIsSupertype && !bIsSupertype)
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Subtypes = [Capitalize(nounA), Capitalize(nounB)],
        Controller = ControllerFilter.You,
      };
    }

    return null;
  }

  // Card types the filter may name (lowercased on emit, matching the
  // existing CardTypes convention — see GaddockTeeg).
  private static readonly HashSet<string> _spellFilterCardTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "Artifact", "Creature", "Enchantment", "Instant", "Sorcery",
      "Planeswalker", "Land", "Battle", "Tribal",
    };

  // Supertypes the filter may name (PascalCase on emit — the supertype
  // axis preserves casing, matching how the TypeLine record encodes them).
  private static readonly HashSet<string> _spellFilterSupertypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "Legendary", "Snow", "Basic", "World", "Ongoing",
    };

  /// <summary>
  /// Maps the filter noun captured before "spells" onto an
  /// <see cref="ObjectFilter"/> rooted at <c>CardTypes: ["spell"]</c>. The
  /// noun is classified in priority order: colour → card type → supertype →
  /// subtype (catch-all). Returns <see langword="null"/> when the noun is
  /// empty (defensive — the regex requires at least one letter).
  /// </summary>
  internal static ObjectFilter? BuildTypeSpellFilter(string filterNoun)
  {
    if (string.IsNullOrWhiteSpace(filterNoun))
    {
      return null;
    }

    // Colour adjective (Rule 105) — emit as Colors single-letter code.
    if (_colorNameToCode.TryGetValue(filterNoun, out var colorCode))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Colors = [colorCode],
        Controller = ControllerFilter.You,
      };
    }

    // Colorless filter (Rule 105.1 — "Colorless is not a color"); encoded
    // as IsColorless rather than on the Colors axis.
    if (filterNoun.Equals("Colorless", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        IsColorless = true,
        Controller = ControllerFilter.You,
      };
    }

    // Card type (Rule 205.2) — appended to the CardTypes axis so the filter
    // reads as "a spell that is also of type X" (multi-element CardTypes
    // precedent: e.g. "artifact land" → ["artifact", "land"]).
    if (_spellFilterCardTypes.Contains(filterNoun))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell", filterNoun.ToLowerInvariant()],
        Controller = ControllerFilter.You,
      };
    }

    // Supertype (Rule 205.4) — emit on the Supertypes axis, PascalCase.
    if (_spellFilterSupertypes.Contains(filterNoun))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Supertypes = [Capitalize(filterNoun)],
        Controller = ControllerFilter.You,
      };
    }

    // Otherwise treat as a creature/permanent subtype (Rule 205.3) — the
    // catch-all branch handles tribal lords ("Angel spells", "Giant spells",
    // "Goblin spells", …) and equipment/aura subtypes ("Aura spells",
    // "Equipment spells", …).
    return new ObjectFilter
    {
      CardTypes = ["spell"],
      Subtypes = [Capitalize(filterNoun)],
      Controller = ControllerFilter.You,
    };
  }

  // Lowercase the rest, uppercase the first letter — matches oracle-text
  // casing for both subtypes and supertypes.
  internal static string Capitalize(string s) =>
    s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

  /// <summary>
  /// Maps the captured subject phrase of a combat-requirement line onto the
  /// matching <see cref="ObjectReference"/>. Lines like "All creatures" become
  /// an <c>Each</c>-kinded reference with a creature filter (Grand Melee
  /// shape); everything else — including the card's own name or "This
  /// creature" / "This permanent" — collapses to <c>Self</c> (the
  /// long-standing convention for self-referential continuous abilities).
  /// </summary>
  internal static ObjectReference ClassifyCombatRequirementSubject(string subjectText)
  {
    var subject = subjectText.Trim();
    if (subject.Equals("All creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    return ObjectReference.Self();
  }

  /// <summary>
  /// Maps a keyword phrase to its canonical <see cref="StaticAbility"/> node.
  /// Returns null for keywords not yet supported, causing the caller to fall
  /// through to the fallback path.
  /// </summary>
  internal static StaticAbility? MapKeywordToStaticAbility(string keyword)
  {
    return keyword.ToLowerInvariant() switch
    {
      "first strike" => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.First,
        }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.Both,
        }],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["flying", "reach"],
          },
        }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = "Indestructible",
        Effects = [new MagicAST.AST.Effects.Keyword.IndestructibleEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new MagicAST.AST.Effects.Keyword.VigilanceEffect()],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = "Haste",
        Effects = [new MagicAST.AST.Effects.Keyword.HasteEffect()],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new MagicAST.AST.Effects.Damage.LifelinkEffect()],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = "Reach",
        Effects = [new MagicAST.AST.Effects.Keyword.ReachEffect()],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = "Trample",
        Effects = [new MagicAST.AST.Effects.Keyword.TrampleEffect()],
      },
      "defender" => new StaticAbility
      {
        KeywordSource = "Defender",
        Effects = [new MagicAST.AST.Effects.Keyword.DefenderEffect { IsOptional = false }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = "Deathtouch",
        Effects = [new MagicAST.AST.Effects.Keyword.DeathtouchEffect { IsOptional = false }],
      },
      // Menace: this creature can't be blocked except by two or more creatures.
      // Rule 702.111. EvasionEffect with MinimumBlockers=2; CanBeBlockedBy carries
      // the creature-typed filter (any two-or-more creatures qualify as blockers).
      "menace" => new StaticAbility
      {
        KeywordSource = "Menace",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
          MinimumBlockers = 2,
        }],
      },
      // Shadow: only creatures with shadow can block this creature, and this creature
      // can only block other shadow creatures. Rule 702.28. Mutual evasion —
      // EvasionEffect with CanBeBlockedBy restricted to the "shadow" characteristic.
      "shadow" => new StaticAbility
      {
        KeywordSource = "Shadow",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["shadow"],
          },
        }],
      },
      // Intimidate: can't be blocked except by artifact creatures and/or creatures
      // sharing a color. Rule 702.13. Mirrors Fear (702.36) but with the color-share
      // predicate instead of the fixed black-color predicate.
      "intimidate" => new StaticAbility
      {
        KeywordSource = "Intimidate",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "shares a color"],
          },
        }],
      },
      // Myriad: triggered keyword. MAST records keyword presence; the per-opponent
      // copy-creation and delayed-exile semantics are engine territory. Rule 702.116.
      "myriad" => new StaticAbility
      {
        KeywordSource = "Myriad",
        Effects = [new MagicAST.AST.Effects.Keyword.MyriadEffect()],
      },
      // Melee: triggered keyword. MAST records keyword presence; the per-opponent
      // attack counting and P/T buff are engine territory. Rule 702.121.
      "melee" => new StaticAbility
      {
        KeywordSource = "Melee",
        Effects = [new MagicAST.AST.Effects.Keyword.MeleeEffect()],
      },
      // Hexproof (Rule 702.11): this permanent can't be the target of spells or
      // abilities opponents control. MAST records the keyword's presence; the
      // targeting restriction is engine territory.
      "hexproof" => new StaticAbility
      {
        KeywordSource = "Hexproof",
        Effects = [new MagicAST.AST.Effects.Keyword.HexproofEffect()],
      },
      // Fear: can't be blocked except by artifact creatures and/or black creatures.
      // Rule 702.36. Mirrors Intimidate (702.13) but with a fixed black-color predicate
      // instead of the color-share predicate. EvasionEffect with CanBeBlockedBy carrying
      // Characteristics: ["artifact", "black"].
      "fear" => new StaticAbility
      {
        KeywordSource = "Fear",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "black"],
          },
        }],
      },
      // Shroud (Rule 702.18): this permanent can't be the target of spells or abilities.
      // A legacy protection keyword largely superseded by Hexproof; differs from Hexproof
      // in that it applies to the controller's own spells and abilities as well.
      // MAST records the keyword's presence; the targeting restriction is engine territory.
      "shroud" => new StaticAbility
      {
        KeywordSource = "Shroud",
        Effects = [new MagicAST.AST.Effects.Keyword.ShroudEffect()],
      },
      // Daybound (Rule 702.145b). Found on front faces of day/night DFCs. MAST
      // records the keyword's presence and phase; the day/night transformation
      // rules (Rule 731) are engine territory.
      "daybound" => new StaticAbility
      {
        KeywordSource = "Daybound",
        Effects = [new MagicAST.AST.Effects.Keyword.DayNightEffect
        {
          Phase = MagicAST.AST.Effects.Keyword.DayNightPhase.Daybound,
        }],
      },
      // Nightbound (Rule 702.145e). Found on back faces of day/night DFCs. MAST
      // records the keyword's presence and phase; the day/night transformation
      // rules (Rule 731) are engine territory.
      "nightbound" => new StaticAbility
      {
        KeywordSource = "Nightbound",
        Effects = [new MagicAST.AST.Effects.Keyword.DayNightEffect
        {
          Phase = MagicAST.AST.Effects.Keyword.DayNightPhase.Nightbound,
        }],
      },
      // Phasing (Rule 702.26): this permanent phases in or out before the controller's
      // untap step. MAST records the keyword's presence; phase bookkeeping is engine territory.
      "phasing" => new StaticAbility
      {
        KeywordSource = "Phasing",
        Effects = [new MagicAST.AST.Effects.Keyword.PhasingEffect { IsOptional = false }],
      },
      // Provoke (Rule 702.39): a triggered keyword ability. Whenever this creature attacks,
      // the controller may have a target creature the defending player controls untap and block
      // this creature if able. MAST records the keyword's presence; the trigger and force-block
      // mechanics are engine territory.
      "provoke" => new StaticAbility
      {
        KeywordSource = "Provoke",
        Effects = [new MagicAST.AST.Effects.Keyword.ProvokeEffect { IsOptional = false }],
      },
      // Cipher (Rule 702.99): exile this spell card encoded on a creature you control;
      // whenever that creature deals combat damage to a player, cast a copy for free.
      // MAST records the keyword's presence; the encoding and free-cast mechanics are engine territory.
      "cipher" => new StaticAbility
      {
        KeywordSource = "Cipher",
        Effects = [new MagicAST.AST.Effects.Keyword.CipherEffect { IsOptional = false }],
      },
      // Haunt (Rule 702.55): when this creature dies, exile it haunting target creature.
      // MAST records the keyword's presence; the exile-on-death and haunt-trigger mechanics
      // are engine territory.
      "haunt" => new StaticAbility
      {
        KeywordSource = "Haunt",
        Effects = [new MagicAST.AST.Effects.Keyword.HauntEffect { IsOptional = false }],
      },
      _ => null,
    };
  }

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
}
