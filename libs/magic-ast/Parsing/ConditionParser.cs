namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Parses a condition phrase into a structured <see cref="Condition"/> (ADR 0007).
/// Recognises the dominant count shape ("you control a/N+ [filter]", "there are
/// N+ [filter] in [zone]") as a <see cref="CountCondition"/>; anything else falls
/// back to the <see cref="OtherCondition"/> residual, preserving the verbatim
/// phrase. The single entry point every producer site calls in place of building
/// a condition by hand — grown worst-first as new shapes earn a structured arm.
/// </summary>
public static class ConditionParser
{
  private static readonly IReadOnlySet<string> CardTypeNouns = new HashSet<string>(
    StringComparer.OrdinalIgnoreCase)
  {
    "card", "creature", "artifact", "enchantment", "land", "planeswalker",
    "instant", "sorcery", "permanent", "spell", "token",
  };

  private static readonly Dictionary<string, int> NumberWords = new(StringComparer.OrdinalIgnoreCase)
  {
    ["a"] = 1, ["an"] = 1, ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4,
    ["five"] = 5, ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
  };

  /// <summary>"you control a Wizard", "you control two or more other lands".</summary>
  private static readonly Regex Control = new(
    @"^you\s+control\s+(?<quant>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer))?\s+(?<noun>.+?)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>"there are seven or more cards in your graveyard".</summary>
  private static readonly Regex ThereAre = new(
    @"^there\s+(?:are|is)\s+(?<quant>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer))?\s+(?<noun>.+?)\s+in\s+(?<zone>your\s+graveyard|your\s+hand|your\s+library|a\s+graveyard|exile)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it was kicked", "this spell/creature/permanent was kicked" — the kicked-state
  /// predicate (CR 702.33d). The consumer half of the keyword cost-paid duality (ADR 0004):
  /// structured to <see cref="KeywordCostPaidCondition"/> keyed on
  /// <see cref="KeywordAbility.Kicker"/> (a multikicker cost is a kicker cost, CR 702.33c),
  /// not left as a free-text residual. Evoke/Dash/Blitz reuse the same node keyed on their
  /// own keyword.
  /// </summary>
  private static readonly Regex WasKicked = new(
    @"^(?:it|this\s+(?:spell|creature|permanent|card))\s+was\s+kicked$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "that player has two or fewer cards in hand" / "you have N or more cards in
  /// hand" — a hand-size predicate (Prickle Faeries' upkeep intervening-if). The
  /// possessive subject maps to the owner of the counted cards (hand membership is
  /// by ownership, CR 108.3): "that player" → <see cref="ControllerFilter.ThatPlayer"/>
  /// (the player whose step fired the trigger, CR 109.5), "you/your" → You. Structured
  /// to a <see cref="CountCondition"/> over the Hand zone rather than left as a
  /// free-text residual.
  /// </summary>
  private static readonly Regex HandSize = new(
    @"^(?<who>that\s+player|you|your)\s+(?:has|have)\s+(?<quant>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer))?\s+cards?\s+in\s+(?:hand|their\s+hand|your\s+hand)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "you have exactly 1 life" / "that player has 10 or less life" — a life-total
  /// threshold predicate (Near-Death Experience's upkeep intervening-if: "At the
  /// beginning of your upkeep, if you have exactly 1 life, you win the game.").
  /// Structured to a <see cref="QuantityComparisonCondition"/> whose left operand
  /// is a <see cref="DerivedQuantity"/> keyed on <see cref="DerivedKind.LifeTotal"/>
  /// (the <c>Source</c> pronoun carries whose life total — "you"/"that player" —
  /// mirroring the "it" pronoun convention used elsewhere for derived quantities)
  /// rather than left as a free-text <see cref="OtherCondition"/> residual.
  /// CR 119.1: "Each player begins the game with a starting life total of 20."
  /// Anchored (^…$).
  /// </summary>
  private static readonly Regex LifeTotal = new(
    @"^(?<who>you|that\s+player)\s+(?:have|has)\s+(?:exactly\s+)?(?<quant>\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer|less))?\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "your life total is less than or equal to half your starting life total" — the
  /// God-template life-threshold predicate (Bane, Lord of Darkness: "As long as your life
  /// total is less than or equal to half your starting life total, Bane has
  /// indestructible."). CR 119.1: "Each player begins the game with a starting life total
  /// of 20." (format-dependent — CR 903.7 sets 40 for Commander); "starting life total" is
  /// the FIXED value set at the beginning of the game, distinct from the player's CURRENT
  /// life total (<see cref="DerivedKind.LifeTotal"/>) that changes as the game progresses.
  /// Structured to a <see cref="QuantityComparisonCondition"/> whose <c>Right</c> operand
  /// is a <see cref="CalculatedQuantity"/> halving a <see cref="DerivedQuantity"/> keyed on
  /// the new <see cref="DerivedKind.StartingLifeTotal"/> — the sibling shape of the plain
  /// <see cref="LifeTotal"/> predicate above, generalised to a comparison operator phrase
  /// ("is less than or equal to") and a derived (not literal) right-hand side. Anchored
  /// (^…$).
  /// </summary>
  private static readonly Regex LifeTotalVsHalfStarting = new(
    @"^(?<who>your|that\s+player's)\s+life\s+total\s+is\s+(?<op>less\s+than\s+or\s+equal\s+to|greater\s+than\s+or\s+equal\s+to|less\s+than|greater\s+than|equal\s+to)\s+half\s+(?:your|their)\s+starting\s+life\s+total$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it had a +1/+1 counter on it" / "it had no +1/+1 counters on it" — the dying/triggering object's
  /// counter-gate (Basri's Lieutenant, Persist "had no -1/-1", Undying "had no +1/+1"). Structured to
  /// <see cref="TriggeringObjectCounterCondition"/> rather than left as a free-text residual.
  /// </summary>
  private static readonly Regex TriggeringObjectCounter = new(
    @"^it\s+had\s+(?:(?<neg>no)|a|an|one|\d+)\s+(?<counter>[+\-]?\d+/[+\-]?\d+)\s+counters?\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "this enchantment has three or more quest counters on it" — a counter-count threshold
  /// predicate gating an ability on the source permanent's own counter accumulation
  /// (Bloodchief Ascension's quest-counter gate; CR 122.1: a counter is a marker placed
  /// on an object; the count threshold is an engine-resolved integer). Structured to a
  /// <see cref="QuantityComparisonCondition"/> whose left operand is a
  /// <see cref="CounterCountQuantity"/> on <see cref="ObjectReferenceKind.Self"/> and
  /// whose right operand is the literal threshold — reference-not-resolution (ADR 0004).
  /// </summary>
  private static readonly Regex SelfCounterThreshold = new(
    @"^this\s+\w+\s+has\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+or\s+(?<dir>more|fewer)\s+(?<type>[\w\-]+)\s+counters?\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it isn't a mana ability" / "it's a mana ability" — the triggering-ability mana-ability
  /// gate on a <see cref="MagicAST.AST.Triggers.TriggerEvent.AbilityActivated"/> trigger
  /// (Rings of Brighthearth's intervening-if; CR 605.1 — a mana ability is an activated/triggered
  /// ability that could add mana, doesn't target, and isn't a loyalty ability). Structured to a
  /// <see cref="MagicAST.AST.Abilities.TriggeringAbilityIsManaCondition"/> rather than left as a
  /// free-text <see cref="OtherCondition"/> residual; the <c>neg</c> group carries the polarity.
  /// </summary>
  private static readonly Regex ManaAbilityGate = new(
    @"^it(?:'s|\s+is|\s+(?<neg>isn't|is\s+not))\s+a\s+mana\s+ability$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "you cast it" — the cast-this-object intervening-if (CR 603.4) on a self ETB trigger. The One
  /// Ring's "When this enters, if you cast it, …": gates the consequent on the source having entered
  /// by being cast (CR 601) rather than copied/reanimated (CR 707.10). Structured to
  /// <see cref="CastThisObjectCondition"/> rather than left as a free-text residual.
  /// </summary>
  private static readonly Regex CastThisObject = new(
    @"^you\s+cast\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "this [permanent|creature|card] is attacking" / "this [permanent|creature|card] is blocking" —
  /// the source object's own combat-state gate (CR 508/509). The "Activate only if this creature is
  /// attacking" restriction family (Glint-Horn Buccaneer, Spectral Sailor, Boltbender). Structured to a
  /// <see cref="SourceCombatStateCondition"/> rather than left as a free-text <see cref="OtherCondition"/>
  /// residual. Anchored (^…$) so it cannot match a substring of a longer clause.
  /// </summary>
  private static readonly Regex SourceCombatState = new(
    @"^this\s+(?:creature|permanent|card)\s+is\s+(?<state>attacking|blocking|attacking\s+or\s+blocking|attacking\s+alone)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it's a Unicorn" / "it's an Elf" — a subtype predicate on the "it" pronoun,
  /// checking whether the designated object has the stated creature subtype. The standard
  /// oracle form for subtype-conditional counter boosts and similar effects
  /// (e.g. Emiel the Blessed: "if it's a Unicorn, put two +1/+1 counters on it instead").
  /// Structured to <see cref="ObjectHasSubtypeCondition"/> rather than left as a free-text
  /// <see cref="OtherCondition"/> residual. Anchored (^…$); uppercase-first subtype word.
  /// CR 205.3m: creature subtypes are always a single proper-cased word.
  /// </summary>
  private static readonly Regex ItsASubtype = new(
    @"^it(?:'s|\s+is)\s+an?\s+(?<subtype>[A-Z][a-zA-Z]*)$",
    RegexOptions.Compiled);

  /// <summary>
  /// "there are four or more card types among cards in your graveyard" — the Delirium
  /// mechanic's activation gate (CR 207.2c: Delirium is an ability word with no special
  /// rules meaning; the condition is the diversity-count predicate). Structured to a
  /// <see cref="CardTypeDiversityCondition"/> rather than left as a free-text residual.
  /// Covers both "your graveyard" (Owner=You) and "a graveyard" (Owner=null) forms.
  /// </summary>
  private static readonly Regex CardTypeDiversity = new(
    @"^there\s+(?:are|is)\s+(?<quant>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer))?\s+card\s+types?\s+among\s+cards?\s+in\s+(?<zone>your\s+graveyard|a\s+graveyard|your\s+hand|your\s+library)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "you've cast a noncreature spell this turn" / "you've cast a spell this turn" /
  /// "you've cast an instant or sorcery spell this turn" — a backward-looking
  /// spell-count intervening-if (CR 603.4) gating on whether the controller has
  /// cast a (optionally type-qualified) spell during the current turn. Council of
  /// Reeds' "if you've cast a noncreature spell this turn"; Sanar's Treasure
  /// ability "Activate only if you've cast an instant or sorcery spell this turn."
  /// Structured to a <see cref="CountCondition"/> whose
  /// <see cref="ObjectFilter.History"/> is a <see cref="CastThisTurnPredicate"/>
  /// (CR 601 casting), composing the same <c>CardTypes=["spell"], Controller=You,
  /// History=castThisTurn</c> shape used by Aetherflux Reservoir's spell-count
  /// quantity, plus two type axes: the "non-[type]" negation
  /// (<see cref="ObjectFilter.ExcludedCardTypes"/>) already used for "cast a
  /// noncreature spell" trigger filters (Spellgorger Weird), and the
  /// "instant or sorcery" disjunction (<c>CardTypes=["spell","instant","sorcery"]</c>,
  /// the same composition Thousand-Year Storm/Doublecast use for the trigger-side
  /// filter). The threshold is "at least one" (GreaterThanOrEqual 1) — "you've cast
  /// a spell" is an existence check, not a literal count. Anchored (^…$).
  /// </summary>
  private static readonly Regex CastSpellThisTurn = new(
    @"^you(?:'ve|\s+have)\s+cast\s+(?:a|an)\s+(?:(?<disjunction>instant\s+or\s+sorcery)\s+|non(?<excluded>[a-z]+)\s+)?(?<noun>spell)\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "red is the most common color among all permanents [or is tied for most common]" —
  /// a color-prevalence gate (Halam Djinn). Structured to a
  /// <see cref="MostCommonColorCondition"/> (a max-by-color tally, not an object count)
  /// rather than left as a free-text residual. There is no CR rule for "most common
  /// color"; it is a card-defined, engine-evaluated tally recorded as written (ADR 0004).
  /// The "or is tied for most common" tail sets <c>IncludeTies</c>. Anchored (^…$).
  /// </summary>
  private static readonly Regex MostCommonColor = new(
    @"^(?<color>white|blue|black|red|green)\s+is\s+the\s+most\s+common\s+color\s+among\s+all\s+(?<noun>[a-z]+?)(?:\s+or\s+is\s+tied\s+for\s+most\s+common)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  private static readonly IReadOnlyDictionary<string, string> ColorWordToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

  /// <summary>Parse a condition phrase; never throws — unrecognised phrases become a residual.</summary>
  public static Condition Parse(string phrase)
  {
    var verbatim = phrase.Trim();
    // Strip a leading "if " / "as long as " connector before matching the predicate.
    var body = Regex.Replace(verbatim, @"^(if|as\s+long\s+as)\s+", "", RegexOptions.IgnoreCase).Trim();

    if (Control.Match(body) is { Success: true } cm)
    {
      var filter = NounToFilter(cm.Groups["noun"].Value.Trim()) with { Controller = ControllerFilter.You };
      return new CountCondition { Filter = filter, Count = Quant(cm.Groups["quant"].Value, cm.Groups["dir"].Value) };
    }

    if (ThereAre.Match(body) is { Success: true } tm)
    {
      // Guard: "card types among cards in your graveyard" is a distinct-type-count
      // predicate (e.g. Delirium), NOT an object count. The noun contains " among "
      // which signals we are counting type-diversity, not objects. Fall through to
      // OtherCondition so a structurally wrong CountCondition isn't emitted.
      var nounRaw = tm.Groups["noun"].Value.Trim();
      if (!nounRaw.Contains(" among ", StringComparison.OrdinalIgnoreCase))
      {
        var filter = NounToFilter(nounRaw) with
        {
          Zone = ZoneOf(tm.Groups["zone"].Value),
          Controller = tm.Groups["zone"].Value.Contains("your", StringComparison.OrdinalIgnoreCase)
            ? ControllerFilter.You
            : null,
        };
        return new CountCondition { Filter = filter, Count = Quant(tm.Groups["quant"].Value, tm.Groups["dir"].Value) };
      }
    }

    if (WasKicked.IsMatch(body))
    {
      return new KeywordCostPaidCondition { Keyword = KeywordAbility.Kicker };
    }

    if (TriggeringObjectCounter.Match(body) is { Success: true } ocm)
    {
      return new TriggeringObjectCounterCondition
      {
        CounterType = ocm.Groups["counter"].Value,
        Present = !ocm.Groups["neg"].Success,
      };
    }

    if (HandSize.Match(body) is { Success: true } hm)
    {
      var owner = hm.Groups["who"].Value.StartsWith("that", StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.ThatPlayer
        : ControllerFilter.You;
      var filter = new ObjectFilter
      {
        CardTypes = ["card"],
        Zone = Zone.Hand,
        Owner = owner,
      };
      return new CountCondition
      {
        Filter = filter,
        Count = Quant(hm.Groups["quant"].Value, hm.Groups["dir"].Value),
      };
    }

    if (LifeTotal.Match(body) is { Success: true } lt)
    {
      var value = NumberWords.TryGetValue(lt.Groups["quant"].Value, out var lv)
        ? lv
        : int.Parse(lt.Groups["quant"].Value);
      var op = lt.Groups["dir"].Value.ToLowerInvariant() switch
      {
        "more" => ComparisonOperator.GreaterThanOrEqual,
        "fewer" or "less" => ComparisonOperator.LessThanOrEqual,
        _ => ComparisonOperator.Equal,
      };
      return new QuantityComparisonCondition
      {
        Left = new DerivedQuantity
        {
          DerivedFrom = DerivedKind.LifeTotal,
          Source = lt.Groups["who"].Value.ToLowerInvariant(),
        },
        Operator = op,
        Right = new LiteralQuantity { Value = value },
      };
    }

    if (LifeTotalVsHalfStarting.Match(body) is { Success: true } lths)
    {
      var who = lths.Groups["who"].Value.StartsWith("your", StringComparison.OrdinalIgnoreCase)
        ? "you"
        : "that player";
      var op = lths.Groups["op"].Value.ToLowerInvariant() switch
      {
        "less than or equal to" => ComparisonOperator.LessThanOrEqual,
        "greater than or equal to" => ComparisonOperator.GreaterThanOrEqual,
        "less than" => ComparisonOperator.LessThan,
        "greater than" => ComparisonOperator.GreaterThan,
        _ => ComparisonOperator.Equal,
      };
      return new QuantityComparisonCondition
      {
        Left = new DerivedQuantity { DerivedFrom = DerivedKind.LifeTotal, Source = who },
        Operator = op,
        Right = new CalculatedQuantity
        {
          Operation = "half",
          BaseQuantity = new DerivedQuantity { DerivedFrom = DerivedKind.StartingLifeTotal, Source = who },
        },
      };
    }

    if (SelfCounterThreshold.Match(body) is { Success: true } sct)
    {
      var counterType = sct.Groups["type"].Value.ToLowerInvariant();
      var thresholdValue = NumberWords.TryGetValue(sct.Groups["count"].Value, out var tv)
        ? tv
        : int.Parse(sct.Groups["count"].Value);
      var op = sct.Groups["dir"].Value.ToLowerInvariant() switch
      {
        "more" => ComparisonOperator.GreaterThanOrEqual,
        "fewer" => ComparisonOperator.LessThanOrEqual,
        _ => ComparisonOperator.GreaterThanOrEqual,
      };
      return new QuantityComparisonCondition
      {
        Left = new CounterCountQuantity
        {
          CounterType = counterType,
          On = ObjectReference.Self(),
        },
        Operator = op,
        Right = new LiteralQuantity { Value = thresholdValue },
      };
    }

    if (ManaAbilityGate.Match(body) is { Success: true } mag)
    {
      // The negation group fires only for "isn't"/"is not"; the affirmative
      // ("it's"/"it is a mana ability") leaves it empty → IsManaAbility = true.
      return new TriggeringAbilityIsManaCondition { IsManaAbility = !mag.Groups["neg"].Success };
    }

    if (CastThisObject.IsMatch(body))
    {
      return new CastThisObjectCondition();
    }

    if (CastSpellThisTurn.Match(body) is { Success: true } cstm)
    {
      var filter = new ObjectFilter
      {
        CardTypes = cstm.Groups["disjunction"].Success
          ? ["spell", "instant", "sorcery"]
          : ["spell"],
        ExcludedCardTypes = cstm.Groups["excluded"].Success
          ? [cstm.Groups["excluded"].Value.ToLowerInvariant()]
          : null,
        Controller = ControllerFilter.You,
        History = new CastThisTurnPredicate { Caster = ControllerFilter.You },
      };
      return new CountCondition
      {
        Filter = filter,
        Count = new Comparison { Operator = ComparisonOperator.GreaterThanOrEqual, Value = 1 },
      };
    }

    if (SourceCombatState.Match(body) is { Success: true } scm)
    {
      var stateText = scm.Groups["state"].Value.Trim().ToLowerInvariant();
      var state = stateText switch
      {
        "attacking" => CombatState.Attacking,
        "blocking" => CombatState.Blocking,
        "attacking or blocking" => CombatState.AttackingOrBlocking,
        "attacking alone" => CombatState.AttackingAlone,
        _ => CombatState.Attacking,
      };
      return new SourceCombatStateCondition { State = state };
    }

    if (CardTypeDiversity.Match(body) is { Success: true } ctd)
    {
      var zone = ctd.Groups["zone"].Value.Trim().ToLowerInvariant();
      var zoneEnum = zone.Contains("graveyard") ? Zone.Graveyard
        : zone.Contains("hand") ? Zone.Hand
        : zone.Contains("library") ? Zone.Library
        : Zone.Anywhere;
      var owner = zone.Contains("your") ? (ControllerFilter?)ControllerFilter.You : null;
      return new CardTypeDiversityCondition
      {
        Count = Quant(ctd.Groups["quant"].Value, ctd.Groups["dir"].Value),
        Zone = zoneEnum,
        Owner = owner,
      };
    }

    if (ItsASubtype.Match(body) is { Success: true } ias)
    {
      return new ObjectHasSubtypeCondition
      {
        Subtype = ias.Groups["subtype"].Value,
        Subject = "It",
      };
    }

    if (MostCommonColor.Match(body) is { Success: true } mcc)
    {
      return new MostCommonColorCondition
      {
        Color = ColorWordToCode[mcc.Groups["color"].Value],
        IncludeTies = body.Contains("tied", StringComparison.OrdinalIgnoreCase),
        Among = NounToFilter(mcc.Groups["noun"].Value.Trim()),
      };
    }

    return new OtherCondition { Text = verbatim };
  }

  private static Comparison Quant(string quant, string dir)
  {
    var value = NumberWords.TryGetValue(quant, out var n) ? n : int.Parse(quant);
    var op = dir.ToLowerInvariant() switch
    {
      "more" => ComparisonOperator.GreaterThanOrEqual,
      "fewer" => ComparisonOperator.LessThanOrEqual,
      _ when quant is "a" or "an" => ComparisonOperator.GreaterThanOrEqual,
      _ => ComparisonOperator.Equal,
    };
    return new Comparison { Operator = op, Value = value };
  }

  private static ObjectFilter NounToFilter(string noun)
  {
    // Drop a leading "other" qualifier (e.g. "other lands") — not a structured axis yet.
    noun = Regex.Replace(noun, @"^other\s+", "", RegexOptions.IgnoreCase).Trim();
    var singular = noun.EndsWith("s", StringComparison.Ordinal) ? noun[..^1] : noun;
    return CardTypeNouns.Contains(singular)
      ? new ObjectFilter { CardTypes = [singular.ToLowerInvariant()] }
      : new ObjectFilter { Subtypes = [singular] };
  }

  private static Zone ZoneOf(string zone) =>
    zone.ToLowerInvariant() switch
    {
      var z when z.Contains("graveyard") => Zone.Graveyard,
      var z when z.Contains("hand") => Zone.Hand,
      var z when z.Contains("library") => Zone.Library,
      var z when z.Contains("exile") => Zone.Exile,
      _ => Zone.Anywhere,
    };
}
