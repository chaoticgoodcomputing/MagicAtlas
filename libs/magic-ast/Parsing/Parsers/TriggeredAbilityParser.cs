namespace MagicAST.Parsing.Parsers;

using System.Reflection;
using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Parser for triggered abilities: "When/Whenever/At [trigger], [effect]"
/// Supports multiple trigger events and effect types using compositional parsing.
/// </summary>
/// <remarks>
/// This parser uses a compositional approach to handle families of similar cards
/// rather than overfitting to specific card text. Components are designed to be
/// reusable across different trigger and effect patterns.
/// </remarks>
[OracleAbilityParser(AbilityKind.Triggered)]
public sealed class TriggeredAbilityParser : IAbilityParser
{
  private readonly FallbackParser _fallback = new();
  private readonly AbilityClassifier _classifier = new();

  // Lazy so we don't recurse through registry construction at type-load time.
  // The triggered-modal path dispatches each modal option clause back through
  // the registry, exactly like ModalAbilityParser does for spell-level modals.
  private static readonly Lazy<AbilityParserRegistry> _registry =
    new(() => new AbilityParserRegistry());

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var parsed = TryParse(clause, classification);
    if (parsed is not null)
    {
      return [parsed];
    }
    return
    [
      _fallback.Parse(
        clause,
        classification,
        "Triggered ability parser not yet implemented",
        lastAttemptedRule: "TriggeredAbilityParser.Parse",
        failurePosition: clause.SourceSpan.Start
      ),
    ];
  }

  /// <summary>
  /// Attempts to parse a triggered ability from a clause.
  /// </summary>
  /// <param name="clause">The clause to parse.</param>
  /// <param name="classification">The classification information.</param>
  /// <returns>A parsed TriggeredAbility or null if parsing fails.</returns>
  public TriggeredAbility? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = clause.RawText;
    var tokens = clause.Tokens.ToList();

    if (tokens.Count == 0)
    {
      return null;
    }

    // Peel ability-word prefix ("Landfall — ", "Threshold — ", etc.) if present.
    // Ability words (Rule 207.2c) have no rules meaning; the classifier already
    // extracted the word into classification.AbilityWord. Strip the prefix from
    // the raw text and token list so trigger-timing detection works generically
    // across all ability words without per-word parser branches.
    string? abilityWord = classification.AbilityWord;
    if (abilityWord is not null)
    {
      var emDashIndex = text.IndexOf('—');
      if (emDashIndex >= 0)
      {
        text = text[(emDashIndex + 1)..].TrimStart();
        // Rebuild token list from the stripped text by dropping tokens before and including the em-dash.
        var emDashTokenIndex = tokens.FindIndex(t => t.Kind == OracleToken.EmDash);
        if (emDashTokenIndex >= 0 && emDashTokenIndex + 1 < tokens.Count)
        {
          tokens = tokens[(emDashTokenIndex + 1)..];
        }
      }
    }

    // Parse trigger timing (When/Whenever/At)
    var triggerTiming = ParseTriggerTiming(tokens[0].Kind);
    if (triggerTiming == null)
    {
      return null;
    }

    // Split into trigger and effect parts at comma
    var parts = SplitTriggerAndEffect(text);
    if (parts == null)
    {
      return null;
    }

    var (triggerPart, effectPart) = parts.Value;

    // Rule 603.4 intervening-if: "At/When/Whenever X, if Y, Z." The
    // trigger/effect split may swallow the "if Y" into the trigger half when
    // there's an effect verb after the "if" clause. Extract trailing
    // ", if <cond>" from triggerPart before parsing the trigger condition.
    Condition? interveningIf = null;
    var trailingIfMatch = Regex.Match(
      triggerPart,
      @"^(?<head>.+),\s*if\s+(?<cond>[^,]+)$",
      RegexOptions.IgnoreCase
    );
    if (trailingIfMatch.Success)
    {
      interveningIf = new Condition { Text = trailingIfMatch.Groups["cond"].Value.Trim() };
      triggerPart = trailingIfMatch.Groups["head"].Value.Trim();
    }

    // "Attacks while saddled" — Rule 702.171 Mount mechanic. The "while [condition]"
    // suffix on an attack trigger is an inline condition attached to the trigger text
    // (not a post-comma intervening-if). Extract it and promote to InterveningIf.
    // Pattern: "[subject] attacks while [condition]" → trigger on Attacks + interveningIf.
    // Normalise "while saddled" to "this permanent is saddled" for consistency with
    // the Condition.Text vocabulary used elsewhere (plain predicate, no dangling phrases).
    if (interveningIf is null)
    {
      var whileCondMatch = Regex.Match(
        triggerPart,
        @"^(?<head>.+attacks)\s+while\s+(?<cond>.+)$",
        RegexOptions.IgnoreCase
      );
      if (whileCondMatch.Success)
      {
        var condText = whileCondMatch.Groups["cond"].Value.Trim().ToLowerInvariant();
        // Normalise bare adjective "saddled" → "this permanent is saddled".
        // Other "while [adj]" forms are preserved as-is.
        var normalised = condText == "saddled" ? "this permanent is saddled" : condText;
        interveningIf = new Condition { Text = normalised };
        triggerPart = whileCondMatch.Groups["head"].Value.Trim();
      }
    }

    // Parse trigger event and filter
    var trigger = ParseTriggerCondition(triggerPart, triggerTiming.Value);
    if (trigger == null)
    {
      return null;
    }

    // Same intervening-if shape, but with the condition still sitting at the
    // head of the effect half (e.g., when the split landed on a different
    // comma). Strip it off and assign to interveningIf if not already set.
    if (interveningIf is null)
    {
      var leadingIfMatch = Regex.Match(
        effectPart,
        @"^if\s+(?<cond>.+?),\s+(?<rest>.+)$",
        RegexOptions.IgnoreCase
      );
      if (leadingIfMatch.Success)
      {
        interveningIf = new Condition { Text = leadingIfMatch.Groups["cond"].Value.Trim() };
        effectPart = leadingIfMatch.Groups["rest"].Value.Trim();
      }
    }

    // Modal trigger: "When X, choose one —" with bulleted options absorbed by
    // ClauseSplitter into clause.ModalOptions. The effect-part is the modal
    // selector phrase; the actual effects are the bullet bodies. Build a
    // single ModalEffect to occupy the trigger's Effects list.
    if (clause.ModalOptions is { Count: > 0 } && TryBuildModalEffect(effectPart, clause.ModalOptions) is { } modalEffect)
    {
      return new TriggeredAbility
      {
        Trigger = trigger,
        InterveningIf = interveningIf,
        Effects = [modalEffect],
        AbilityWord = abilityWord,
      };
    }

    // Strip trailing parenthetical reminder text from the effect part before
    // dispatching to effect rules. Reminder text follows the effect sentence
    // as "(explanation...)" — e.g. "surveil 1. (Look at the top card...)".
    // Capture it for the Reminder field on the returned TriggeredAbility.
    var reminder = ExtractTrailingReminder(ref effectPart);

    // Parse effects
    var effects = ParseEffects(effectPart);
    if (effects == null || effects.Count == 0)
    {
      return null;
    }

    return new TriggeredAbility
    {
      Trigger = trigger,
      InterveningIf = interveningIf,
      Effects = effects,
      Instructions = ExtractInstructions(effectPart, effects),
      Reminder = reminder,
      AbilityWord = abilityWord,
    };
  }

  /// <summary>
  /// Strips a trailing parenthetical "(reminder text)" from <paramref name="effectPart"/>,
  /// mutating it in place (via ref), and returns the parenthetical as a
  /// <see cref="Parenthetical"/> if found, or null otherwise.
  /// Only strips the LAST parenthetical so that mid-text parens (e.g. mana symbols) are left intact.
  /// </summary>
  private static Parenthetical? ExtractTrailingReminder(ref string effectPart)
  {
    // Match a trailing (possibly multi-sentence) parenthetical at the end of the text.
    // Pattern: optional whitespace, open-paren, content (no nesting), close-paren,
    // optional trailing period (oracle convention places the sentence-terminating
    // period AFTER the reminder paren — e.g. "you get {E}{E} (two energy counters)."),
    // then end-of-string.
    var m = Regex.Match(effectPart, @"\s*\(([^)]+)\)\s*\.?\s*$");
    if (!m.Success)
    {
      return null;
    }

    var reminderText = m.Groups[1].Value.Trim();
    effectPart = effectPart[..m.Index].Trim();
    return new Parenthetical { Text = reminderText };
  }

  /// <summary>
  /// Returns the "you may pay {X}" instruction list when the effect-half
  /// starts with that phrasing and the parsed effects include the
  /// "If you do, ..." follow-up flagged as optional. The Instruction text
  /// is the bare "you may pay {X}" fragment (without the period or follow-up
  /// clause), matching the gold convention for Mana-Vault-style upkeep
  /// triggers.
  /// </summary>
  private static IReadOnlyList<string>? ExtractInstructions(
    string effectPart,
    IReadOnlyList<Effect> effects
  )
  {
    var match = Regex.Match(
      effectPart,
      @"^(?<instr>you\s+may\s+pay\s+(?:\{[^}]+\})+)\.\s*If\s+you\s+do,",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    return new List<string> { match.Groups["instr"].Value.Trim() };
  }

  /// <summary>
  /// Build a <see cref="ModalEffect"/> for a triggered ability whose effect
  /// stream is the modal preamble "choose one —" (or similar) followed by
  /// bulleted option bodies.
  ///
  /// Each option clause is dispatched back through the classifier+registry —
  /// the same mechanism <see cref="ModalAbilityParser"/> uses — so the option
  /// bodies parse to whatever ability shape their text classifies as (typically
  /// <see cref="SpellAbility"/> with an effect list). Returns null if the
  /// effect-part text isn't a recognised modal selector.
  /// </summary>
  private ModalEffect? TryBuildModalEffect(
    string effectPart,
    IReadOnlyList<OracleClause> optionClauses
  )
  {
    var selection = TryParseModeSelection(effectPart);
    if (selection is null)
    {
      return null;
    }

    var modes = new List<ModalOption>(optionClauses.Count);
    foreach (var optionClause in optionClauses)
    {
      var optionClassification = _classifier.Classify(optionClause);
      var optionAbilities = _registry
        .Value.GetParser(optionClassification.Kind)
        .Parse(optionClause, optionClassification);
      foreach (var ability in optionAbilities)
      {
        modes.Add(new ModalOption { Ability = ability });
      }
    }

    return new ModalEffect
    {
      ModeSelection = selection,
      Modes = modes,
    };
  }

  /// <summary>
  /// Recognises the modal-selector tail of a trigger preamble — the substring
  /// after the trigger-condition comma, e.g. "choose one —". Mirrors the
  /// header-match logic on <see cref="ModalAbilityParser"/> but allows the
  /// trailing em-dash to be present (selectors in trigger-modal preambles
  /// always carry the em-dash because the clause body lives on bullet lines).
  /// </summary>
  private static ModeSelection? TryParseModeSelection(string effectPart)
  {
    var trimmed = effectPart.TrimEnd();
    if (trimmed.EndsWith('—'))
    {
      trimmed = trimmed[..^1].TrimEnd();
    }

    var lower = trimmed.ToLowerInvariant();
    if (lower == "choose one or both")
    {
      return ModeSelection.ChooseOneOrBoth();
    }
    if (lower == "choose one")
    {
      return ModeSelection.ChooseOne();
    }
    if (lower == "choose two")
    {
      return ModeSelection.ChooseTwo();
    }
    if (lower == "choose three")
    {
      return ModeSelection.ChooseExactly(3);
    }
    if (lower == "choose any number")
    {
      return ModeSelection.ChooseUpTo(int.MaxValue);
    }
    var upTo = Regex.Match(lower, @"^choose up to (\w+)$");
    if (upTo.Success && TryParseWordNumber(upTo.Groups[1].Value, out var n))
    {
      return ModeSelection.ChooseUpTo(n);
    }
    return null;
  }

  private static bool TryParseWordNumber(string token, out int value)
  {
    if (int.TryParse(token, out value))
    {
      return true;
    }
    switch (token)
    {
      case "one":
        value = 1;
        return true;
      case "two":
        value = 2;
        return true;
      case "three":
        value = 3;
        return true;
      case "four":
        value = 4;
        return true;
      case "five":
        value = 5;
        return true;
      default:
        value = 0;
        return false;
    }
  }

  #region Trigger Parsing

  /// <summary>
  /// Parses the trigger timing keyword.
  /// </summary>
  private static TriggerTiming? ParseTriggerTiming(OracleToken token) =>
    token switch
    {
      OracleToken.When => TriggerTiming.When,
      OracleToken.Whenever => TriggerTiming.Whenever,
      OracleToken.At => TriggerTiming.At,
      _ => null,
    };

  /// <summary>
  /// Splits the clause into trigger and effect parts at the comma that opens
  /// the resolution sentence. Trigger conditions may contain internal commas
  /// (e.g. "you cast a spell that's white, blue, black, or red, put..."), so
  /// a naive first-comma split misclassifies the colour list. We instead pick
  /// the latest comma whose right-hand side begins with a verb consistent with
  /// an effect description (or "if" / "you may", which introduce conditional
  /// effects). This still bottoms out on the first comma if no later candidate
  /// looks like an effect — matching the previous behaviour for simple shapes.
  /// </summary>
  private static (string Trigger, string Effect)? SplitTriggerAndEffect(string text)
  {
    var firstComma = text.IndexOf(',');
    if (firstComma < 0)
    {
      return null;
    }

    var commaIndices = new List<int>();
    for (var i = 0; i < text.Length; i++)
    {
      if (text[i] == ',')
      {
        commaIndices.Add(i);
      }
    }

    // Trigger condition is, by oracle convention, an unbroken
    // prepositional/imperative phrase up to the resolution clause. Sentence
    // boundaries inside the effect half (e.g. Niambi's "...to its owner's
    // hand. If you do, ...") shouldn't lift commas past them into the
    // trigger. Cap the search at the first period if one exists.
    int searchLimit = text.Length;
    var firstPeriod = text.IndexOf('.');
    if (firstPeriod >= 0)
    {
      searchLimit = firstPeriod;
    }

    // Scan commas left-to-right for the first one whose tail begins with an
    // effect verb. Left-to-right keeps the trigger phrase tight — picking the
    // last effect-flavoured comma would let trigger conditions absorb the
    // entire first resolution sentence.
    foreach (var i in commaIndices)
    {
      if (i >= searchLimit)
      {
        break;
      }
      var tail = text[(i + 1)..].TrimStart();
      if (LooksLikeEffectStart(tail))
      {
        return (text[..i].Trim(), tail);
      }
    }

    // Fallback: first comma split.
    return (text[..firstComma].Trim(), text[(firstComma + 1)..].Trim());
  }

  /// <summary>
  /// True if <paramref name="tail"/> starts with one of the imperative verbs
  /// (or conditional prefixes) the effect-parser recognises. Used by
  /// <see cref="SplitTriggerAndEffect"/> to anchor the trigger/effect boundary
  /// past commas internal to the trigger condition.
  /// </summary>
  private static bool LooksLikeEffectStart(string tail)
  {
    string[] starters =
    [
      "put",
      "draw",
      "create",
      "scry",
      "look",
      "you may",
      "you gain",
      "you lose",
      "target",
      "each opponent",
      "each player",
      "destroy",
      "exile",
      "return",
      "untap",
      "tap",
      "deal",
      "mill",
      "discard",
      "if you",
      "if you do",
      "if",
      "this creature",
      "this permanent",
      "search",
      "shuffle",
      "reveal",
      "counter",
      "until end of turn",
      "for each",
      "surveil",
      "transform",
      "proliferate",
      "remove",
    ];
    foreach (var s in starters)
    {
      if (tail.StartsWith(s, StringComparison.OrdinalIgnoreCase))
      {
        // Require a following space or punctuation so "putrid" doesn't match "put".
        if (
          tail.Length == s.Length
          || char.IsWhiteSpace(tail[s.Length])
          || char.IsPunctuation(tail[s.Length])
        )
        {
          return true;
        }
      }
    }
    return false;
  }

  /// <summary>
  /// Parses the trigger condition (event + filter).
  /// Dispatches to specific event parsers based on keywords.
  /// </summary>
  private static TriggerCondition? ParseTriggerCondition(string triggerText, TriggerTiming timing)
  {
    var lower = triggerText.ToLowerInvariant();

    // Phase/step triggers (At-style): "At the beginning of your upkeep",
    // "At the beginning of your first main phase", etc.
    if (timing == TriggerTiming.At)
    {
      var phaseTrigger = TryParsePhaseTrigger(lower, timing);
      if (phaseTrigger is not null)
      {
        return phaseTrigger;
      }
    }

    // Spell-cast trigger: "Whenever [filter] cast(s) a spell..."
    if (lower.Contains("cast") && lower.Contains("spell"))
    {
      var spellCast = TryParseSpellCastTrigger(triggerText, timing);
      if (spellCast is not null)
      {
        return spellCast;
      }
    }

    // Life-change triggers
    if (lower.Contains("gain") && lower.Contains("life"))
    {
      var gainsLife = TryParseGainsLifeTrigger(triggerText, timing);
      if (gainsLife is not null)
      {
        return gainsLife;
      }
    }

    // Draw-card trigger: "Whenever you draw a card"
    if (lower.Contains("draw") && lower.Contains("card"))
    {
      var drawCard = TryParseDrawCardTrigger(triggerText, timing);
      if (drawCard is not null)
      {
        return drawCard;
      }
    }

    // Scry-or-surveil trigger: "Whenever you scry or surveil"
    if (lower.Contains("scry") || lower.Contains("surveil"))
    {
      var scryOrSurveil = TryParseScryOrSurveilTrigger(triggerText, timing);
      if (scryOrSurveil is not null)
      {
        return scryOrSurveil;
      }
    }

    // Crewing trigger: "Whenever this Vehicle becomes crewed..."
    if (lower.Contains("becomes crewed"))
    {
      var crew = TryParseBecomesCrewedTrigger(triggerText, timing);
      if (crew is not null)
      {
        return crew;
      }
    }

    // Compound "enters or dies" trigger — must be checked BEFORE the individual
    // "dies" and "enters" branches so the disjunction isn't split and
    // misclassified as a plain Dies or Enters event.
    // Rule 603: "When this creature enters or dies, [effect]." fires on either
    // zone-change event. The oracle text for this pattern always has the exact
    // phrase "[subject] enters or dies" (never "dies or enters").
    if (lower.Contains("enters or dies"))
    {
      return ParseEntersOrDiesTrigger(triggerText, timing);
    }

    // Try different trigger event types
    // "dies" is modern oracle. Old oracle uses the longform "is put into a graveyard
    // from the battlefield" (Rule 700.4 — "dies" means exactly this). Both phrasings
    // describe the same game event; normalise to the same TriggerEvent.Dies branch.
    if (lower.Contains("dies") || lower.Contains("is put into a graveyard from the battlefield"))
    {
      return ParseDiesTrigger(triggerText, timing);
    }

    if (lower.Contains("enters"))
    {
      return ParseEntersTrigger(triggerText, timing);
    }

    // AttacksOrBlocks trigger: "Whenever [subject] attacks or blocks" — combined combat trigger
    // (Rule 508/509). The disjunction is a single oracle clause; emit AttacksOrBlocks so the
    // two events aren't modelled as separate triggers. Must be tested before the individual
    // Attacks/Blocks branches so "attacks or blocks" doesn't partially match "attacks".
    if (lower.Contains("attacks or blocks"))
    {
      var attacksOrBlocks = TryParseAttacksOrBlocksTrigger(triggerText, timing);
      if (attacksOrBlocks is not null)
      {
        return attacksOrBlocks;
      }
    }

    // Attack-with trigger: "Whenever you attack with [Name] and another [qualifier] creature"
    // (Merry shape). Emits TriggerEvent.Attacks with the companion-creature filter.
    if (lower.Contains("attack") && lower.Contains("another"))
    {
      var attackWith = TryParseAttackWithAndAnotherTrigger(triggerText, timing);
      if (attackWith is not null)
      {
        return attackWith;
      }
    }

    // Attack trigger: "Whenever [CardName] attacks" / "Whenever a creature you control attacks".
    // Covers self-by-name and controller-filter shapes (Rule 508 — Declare Attackers).
    if (lower.Contains("attacks"))
    {
      var attacks = TryParseAttacksTrigger(triggerText, timing);
      if (attacks is not null)
      {
        return attacks;
      }
    }

    // Block trigger: "Whenever this creature blocks" / "Whenever [CardName] blocks" /
    // "Whenever this creature blocks a creature" — Rule 509 (Declare Blockers Step).
    // Fires when the named or self-referencing creature is declared as a blocker.
    // Exclude "becomes blocked" (BecomesBlocked event) which also contains the word "blocks".
    if (lower.Contains("blocks") && !lower.Contains("becomes blocked"))
    {
      var blocks = TryParseBlocksTrigger(triggerText, timing);
      if (blocks is not null)
      {
        return blocks;
      }
    }

    // DealsCombatDamageToPlayer trigger: "Whenever this creature deals combat damage to a player".
    // Combat damage step: Rule 510 (Combat Damage Step). A triggered ability with
    // TriggerEvent.DealsCombatDamageToPlayer fires whenever the named source deals
    // combat damage to a player (Rule 603.6). The recipient class ("a player",
    // "an opponent", "any player") is implied by the enum value; the Filter captures
    // the subject (what is dealing the damage). Rule 122 (counters) governs the
    // typical effect — put a +1/+1 counter on it.
    if (lower.Contains("deals combat damage"))
    {
      var combatDamage = TryParseDealsCombatDamageTrigger(triggerText, timing);
      if (combatDamage is not null)
      {
        return combatDamage;
      }
    }

    // DealsDamage trigger: "Whenever this creature deals damage" — any damage, not just combat.
    // Rule 120: Damage. A source deals damage to a permanent or player; this trigger fires
    // on any instance of that source dealing damage regardless of source type (combat or
    // noncombat). This is the oracle pattern for lifelink-analog abilities printed before
    // the lifelink keyword existed (e.g. "Whenever this creature deals damage, you gain
    // that much life."). The "combat damage" variant is handled above — check only after
    // the combat-specific branch has failed to match.
    if (lower.Contains("deals damage") && !lower.Contains("deals combat damage"))
    {
      var dealsDamage = TryParseDealsDamageTrigger(triggerText, timing);
      if (dealsDamage is not null)
      {
        return dealsDamage;
      }
    }

    // BecomesTarget trigger: "When this creature becomes the target of a spell or ability".
    // Triggered-ability machinery: Rule 603.1-603.2. The "becomes the target" relationship
    // is defined in Rule 115.1 (Targets — an object becomes a target when a spell or ability
    // that targets it is put on the stack). The subject is the source creature (this creature).
    if (lower.Contains("becomes the target"))
    {
      var becomesTarget = TryParseBecomesTargetTrigger(triggerText, timing);
      if (becomesTarget is not null)
      {
        return becomesTarget;
      }
    }

    // TurnedFaceUp trigger: "When this creature is turned face up" — Rule 702.37 (Morph/Megamorph).
    // Turning a permanent face up is the keyword action defined in Rule 702.37e. The trigger fires
    // when the morph or megamorph cost is paid and the card flips from its face-down state. The
    // subject is always the source permanent ("this creature"), so no subtype filter is needed on
    // the trigger itself; the filter captures only the self-reference.
    if (lower.Contains("turned face up"))
    {
      var turnedFaceUp = TryParseTurnedFaceUpTrigger(triggerText, timing);
      if (turnedFaceUp is not null)
      {
        return turnedFaceUp;
      }
    }

    return null;
  }

  /// <summary>
  /// "Whenever you attack with [CardName] and another [qualifier*] creature" —
  /// models the companion-attack trigger (Rule 508). The card name collapses to
  /// <c>Self</c> per the card-name-as-subject convention. The companion
  /// creature filter carries the "another" characteristic plus any supertype
  /// or card-type qualifiers from the oracle text.
  /// </summary>
  private static TriggerCondition? TryParseAttackWithAndAnotherTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    // Pattern: "Whenever you attack with [Name] and another [adj] creature"
    // The [adj] group captures optional qualifiers like "legendary" before "creature".
    var match = Regex.Match(
      triggerText,
      @"^\s*(?:Whenever\s+)?you\s+attack\s+with\s+\S.*?\s+and\s+another\s+(?<adj>[\w\s]+?)\s+creature\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var adjText = match.Groups["adj"].Value.Trim().ToLowerInvariant();

    // Build companion-creature filter. "legendary" maps to Supertypes;
    // unrecognised qualifiers fall through to null (bail).
    List<string>? supertypes = null;
    List<string>? characteristics = null;

    if (!string.IsNullOrWhiteSpace(adjText))
    {
      // Check for supertypes recognised in oracle text.
      var knownSupertypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
      {
        "legendary",
        "basic",
        "snow",
        "world",
      };
      if (knownSupertypes.Contains(adjText))
      {
        supertypes = [adjText.Substring(0, 1).ToUpperInvariant() + adjText.Substring(1).ToLowerInvariant()];
      }
      else
      {
        // Unrecognised qualifier — bail so the fallback path records the gap.
        return null;
      }
    }

    // "another" is a characteristic exclusion (excludes the source creature).
    characteristics = ["another"];

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = new ObjectFilter
      {
        Supertypes = supertypes,
        Controller = ControllerFilter.You,
        Characteristics = characteristics,
      },
    };
  }

  /// <summary>
  /// "Whenever [CardName] attacks" — self-by-name attack trigger (Rule 508).
  /// "Whenever this creature attacks" — anonymous self-reference attack trigger.
  /// "Whenever a creature you control attacks" — controller-filter attack trigger.
  /// All emit <see cref="TriggerEvent.Attacks"/>. The filter shape is shared with
  /// dies/enters via <see cref="ParseObjectFilter"/>, which already handles
  /// "this creature", self-by-name, and the "a creature [you control]" shapes
  /// uniformly.
  /// </summary>
  private static TriggerCondition? TryParseAttacksTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Attacks,
      Filter = filter,
    };
  }

  /// <summary>
  /// "Whenever this creature blocks" / "Whenever [CardName] blocks" /
  /// "Whenever this creature blocks a creature" — Rule 509 (Declare Blockers Step).
  /// Fires when the named creature is declared as a blocker. The optional
  /// "blocks a creature" qualifier is descriptive (all blockers block creatures);
  /// it doesn't alter the trigger semantics, so the filter is the same either way.
  /// </summary>
  private static TriggerCondition? TryParseBlocksTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Blocks,
      Filter = filter,
    };
  }

  /// <summary>
  /// "Whenever [subject] attacks or blocks" — combined attack/block trigger (Rule 508/509).
  /// The disjunction is modelled as a single <see cref="TriggerEvent.AttacksOrBlocks"/> event
  /// rather than two separate triggers; oracle text always prints the conjunction as a single line.
  /// Subject shapes are the same as the individual Attacks/Blocks triggers.
  /// </summary>
  private static TriggerCondition? TryParseAttacksOrBlocksTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    // Strip "or blocks" so ParseObjectFilter sees a clean "attacks" subject phrase.
    var stripped = Regex.Replace(
      triggerText,
      @"\s+or\s+blocks\b",
      string.Empty,
      RegexOptions.IgnoreCase
    );
    var filter = ParseObjectFilter(stripped);
    if (filter == null)
    {
      // Also try self-by-name with the "attacks or blocks" verb form.
      if (!IsSelfByNameAttacksOrBlocksTrigger(triggerText))
      {
        return null;
      }
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.AttacksOrBlocks,
      Filter = filter,
    };
  }

  /// <summary>
  /// Detects the "[CardName] attacks or blocks" self-by-name shape, extending
  /// <see cref="IsSelfByNameTrigger"/> to cover the combined verb "attacks or blocks".
  /// </summary>
  private static bool IsSelfByNameAttacksOrBlocksTrigger(string triggerText)
  {
    var stripped = Regex.Replace(
      triggerText.Trim(),
      @"^(When|Whenever|At)\s+",
      string.Empty,
      RegexOptions.IgnoreCase
    );
    const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";
    return Regex.IsMatch(
      stripped,
      @"^[A-Z][A-Za-z'\-]*(?:\s+(?:[A-Z][A-Za-z'\-]*|" + FunctionWords + @"))*\s+attacks\s+or\s+blocks\b",
      RegexOptions.CultureInvariant
    );
  }

  /// <summary>
  /// "Whenever [subject] deals combat damage to (a player|an opponent|any player)" —
  /// emits <see cref="TriggerEvent.DealsCombatDamageToPlayer"/> (Rule 510 — Combat
  /// Damage Step; Rule 603.6 — triggered abilities). The recipient class is implied
  /// by the enum value; the Filter captures the subject (what is dealing the damage).
  /// </summary>
  private static TriggerCondition? TryParseDealsCombatDamageTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();

    // Require a player-class recipient: "to a player", "to an opponent", "to any player".
    if (
      !lower.Contains("to a player")
      && !lower.Contains("to an opponent")
      && !lower.Contains("to any player")
    )
    {
      return null;
    }

    // Subject is the thing doing the dealing: "this creature", self-by-name, etc.
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsCombatDamageToPlayer,
      Filter = filter,
    };
  }

  /// <summary>
  /// "Whenever [subject] deals damage" — any damage (Rule 120), not only combat damage.
  /// Emits <see cref="TriggerEvent.DealsDamage"/>. The subject (filter) captures the
  /// source dealing damage — typically "this creature" (CardTypes = ["creature"]).
  /// This pattern covers lifelink-analog oracle text printed before the lifelink keyword
  /// was introduced ("Whenever this creature deals damage, you gain that much life.").
  /// </summary>
  private static TriggerCondition? TryParseDealsDamageTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();

    // Must contain "deals damage" but must NOT contain "deals combat damage"
    // (the combat variant is handled by TryParseDealsCombatDamageTrigger above).
    if (!lower.Contains("deals damage") || lower.Contains("deals combat damage"))
    {
      return null;
    }

    // Subject is the thing doing the dealing: "this creature", self-by-name, etc.
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DealsDamage,
      Filter = filter,
    };
  }

  /// <summary>
  /// "At the beginning of your upkeep" / "At the beginning of your first main phase" /
  /// "At the beginning of your draw step" / "At the beginning of your end step" /
  /// "At the beginning of combat on your turn".
  /// Maps the phase/step word to a <see cref="TriggerEvent"/>. The "your" /
  /// "each player's" possessive lands on the filter as a <c>Controller</c>.
  /// </summary>
  private static TriggerCondition? TryParsePhaseTrigger(string lower, TriggerTiming timing)
  {
    if (!lower.Contains("beginning of"))
    {
      return null;
    }

    TriggerEvent? evt = null;
    if (lower.Contains("upkeep"))
    {
      evt = TriggerEvent.BeginningOfUpkeep;
    }
    else if (lower.Contains("first main phase") || lower.Contains("precombat main phase"))
    {
      evt = TriggerEvent.BeginningOfPreCombatMainPhase;
    }
    else if (lower.Contains("postcombat main phase") || lower.Contains("second main phase"))
    {
      evt = TriggerEvent.BeginningOfPostCombatMainPhase;
    }
    else if (lower.Contains("draw step"))
    {
      evt = TriggerEvent.BeginningOfDrawStep;
    }
    else if (lower.Contains("end step"))
    {
      evt = TriggerEvent.BeginningOfEndStep;
    }
    else if (lower.Contains("combat"))
    {
      evt = TriggerEvent.BeginningOfCombat;
    }

    if (evt is null)
    {
      return null;
    }

    // Possessive cue determines the filter's controller axis. "your" → You,
    // "each opponent's" → Opponent, "each player's" → no filter (universal).
    ObjectFilter? filter = null;
    if (lower.Contains("your"))
    {
      filter = new ObjectFilter { Controller = ControllerFilter.You };
    }
    else if (lower.Contains("each opponent"))
    {
      filter = new ObjectFilter { Controller = ControllerFilter.Opponent };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = evt.Value,
      Filter = filter,
    };
  }

  /// <summary>
  /// "Whenever you cast a spell" / "Whenever an opponent casts a spell" /
  /// "Whenever you cast a spell that's white, blue, black, or red" / etc.
  /// Encodes the caster and any inline spell-color/type qualifiers on the filter.
  /// </summary>
  private static TriggerCondition? TryParseSpellCastTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    // Recognize "[subject] cast(s) [some] spell ..."
    if (!Regex.IsMatch(triggerText, @"\bcasts?\b", RegexOptions.IgnoreCase))
    {
      return null;
    }
    if (!Regex.IsMatch(triggerText, @"\bspell\b", RegexOptions.IgnoreCase))
    {
      return null;
    }

    var lower = triggerText.ToLowerInvariant();

    // Caster (controller filter)
    ControllerFilter? controller = null;
    if (Regex.IsMatch(lower, @"\b(you|an?\s+opponent|an?\s+player)\b"))
    {
      controller = lower.Contains("opponent")
        ? ControllerFilter.Opponent
        : (lower.Contains("you") ? ControllerFilter.You : null);
    }

    // Card-type qualifiers on the cast spell ("creature spell", "noncreature spell", etc.)
    var characteristics = new List<string>();

    // "instant or sorcery spell" — combined disjunction (Rule 700.4). Must be
    // detected before the per-word loop so both halves are captured: the loop
    // pattern \binstant\s+spell\b won't match when "or sorcery" sits between the
    // two words, and \bsorcery\s+spell\b would only capture the second half.
    if (Regex.IsMatch(lower, @"\binstant\s+or\s+sorcery\s+spell\b"))
    {
      characteristics.Add("instant");
      characteristics.Add("sorcery");
    }
    else
    {
      foreach (var word in new[] { "creature", "noncreature", "instant", "sorcery", "artifact", "enchantment" })
      {
        if (Regex.IsMatch(lower, $@"\b{Regex.Escape(word)}\s+spell\b"))
        {
          characteristics.Add(word);
        }
      }
    }

    // Color qualifiers: "that's white" / "that's white, blue, black, or red" /
    // "white spell" etc. Look for any colour word in the trigger fragment.
    var colors = new List<string>();
    var colorMap = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMap)
    {
      if (Regex.IsMatch(lower, $@"\b{name}\b"))
      {
        colors.Add(code);
      }
    }

    // "this spell from anywhere other than exile" — Rory Williams shape.
    // Encode the qualifier on Characteristics so the trigger remains a
    // structured filter rather than a free-text condition.
    if (lower.Contains("this spell from anywhere other than exile"))
    {
      characteristics.Add("this spell from anywhere other than exile");
    }

    // Heroic ability-word (Rule 702.108): "Whenever you cast a spell that targets
    // this creature, ...". The "that targets this creature" clause is the
    // Heroic-specific constraint on the cast spell (Rule 115.1 — targeted spells).
    // Model it as a Characteristic so the trigger filter describes the oracle text
    // faithfully without introducing engine targeting semantics.
    if (Regex.IsMatch(lower, @"\bthat\s+targets?\s+this\s+(creature|permanent|card)\b"))
    {
      characteristics.Add("targeting this creature");
    }

    // Multicolored qualifier: "a multicolored spell" — Rule 105.5 ("An object is
    // multicolored if it has two or more colors"). Encoded on IsMulticolored rather
    // than Colors (which encodes "has any of these colors") to preserve the
    // two-or-more constraint faithfully.
    bool? isMulticolored = null;
    if (Regex.IsMatch(lower, @"\bmulticolored\b"))
    {
      isMulticolored = true;
    }

    // Build filter. Suppress CardTypes=["spell"] when no qualifiers were
    // detected and the controller is non-You (matches RhysticStudy's gold).
    var hasAnyQualifier = characteristics.Count > 0 || colors.Count > 0 || isMulticolored == true;
    IReadOnlyList<string>? cardTypes = hasAnyQualifier ? new List<string> { "spell" } : null;

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors.Count > 0 ? colors : null,
      IsMulticolored = isMulticolored,
      Controller = controller,
    };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = filter,
    };
  }

  /// <summary>
  /// "Whenever you gain life" — life-gain trigger. Controller defaults to You.
  /// </summary>
  private static TriggerCondition? TryParseGainsLifeTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"\b(you|opponent|a player)\s+gain(s)?\s+life\b"))
    {
      return null;
    }

    ControllerFilter controller = lower.Contains("opponent")
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.GainsLife,
      Filter = new ObjectFilter { Controller = controller },
    };
  }

  /// <summary>
  /// "Whenever you scry or surveil" — fires on either keyword action (Rule 701.18 / 701.43).
  /// </summary>
  private static TriggerCondition? TryParseScryOrSurveilTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"\byou\s+scry\s+or\s+surveil\b"))
    {
      return null;
    }
    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.ScryOrSurveil,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }

  /// <summary>
  /// "Whenever you draw a card" — draw-card trigger (Rule 121: Drawing a Card).
  /// Fires whenever the controller draws a card by any means. Controller defaults to You.
  /// </summary>
  private static TriggerCondition? TryParseDrawCardTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"\byou\s+draw\s+a\s+card\b"))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.DrawsCard,
      Filter = new ObjectFilter { Controller = ControllerFilter.You },
    };
  }

  /// <summary>
  /// "Whenever this Vehicle becomes crewed [for the first time each turn]" — Rule 702.122 trigger.
  /// </summary>
  private static TriggerCondition? TryParseBecomesCrewedTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!lower.Contains("becomes crewed"))
    {
      return null;
    }

    // Filter expresses the subject. "this Vehicle" is the common shape.
    ObjectFilter? filter = null;
    if (lower.Contains("this vehicle"))
    {
      filter = new ObjectFilter { Characteristics = ["this Vehicle"] };
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesCrewed,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses "dies" triggers.
  /// Supports: "this creature dies", "a creature dies", "another creature dies", etc.
  /// </summary>
  private static TriggerCondition? ParseDiesTrigger(string triggerText, TriggerTiming timing)
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses "enters" triggers.
  /// Supports: "this creature enters", "a creature enters", etc.
  /// </summary>
  private static TriggerCondition? ParseEntersTrigger(string triggerText, TriggerTiming timing)
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses "enters or dies" compound triggers — fires on either zone-change event.
  /// Rule 603: "When [subject] enters or dies, [effect]."
  /// The oracle phrase "enters or dies" (always in that order) denotes a single triggered
  /// ability that watches for the permanent either entering the battlefield or dying.
  /// Subject filter is resolved via <see cref="ParseObjectFilter"/>, which handles
  /// "this creature", self-by-name, and generic creature shapes uniformly.
  /// </summary>
  private static TriggerCondition? ParseEntersOrDiesTrigger(string triggerText, TriggerTiming timing)
  {
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.EntersOrDies,
      Filter = filter,
    };
  }

  /// <summary>
  /// Parses object filters from trigger text.
  /// Compositional component used by multiple trigger types.
  /// </summary>
  private static ObjectFilter? ParseObjectFilter(string text)
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
        // Vehicle, Spacecraft; enchantment subtypes include Aura. Each is
        // treated descriptively as a CardTypes singleton; MAST records the
        // word the text used, not the resolved card-type hierarchy.
        "aura",
        "equipment",
        "vehicle",
        "spacecraft",
      }
    )
    {
      if (Regex.IsMatch(lower, $@"\bthis\s+{selfType}\b"))
      {
        return new ObjectFilter { CardTypes = [selfType], Controller = controller };
      }
    }

    // "enchanted creature" — the creature to which this Aura is currently attached.
    // Rule 303.4c: an Aura's "enchanted [type]" refers to the permanent it's attached to.
    // The "enchanted" characteristic is recorded as a Characteristics entry on the filter;
    // the card type remains "creature" per the oracle text. This is descriptive — MAST
    // records the oracle word, not a resolved attachment reference.
    if (Regex.IsMatch(lower, @"\bthe\s+enchanted\s+creature\b") || Regex.IsMatch(lower, @"\benchanted\s+creature\b"))
    {
      return new ObjectFilter { CardTypes = ["creature"], Characteristics = ["enchanted"] };
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
      return new ObjectFilter { CardTypes = ["creature"], Controller = controller };
    }

    return null;
  }

  /// <summary>
  /// "When this creature becomes the target of a spell or ability" —
  /// triggered-ability machinery: Rule 603.1-603.2; "becomes the target"
  /// relationship: Rule 115.1. The subject "this creature" is the source permanent;
  /// the filter carries the subject's card type. Only the self-reference shape
  /// is modelled here; third-party-target shapes ("whenever target creature
  /// becomes the target...") are out of scope for this surface.
  /// </summary>
  private static TriggerCondition? TryParseBecomesTargetTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!lower.Contains("becomes the target"))
    {
      return null;
    }

    // Subject is "this [type]"; delegate to the shared self-reference helper.
    var filter = ParseObjectFilter(triggerText);
    if (filter == null)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesTarget,
      Filter = filter,
    };
  }

  /// <summary>
  /// "When this creature is turned face up" — Rule 702.37 (Morph/Megamorph) face-up trigger.
  /// The trigger fires when the face-down permanent's morph or megamorph cost is paid and
  /// the card becomes face up (Rule 702.37e/f). Subject is always "this creature" — the
  /// permanent whose triggered ability this is.
  /// </summary>
  private static TriggerCondition? TryParseTurnedFaceUpTrigger(
    string triggerText,
    TriggerTiming timing
  )
  {
    var lower = triggerText.ToLowerInvariant();
    if (!lower.Contains("turned face up"))
    {
      return null;
    }

    // Subject filter: "this creature" is the only oracle shape for this trigger.
    var filter = new ObjectFilter { CardTypes = ["creature"] };

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.TurnedFaceUp,
      Filter = filter,
    };
  }

  /// <summary>
  /// Detects the "[Self-name] enters/dies/attacks" shape, where the card refers
  /// to itself by its own name rather than by "this creature". The heuristic:
  /// after stripping the leading trigger timing keyword, the remaining trigger
  /// text begins with one or more name-words and ends with a recognized event
  /// verb. Name-words are either capitalised content words (e.g. "Goblin",
  /// "Chieftain") or lowercase function words that legally appear in MTG card
  /// names ("of", "the", "a", "an", "from", "for", "to", "in", "at", "with").
  /// The parser does not have access to the card name at this point, so this is
  /// a structural match, not a name-equality check.
  /// </summary>
  private static bool IsSelfByNameTrigger(string triggerText)
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
    const string FunctionWords = "of|the|a|an|from|for|to|in|at|with|by|and|or|as";
    return Regex.IsMatch(
      stripped,
      @"^[A-Z][A-Za-z'\-]*(?:\s+(?:[A-Z][A-Za-z'\-]*|" + FunctionWords + @"))*\s+(enters\s+or\s+dies|enters|dies|attacks|blocks)\b",
      RegexOptions.CultureInvariant
    );
  }

  #endregion

  #region Effect Parsing — Dispatcher

  /// <summary>
  /// Per-rule entry for dispatch and telemetry attribution.
  /// </summary>
  private readonly record struct RuleEntry(Triggered.ITriggeredRule Rule, string Name, int Priority);

  private static readonly Lazy<IReadOnlyList<RuleEntry>> _rules =
    new(DiscoverRules, LazyThreadSafetyMode.ExecutionAndPublication);

  private static IReadOnlyList<RuleEntry> DiscoverRules()
  {
    var assembly = typeof(TriggeredAbilityParser).Assembly;
    var found = new List<RuleEntry>();
    foreach (var type in assembly.GetTypes())
    {
      var attr = type.GetCustomAttribute<Triggered.TriggeredRuleAttribute>(inherit: false);
      if (attr is null)
      {
        continue;
      }
      if (!typeof(Triggered.ITriggeredRule).IsAssignableFrom(type))
      {
        throw new InvalidOperationException(
          $"{type.FullName} has [TriggeredRule] but does not implement ITriggeredRule."
        );
      }
      var instance = (Triggered.ITriggeredRule?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException(
          $"Failed to instantiate {type.FullName} (parameterless constructor required)."
        );
      found.Add(new RuleEntry(instance, $"TriggeredAbilityParser.{type.Name}", attr.Priority));
    }
    return found
      .OrderByDescending(r => r.Priority)
      .ThenBy(r => r.Name, StringComparer.Ordinal)
      .ToList();
  }

  /// <summary>
  /// Multi-effect dispatch. Tries the two composite-effect orchestration paths
  /// first (Sanctum and Stormfist shapes), then dispatches to single-effect rules
  /// via reflection-discovered <see cref="Triggered.ITriggeredRule"/> implementations.
  /// </summary>
  private static IReadOnlyList<Effect>? ParseEffects(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    var loseGainLifeWhereX = TryParseLoseAndGainLifeWhereX(trimmed);
    if (loseGainLifeWhereX is not null)
    {
      return loseGainLifeWhereX;
    }

    var opponentLoseYouGain = TryParseEachOpponentLoseAndYouGainLife(trimmed);
    if (opponentLoseYouGain is not null)
    {
      return opponentLoseYouGain;
    }

    var drawAndLose = TryParseEachPlayerDrawAndLoseLife(trimmed);
    if (drawAndLose is not null)
    {
      return drawAndLose;
    }

    // "create a P1/T1 color1 sub1 creature token, a P2/T2 color2 sub2 creature token,
    // and a P3/T3 color3 sub3 creature token" — multi-token composite triggered
    // effect (Rule 111). Tried before the single-rule loop so the comma-separated
    // series isn't mismatched by the single-token CreateTokenRule.
    var multiToken = TryParseCompositeCreateTokens(trimmed);
    if (multiToken is not null)
    {
      return multiToken;
    }

    foreach (var entry in _rules.Value)
    {
      if (entry.Rule.TryMatch(trimmed, out var effect) && effect is not null)
      {
        return new List<Effect> { effect };
      }
    }

    return null;
  }

  // One token spec: "a P/T color subtype creature token" (article + P/T + color + subtype).
  private static readonly Regex _singleTokenSpec = new(
    @"(?:a|an|\d+)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+token",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, string> _triggeredColorMap =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
    };

  /// <summary>
  /// "create [spec1], [spec2], and [spec3]" — comma-separated multi-token
  /// creation. Each spec is "a P/T color subtype creature token". Wraps the
  /// individual <see cref="CreateTokenEffect"/> nodes in a single
  /// <see cref="CompositeEffect"/> matching the gold for Somberwald Beastmaster.
  /// Returns <see langword="null"/> for any other shape (single token or
  /// non-create text) so the single-rule dispatch below handles it.
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseCompositeCreateTokens(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    if (!lower.StartsWith("create"))
    {
      return null;
    }

    var specs = _singleTokenSpec.Matches(effectText);
    if (specs.Count < 2)
    {
      // Single token — let the existing CreateTokenRule handle it.
      return null;
    }

    var creates = new List<Effect>();
    foreach (System.Text.RegularExpressions.Match spec in specs)
    {
      if (!_triggeredColorMap.TryGetValue(spec.Groups["color"].Value, out var colorCode))
      {
        return null;
      }
      var subtype = spec.Groups["subtype"].Value;
      subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..];

      creates.Add(new CreateTokenEffect
      {
        Count = LiteralQuantity.Of(1),
        Token = new TokenDefinition
        {
          Power = spec.Groups["power"].Value,
          Toughness = spec.Groups["toughness"].Value,
          Colors = [colorCode],
          Types = ["creature"],
          Subtypes = [subtype],
          IsCopy = false,
        },
        IsOptional = false,
      });
    }

    return new List<Effect>
    {
      new CompositeEffect
      {
        Effects = creates,
        IsOptional = false,
      },
    };
  }

  /// <summary>
  /// Drain composite: "each opponent loses N life and you gain N life." —
  /// the Zulaport Cutthroat / Drana's Emissary family. Returns a flat
  /// two-element list [loseLife(EachOpponent, N), gainLife(You, N)].
  ///
  /// <para>
  /// Rule 119.3: losing life reduces the life total. Rule 119.7: gaining life
  /// increases it. The two effects share the same literal amount N. Simultaneous
  /// resolution ordering in multiplayer is engine territory.
  /// </para>
  /// </summary>
  internal static IReadOnlyList<Effect>? TryParseEachOpponentLoseAndYouGainLife(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^each\s+opponent\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\s+and\s+you\s+gain\s+(?<gain>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    static int ParseAmount(string raw) => raw.ToLowerInvariant() switch
    {
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
      _ => int.Parse(raw),
    };
    var loseAmount = ParseAmount(match.Groups["amount"].Value);
    var gainAmount = ParseAmount(match.Groups["gain"].Value);
    return new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = LiteralQuantity.Of(loseAmount),
        Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      },
      new GainLifeEffect
      {
        Amount = LiteralQuantity.Of(gainAmount),
        Player = ObjectReference.You(),
      },
    };
  }

  /// <summary>
  /// Stormfist-style composite: "each player draws a card and loses N life."
  /// Returns the gold's flat two-element list (drawCards, loseLife).
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseEachPlayerDrawAndLoseLife(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^each\s+player\s+draws\s+(?<draw>a|one|two|three|\d+)\s+cards?\s+and\s+loses\s+(?<life>\d+|one|two|three)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var drawRaw = match.Groups["draw"].Value.ToLowerInvariant();
    var lifeRaw = match.Groups["life"].Value.ToLowerInvariant();
    int drawCount = drawRaw switch
    {
      "a" or "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.Parse(drawRaw),
    };
    int lifeCount = lifeRaw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.Parse(lifeRaw),
    };
    var eachPlayer = new ObjectReference { Kind = ObjectReferenceKind.EachPlayer };
    return new List<Effect>
    {
      new DrawCardsEffect { Count = LiteralQuantity.Of(drawCount), Player = eachPlayer },
      new LoseLifeEffect { Amount = LiteralQuantity.Of(lifeCount), Player = eachPlayer },
    };
  }

  /// <summary>
  /// Sanctum-style composite: "each opponent loses X life and you gain X life,
  /// where X is the number of [filter]". Encodes both halves as
  /// <see cref="CountQuantity"/> sharing the same <c>CountOf</c> description.
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseLoseAndGainLifeWhereX(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^each\s+opponent\s+loses\s+X\s+life\s+and\s+you\s+gain\s+X\s+life,\s*where\s+X\s+is\s+the\s+number\s+of\s+(?<count>.+)$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var countOf = match.Groups["count"].Value.Trim();
    return new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = new CountQuantity { CountOf = countOf },
        Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      },
      new GainLifeEffect
      {
        Amount = new CountQuantity { CountOf = countOf },
        Player = ObjectReference.You(),
      },
    };
  }

  #endregion
}
