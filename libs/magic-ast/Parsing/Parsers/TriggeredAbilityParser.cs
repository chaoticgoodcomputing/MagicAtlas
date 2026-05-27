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

    // Try different trigger event types
    if (lower.Contains("dies"))
    {
      return ParseDiesTrigger(triggerText, timing);
    }

    if (lower.Contains("enters"))
    {
      return ParseEntersTrigger(triggerText, timing);
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
    foreach (var word in new[] { "creature", "noncreature", "instant", "sorcery", "artifact", "enchantment" })
    {
      if (Regex.IsMatch(lower, $@"\b{Regex.Escape(word)}\s+spell\b"))
      {
        characteristics.Add(word);
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

    // Build filter. Suppress CardTypes=["spell"] when no qualifiers were
    // detected and the controller is non-You (matches RhysticStudy's gold).
    var hasAnyQualifier = characteristics.Count > 0 || colors.Count > 0;
    IReadOnlyList<string>? cardTypes = hasAnyQualifier ? new List<string> { "spell" } : null;

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Colors = colors.Count > 0 ? colors : null,
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
  /// Parses object filters from trigger text.
  /// Compositional component used by multiple trigger types.
  /// </summary>
  private static ObjectFilter? ParseObjectFilter(string text)
  {
    var lower = text.ToLowerInvariant();

    // Possessive cue: any "you control" / "an opponent controls" qualifier
    // lands on the filter's Controller axis. Applies on top of card-type
    // matching below.
    ControllerFilter? controller = null;
    if (Regex.IsMatch(lower, @"\byou\s+control\b"))
    {
      controller = ControllerFilter.You;
    }
    else if (Regex.IsMatch(lower, @"\ban\s+opponent\s+controls\b"))
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
        // Subtype self-reference: "this Aura" — oracle text uses the
        // subtype word when the card has no relevant supertype/cardtype
        // distinction (e.g., Aura enchantments). Treated descriptively
        // as a CardTypes singleton; MAST records the word the text used.
        "aura",
      }
    )
    {
      if (Regex.IsMatch(lower, $@"\bthis\s+{selfType}\b"))
      {
        return new ObjectFilter { CardTypes = [selfType], Controller = controller };
      }
    }

    if (lower.Contains("a creature") || lower.Contains("another creature"))
    {
      return new ObjectFilter { CardTypes = ["creature"], Controller = controller };
    }

    if (lower.Contains("a land") || lower.Contains("another land"))
    {
      return new ObjectFilter { CardTypes = ["land"], Controller = controller };
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
  /// Detects the "[Self-name] enters/dies/attacks" shape, where the card refers
  /// to itself by its own name rather than by "this creature". The heuristic:
  /// after stripping the leading trigger timing keyword, the remaining trigger
  /// text begins with one or more capitalized words and ends with a recognized
  /// event verb. The parser does not currently have access to the card name, so
  /// this is a structural match, not a name-equality check.
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

    // Capitalized name (one or more comma-free words), then an event verb.
    return Regex.IsMatch(
      stripped,
      @"^[A-Z][A-Za-z'\-]*(?:\s+[A-Z][A-Za-z'\-]*)*\s+(enters|dies|attacks)\b",
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
