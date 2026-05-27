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
using MagicAST.Keywords;
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
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Fear" keyword.
  /// Pattern: "Fear" [reminder]
  /// Rule 702.36. This creature can't be blocked except by artifact creatures
  /// and/or black creatures. MAST records keyword presence; the evasion
  /// semantics are expressed via EvasionEffect with a Characteristics-stretch
  /// ObjectFilter covering both the artifact type and the black color qualifier.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Fear = (
    from keyword in Keyword("Fear")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Fear",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["artifact", "black"],
        },
      }],
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
      Effects = [new VigilanceEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Menace" keyword.
  /// Pattern: "Menace" [reminder]
  /// Rule 702.111. Evasion ability requiring two or more blockers.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Menace = (
    from keyword in Keyword("Menace")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Menace",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
        MinimumBlockers = 2,
      }],
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
      Effects = [new TrampleEffect()],
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
      KeywordSource = keyword.ToStringValue(),
      Effects = [new HasteEffect()],
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
      Effects = [new LifelinkEffect()],
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
      Effects = [new ReachEffect()],
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
      Effects = [new TimingModificationEffect
      {
        Modification = TimingModificationType.Grant,
        Timing = TimingWindow.Instant,
      }],
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
      Effects = [new StormEffect()],
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
      Effects = [new DefenderEffect()],
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
      Effects = [new CascadeEffect()],
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
      Effects = [new DeathtouchEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Indestructible" keyword.
  /// Pattern: "Indestructible" [reminder]
  /// Rule 702.12. This permanent can't be destroyed. Effects that say "destroy"
  /// don't destroy it, and if its toughness is 0 or less it isn't destroyed.
  /// MAST records keyword presence; engine-territory semantics are omitted.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Indestructible = (
    from keyword in Keyword("Indestructible")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Indestructible",
      Effects = [new IndestructibleEffect()],
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
      Effects = [new HexproofEffect()],
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
      Effects = [new ShroudEffect()],
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
      Effects = [new ProwessEffect()],
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
      Effects = [new DevoidEffect()],
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
      Effects = [new ChangelingEffect()],
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
      Effects = [new BandingEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Convoke" keyword.
  /// Pattern: "Convoke" [reminder]
  /// Rule 702.51. Lets you tap creatures to help pay for this spell.
  /// MAST records keyword presence; the per-creature cost-reduction mechanic
  /// is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Convoke = (
    from keyword in Keyword("Convoke")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Convoke",
      Effects = [new ConvokeEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Delve" keyword.
  /// Pattern: "Delve" [reminder]
  /// Rule 702.66. Each card you exile from your graveyard while casting this spell
  /// pays for {1}. MAST records keyword presence; the per-card graveyard-exile
  /// cost-reduction mechanic is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Delve = (
    from keyword in Keyword("Delve")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Delve",
      Effects = [new DelveEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Improvise" keyword.
  /// Pattern: "Improvise" [reminder]
  /// Rule 702.126. Each artifact you tap after you're done activating mana abilities
  /// pays for {1}. MAST records keyword presence; the per-artifact cost-reduction
  /// mechanic is engine territory. Mirrors Convoke/Delve exactly.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Improvise = (
    from keyword in Keyword("Improvise")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Improvise",
      Effects = [new ImproviseEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Exalted" keyword.
  /// Pattern: "Exalted" [reminder]
  /// Rule 702.83. Whenever a creature you control attacks alone, that creature
  /// gets +1/+1 until end of turn. Although mechanically a triggered ability,
  /// MAST models it as a keyword marker (same approach as Prowess); the
  /// trigger-and-buff expansion is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Exalted = (
    from kw in Keyword("Exalted")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Exalted",
      Effects = [new ExaltedEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Infect" keyword.
  /// Pattern: "Infect" [reminder]
  /// Rule 702.91. This creature deals damage to creatures in the form of
  /// -1/-1 counters and to players in the form of poison counters.
  /// MAST records keyword presence; the damage-redirection semantics are
  /// engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Infect = (
    from kw in Keyword("Infect")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Infect",
      Effects = [new InfectEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Wither" keyword.
  /// Pattern: "Wither" [reminder]
  /// Rule 702.80. This creature deals damage to creatures in the form of
  /// -1/-1 counters. MAST records keyword presence; the damage-redirection
  /// semantics are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Wither = (
    from kw in Keyword("Wither")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Wither",
      Effects = [new WitherEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Persist" keyword.
  /// Pattern: "Persist" [reminder]
  /// Rule 702.78. When this creature dies, if it had no -1/-1 counters on it,
  /// return it to the battlefield under its owner's control with a -1/-1 counter
  /// on it. MAST records keyword presence; the dies-trigger and counter-placement
  /// semantics are engine territory. Mirrors the Infect/Wither pattern exactly.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Persist = (
    from kw in Keyword("Persist")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Persist",
      Effects = [new PersistEffect { IsOptional = false }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Undying" keyword.
  /// Pattern: "Undying" [reminder]
  /// Rule 702.93. When this creature dies, if it had no +1/+1 counters on it,
  /// return it to the battlefield under its owner's control with a +1/+1 counter
  /// on it. Mirror of Persist with opposite polarity. MAST records keyword
  /// presence; the dies-trigger, counter-check, and return semantics are engine
  /// territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Undying = (
    from kw in Keyword("Undying")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Undying",
      Effects = [new UndyingEffect { IsOptional = false }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Flanking" keyword.
  /// Pattern: "Flanking" [reminder]
  /// Rule 702.25. A triggered keyword ability: whenever a creature without flanking
  /// blocks this creature, the blocking creature gets -1/-1 until end of turn.
  /// Although mechanically triggered, MAST models it as a keyword marker (same
  /// approach as Exalted and Prowess); the trigger-and-debuff expansion is engine
  /// territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Flanking = (
    from kw in Keyword("Flanking")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Flanking",
      Effects = [new FlankingEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Enlist" keyword.
  /// Pattern: "Enlist" [reminder]
  /// Rule 702.154. A static ability and triggered ability: as this creature attacks,
  /// you may tap another untapped creature you control that could have attacked; if
  /// you do, this creature gets +X/+0 until end of turn where X is that creature's
  /// power. Although mechanically a static + triggered pair, MAST models it as a
  /// keyword marker — same approach as Flanking and Evolve; the tapping cost and
  /// power-addition are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Enlist = (
    from kw in Keyword("Enlist")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Enlist",
      Effects = [new EnlistEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Melee" keyword.
  /// Pattern: "Melee" [reminder]
  /// Rule 702.121. A triggered keyword ability: whenever this creature attacks,
  /// it gets +1/+1 until end of turn for each opponent you attacked with a creature
  /// this combat. Although mechanically a triggered ability, MAST models it as a
  /// keyword marker (same approach as Flanking, Evolve, Exalted); the attack-count
  /// comparison and +1/+1 grant are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Melee = (
    from kw in Keyword("Melee")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Melee",
      Effects = [new MeleeEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Ascend" keyword.
  /// Pattern: "Ascend" [reminder]
  /// Rule 702.131. If you control ten or more permanents, you get the city's
  /// blessing for the rest of the game. Applies to both permanents (Rule
  /// 702.131b) and spells (Rule 702.131a); the parser treats both as keyword
  /// presence. MAST records the keyword's presence; the city's-blessing
  /// designation and downstream effects are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Ascend = (
    from kw in Keyword("Ascend")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Ascend",
      Effects = [new AscendEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Evolve" keyword.
  /// Pattern: "Evolve" [reminder]
  /// Rule 702.100. Whenever a creature you control enters, if that creature has
  /// greater power or toughness than this creature, put a +1/+1 counter on this
  /// creature. Although mechanically a triggered ability, MAST models it as a
  /// keyword marker (same approach as Prowess, Exalted); the trigger /
  /// power-comparison / counter-placement are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Evolve = (
    from kw in Keyword("Evolve")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Evolve",
      Effects = [new EvolveEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Mentor" keyword.
  /// Pattern: "Mentor" [reminder]
  /// Rule 702.134. Whenever this creature attacks, put a +1/+1 counter on target
  /// attacking creature with power less than this creature's power. Although
  /// mechanically a triggered ability, MAST models it as a keyword marker
  /// (same approach as Evolve, Flanking, Exalted); the trigger / target-selection /
  /// counter-placement are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Mentor = (
    from kw in Keyword("Mentor")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Mentor",
      Effects = [new MentorEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Shadow" keyword.
  /// Pattern: "Shadow" [reminder]
  /// Rule 702.28. This creature can't be blocked except by creatures with shadow,
  /// and a creature without shadow can't be blocked by creatures with shadow.
  /// Mutual evasion: only shadow can block shadow. EvasionEffect with CanBeBlockedBy
  /// restricted to the "shadow" characteristic (same convention as Flying's
  /// "flying"/"reach" filter).
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Shadow = (
    from kw in Keyword("Shadow")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Shadow",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["shadow"],
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Intimidate" keyword.
  /// Pattern: "Intimidate" [reminder]
  /// Rule 702.13. This creature can't be blocked except by artifact creatures
  /// and/or creatures that share a color with it. EvasionEffect with CanBeBlockedBy
  /// covering the artifact-type and "shares a color" predicates; mirrors Fear
  /// (702.36) but substitutes the color-share predicate for the fixed black-color
  /// predicate.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Intimidate = (
    from kw in Keyword("Intimidate")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Intimidate",
      Effects = [new EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["artifact", "shares a color"],
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Myriad" keyword.
  /// Pattern: "Myriad" [reminder]
  /// Rule 702.116. Whenever this creature attacks, for each opponent other than
  /// defending player, you may create a token copy tapped and attacking that player
  /// or a planeswalker they control; exile the tokens at end of combat. Although
  /// mechanically a triggered ability (and may create a delayed triggered ability),
  /// MAST models it as a keyword marker (same approach as Evolve, Flanking, Mentor);
  /// the per-opponent copy-creation, tapped-and-attacking, and delayed-exile semantics
  /// are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Myriad = (
    from kw in Keyword("Myriad")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Myriad",
      Effects = [new MyriadEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Daybound" keyword.
  /// Pattern: "Daybound" [reminder]
  /// Rule 702.145b. Found on the front faces of day/night double-faced cards.
  /// MAST records the keyword's presence and phase; the day/night transformation
  /// rules (Rule 731) are engine territory per the descriptive-not-engine doctrine.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Daybound = (
    from kw in Keyword("Daybound")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Daybound",
      Effects = [new DayNightEffect { Phase = DayNightPhase.Daybound }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Nightbound" keyword.
  /// Pattern: "Nightbound" [reminder]
  /// Rule 702.145e. Found on the back faces of day/night double-faced cards.
  /// MAST records the keyword's presence and phase; the day/night transformation
  /// rules (Rule 731) are engine territory per the descriptive-not-engine doctrine.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Nightbound = (
    from kw in Keyword("Nightbound")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Nightbound",
      Effects = [new DayNightEffect { Phase = DayNightPhase.Nightbound }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Melee" keyword.
  #endregion

  /// <summary>
  /// Parser for the "Split second" keyword.
  /// Pattern: "Split" "second" [reminder]
  /// Rule 702.61. As long as this spell is on the stack, players can't cast spells or
  /// activate abilities that aren't mana abilities. MAST records the keyword's presence;
  /// the stack-restriction semantics are engine territory. Multi-word keyword via
  /// sequential Keyword() combinators, mirroring FirstStrike and CumulativeUpkeep.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> SplitSecond = (
    from split in Keyword("Split")
    from second in Keyword("second")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Split second",
      Effects = [new SplitSecondEffect()],
      Reminder = reminder,
    }
  );

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
      Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
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
      Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
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
        Effects = [new EvasionEffect
        {
          UnblockableCondition = new EvasionCondition
          {
            ConditionType = EvasionConditionType.DefendingPlayerControls,
            PermanentFilter = new ObjectFilter { Subtypes = [landType] },
          },
        }],
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
      Effects = [new ProtectionEffect { From = new[] { first }.Concat(rest).ToArray() }],
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
      Effects = [new CrewEffect
      {
        Power = new LiteralQuantity { Value = int.Parse(n.ToStringValue()) },
      }],
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
      Effects = [new PartnerEffect
      {
        PartnerType = PartnerType.PartnerWith,
        PartnerName = string.Join(" ", nameWords.Select(t => t.ToStringValue())),
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the bare "Partner" keyword (parameterless).
  /// Pattern: "Partner" [reminder]
  /// Rule 702.124. Allows any two Partner commanders to be paired together.
  /// Must be placed AFTER PartnerWith in the Or-chain: "Partner with [Name]"
  /// leads with the same "Partner" token; PartnerWith.Try() backtracks if
  /// "with" is absent, leaving this parser to match the bare form.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Partner = (
    from kw in Keyword("Partner")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Partner",
      Effects = [new PartnerEffect
      {
        PartnerType = PartnerType.Partner,
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Affinity for [text]" keyword.
  /// Pattern: "Affinity" "for" Word+ [reminder]
  /// Rule 702.41. The parameter text between "for" and the optional reminder
  /// (or end-of-input) is captured as the literal type/subtype label; the
  /// <see cref="KeywordDefinitions.Affinity"/> expansion maps it to an
  /// <see cref="ObjectFilter"/> on the cost-reduction's per-object axis.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Affinity = (
    from keyword in Keyword("Affinity")
    from forKw in Keyword("for")
    from typeWords in Token.EqualTo(OracleToken.Word).AtLeastOnce()
    from reminder in _optionalReminder
    let parameter = string.Join(" ", typeWords.Select(t => t.ToStringValue()))
    select (StaticAbility)KeywordDefinitions.Affinity.CreateExpansion(parameter) with
    {
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Entwine {cost}" — Rule 702.42. The cost is parsed by the
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
      Effects = [new EntwineEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
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
      Effects = [new CyclingEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "[Type]cycling {cost}" keyword variants (Swampcycling, Mountaincycling,
  /// Plainscycling, Islandcycling, Forestcycling).
  /// Pattern: "&lt;word-ending-in-cycling&gt;" mana-symbol+ [reminder]
  /// Rule 702.32f. Each typecycling ability is an activated ability:
  /// "[Cost], Discard this card: Search your library for a [Type] card, reveal it, put it
  /// into your hand, then shuffle." The land type is extracted from the keyword prefix
  /// (e.g., "Forest" from "Forestcycling"). MAST records the keyword, the land type,
  /// and the cost; the inner search/reveal/shuffle structure is inferred from the rules.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Typecycling = (
    from kwToken in Token
      .EqualTo(OracleToken.Word)
      .Try()
      .Where(t =>
      {
        var s = t.ToStringValue();
        return s.EndsWith("cycling", StringComparison.OrdinalIgnoreCase)
          && !s.Equals("cycling", StringComparison.OrdinalIgnoreCase);
      })
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
    let kwText = kwToken.ToStringValue()
    let landType = kwText[..^"cycling".Length]
    select new StaticAbility
    {
      KeywordSource = kwText,
      Effects = [new TypecyclingEffect
      {
        Type = char.ToUpperInvariant(landType[0]) + landType[1..],
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
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
      Effects = [new MadnessEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
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
      Effects = [new ForetellEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
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
      Effects = [new FlashbackEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
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
      Effects = [new PartnerEffect { PartnerType = PartnerType.ChooseABackground }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Morph {cost}" keyword.
  /// Pattern: "Morph" mana-symbol+ [reminder]
  /// Rule 702.37. A static ability: the player may cast this card face down as
  /// a 2/2 colorless creature for {3}, and may turn it face up later by paying
  /// its morph cost. MAST records the keyword and the morph cost; the
  /// cast-face-down rules and turn-face-up mechanics are conventionally
  /// inferred from the rules (per the descriptive-not-engine doctrine).
  /// The cost uses the polymorphic <see cref="MagicAST.AST.Costs.Cost"/> base,
  /// mirroring <see cref="CyclingEffect"/> and <see cref="EquipEffect"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Morph = (
    from keyword in Keyword("Morph")
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
      KeywordSource = "Morph",
      Effects = [new MorphEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Megamorph {cost}" keyword.
  /// Pattern: "Megamorph" mana-symbol+ [reminder]
  /// Rule 702.37b. A variant of Morph: the player may cast this card face down as a
  /// 2/2 colorless creature for {3}, and may turn it face up by paying its megamorph
  /// cost; when turned face up via the megamorph cost, a +1/+1 counter is placed on
  /// the permanent. MAST records the keyword and the megamorph cost; the
  /// cast-face-down rules, turn-face-up mechanics, and counter-placement are
  /// conventionally inferred from the rules (per the descriptive-not-engine doctrine).
  /// Direct structural mirror of <see cref="Morph"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Megamorph = (
    from keyword in Keyword("Megamorph")
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
      KeywordSource = "Megamorph",
      Effects = [new MegamorphEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Bushido N" keyword.
  /// Pattern: "Bushido" number [reminder]
  /// Rule 702.45. A triggered keyword ability: whenever this creature blocks or becomes
  /// blocked, it gets +N/+N until end of turn. MAST records the keyword and its integer
  /// value; the trigger-and-buff expansion is engine territory.
  ///
  /// This is the first integer-parameterized keyword in the AST. It uses
  /// <see cref="OracleToken.Number"/> directly (same token kind used by <see cref="Crew"/>),
  /// not a mana-symbol sequence. Future integer-parameterized keywords (Annihilator,
  /// Modular, Soulshift, etc.) should follow this same combinator shape.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Bushido = (
    from keyword in Keyword("Bushido")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Bushido",
      Effects = [new BushidoEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Soulshift N" keyword.
  /// Pattern: "Soulshift" number [reminder]
  /// Rule 702.46. A triggered keyword ability: when this creature dies, you may
  /// return target Spirit card with mana value N or less from your graveyard to
  /// your hand. MAST records the keyword and its integer value; the trigger-and-
  /// return expansion is engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Soulshift = (
    from keyword in Keyword("Soulshift")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Soulshift",
      Effects = [new SoulshiftEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Saddle N" keyword.
  /// Pattern: "Saddle" number [reminder]
  /// Rule 702.171. An activated keyword ability: tap any number of other untapped
  /// creatures you control with total power N or greater — this Mount becomes
  /// saddled until end of turn. Activate only as a sorcery. MAST records the
  /// keyword and its integer threshold value; the activation semantics and saddled
  /// designation are engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/> and
  /// <see cref="Soulshift"/>. Structurally mirrors Crew (702.122) but for Mounts.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Saddle = (
    from keyword in Keyword("Saddle")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Saddle",
      Effects = [new SaddleEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Toxic N" keyword.
  /// Pattern: "Toxic" number [reminder]
  /// Rule 702.164. A static ability: whenever this creature deals combat damage to
  /// a player, that player gets N poison counters in addition to the damage.
  /// MAST records the keyword and its integer value; the poison-counter placement
  /// is engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Toxic = (
    from keyword in Keyword("Toxic")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Toxic",
      Effects = [new ToxicEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Modular N" keyword.
  /// Pattern: "Modular" number [reminder]
  /// Rule 702.43. A static ability and triggered ability: this permanent enters
  /// with N +1/+1 counters on it; when put into a graveyard from the battlefield
  /// you may move one counter per counter on it to target artifact creature.
  /// MAST records the keyword and its integer value; the counter placement and
  /// death trigger are engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/> and
  /// <see cref="Soulshift"/>. Explicitly anticipated in BushidoEffect.cs remarks.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Modular = (
    from keyword in Keyword("Modular")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Modular",
      Effects = [new ModularEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Backup N" keyword.
  /// Pattern: "Backup" number [reminder]
  /// Rule 702.165. A triggered keyword ability: when this creature enters, put N
  /// +1/+1 counters on target creature. If that is another creature, it also gains
  /// the non-backup abilities of this creature printed below this one until end of
  /// turn. MAST records the keyword and its integer value; the ability-grant and
  /// "printed below this one" scoping are engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/> and
  /// <see cref="Soulshift"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Backup = (
    from keyword in Keyword("Backup")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Backup",
      Effects = [new BackupEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Renown N" keyword.
  /// Pattern: "Renown" number [reminder]
  /// Rule 702.112. A triggered keyword ability: when this creature deals combat
  /// damage to a player, if it isn't renowned, put N +1/+1 counters on it and
  /// it becomes renowned. MAST records the keyword and its integer value; the
  /// trigger, renowned designation, and counter-placement are engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Renown = (
    from keyword in Keyword("Renown")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Renown",
      Effects = [new RenownEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Bloodthirst N" keyword.
  /// Pattern: "Bloodthirst" number [reminder]
  /// Rule 702.54. A static ability: if an opponent was dealt damage this turn,
  /// this creature enters with N +1/+1 counters on it. MAST records the keyword
  /// and its integer value; the condition check and counter-placement are engine
  /// territory. Integer-parameterized keyword; mirrors <see cref="Bushido"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Bloodthirst = (
    from keyword in Keyword("Bloodthirst")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Bloodthirst",
      Effects = [new BloodthirstEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Fabricate N" keyword.
  /// Pattern: "Fabricate" number [reminder]
  /// Rule 702.118. A triggered keyword ability: when this creature enters, put N
  /// +1/+1 counters on it or create N 1/1 colorless Servo artifact creature tokens.
  /// MAST records the keyword and its integer value; the enters trigger, choice
  /// between counters and tokens, and token creation are engine territory.
  /// Integer-parameterized keyword; mirrors <see cref="Bushido"/> and
  /// <see cref="Backup"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Fabricate = (
    from keyword in Keyword("Fabricate")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Fabricate",
      Effects = [new FabricateEffect { Value = int.Parse(value.ToStringValue()) }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Hideaway N" keyword.
  /// Pattern: "Hideaway" number [reminder]
  /// Rule 702.74. A triggered keyword ability: when this permanent enters, look
  /// at the top N cards of your library, exile one face down, then put the rest
  /// on the bottom in a random order. MAST records the keyword and its integer
  /// lookahead count; the ETB trigger, look/exile/shuffle semantics are engine
  /// territory.
  /// Integer-parameterized keyword using <see cref="LiteralQuantity"/>; mirrors
  /// the <see cref="Crew"/> combinator shape.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Hideaway = (
    from keyword in Keyword("Hideaway")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Hideaway",
      Effects = [new HideawayEffect
      {
        Amount = new LiteralQuantity { Value = int.Parse(value.ToStringValue()) },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Mobilize N" keyword.
  /// Pattern: "Mobilize" number [reminder]
  /// Rule 702.175 (Tarkir: Dragonstorm). A triggered keyword ability: whenever
  /// this creature attacks, create N tapped and attacking 1/1 red Warrior
  /// creature tokens. Sacrifice them at the beginning of the next end step.
  /// MAST records the keyword and its integer token-creation count; the attack
  /// trigger, token-creation, and delayed-sacrifice semantics are engine territory.
  /// Integer-parameterized keyword using <see cref="LiteralQuantity"/>; mirrors
  /// <see cref="Hideaway"/> and <see cref="Crew"/>.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Mobilize = (
    from keyword in Keyword("Mobilize")
    from value in Token.EqualTo(OracleToken.Number)
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Mobilize",
      Effects = [new MobilizeEffect
      {
        Amount = new LiteralQuantity { Value = int.Parse(value.ToStringValue()) },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Equip {cost}" keyword.
  /// Pattern: "Equip" mana-symbol+ [reminder]
  /// Rule 702.6. An activated ability that attaches this Equipment to a creature
  /// you control. MAST records the keyword and its activation cost; the attach
  /// mechanics and sorcery-speed restriction are derived from the rules
  /// (per the descriptive-not-engine doctrine).
  /// The cost uses the polymorphic <see cref="MagicAST.AST.Costs.Cost"/> base
  /// to accommodate future non-mana equip costs (e.g. "Equip — Sacrifice a creature").
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Equip = (
    from keyword in Keyword("Equip")
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
      KeywordSource = "Equip",
      Effects = [new EquipEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Reconfigure {cost}" keyword.
  /// Pattern: "Reconfigure" mana-symbol+ [reminder]
  /// Rule 702.173. An activated ability that lets a player attach this Equipment
  /// creature to a creature they control, or unattach it, at sorcery speed.
  /// MAST records the keyword and its reconfigure cost; the attach/unattach
  /// mechanics, sorcery-speed restriction, and creature-status switching are
  /// conventionally inferred from the rules (per the descriptive-not-engine
  /// doctrine), mirroring the EquipEffect pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Reconfigure = (
    from keyword in Keyword("Reconfigure")
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
      KeywordSource = "Reconfigure",
      Effects = [new MagicAST.AST.Effects.Keyword.ReconfigureEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Echo {cost}" keyword.
  /// Pattern: "Echo" mana-symbol+ [reminder]
  /// Rule 702.30. "At the beginning of your upkeep, if this permanent came under
  /// your control since the beginning of your last upkeep, sacrifice it unless
  /// you pay [cost]." MAST records the keyword and the echo cost; the
  /// upkeep-trigger / sacrifice-unless-pay semantics are engine territory.
  /// Mirrors the Bestow/Equip/Cycling pattern exactly.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Echo = (
    from keyword in Keyword("Echo")
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
      KeywordSource = "Echo",
      Effects = [new EchoEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Bestow {cost}" keyword.
  /// Pattern: "Bestow" mana-symbol+ [reminder]
  /// Rule 702.103. If you cast this card for its bestow cost, it's an Aura spell
  /// with enchant creature. It becomes a creature again if it's not attached.
  /// MAST records the keyword and the bestow cost; the alternative-cast /
  /// Aura-mode / unattach semantics are engine territory. Mirrors the
  /// Cycling/Equip pattern exactly.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Bestow = (
    from keyword in Keyword("Bestow")
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
      KeywordSource = "Bestow",
      Effects = [new BestowEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Kicker {cost}" keyword.
  /// Pattern: "Kicker" mana-symbol+ [reminder]
  /// Rule 702.33. "You may pay an additional [cost] as you cast this spell."
  /// MAST records the keyword and the kicker cost; the conditional resolution
  /// of kicked effects is engine territory. Mirrors the Bestow/Echo/Equip pattern.
  /// Scope: single-cost kicker only (Rule 702.33a). Multi-cost ("and/or") and
  /// Multikicker are deferred to a future batch.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Kicker = (
    from keyword in Keyword("Kicker")
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
      KeywordSource = "Kicker",
      Effects = [new KickerEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Unearth {cost}" keyword.
  /// Pattern: "Unearth" mana-symbol+ [reminder]
  /// Rule 702.84. "[Cost]: Return this card from your graveyard to the battlefield.
  /// It gains haste. Exile it at the beginning of the next end step or if it would
  /// leave the battlefield. Unearth only as a sorcery." MAST records the keyword
  /// and its activation cost; the return, haste-grant, and exile-at-end-step
  /// semantics are engine territory. Mirrors the Kicker/Bestow/Echo/Equip pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Unearth = (
    from keyword in Keyword("Unearth")
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
      KeywordSource = "Unearth",
      Effects = [new UnearthEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Plot {cost}" keyword.
  /// Pattern: "Plot" mana-symbol+ [reminder]
  /// Rule 702.170. "You may pay [cost] and exile this card from your hand. Cast it
  /// as a sorcery on a later turn without paying its mana cost. Plot only as a
  /// sorcery." MAST records the keyword and the plot cost; the exile-from-hand,
  /// deferred-cast, and sorcery-speed restrictions are engine territory. Mirrors
  /// the Kicker/Unearth/Bestow/Echo/Equip pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Plot = (
    from keyword in Keyword("Plot")
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
      KeywordSource = "Plot",
      Effects = [new PlotEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Cumulative upkeep {cost}" keyword.
  /// Pattern: "Cumulative" "upkeep" mana-symbol+ [reminder]
  /// Rule 702.24. "At the beginning of your upkeep, put an age counter on this
  /// permanent, then sacrifice it unless you pay its upkeep cost for each age
  /// counter on it." MAST records the keyword and the cumulative upkeep cost;
  /// the upkeep-trigger, age-counter-scaling, and sacrifice-unless-pay semantics
  /// are engine territory. Mirrors the Echo/Kicker/Bestow/Unearth/Plot pattern,
  /// extended for a multi-word keyword via sequential Keyword() combinators.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> CumulativeUpkeep = (
    from cumulative in Keyword("Cumulative")
    from upkeep in Keyword("upkeep")
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
      KeywordSource = "Cumulative upkeep",
      Effects = [new CumulativeUpkeepEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Dash {cost}" keyword.
  /// Pattern: "Dash" mana-symbol+ [reminder]
  /// Rule 702.109. "You may cast this card for its dash cost. If you do, it gains
  /// haste, and it's returned from the battlefield to its owner's hand at the
  /// beginning of the next end step." MAST records the keyword and its cost; the
  /// haste-grant and return-to-hand semantics are engine territory. Mirrors the
  /// Kicker/Bestow/Echo/Equip pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Dash = (
    from keyword in Keyword("Dash")
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
      KeywordSource = "Dash",
      Effects = [new DashEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for "Ninjutsu {cost}" keyword.
  /// Pattern: "Ninjutsu" mana-symbol+ [reminder]
  /// Rule 702.49. "[Cost], Return an unblocked attacker you control to hand: Put
  /// this card onto the battlefield from your hand tapped and attacking." MAST
  /// records the keyword and the ninjutsu cost; the return-attacker and
  /// enter-tapped-and-attacking semantics are engine territory. Mirrors the
  /// Bestow/Kicker/Echo/Equip pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Ninjutsu = (
    from keyword in Keyword("Ninjutsu")
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
      KeywordSource = "Ninjutsu",
      Effects = [new NinjutsuEffect
      {
        Cost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Extort" keyword.
  /// Pattern: "Extort" [reminder]
  /// Rule 702.101. "Whenever you cast a spell, you may pay {W/B}. If you do,
  /// each opponent loses 1 life and you gain that much life." MAST records the
  /// keyword's presence; the spell-cast trigger and life-drain/gain are engine
  /// territory. Mirrors AscendEffect, ConvokeEffect, EvolveEffect, and
  /// PersistEffect: parameterless keyword marker.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Extort = (
    from kw in Keyword("Extort")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Extort",
      Effects = [new ExtortEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Rebound" keyword.
  /// Pattern: "Rebound" [reminder]
  /// Rule 702.88. If you cast this spell from your hand, exile it as it
  /// resolves; at the beginning of your next upkeep, you may cast this card
  /// from exile without paying its mana cost. MAST records keyword presence;
  /// the exile-on-resolution and free-cast-from-exile machinery are engine
  /// territory. Parameterless keyword marker — mirrors Extort, Ascend.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Rebound = (
    from kw in Keyword("Rebound")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Rebound",
      Effects = [new ReboundEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Skulk" keyword.
  /// Pattern: "Skulk" [reminder]
  /// Rule 702.116b. This creature can't be blocked by creatures with greater power.
  /// MAST records the keyword's presence; the power-comparison blocking restriction
  /// is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Skulk = (
    from kw in Keyword("Skulk")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Skulk",
      Effects = [new SkulkEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Sunburst" keyword.
  /// Pattern: "Sunburst" [reminder]
  /// Rule 702.44. This permanent enters with a +1/+1 counter on it for each
  /// color of mana spent to cast it. MAST records keyword presence; the
  /// color-counting and counter-placement are engine territory.
  /// Parameterless keyword marker; mirrors EvolveEffect and AscendEffect.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Sunburst = (
    from kw in Keyword("Sunburst")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Sunburst",
      Effects = [new SunburstEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Horsemanship" keyword.
  /// Pattern: "Horsemanship" [reminder]
  /// Rule 702.32. This creature can't be blocked except by creatures with
  /// horsemanship. MAST records the keyword's presence; the mutual-horsemanship
  /// blocking restriction is engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Horsemanship = (
    from kw in Keyword("Horsemanship")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Horsemanship",
      Effects = [new HorsemanshipEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Unleash" keyword.
  /// Pattern: "Unleash" [reminder]
  /// Rule 702.97. You may have this creature enter with a +1/+1 counter on it;
  /// it can't block as long as it has a +1/+1 counter on it. MAST records
  /// keyword presence; the optional counter-on-entry and can't-block
  /// restriction are engine territory. Parameterless keyword marker.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Unleash = (
    from kw in Keyword("Unleash")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Unleash",
      Effects = [new UnleashEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Battle cry" keyword.
  /// Pattern: "Battle" "cry" [reminder]
  /// Rule 702.91. Whenever this creature attacks, each other attacking creature gets
  /// +1/+0 until end of turn. Although mechanically a triggered ability, MAST models
  /// it as a keyword marker (same approach as Flanking, Evolve, Exalted); the attack
  /// trigger and pump expansion are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> BattleCry = (
    from battle in Keyword("Battle")
    from cry in Keyword("cry")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Battle cry",
      Effects = [new BattleCryEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Learn" keyword.
  /// Pattern: "Learn" ["." or end] [reminder]
  /// Rule 702.148. A keyword action: you may reveal a Lesson card you own from
  /// outside the game and put it into your hand, or discard a card to draw a
  /// card. MAST records keyword presence; the choice semantics are engine
  /// territory. Parameterless keyword marker — mirrors Extort, Ascend.
  /// Note: oracle text uses "Learn." (with a trailing period absorbed by the
  /// sentence tokenizer) followed by the reminder in parentheses.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Learn = (
    from kw in Keyword("Learn")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Learn",
      Effects = [new LearnEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Retrace" keyword.
  /// Pattern: "Retrace" [reminder]
  /// Rule 702.75. You may cast this card from your graveyard by discarding a
  /// land card in addition to paying its other costs. MAST records keyword
  /// presence; the graveyard-cast and discard-land additional-cost mechanics
  /// are engine territory. Parameterless keyword marker — mirrors Extort, Ascend.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Retrace = (
    from kw in Keyword("Retrace")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Retrace",
      Effects = [new RetraceEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Ingest" keyword.
  /// Pattern: "Ingest" [reminder]
  /// Rule 702.115. Whenever this creature deals combat damage to a player,
  /// that player exiles the top card of their library. MAST records keyword
  /// presence; the combat-damage trigger and exile-top-of-library action are
  /// engine territory. Parameterless keyword marker.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Ingest = (
    from kw in Keyword("Ingest")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Ingest",
      Effects = [new IngestEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Soulbond" keyword.
  /// Pattern: "Soulbond" [reminder]
  /// Rule 702.95. You may pair this creature with another unpaired creature when
  /// either enters. They remain paired for as long as you control both of them.
  /// MAST records the keyword's presence; the pairing mechanics and any granted
  /// abilities are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Soulbond = (
    from kw in Keyword("Soulbond")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Soulbond",
      Effects = [new SoulbondEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Living weapon" keyword.
  /// Pattern: "Living" "weapon" [reminder]
  /// Rule 702.77. When this Equipment enters, create a 0/0 black Phyrexian Germ
  /// creature token, then attach this to it. Although mechanically a triggered
  /// ability, MAST records keyword presence; the ETB trigger, token-creation,
  /// and auto-attach semantics are engine territory. Multi-word keyword via
  /// sequential Keyword() combinators, mirroring FirstStrike and CumulativeUpkeep.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> LivingWeapon = (
    from living in Keyword("Living")
    from weapon in Keyword("weapon")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Living weapon",
      Effects = [new LivingWeaponEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Umbra armor" keyword (Totem armor in comp-rules).
  /// Pattern: "Umbra" "armor" [reminder]
  /// Rule 702.102. "If enchanted creature would be destroyed, instead remove all
  /// damage from it and destroy this Aura." Oracle text uses "Umbra armor"; the
  /// comp-rules name is "totem armor". MAST records keyword presence using the
  /// comp-rules discriminator "totemArmor"; the replacement-effect semantics are
  /// engine territory. Multi-word keyword; mirrors LivingWeapon.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> UmbraArmor = (
    from umbra in Keyword("Umbra")
    from armor in Keyword("armor")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Totem armor",
      Effects = [new TotemArmorEffect()],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parser for the "Start your engines!" keyword.
  /// Pattern: "Start" "your" "engines" [reminder]
  /// (Aetherdrift). "If you have no speed, it starts at 1. It increases once on
  /// each of your turns when an opponent loses life. Max speed is 4." The '!'
  /// is silently dropped by the tokenizer (unknown-character handling), so the
  /// combinator matches "Start your engines". MAST records keyword presence;
  /// the speed-counter semantics are engine territory.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> StartYourEngines = (
    from start in Keyword("Start")
    from your in Token.EqualTo(OracleToken.Your)
    from engines in Keyword("engines")
    from reminder in _optionalReminder
    select new StaticAbility
    {
      KeywordSource = "Start your engines",
      Effects = [new StartYourEnginesEffect()],
      Reminder = reminder,
    }
  );

  #endregion

  #region Composite Parsers

  /// <summary>
  /// Parser for "Buyback {cost}" keyword.
  /// Pattern: "Buyback" mana-symbol+ [reminder]
  /// Rule 702.26. "You may pay an additional [cost] as you cast this spell.
  /// If you do, put this card into your hand as it resolves." MAST records the
  /// keyword and the buyback cost; the conditional-hand-return resolution is
  /// engine territory. Mirrors the Kicker/Flashback/Unearth mana-cost pattern.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> Buyback = (
    from keyword in Keyword("Buyback")
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
      KeywordSource = "Buyback",
      Effects = [new BuybackEffect
      {
        BuybackCost = new MagicAST.AST.Costs.ManaCost
        {
          Symbols = costSymbols
            .Select(t => new MagicAST.Parsing.ManaCostParser().Parse(t.ToStringValue()).Symbols[0])
            .ToList(),
        },
      }],
      Reminder = reminder,
    }
  );

  /// <summary>
  /// Parses any simple keyword ability.
  /// Tries each keyword parser in sequence.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> SimpleKeyword = Flying
    .Try()
    .Or(Fear)
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
    .Or(Indestructible)
    .Or(Hexproof)
    .Or(Shroud)
    .Or(Prowess)
    .Or(Devoid)
    .Or(Changeling)
    .Or(Banding)
    .Or(Convoke)
    .Or(Delve)
    .Or(Improvise)
    .Or(Exalted)
    .Or(Infect)
    .Or(Wither)
    .Or(Persist)
    .Or(Undying)
    .Or(Flanking)
    .Or(Enlist)
    .Or(Melee)
    .Or(Ascend)
    .Or(Evolve)
    .Or(Mentor)
    .Or(Shadow)
    .Or(Intimidate)
    .Or(Myriad)
    .Or(Daybound)
    .Or(Nightbound)
    .Or(FirstStrike)
    .Or(DoubleStrike)
    .Or(Forestwalk)
    .Or(Islandwalk)
    .Or(Mountainwalk)
    .Or(Plainswalk)
    .Or(Swampwalk)
    .Or(Extort)
    .Or(Rebound)
    .Or(Unleash)
    .Or(Learn)
    .Or(Retrace)
    .Or(Sunburst)
    .Or(Ingest)
    .Or(Skulk)
    .Or(Horsemanship)
    .Or(SplitSecond)
    .Or(BattleCry)
    .Or(Soulbond)
    .Or(LivingWeapon)
    .Or(UmbraArmor)
    .Or(StartYourEngines);

  /// <summary>
  /// Parses any parameterized keyword ability.
  /// </summary>
  public static readonly TokenListParser<OracleToken, StaticAbility> ParameterizedKeyword =
    Protection
      .Try()
      .Or(Crew.Try())
      .Or(Bushido.Try())
      .Or(Soulshift.Try())
      .Or(Saddle.Try())
      .Or(Toxic.Try())
      .Or(Modular.Try())
      .Or(Backup.Try())
      .Or(Renown.Try())
      .Or(Bloodthirst.Try())
      .Or(Fabricate.Try())
      .Or(PartnerWith.Try())
      .Or(Partner.Try())
      .Or(ChooseABackground.Try())
      .Or(Affinity.Try())
      .Or(Entwine.Try())
      .Or(Typecycling.Try())
      .Or(Cycling.Try())
      .Or(Madness.Try())
      .Or(Foretell.Try())
      .Or(Flashback.Try())
      .Or(Equip.Try())
      .Or(Reconfigure.Try())
      .Or(Morph.Try())
      .Or(Megamorph.Try())
      .Or(Bestow.Try())
      .Or(Echo.Try())
      .Or(Kicker.Try())
      .Or(Unearth.Try())
      .Or(Plot.Try())
      .Or(CumulativeUpkeep.Try())
      .Or(Dash.Try())
      .Or(Ninjutsu.Try())
      .Or(Buyback.Try())
      .Or(Hideaway.Try())
      .Or(Mobilize.Try());

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
