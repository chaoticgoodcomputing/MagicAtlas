namespace MagicAST.Parsing.Combinators;

using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;
using Superpower.Parsers;

/// <summary>
/// Composable parser combinators for Magic: The Gathering oracle text.
/// This module provides reusable building blocks that can be combined to parse complex ability patterns.
/// </summary>
/// <remarks>
/// Design philosophy:
/// - Small, focused parsers that do one thing well
/// - Compose via monadic combinators (Select, Then, Or, Many)
/// - Token-based, not string-based
/// - Type-safe and declarative
/// </remarks>
public static class OracleParsers
{
  #region Primitives

  /// <summary>
  /// Parses a specific keyword token (case-insensitive word match).
  /// Returns the matched token for use in further combinators.
  /// </summary>
  private static TokenListParser<OracleToken, Token<OracleToken>> Keyword(string keyword)
  {
    return Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t => t.ToStringValue().Equals(keyword, StringComparison.OrdinalIgnoreCase));
  }

  /// <summary>
  /// Parses optional reminder text (parenthesized content).
  /// </summary>
  private static readonly TokenListParser<OracleToken, Parenthetical?> _optionalReminder = Token
    .EqualTo(OracleToken.ReminderText)
    .Select(t => (Parenthetical?)new Parenthetical { Text = t.ToStringValue() })
    .OptionalOrDefault();

  #endregion

  #region Simple Keywords

  /// <summary>
  /// Parser for the "Flying" keyword.
  /// Pattern: "Flying" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flying = (
    from keyword in Keyword("Flying")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flying",
      Effect = new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Vigilance" keyword.
  /// Pattern: "Vigilance" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Vigilance = (
    from keyword in Keyword("Vigilance")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Vigilance",
      Effect = new VigilanceEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Menace" keyword.
  /// Pattern: "Menace" [reminder]
  /// Rule 702.110. Evasion ability requiring two or more blockers.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Menace = (
    from keyword in Keyword("Menace")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Menace",
      Effect = new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
        MinimumBlockers = 2,
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Trample" keyword.
  /// Pattern: "Trample" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Trample = (
    from keyword in Keyword("Trample")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Trample",
      Effect = new TrampleEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Haste" keyword.
  /// Pattern: "Haste" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Haste = (
    from keyword in Keyword("Haste")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Haste",
      Effect = new HasteEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Lifelink" keyword.
  /// Pattern: "Lifelink" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Lifelink = (
    from keyword in Keyword("Lifelink")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Lifelink",
      Effect = new LifelinkEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Reach" keyword.
  /// Pattern: "Reach" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Reach = (
    from keyword in Keyword("Reach")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Reach",
      Effect = new ReachEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Flash" keyword.
  /// Pattern: "Flash" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flash = (
    from keyword in Keyword("Flash")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flash",
      Effect = new TimingModificationEffect
      {
        Modification = TimingModificationType.Grant,
        Timing = TimingWindow.Instant,
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Storm" keyword.
  /// Pattern: "Storm" [reminder]
  /// Rule 702.40
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Storm = (
    from keyword in Keyword("Storm")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Storm",
      Effect = new StormEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Defender" keyword.
  /// Pattern: "Defender" [reminder]
  /// Rule 702.3. A creature with defender can't attack. MAST records keyword
  /// presence; the can't-attack semantics are derived by the rules engine.
  /// Handles both bare "Defender" and "Defender (This creature can't attack.)"
  /// — reminder text is consumed but not stored in the AST.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Defender = (
    from keyword in Keyword("Defender")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Defender",
      Effect = new DefenderEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Cascade" keyword.
  /// Pattern: "Cascade" [reminder]
  /// Rule 702.85. Records keyword presence; the exile-and-cast machinery is
  /// handled by the rules engine. Reminder text is consumed but not stored.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Cascade = (
    from keyword in Keyword("Cascade")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Cascade",
      Effect = new CascadeEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Deathtouch" keyword.
  /// Pattern: "Deathtouch" [reminder]
  /// Rule 702.2. Any nonzero damage dealt by a source with deathtouch destroys the
  /// damaged creature. MAST records keyword presence; semantics are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Deathtouch = (
    from keyword in Keyword("Deathtouch")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Deathtouch",
      Effect = new DeathtouchEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Hexproof" keyword.
  /// Pattern: "Hexproof" [reminder]
  /// Rule 702.11. This permanent can't be the target of spells or abilities
  /// your opponents control. MAST records keyword presence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Hexproof = (
    from keyword in Keyword("Hexproof")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Hexproof",
      Effect = new HexproofEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Shroud" keyword.
  /// Pattern: "Shroud" [reminder]
  /// Rule 702.18. This permanent can't be the target of spells or abilities
  /// (including by its controller). MAST records keyword presence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Shroud = (
    from keyword in Keyword("Shroud")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Shroud",
      Effect = new ShroudEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Prowess" keyword.
  /// Pattern: "Prowess" [reminder]
  /// Rule 702.108. Modeled as a keyword marker per MAST convention;
  /// the trigger-and-buff expansion is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Prowess = (
    from keyword in Keyword("Prowess")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Prowess",
      Effect = new ProwessEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Devoid" keyword.
  /// Pattern: "Devoid" [reminder]
  /// Rule 702.114. Characteristic-defining ability — this card has no color.
  /// MAST records keyword presence; the colorless-derived semantics are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Devoid = (
    from keyword in Keyword("Devoid")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Devoid",
      Effect = new DevoidEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Changeling" keyword.
  /// Pattern: "Changeling" [reminder]
  /// Rule 702.73. This card is every creature type. MAST records keyword presence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Changeling = (
    from keyword in Keyword("Changeling")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Changeling",
      Effect = new ChangelingEffect(),
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Banding" keyword.
  /// Pattern: "Banding" [reminder]
  /// Rule 702.22. Legacy ability. MAST records keyword presence; combat-band
  /// semantics are engine territory. Reminder text varies but keyword token is uniform.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Banding = (
    from keyword in Keyword("Banding")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Banding",
      Effect = new BandingEffect(),
      Reminder = reminder,
    }
  );

  #endregion

  #region Combat Timing Keywords

  /// <summary>
  /// Parser for "First strike" keyword (handles both "First strike" and "First Strike").
  /// Pattern: "First" "strike" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> FirstStrike = (
    from first in Keyword("First")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "First strike",
      Effect = new CombatDamageTimingEffect { Timing = CombatDamageTiming.First },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Double strike" keyword.
  /// Pattern: "Double" "strike" [reminder]
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> DoubleStrike = (
    from double_ in Keyword("Double")
    from strike in Keyword("Strike").Or(Keyword("strike"))
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Double strike",
      Effect = new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both },
      Reminder = reminder,
    }
  );

  #endregion

  #region Landwalk Keywords

  /// <summary>
  /// Creates a landwalk parser for a specific land type.
  /// </summary>
  private static TokenListParser<OracleToken, StaticAbility> Landwalk(
    string keywordName,
    string landType
  )
  {
    return from keyword in Keyword(keywordName)
      from reminder in _optionalReminder
      select new StaticAbility
      {
        KeywordSource = keywordName,
        Effect = new EvasionEffect
        {
          UnblockableCondition = new EvasionCondition
          {
            ConditionType = EvasionConditionType.DefendingPlayerControls,
            PermanentFilter = new ObjectFilter { Subtypes = [landType] },
          },
        },
        Reminder = reminder,
      };
  }

  /// <summary>Parser for "Forestwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Forestwalk = Landwalk(
    "Forestwalk",
    "Forest"
  );

  /// <summary>Parser for "Islandwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Islandwalk = Landwalk(
    "Islandwalk",
    "Island"
  );

  /// <summary>Parser for "Mountainwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Mountainwalk = Landwalk(
    "Mountainwalk",
    "Mountain"
  );

  /// <summary>Parser for "Plainswalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Plainswalk = Landwalk(
    "Plainswalk",
    "Plains"
  );

  /// <summary>Parser for "Swampwalk" keyword.</summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Swampwalk = Landwalk(
    "Swampwalk",
    "Swamp"
  );

  #endregion

  #region Parameterized Keywords

  /// <summary>
  /// Parses additional protection qualities after "and from".
  /// Pattern: "and" "from" quality
  /// </summary>
  private static readonly TokenListParser<
    OracleToken,
    ProtectionQuality
  > _additionalProtectionQuality =
    from and in Token.EqualTo(OracleToken.And)
    from from_ in Keyword("from")
    from quality in _protectionQuality!
    select quality;

  /// <summary>
  /// Parser for "Protection from X" keyword.
  /// Pattern: "Protection" "from" quality ["and" "from" quality]*
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Protection = (
    from keyword in Keyword("Protection")
    from from_ in Keyword("from")
    from first in _protectionQuality!
    from rest in _additionalProtectionQuality.Try().Many()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Protection",
      Effect = new ProtectionEffect { From = new[] { first }.Concat(rest).ToArray() },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parses a single protection quality (color, subtype, etc.).
  /// Examples: "red", "Demons", "everything"
  /// </summary>
  private static readonly TokenListParser<OracleToken, ProtectionQuality> _protectionQuality = Token
    .EqualTo(OracleToken.Word)
    .Select(token =>
    {
      var value = token.ToStringValue();
      var normalized = value.ToLowerInvariant();

      // Special case: "everything"
      if (normalized == "everything")
      {
        return new ProtectionQuality { Kind = ProtectionQualityKind.Everything };
      }

      // Colors: red, blue, white, black, green
      if (normalized is "red" or "blue" or "white" or "black" or "green" or "colorless")
      {
        return new ProtectionQuality { Kind = ProtectionQualityKind.Color, Value = normalized };
      }

      // Card types: creatures, artifacts, enchantments, instants, sorceries
      var cardTypes = new[]
      {
        "creatures",
        "artifacts",
        "enchantments",
        "instants",
        "sorceries",
        "planeswalkers",
      };
      if (cardTypes.Contains(normalized))
      {
        // Singularize (remove trailing 's')
        var singular = normalized.EndsWith("s") ? normalized[..^1] : normalized;
        return new ProtectionQuality { Kind = ProtectionQualityKind.CardType, Value = singular };
      }

      // Characteristics: multicolored, monocolored, etc. — lowercase
      // single-word qualifiers that aren't a color name. Oracle text
      // capitalizes creature-type names (Demons, Dragons), so a lowercase
      // token here means a state predicate rather than a type.
      var characteristics = new[] { "multicolored", "monocolored", "colored" };
      if (characteristics.Contains(normalized))
      {
        return new ProtectionQuality
        {
          Kind = ProtectionQualityKind.Characteristic,
          Value = normalized,
        };
      }

      // Otherwise, assume it's a subtype (capitalized in oracle text)
      // Examples: "Demons", "Dragons", "Elves"
      return new ProtectionQuality { Kind = ProtectionQualityKind.Subtype, Value = value };
    });

  /// <summary>
  /// Parser for "Crew N" keyword.
  /// Pattern: "Crew" number [reminder]
  /// Rule 702.122. Records the keyword's presence and parameter; MAST is
  /// descriptive, so cost/resolution semantics aren't expanded here.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Crew = (
    from keyword in Keyword("Crew")
    from n in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Crew",
      Effect = new CrewEffect
      {
        Power = new LiteralQuantity { Value = int.Parse(n.ToStringValue()) },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Partner with [Name]" keyword.
  /// Pattern: "Partner" "with" Word+ [reminder]
  /// Rule 702.124. The partner-name parameter is captured as the literal
  /// joined string of Word tokens between "with" and either end-of-input or
  /// optional reminder text. Names span multiple words (e.g., "Amy Pond",
  /// "Brallin, Skyshark Rider"); commas and periods are not consumed.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> PartnerWith = (
    from partner in Keyword("Partner")
    from with_ in Keyword("with")
    from nameWords in Token.EqualTo(OracleToken.Word).AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Partner with",
      Effect = new PartnerEffect
      {
        PartnerType = PartnerType.PartnerWith,
        PartnerName = string.Join(" ", nameWords.Select(t => t.ToStringValue())),
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Entwine {cost}" — Rule 702.41. The cost is parsed by the
  /// shared mana-cost parser so multi-symbol entwine costs (e.g. {1}{B}) land
  /// as full <see cref="MagicAST.AST.Costs.ManaCost"/> nodes rather than
  /// free-text fragments.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Entwine = (
    from keyword in Keyword("Entwine")
    from costSymbols in Token
      .Matching<OracleToken>(
        k =>
          k == OracleToken.GenericMana
          || k == OracleToken.WhiteMana
          || k == OracleToken.BlueMana
          || k == OracleToken.BlackMana
          || k == OracleToken.RedMana
          || k == OracleToken.GreenMana
          || k == OracleToken.ColorlessMana
          || k == OracleToken.VariableMana
          || k == OracleToken.HybridMana
          || k == OracleToken.PhyrexianMana,
        "mana symbol"
      )
      .AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Entwine",
      Effect = new EntwineEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Cycling {cost}" keyword.
  /// Pattern: "Cycling" mana-symbol+ [reminder]
  /// Rule 702.32. An activated ability playable only from hand:
  /// "[Cost], Discard this card: Draw a card." MAST records the keyword and
  /// its associated mana cost; the inner discard/draw structure is inferred
  /// from the rules. Reminder text is consumed but not stored.
  /// The cost is the polymorphic <see cref="MagicAST.AST.Costs.Cost"/> base
  /// to accommodate typecycling and similar variants.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Cycling = (
    from keyword in Keyword("Cycling")
    from costSymbols in Token
      .Matching<OracleToken>(
        k =>
          k == OracleToken.GenericMana
          || k == OracleToken.WhiteMana
          || k == OracleToken.BlueMana
          || k == OracleToken.BlackMana
          || k == OracleToken.RedMana
          || k == OracleToken.GreenMana
          || k == OracleToken.ColorlessMana
          || k == OracleToken.VariableMana
          || k == OracleToken.HybridMana
          || k == OracleToken.PhyrexianMana,
        "mana symbol"
      )
      .AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Cycling",
      Effect = new CyclingEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Madness {cost}" keyword.
  /// Pattern: "Madness" mana-symbol+ [reminder]
  /// Rule 702.35. Lets a player cast a discarded card for its madness cost.
  /// MAST records the keyword and the alternative cost; discard-into-exile-and-cast
  /// machinery is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Madness = (
    from keyword in Keyword("Madness")
    from costSymbols in Token
      .Matching<OracleToken>(
        k =>
          k == OracleToken.GenericMana
          || k == OracleToken.WhiteMana
          || k == OracleToken.BlueMana
          || k == OracleToken.BlackMana
          || k == OracleToken.RedMana
          || k == OracleToken.GreenMana
          || k == OracleToken.ColorlessMana
          || k == OracleToken.VariableMana
          || k == OracleToken.HybridMana
          || k == OracleToken.PhyrexianMana,
        "mana symbol"
      )
      .AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Madness",
      Effect = new MadnessEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Foretell {cost}" keyword.
  /// Pattern: "Foretell" mana-symbol+ [reminder]
  /// Rule 702.143. Lets a player exile a card face down for {2}, then cast it
  /// for its foretell cost on a later turn. MAST records the keyword and the
  /// foretell cost; deferred-cast machinery is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Foretell = (
    from keyword in Keyword("Foretell")
    from costSymbols in Token
      .Matching<OracleToken>(
        k =>
          k == OracleToken.GenericMana
          || k == OracleToken.WhiteMana
          || k == OracleToken.BlueMana
          || k == OracleToken.BlackMana
          || k == OracleToken.RedMana
          || k == OracleToken.GreenMana
          || k == OracleToken.ColorlessMana
          || k == OracleToken.VariableMana
          || k == OracleToken.HybridMana
          || k == OracleToken.PhyrexianMana,
        "mana symbol"
      )
      .AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Foretell",
      Effect = new ForetellEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Flashback {cost}" keyword.
  /// Pattern: "Flashback" mana-symbol+ [reminder]
  /// Rule 702.34. Lets a player cast a card from their graveyard for its
  /// flashback cost; the card is then exiled. MAST records the keyword and the
  /// flashback cost; cast-from-graveyard-then-exile machinery is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flashback = (
    from keyword in Keyword("Flashback")
    from costSymbols in Token
      .Matching<OracleToken>(
        k =>
          k == OracleToken.GenericMana
          || k == OracleToken.WhiteMana
          || k == OracleToken.BlueMana
          || k == OracleToken.BlackMana
          || k == OracleToken.RedMana
          || k == OracleToken.GreenMana
          || k == OracleToken.ColorlessMana
          || k == OracleToken.VariableMana
          || k == OracleToken.HybridMana
          || k == OracleToken.PhyrexianMana,
        "mana symbol"
      )
      .AtLeastOnce()
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flashback",
      Effect = new FlashbackEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      },
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Choose a Background" — a fixed-name partner variant from
  /// Commander Legends: Battle for Baldur's Gate (Rule 702.124g, descriptive
  /// reference). Emits a static ability whose effect carries
  /// <see cref="PartnerType.ChooseABackground"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> ChooseABackground = (
    from choose in Token.EqualTo(OracleToken.Choose)
    from a in Keyword("a")
    from background in Keyword("Background")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Choose a Background",
      Effect = new PartnerEffect { PartnerType = PartnerType.ChooseABackground },
      Reminder = reminder,
    }
  );

  #endregion

  #region Composite Parsers

  /// <summary>
  /// Parses any simple keyword ability.
  /// Tries each keyword parser in sequence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> SimpleKeyword = Flying
    .Try()
    .Or(Menace)
    .Or(Vigilance)
    .Or(Trample)
    .Or(Haste)
    .Or(Lifelink)
    .Or(Reach)
    .Or(Flash)
    .Or(Storm)
    .Or(Defender)
    .Or(Cascade)
    .Or(Deathtouch)
    .Or(Hexproof)
    .Or(Shroud)
    .Or(Prowess)
    .Or(Devoid)
    .Or(Changeling)
    .Or(Banding)
    .Or(FirstStrike)
    .Or(DoubleStrike)
    .Or(Forestwalk)
    .Or(Islandwalk)
    .Or(Mountainwalk)
    .Or(Plainswalk)
    .Or(Swampwalk);

  /// <summary>
  /// Parses any parameterized keyword ability.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> ParameterizedKeyword =
    Protection
      .Try()
      .Or(Crew.Try())
      .Or(PartnerWith.Try())
      .Or(ChooseABackground.Try())
      .Or(Entwine.Try())
      .Or(Cycling.Try())
      .Or(Madness.Try())
      .Or(Foretell.Try())
      .Or(Flashback.Try());

  /// <summary>
  /// Parses any keyword ability (simple or parameterized).
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> AnyKeyword = SimpleKeyword
    .Try()
    .Or(ParameterizedKeyword);

  /// <summary>
  /// Parses a comma-separated list of keyword abilities.
  /// Example: "Flying, vigilance, trample"
  /// </summary>
  public static readonly TokenListParser<OracleToken, IReadOnlyList<StaticAbility>> KeywordList =
    AnyKeyword
      .ManyDelimitedBy(Token.EqualTo(OracleToken.Comma))
      .Select(arr => (IReadOnlyList<StaticAbility>)arr);

  #endregion
}
