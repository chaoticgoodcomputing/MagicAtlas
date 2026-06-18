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
      var filter = NounToFilter(tm.Groups["noun"].Value.Trim()) with
      {
        Zone = ZoneOf(tm.Groups["zone"].Value),
        Controller = tm.Groups["zone"].Value.Contains("your", StringComparison.OrdinalIgnoreCase)
          ? ControllerFilter.You
          : null,
      };
      return new CountCondition { Filter = filter, Count = Quant(tm.Groups["quant"].Value, tm.Groups["dir"].Value) };
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
