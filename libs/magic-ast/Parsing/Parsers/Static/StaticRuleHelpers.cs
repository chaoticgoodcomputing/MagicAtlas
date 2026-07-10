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
        KeywordSource = KeywordAbility.FirstStrike,
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.First,
        }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.DoubleStrike,
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.Both,
        }],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Flying,
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
          },
        }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Indestructible,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new MagicAST.AST.Effects.Damage.LifelinkEffect()],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Reach,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      },
      "defender" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Defender,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Defender }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      },
      // Menace: this creature can't be blocked except by two or more creatures.
      // Rule 702.111. EvasionEffect with MinimumBlockers=2; CanBeBlockedBy carries
      // the creature-typed filter (any two-or-more creatures qualify as blockers).
      "menace" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Menace,
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
        KeywordSource = KeywordAbility.Shadow,
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [Characteristic.HasKeyword(KeywordAbility.Shadow)],
          },
        }],
      },
      // Intimidate: can't be blocked except by artifact creatures and/or creatures
      // sharing a color. Rule 702.13. Mirrors Fear (702.36) but with the color-share
      // predicate (SharesColorWith=Self) instead of the fixed black Colors entry.
      "intimidate" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Intimidate,
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature", "artifact"],
            SharesColorWith = ObjectReference.Self(),
          },
        }],
      },
      // Myriad: triggered keyword. MAST records keyword presence; the per-opponent
      // copy-creation and delayed-exile semantics are engine territory. Rule 702.116.
      "myriad" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Myriad,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Myriad }],
      },
      // Melee: triggered keyword. MAST records keyword presence; the per-opponent
      // attack counting and P/T buff are engine territory. Rule 702.121.
      "melee" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Melee,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Melee }],
      },
      // Hexproof (Rule 702.11): this permanent can't be the target of spells or
      // abilities opponents control. MAST records the keyword's presence; the
      // targeting restriction is engine territory.
      "hexproof" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Hexproof,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      },
      // Fear: can't be blocked except by artifact creatures and/or black creatures.
      // Rule 702.36. Mirrors Intimidate (702.13) but with a fixed black Colors entry
      // instead of the color-share predicate. EvasionEffect with CanBeBlockedBy structuring
      // the artifact type (CardTypes) and the black color (Colors).
      "fear" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Fear,
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature", "artifact"],
            Colors = ["B"],
          },
        }],
      },
      // Shroud (Rule 702.18): this permanent can't be the target of spells or abilities.
      // A legacy protection keyword largely superseded by Hexproof; differs from Hexproof
      // in that it applies to the controller's own spells and abilities as well.
      // MAST records the keyword's presence; the targeting restriction is engine territory.
      "shroud" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shroud,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }],
      },
      // Daybound (Rule 702.145b). Found on front faces of day/night DFCs. MAST
      // records the keyword's presence and phase; the day/night transformation
      // rules (Rule 731) are engine territory.
      "daybound" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Daybound,
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
        KeywordSource = KeywordAbility.Nightbound,
        Effects = [new MagicAST.AST.Effects.Keyword.DayNightEffect
        {
          Phase = MagicAST.AST.Effects.Keyword.DayNightPhase.Nightbound,
        }],
      },
      // Phasing (Rule 702.26): this permanent phases in or out before the controller's
      // untap step. MAST records the keyword's presence; phase bookkeeping is engine territory.
      "phasing" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Phasing,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Phasing }],
      },
      // Provoke (Rule 702.39): a triggered keyword ability. Whenever this creature attacks,
      // the controller may have a target creature the defending player controls untap and block
      // this creature if able. MAST records the keyword's presence; the trigger and force-block
      // mechanics are engine territory.
      "provoke" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Provoke,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Provoke }],
      },
      // Cipher (Rule 702.99): exile this spell card encoded on a creature you control;
      // whenever that creature deals combat damage to a player, cast a copy for free.
      // MAST records the keyword's presence; the encoding and free-cast mechanics are engine territory.
      "cipher" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Cipher,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Cipher }],
      },
      // Haunt (Rule 702.55): when this creature dies, exile it haunting target creature.
      // MAST records the keyword's presence; the exile-on-death and haunt-trigger mechanics
      // are engine territory.
      "haunt" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haunt,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haunt }],
      },
      // Riot (Rule 702.136): this creature enters with your choice of a +1/+1 counter or
      // haste. A parameterless keyword marker — MAST records keyword presence; the choice
      // and counter/haste application semantics are engine territory.
      "riot" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Riot,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Riot }],
      },
      // Infect (Rule 702.90a: "Infect is a static ability."): this creature deals
      // damage to creatures in the form of -1/-1 counters and to players in the
      // form of poison counters (Rule 702.90b-c). MAST records the keyword's
      // presence via the same KeywordAbilityEffect shape as the other parameterless
      // markers; the damage-redirection semantics are engine territory. Enables the
      // "Enchanted/Equipped creature has infect" grant shape (BareKeywordGrantRule
      // Arm 1), mirroring how it already grants Defender/Deathtouch/etc.
      "infect" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Infect,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Infect }],
      },
      _ => null,
    };
  }

  /// <summary>
  /// Parses an object-count phrase — the noun phrase that follows "for each" or
  /// "the number of" — into a structured <see cref="ObjectFilter"/>. Handles the
  /// shapes in the count corpus:
  /// <list type="bullet">
  /// <item>"&lt;type/subtype&gt; you control" — e.g. "lands you control",
  ///   "legendary creature you control", "Mountain you control".</item>
  /// <item>"&lt;subtype&gt; [and &lt;subtype&gt;] attached to it" — e.g.
  ///   "Aura attached to it", "Aura and Equipment attached to it".</item>
  /// <item>"&lt;type&gt; cards in all graveyards" — e.g. "land cards in all
  ///   graveyards" (Terravore) — a typed count across every player's graveyard;
  ///   no Controller/Owner axis, mirroring the all-graveyards convention used
  ///   by <c>ExileAllRule.GraveyardPattern</c>.</item>
  /// </list>
  /// The leading noun(s) are classified supertype → card type → subtype, mirroring
  /// <see cref="BuildTypeSpellFilter"/>. Plurals are singularized for matching and
  /// emit the canonical singular type/subtype. Returns <see langword="null"/> for a
  /// phrase that does not match any shape (honest fallback).
  /// </summary>
  internal static ObjectFilter? BuildObjectCountFilter(string phrase)
  {
    var text = phrase.Trim().TrimEnd('.');

    // "<nouns> attached to it" — relational count (Strong Back, Kor Spiritdancer).
    var attachedMatch = Regex.Match(
      text,
      @"^(?<nouns>.+?)\s+attached\s+to\s+it$",
      RegexOptions.IgnoreCase
    );
    if (attachedMatch.Success)
    {
      var subtypes = SplitConjunctiveNouns(attachedMatch.Groups["nouns"].Value)
        .Select(Capitalize)
        .ToList();
      if (subtypes.Count == 0)
      {
        return null;
      }
      return new ObjectFilter
      {
        Subtypes = subtypes,
        AttachedTo = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
      };
    }

    // "<type> cards in all graveyards" — typed count across every player's
    // graveyard (Terravore's characteristic-defining ability; CR 604.3). The
    // trailing "card"/"cards" head noun is stripped before classification so
    // only the leading type noun (e.g. "land") is fed to
    // ClassifyTypeNounPhrase — feeding the whole "land cards" phrase would
    // wrongly classify "cards" itself as a subtype. Absence of a Controller
    // axis is the established "all graveyards"/all-players encoding, mirroring
    // ExileAllRule.GraveyardPattern's "all <type> cards from all graveyards".
    var allGraveyardsMatch = Regex.Match(
      text,
      @"^(?<type>.+?)\s+cards?\s+in\s+all\s+graveyards$",
      RegexOptions.IgnoreCase
    );
    if (allGraveyardsMatch.Success)
    {
      var filter = ClassifyTypeNounPhrase(allGraveyardsMatch.Groups["type"].Value);
      return filter is null
        ? null
        : filter with { Zone = Zone.Graveyard };
    }

    // "<type> cards in your graveyard" — typed count scoped to the
    // controller's own graveyard (Salvage Slasher's characteristic-defining
    // P/T bonus, CR 604.3; the layer-7c modifier itself is CR 613.4c). Distinct
    // from the "all graveyards" case above by the added Controller axis
    // (mirrors the "you control" board-count case's Controller.You), matching
    // the established Controller+Zone shape already used for cost-reduction
    // counts over "your graveyard" (Ghoultree, Cryptic Serpent).
    var yourGraveyardMatch = Regex.Match(
      text,
      @"^(?<type>.+?)\s+cards?\s+in\s+your\s+graveyard$",
      RegexOptions.IgnoreCase
    );
    if (yourGraveyardMatch.Success)
    {
      var filter = ClassifyTypeNounPhrase(yourGraveyardMatch.Groups["type"].Value);
      return filter is null
        ? null
        : filter with { Zone = Zone.Graveyard, Controller = ControllerFilter.You };
    }

    // "<nouns> you control" — board count.
    var controlMatch = Regex.Match(
      text,
      @"^(?<nouns>.+?)\s+you\s+control$",
      RegexOptions.IgnoreCase
    );
    if (controlMatch.Success)
    {
      var filter = ClassifyTypeNounPhrase(controlMatch.Groups["nouns"].Value);
      return filter is null
        ? null
        : filter with { Controller = ControllerFilter.You };
    }

    return null;
  }

  // Splits "Aura and Equipment" into ["Aura", "Equipment"]; a single noun yields
  // a one-element list.
  private static IReadOnlyList<string> SplitConjunctiveNouns(string nouns) =>
    Regex.Split(nouns.Trim(), @"\s*,?\s+and\s+|\s*,\s*", RegexOptions.IgnoreCase)
      .Select(n => n.Trim())
      .Where(n => n.Length > 0)
      .ToList();

  /// <summary>
  /// Maps a type noun phrase like "lands", "legendary creature", "Mountain",
  /// "other Rat" onto an <see cref="ObjectFilter"/>. A leading "other" qualifier
  /// (Rule 109.2 — excludes the source object itself) is peeled off and recorded
  /// as an <see cref="OtherCharacteristic"/>, mirroring the "Other creatures you
  /// control" anthem convention. Remaining leading words are treated as supertypes
  /// when recognised (e.g. "legendary"); the head noun is classified card type →
  /// subtype. Returns null when the head noun is empty.
  /// </summary>
  private static ObjectFilter? ClassifyTypeNounPhrase(string phrase)
  {
    var words = phrase.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (words.Length == 0)
    {
      return null;
    }

    // Peel a leading "other" — the self-exclusion qualifier (Rule 109.2). It is
    // never a head noun, so consuming it here leaves the type classification to
    // run on the remaining "[supertypes] head" words.
    var isOther = false;
    var start = 0;
    if (words.Length > 1 && words[0].Equals("other", StringComparison.OrdinalIgnoreCase))
    {
      isOther = true;
      start = 1;
    }

    var supertypes = new List<string>();
    var i = start;
    while (i < words.Length - 1 && _spellFilterSupertypes.Contains(words[i]))
    {
      supertypes.Add(Capitalize(words[i]));
      i++;
    }

    var head = Singularize(words[i]);
    var filter = _spellFilterCardTypes.Contains(head)
      ? new ObjectFilter { CardTypes = [head.ToLowerInvariant()] }
      : new ObjectFilter { Subtypes = [Capitalize(head)] };

    if (supertypes.Count > 0)
    {
      filter = filter with { Supertypes = supertypes };
    }
    if (isOther)
    {
      filter = filter with { ExcludeSelf = true };
    }
    return filter;
  }

  // Strips a trailing plural "s" so "lands"/"artifacts"/"Shrines" classify on
  // their singular type/subtype. Conservative: only a simple trailing "s".
  private static string Singularize(string noun) =>
    noun.Length > 1 && noun.EndsWith('s') ? noun[..^1] : noun;

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
