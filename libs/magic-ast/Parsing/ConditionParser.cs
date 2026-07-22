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
  /// hand" / "you have fewer than ten cards in hand" (The Ten Rings) — a hand-size
  /// predicate (Prickle Faeries' upkeep intervening-if). The possessive subject maps
  /// to the owner of the counted cards (hand membership is by ownership, CR 108.3):
  /// "that player" → <see cref="ControllerFilter.ThatPlayer"/> (the player whose
  /// step fired the trigger, CR 109.5), "you/your" → You. Structured to a
  /// <see cref="CountCondition"/> over the Hand zone rather than left as a free-text
  /// residual. Covers both the trailing "N or more/fewer" suffix form and the
  /// strict leading "fewer than N"/"more than N" prefix form (a plain
  /// <see cref="ComparisonOperator.LessThan"/>/<see cref="ComparisonOperator.GreaterThan"/>,
  /// distinct from the suffix form's inclusive "or fewer"/"or more").
  /// </summary>
  private static readonly Regex HandSize = new(
    @"^(?<who>that\s+player|you|your)\s+(?:has|have)\s+(?:(?<prefixdir>fewer|more)\s+than\s+)?(?<quant>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer))?\s+cards?\s+in\s+(?:hand|their\s+hand|your\s+hand)$",
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
  /// "this creature is equipped" / "it's equipped" — the source object's own
  /// attachment-state gate for an "as long as …" grant (Leonin Den-Guard: "As long
  /// as this creature is equipped, it gets +1/+1."; Merry, Esquire of Rohan: "Merry
  /// has first strike as long as it's equipped."). CR 702.6f: a creature that has an
  /// Equipment attached to it is "equipped". Structured to an
  /// <see cref="ObjectIsEquippedCondition"/> — the attachment-state sibling of the
  /// zone-state <see cref="ObjectInZoneCondition"/> and combat-state
  /// <see cref="SourceCombatStateCondition"/> — rather than left as a free-text
  /// <see cref="OtherCondition"/> residual. The pronoun is preserved as-written
  /// (reference-not-resolution, ADR 0004): "this creature" →
  /// <see cref="ObjectReferenceKind.Self"/>, the bare pronoun "it's" →
  /// <see cref="ObjectReferenceKind.It"/>. Anchored (^…$).
  /// </summary>
  private static readonly Regex SourceIsEquipped = new(
    @"^(?:this\s+(?<self>creature|permanent|card)\s+is|it's|it\s+is)\s+equipped$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "this artifact is tapped" / "this artifact remains tapped" / "it's untapped" /
  /// "this permanent is saddled" — the source (or back-referenced) object's own
  /// status/designation gate (Mana Vault's draw-step intervening-if; Endoskeleton's
  /// "for as long as this artifact remains tapped"; Giant Tortoise's "as long as it's
  /// untapped"; the OTJ Mounts' "attacks while saddled"). CR 110.6: a permanent is
  /// either tapped or untapped; CR 702.166: a Mount that has resolved its saddle
  /// ability is "saddled". Structured to an <see cref="ObjectStatusCondition"/> — the
  /// status-state sibling of the zone-state <see cref="ObjectInZoneCondition"/>, the
  /// attachment-state <see cref="ObjectIsEquippedCondition"/>, and the combat-state
  /// <see cref="SourceCombatStateCondition"/> — rather than left as a free-text
  /// <see cref="OtherCondition"/> residual. The subject is preserved as-written
  /// (reference-not-resolution, ADR 0004): "this artifact/permanent/land/creature" →
  /// <see cref="ObjectReferenceKind.Self"/>, the bare pronoun "it's" →
  /// <see cref="ObjectReferenceKind.It"/>; "untapped" survives as its own status value,
  /// not folded to a negation. Anchored (^…$).
  /// </summary>
  private static readonly Regex ObjectStatusState = new(
    @"^(?:this\s+(?<self>artifact|land|permanent|creature|card)|it)(?:'s|\s+is|\s+remains)\s+(?<status>tapped|untapped|saddled)$",
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
  /// "a nonland permanent left the battlefield this turn or a spell was warped this turn"
  /// — the fixed backward-looking event-history disjunction the Edge of Eternities
  /// <em>Void</em> ability word denotes (CR 207.2c: "void" is an ability word with no
  /// special rules meaning, so this printed disjunction — byte-identical on every Void
  /// card — is the whole condition). The two disjuncts are two independent this-turn
  /// events: a nonland permanent left the battlefield this turn (a leaves-the-battlefield
  /// event, CR 603.6c / 603.10a) OR a spell was warped this turn (CR 702.185c: a spell
  /// was cast for its warp cost this turn). Structured to a fixed-idiom marker
  /// <see cref="VoidCondition"/> — the disjunction never varies, so there is nothing to
  /// parameterise — rather than left as a free-text <see cref="OtherCondition"/> residual.
  /// Anchored (^…$).
  /// </summary>
  private static readonly Regex VoidEventHistory = new(
    @"^a\s+nonland\s+permanent\s+left\s+the\s+battlefield\s+this\s+turn\s+or\s+a\s+spell\s+was\s+warped\s+this\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

  /// <summary>
  /// "it targets a blue spell" — Mystical Dispute's conditional self-cost-reduction
  /// gate (CR 118.7). Anchored on the "[color] spell" tail so it cannot collide with
  /// the "it targets a tapped creature/permanent" sibling (a distinct free-text arm
  /// consumed only by <see cref="ConditionalSpellCostReductionRule"/>'s own regex,
  /// not handled here).
  /// </summary>
  private static readonly Regex ItTargetsColoredSpell = new(
    @"^it\s+targets\s+an?\s+(?<color>white|blue|black|red|green)\s+spell$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "you don't control a Pest creature token" — a control-count intervening-if whose
  /// polarity is "none" (the controller has zero objects matching the filter). Pest Rescuer's
  /// upkeep gate. Structured to a <see cref="CountCondition"/> with an
  /// <see cref="ComparisonOperator.Equal"/>-0 threshold — the negation of the affirmative
  /// <see cref="Control"/> arm — rather than left as a free-text residual. The noun phrase is
  /// mapped through <see cref="NounToFilter"/> (subtype + card type + token axes), then scoped
  /// to <see cref="ControllerFilter.You"/>. Anchored (^…$).
  /// </summary>
  private static readonly Regex DontControl = new(
    @"^you\s+don'?t\s+control\s+(?:a|an|any)\s+(?<noun>.+?)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it has a +1/+1 counter on it" — a present-tense counter-PRESENCE gate on the source
  /// object (Unleash's "This permanent can't block as long as it has a +1/+1 counter on it",
  /// CR 702.98a). Structured to <see cref="ObjectHasCounterCondition"/> with
  /// <see cref="ObjectReference.Self"/> as the subject (the doc's stated convention for the
  /// "it has a [counter] on it" self-reference) rather than left as a free-text residual.
  /// Present tense ("has"), distinct from the past-tense look-back
  /// <see cref="TriggeringObjectCounter"/> ("it HAD a +1/+1 counter") and the threshold form
  /// <see cref="SelfCounterThreshold"/> ("this permanent has N or more … counters"). Anchored (^…$).
  /// </summary>
  private static readonly Regex ObjectHasCounterPresent = new(
    @"^it\s+has\s+(?:a|an|one)\s+(?<counter>[+\-]?\d+/[+\-]?\d+|[A-Za-z][\w\-]*)\s+counters?\s+on\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "enchanted creature is black" — a present-tense COLOR gate on the enchanted permanent
  /// (Gift of the Deity: "As long as enchanted creature is black, …" / "… is green, …";
  /// CR 105.1). Structured to <see cref="ObjectHasColorCondition"/> keyed on
  /// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> rather than left as a free-text
  /// residual — the colour sibling of <see cref="MagicAST.AST.Abilities.ObjectHasCardTypeCondition"/>
  /// ("enchanted permanent is a creature"). Anchored (^…$).
  /// </summary>
  private static readonly Regex EnchantedColorState = new(
    @"^enchanted\s+creature\s+is\s+(?<color>white|blue|black|red|green)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it's legendary" / "it is legendary" — a present-tense SUPERTYPE gate on the "it" object
  /// (Toralf's Hammer: "Equipped creature gets +3/+0 as long as it's legendary."; CR 205.4a).
  /// Structured to <see cref="ObjectHasSupertypeCondition"/> keyed on
  /// <see cref="ObjectReferenceKind.It"/> rather than left as a free-text residual — the
  /// supertype sibling of <see cref="MagicAST.AST.Abilities.ObjectHasCardTypeCondition"/>. Anchored (^…$).
  /// </summary>
  private static readonly Regex ItsLegendary = new(
    @"^it(?:'s|\s+is)\s+legendary$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "it was a creature" / "it wasn't a creature" — a PAST-TENSE last-known-information card-type
  /// gate on the triggering object of a leaves-the-battlefield trigger (Enduring Tenacity:
  /// "When Enduring Tenacity dies, if it was a creature, …"; CR 603.10a). Structured to
  /// <see cref="MagicAST.AST.Abilities.TriggeringObjectTypeCondition"/> rather than left as a
  /// free-text residual — the past-tense sibling of the present-tense
  /// <see cref="MagicAST.AST.Abilities.ObjectHasCardTypeCondition"/>. The <c>neg</c> group carries
  /// the polarity. Anchored (^…$).
  /// </summary>
  private static readonly Regex TriggeringObjectType = new(
    @"^it\s+was(?<neg>n'?t)?\s+an?\s+(?<type>creature|artifact|enchantment|land|planeswalker|permanent|instant|sorcery)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// "target player has fewer than nine poison counters" — a poison-counter threshold on a
  /// player (Vraska, Betrayal's Sting's −9). Structured to a
  /// <see cref="QuantityComparisonCondition"/> whose left operand is a
  /// <see cref="CounterCountQuantity"/> of poison counters on the referenced player, rather than
  /// left as a free-text residual. Poison is a per-player counter (CR 122.1d / 104.3d), so its
  /// count is a <see cref="CounterCountQuantity"/> on a player <see cref="ObjectReference"/>, not
  /// an object count. Covers "target player"/"you"/"that player" subjects and both the strict
  /// "fewer than N" prefix form and the inclusive "N or more/fewer" suffix form. Anchored (^…$).
  /// </summary>
  private static readonly Regex PoisonCounterThreshold = new(
    @"^(?<who>target\s+player|you|that\s+player)\s+(?:has|have)\s+(?:(?<prefixdir>fewer|more|less)\s+than\s+)?(?<quant>\d+|one|two|three|four|five|six|seven|eight|nine|ten)(?:\s+or\s+(?<dir>more|fewer|less))?\s+poison\s+counters?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled);

  /// <summary>
  /// The most recent hand-size upper-bound threshold parsed by <see cref="Parse"/>'s
  /// <see cref="HandSize"/> arm (e.g. 10 for "fewer than ten cards in hand"), for a
  /// paired effect rule that needs the numeral — see the hand-off note on the
  /// <see cref="HandSize"/> match site in <see cref="Parse"/>. The consumer (e.g.
  /// <c>DrawCardsEqualToHandSizeDifferenceRule</c>) reads and clears it immediately
  /// after, in the same synchronous ability parse, mirroring the
  /// <c>AttacksPlayerAndIsntBlockedConditionRule.PendingInterveningIf</c> hand-off.
  /// </summary>
  [ThreadStatic]
  public static int? PendingHandSizeUpperBound;

  /// <summary>Parse a condition phrase; never throws — unrecognised phrases become a residual.</summary>
  public static Condition Parse(string phrase)
  {
    // Reset the hand-off from any earlier, unrelated Parse call on this thread —
    // only the HandSize match (if any) made by THIS call should reach the paired
    // effect rule that runs immediately afterward. Set again below if this call's
    // body matches HandSize with an upper-bound comparison.
    PendingHandSizeUpperBound = null;

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

      Comparison count;
      if (hm.Groups["prefixdir"].Success)
      {
        // Strict leading form: "fewer than N"/"more than N" (no "or").
        var value = NumberWords.TryGetValue(hm.Groups["quant"].Value, out var pv)
          ? pv
          : int.Parse(hm.Groups["quant"].Value);
        var op = hm.Groups["prefixdir"].Value.Equals("fewer", StringComparison.OrdinalIgnoreCase)
          ? ComparisonOperator.LessThan
          : ComparisonOperator.GreaterThan;
        count = new Comparison { Operator = op, Value = value };
      }
      else
      {
        count = Quant(hm.Groups["quant"].Value, hm.Groups["dir"].Value);
      }

      // Communicate the upper-bound threshold to a paired effect rule that needs
      // the numeral to build a "draw cards equal to the difference" quantity (The
      // Ten Rings: "if you have fewer than ten cards in hand, draw cards equal to
      // the difference" — the effect rule only sees the post-intervening-if effect
      // fragment, so the numeral this condition just parsed is handed off here).
      // Only set for an upper-bound (LessThan/LessThanOrEqual): "the difference"
      // reads as threshold-minus-count, which only makes sense against an upper
      // bound. Mirrors the AttacksPlayerAndIsntBlockedConditionRule.PendingInterveningIf
      // cross-rule hand-off pattern used elsewhere in the triggered-ability pipeline.
      if (count.Operator is ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual && count.Value is int threshold)
      {
        PendingHandSizeUpperBound = threshold;
      }

      return new CountCondition
      {
        Filter = filter,
        Count = count,
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

    if (SourceIsEquipped.Match(body) is { Success: true } sie)
    {
      return new ObjectIsEquippedCondition
      {
        Reference = sie.Groups["self"].Success
          ? ObjectReference.Self()
          : new ObjectReference { Kind = ObjectReferenceKind.It },
      };
    }

    if (ObjectStatusState.Match(body) is { Success: true } oss)
    {
      var status = oss.Groups["status"].Value.ToLowerInvariant() switch
      {
        "tapped" => ObjectStatus.Tapped,
        "untapped" => ObjectStatus.Untapped,
        "saddled" => ObjectStatus.Saddled,
        _ => ObjectStatus.Tapped,
      };
      return new ObjectStatusCondition
      {
        Reference = oss.Groups["self"].Success
          ? ObjectReference.Self()
          : new ObjectReference { Kind = ObjectReferenceKind.It },
        Status = status,
      };
    }

    if (VoidEventHistory.IsMatch(body))
    {
      return new VoidCondition();
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

    if (ItTargetsColoredSpell.Match(body) is { Success: true } itcs)
    {
      return new TargetsFilterCondition
      {
        Subject = "It",
        Filter = new ObjectFilter
        {
          CardTypes = ["spell"],
          Colors = [ColorWordToCode[itcs.Groups["color"].Value]],
        },
      };
    }

    if (DontControl.Match(body) is { Success: true } dcm)
    {
      var filter = NounToFilter(dcm.Groups["noun"].Value.Trim()) with { Controller = ControllerFilter.You };
      return new CountCondition
      {
        Filter = filter,
        Count = new Comparison { Operator = ComparisonOperator.Equal, Value = 0 },
      };
    }

    if (ObjectHasCounterPresent.Match(body) is { Success: true } ohc)
    {
      return new ObjectHasCounterCondition
      {
        Subject = ObjectReference.Self(),
        CounterType = ohc.Groups["counter"].Value,
      };
    }

    if (EnchantedColorState.Match(body) is { Success: true } ecs)
    {
      return new ObjectHasColorCondition
      {
        Color = ColorWordToCode[ecs.Groups["color"].Value],
        Subject = "EnchantedOrEquipped",
      };
    }

    if (ItsLegendary.IsMatch(body))
    {
      return new ObjectHasSupertypeCondition { Supertype = "Legendary", Subject = "It" };
    }

    if (TriggeringObjectType.Match(body) is { Success: true } tot)
    {
      return new TriggeringObjectTypeCondition
      {
        CardType = tot.Groups["type"].Value.ToLowerInvariant(),
        Present = !tot.Groups["neg"].Success,
      };
    }

    if (PoisonCounterThreshold.Match(body) is { Success: true } pct)
    {
      var value = NumberWords.TryGetValue(pct.Groups["quant"].Value, out var pv)
        ? pv
        : int.Parse(pct.Groups["quant"].Value);
      ComparisonOperator op;
      if (pct.Groups["prefixdir"].Success)
      {
        op = pct.Groups["prefixdir"].Value.Equals("more", StringComparison.OrdinalIgnoreCase)
          ? ComparisonOperator.GreaterThan
          : ComparisonOperator.LessThan;
      }
      else
      {
        op = pct.Groups["dir"].Value.ToLowerInvariant() switch
        {
          "more" => ComparisonOperator.GreaterThanOrEqual,
          "fewer" or "less" => ComparisonOperator.LessThanOrEqual,
          _ => ComparisonOperator.Equal,
        };
      }

      var who = pct.Groups["who"].Value.ToLowerInvariant();
      var on = who.StartsWith("target", StringComparison.Ordinal)
        ? new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = new ObjectFilter { EntityType = "player" } }
        : who.StartsWith("that", StringComparison.Ordinal)
          ? new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer }
          : new ObjectReference { Kind = ObjectReferenceKind.You };

      return new QuantityComparisonCondition
      {
        Left = new CounterCountQuantity { CounterType = "poison", On = on },
        Operator = op,
        Right = new LiteralQuantity { Value = value },
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

    // "… with different names" — a set-level uniqueness qualifier on the counted population
    // (The Necrobloom: "seven or more lands with different names"). Strip it onto the
    // DifferentNames axis, leaving the base noun for type classification.
    var differentNames = false;
    var dnMatch = Regex.Match(noun, @"^(?<base>.+?)\s+with\s+different\s+names$", RegexOptions.IgnoreCase);
    if (dnMatch.Success)
    {
      noun = dnMatch.Groups["base"].Value.Trim();
      differentNames = true;
    }

    // Classify each word of the (possibly multi-word) noun phrase onto its axis: a card-type
    // noun ("creature") → CardTypes, "token"/"tokens" → the IsToken axis (CR 111), anything
    // else → a subtype ("Pest"). A single-word card-type/subtype noun round-trips exactly as
    // the former single-noun mapping did.
    var words = noun.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var cardTypes = new List<string>();
    var subtypes = new List<string>();
    var sawToken = false;
    foreach (var w in words)
    {
      var singular = w.EndsWith("s", StringComparison.Ordinal) ? w[..^1] : w;
      if (w.Equals("token", StringComparison.OrdinalIgnoreCase)
        || w.Equals("tokens", StringComparison.OrdinalIgnoreCase))
      {
        sawToken = true;
      }
      else if (CardTypeNouns.Contains(singular))
      {
        cardTypes.Add(singular.ToLowerInvariant());
      }
      else
      {
        subtypes.Add(singular);
      }
    }

    return new ObjectFilter
    {
      CardTypes = cardTypes.Count > 0 ? cardTypes : null,
      Subtypes = subtypes.Count > 0 ? subtypes : null,
      IsToken = sawToken ? true : null,
      DifferentNames = differentNames ? true : null,
    };
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
