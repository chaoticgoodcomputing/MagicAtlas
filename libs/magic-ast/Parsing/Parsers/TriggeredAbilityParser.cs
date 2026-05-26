namespace MagicAST.Parsing.Parsers;

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
      };
    }

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
    };
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

    // Add more trigger types here as needed (attacks, etc.)

    return null;
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

  #region Effect Parsing

  /// <summary>
  /// Parses the effects portion of the triggered ability.
  /// Tries different effect parsers in sequence.
  /// </summary>
  private static IReadOnlyList<Effect>? ParseEffects(string effectText)
  {
    // Strip a trailing period for downstream pattern matchers.
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    // Composite phase-step effects: "each opponent loses X life and you gain X life,
    // where X is the number of <count>" — Sanctum-of-Stone-Fangs shape. Match these
    // before the single-effect parsers so the conjunction binds tightly.
    var loseGainLifeWhereX = TryParseLoseAndGainLifeWhereX(trimmed);
    if (loseGainLifeWhereX is not null)
    {
      return loseGainLifeWhereX;
    }

    // "each player draws a card and loses 1 life" / "each player draws N cards
    // and loses N life" — Stormfist Crusader's symmetric upkeep effect.
    var drawAndLose = TryParseEachPlayerDrawAndLoseLife(trimmed);
    if (drawAndLose is not null)
    {
      return drawAndLose;
    }

    // Try different effect types
    var effect = TryParseCreateTokenEffect(trimmed);
    if (effect != null)
    {
      return new List<Effect> { effect };
    }

    var scry = TryParseScryEffect(trimmed);
    if (scry != null)
    {
      return new List<Effect> { scry };
    }

    var addMana = TryParseAddManaEffect(trimmed);
    if (addMana != null)
    {
      return new List<Effect> { addMana };
    }

    var draw = TryParseDrawCardsEffect(trimmed);
    if (draw != null)
    {
      return new List<Effect> { draw };
    }

    var putCounters = TryParsePutCountersEffect(trimmed);
    if (putCounters != null)
    {
      return new List<Effect> { putCounters };
    }

    var untap = TryParseUntapSelfEffect(trimmed);
    if (untap != null)
    {
      return new List<Effect> { untap };
    }

    // "you may return ... to its owner's hand. If you do, you gain life
    // equal to that creature's mana value." — Niambi shape. Splits on the
    // first sentence boundary and parses the tail as an IfYouDo gain-life.
    // Tried before the plain return-to-hand so the IfYouDo doesn't get
    // discarded by the simpler matcher.
    var returnWithIfYouDo = TryParseReturnToHandWithIfYouDoGainLife(trimmed);
    if (returnWithIfYouDo != null)
    {
      return new List<Effect> { returnWithIfYouDo };
    }

    var returnHand = TryParseReturnToHandEffect(trimmed);
    if (returnHand != null)
    {
      return new List<Effect> { returnHand };
    }

    var loseLife = TryParseLoseLifeDerivedEffect(trimmed);
    if (loseLife != null)
    {
      return new List<Effect> { loseLife };
    }

    var dealDamage = TryParseSelfDealsDamageToYouEffect(trimmed);
    if (dealDamage != null)
    {
      return new List<Effect> { dealDamage };
    }

    var youLoseLife = TryParseYouLoseLifeEffect(trimmed);
    if (youLoseLife != null)
    {
      return new List<Effect> { youLoseLife };
    }

    return null;
  }

  /// <summary>
  /// "you lose N life" — straightforward life-loss effect (Deadpool's upkeep
  /// tax). Player defaults to You.
  /// </summary>
  private static LoseLifeEffect? TryParseYouLoseLifeEffect(string effectText)
  {
    var m = Regex.Match(
      effectText,
      @"^you\s+lose\s+(?<amount>\d+|one|two|three|four|five)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(raw),
    };
    return new LoseLifeEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Player = ObjectReference.You(),
    };
  }

  /// <summary>
  /// "it deals N damage to you" / "this creature deals N damage to you" —
  /// reflexive damage from the source permanent back to its controller.
  /// </summary>
  private static MagicAST.AST.Effects.Damage.DealDamageEffect? TryParseSelfDealsDamageToYouEffect(
    string effectText
  )
  {
    var m = Regex.Match(
      effectText,
      @"^(it|this\s+(?:creature|permanent|artifact|enchantment))\s+deals?\s+(?<amount>\d+|one|two|three)\s+damage\s+to\s+you$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      _ => int.Parse(raw),
    };
    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = ObjectReference.You(),
    };
  }

  /// <summary>
  /// "draw a card" / "you may draw a card unless that player pays {N}" — produces
  /// a <see cref="DrawCardsEffect"/> with the optional <see cref="UnlessClause"/>.
  /// </summary>
  private static DrawCardsEffect? TryParseDrawCardsEffect(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"\bdraw\s+(a|one|two|three|four|five|\d+)\s+cards?\b"))
    {
      return null;
    }

    var isOptional = lower.Contains("you may draw") || lower.StartsWith("you may ");
    var count = ParseWordOrDigitCount(effectText) ?? 1;

    // Unless clause: "...unless that player pays {N}." or "...unless [player] pays [cost]."
    var unlessMatch = Regex.Match(
      effectText,
      @"unless\s+(?<who>that\s+player|you|an\s+opponent)\s+pays?\s+(?<cost>\{[^}]+\}(?:\{[^}]+\})*)",
      RegexOptions.IgnoreCase
    );

    UnlessClause? unless = null;
    if (unlessMatch.Success)
    {
      var who = unlessMatch.Groups["who"].Value.ToLowerInvariant().Trim();
      var costStr = unlessMatch.Groups["cost"].Value;
      ObjectReference player = who switch
      {
        "that player" => new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        "you" => ObjectReference.You(),
        _ => new ObjectReference { Kind = ObjectReferenceKind.Opponent },
      };
      var manaCost = TryBuildManaCost(costStr);
      if (manaCost is not null)
      {
        unless = new UnlessClause { Player = player, Cost = manaCost };
      }
    }

    return new DrawCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      IsOptional = isOptional,
      UnlessClause = unless,
    };
  }

  /// <summary>
  /// "put a +1/+1 counter on this creature" / "put a +1/+1 counter on target creature you control".
  /// Recognises +1/+1 and -1/-1 counter shapes with self / target / target-you-control targets.
  /// </summary>
  private static PutCountersEffect? TryParsePutCountersEffect(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    if (!lower.Contains("put") || !lower.Contains("counter"))
    {
      return null;
    }

    string counterType;
    if (effectText.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (effectText.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      return null;
    }

    var count = ParseWordOrDigitCount(effectText) ?? 1;
    ObjectReference target;
    if (lower.Contains("target creature you control"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      };
    }
    else if (lower.Contains("target creature"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    else if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      target = ObjectReference.Self();
    }
    else
    {
      target = ObjectReference.Self();
    }

    return new PutCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
  }

  /// <summary>
  /// "untap this artifact" / "untap it" / "untap this permanent" — Self-targeting untap.
  /// Recognises the "you may pay {X}. If you do, untap ..." gating shape too,
  /// flagging the produced effect as <c>IsOptional=true</c> since the controller
  /// chooses whether to pay (matches Mana Vault's gold).
  /// </summary>
  private static UntapEffect? TryParseUntapSelfEffect(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    if (!Regex.IsMatch(lower, @"untap\s+(this|it)\b"))
    {
      return null;
    }
    var isOptional = Regex.IsMatch(
      lower,
      @"you\s+may\s+pay\s+\{[^}]+\}",
      RegexOptions.IgnoreCase
    );
    return new UntapEffect { Target = ObjectReference.Self(), IsOptional = isOptional };
  }

  /// <summary>
  /// "return up to one other target creature or planeswalker to its owner's hand" —
  /// Barrin shape. Encodes the "other" qualifier on Characteristics and the
  /// type disjunction on CardTypes. Also handles the simpler "return [target] to
  /// its owner's hand" and "return another target creature you control to its
  /// owner's hand".
  /// </summary>
  private static ReturnToHandEffect? TryParseReturnToHandEffect(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    if (!lower.Contains("return") || !lower.Contains("hand"))
    {
      return null;
    }

    // Hand qualifier
    if (!Regex.IsMatch(lower, @"to\s+(its\s+owner'?s|your)\s+hand"))
    {
      return null;
    }

    // "you may return ..." and "return up to N ..." both make this effect
    // optional in oracle convention — the controller can choose 0 targets.
    var isOptional =
      lower.Contains("you may return")
      || lower.StartsWith("you may ")
      || Regex.IsMatch(lower, @"return\s+up\s+to\s+");
    var characteristics = new List<string>();
    if (lower.Contains("another target"))
    {
      characteristics.Add("another");
    }
    else if (Regex.IsMatch(lower, @"\bother\s+target\b"))
    {
      characteristics.Add("other");
    }

    var cardTypes = new List<string>();
    foreach (var t in new[] { "creature", "planeswalker", "artifact", "enchantment", "permanent" })
    {
      if (Regex.IsMatch(lower, $@"\b{t}\b"))
      {
        cardTypes.Add(t);
      }
    }
    if (cardTypes.Count == 0)
    {
      return null;
    }

    ControllerFilter? controller = null;
    if (lower.Contains("you control"))
    {
      controller = ControllerFilter.You;
    }

    var filter = new ObjectFilter
    {
      CardTypes = cardTypes,
      Characteristics = characteristics.Count > 0 ? characteristics : null,
      Controller = controller,
    };

    return new ReturnToHandEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.Target, Filter = filter },
      IsOptional = isOptional,
    };
  }

  /// <summary>
  /// Niambi shape: "you may return ... to its owner's hand. If you do, you
  /// gain life equal to that creature's mana value." Builds a
  /// <see cref="ReturnToHandEffect"/> with <c>IsOptional=true</c> and an
  /// <c>IfYouDo</c> gain-life clause whose amount is derived from the
  /// returned creature's mana value.
  /// </summary>
  private static ReturnToHandEffect? TryParseReturnToHandWithIfYouDoGainLife(string effectText)
  {
    var split = Regex.Match(
      effectText,
      @"^(?<ret>you\s+may\s+return\s+.+?to\s+(?:its?\s+owner'?s|your)\s+hand)\.\s*If\s+you\s+do,\s*(?<rest>you\s+gain\s+life\s+equal\s+to\s+(?<src>that\s+creature'?s\s+mana\s+value))$",
      RegexOptions.IgnoreCase
    );
    if (!split.Success)
    {
      return null;
    }

    var returnEffect = TryParseReturnToHandEffect(split.Groups["ret"].Value.Trim());
    if (returnEffect is null)
    {
      return null;
    }

    var source = split.Groups["src"].Value.Trim();
    // Singularize possessive: "that creature's mana value" → source "that creature".
    var sourceObject = Regex.Replace(source, @"'?s\s+mana\s+value$", "", RegexOptions.IgnoreCase).Trim();

    var gainLife = new GainLifeEffect
    {
      Amount = new DerivedQuantity
      {
        DerivedFrom = DerivedKind.ManaValue,
        Source = sourceObject,
      },
      Player = ObjectReference.You(),
    };

    return returnEffect with { IfYouDo = gainLife, IsOptional = true };
  }

  /// <summary>
  /// "target opponent loses that much life" — Vito's drain-on-life-gain effect.
  /// Encodes the "that much" antecedent as a derived <see cref="DerivedKind.LifeGained"/>
  /// quantity, matching the gold AST.
  /// </summary>
  private static LoseLifeEffect? TryParseLoseLifeDerivedEffect(string effectText)
  {
    var lower = effectText.ToLowerInvariant();
    var match = Regex.Match(
      lower,
      @"target\s+opponent\s+loses?\s+that\s+much\s+life",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    return new LoseLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.LifeGained },
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
  }

  /// <summary>
  /// Stormfist-style composite: "each player draws a card and loses N life."
  /// Returns the gold's flat two-element list (drawCards, loseLife) so the
  /// trigger's Effects field carries both side-effects, with EachPlayer as
  /// the subject for both.
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

  /// <summary>
  /// Builds a <see cref="ManaCost"/> from a sequence of "{X}" mana symbols.
  /// Returns null when the string isn't a well-formed mana sequence.
  /// </summary>
  private static ManaCost? TryBuildManaCost(string manaText)
  {
    try
    {
      var parsed = new ManaCostParser().Parse(manaText);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      return new ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }
  }

  /// <summary>
  /// Parses number words ("one", "two", ..., "ten") and digit forms into integers.
  /// Returns null when the text doesn't carry a count word.
  /// </summary>
  private static int? ParseWordOrDigitCount(string text)
  {
    var lower = text.ToLowerInvariant();
    if (lower.Contains("two")) return 2;
    if (lower.Contains("three")) return 3;
    if (lower.Contains("four")) return 4;
    if (lower.Contains("five")) return 5;
    if (lower.Contains("six")) return 6;
    if (lower.Contains("seven")) return 7;
    if (lower.Contains("eight")) return 8;
    if (lower.Contains("nine")) return 9;
    if (lower.Contains("ten")) return 10;
    var m = Regex.Match(lower, @"\b(\d+)\b");
    if (m.Success) return int.Parse(m.Groups[1].Value);
    if (Regex.IsMatch(lower, @"\b(a|an|one)\b")) return 1;
    return null;
  }

  /// <summary>
  /// Tries to parse "scry N" effects (Rule 701.18 — keyword action). Mirrors
  /// the equivalent parser on <see cref="ActivatedAbilityParser"/>; eventually
  /// these should be hoisted into a shared effect-parser combinator.
  /// </summary>
  private static ScryEffect? TryParseScryEffect(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = Regex.Match(trimmed, @"^scry\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new ScryEffect { Count = LiteralQuantity.Of(count) };
  }

  /// <summary>
  /// "add {C}" / "add {G}{G}" / "add one mana of any color" — Rule 106 mana-add
  /// effect on the resolution side of a triggered ability. Mirrors the matcher
  /// on <see cref="ActivatedAbilityParser"/>; the parsers will be consolidated
  /// into a shared effect combinator once enough cases accumulate. Note that
  /// triggered mana production is *not* a Rule 605 mana ability (those require
  /// activation), so no IsManaAbility flag is set here.
  /// </summary>
  private static AddManaEffect? TryParseAddManaEffect(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.').Trim();
    if (!text.StartsWith("add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var manaText = text[4..].Trim();

    if (Regex.IsMatch(manaText, @"^one\s+mana\s+of\s+any\s+color$", RegexOptions.IgnoreCase))
    {
      return new AddManaEffect { Mana = string.Empty, AnyColor = true };
    }

    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
  }

  /// <summary>
  /// Parses "create [article] [P/T] [colors] [subtypes] creature token [abilities]" patterns.
  /// Supports variations like:
  /// - "create a 1/1 green Saproling creature token"
  /// - "create two 2/2 black Zombie creature tokens"
  /// - "create a 1/1 white and black Spirit creature token with flying"
  /// </summary>
  private static CreateTokenEffect? TryParseCreateTokenEffect(string text)
  {
    if (!text.Contains("create", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Parse article/quantity
    var (article, count) = ParseArticle(text);

    // Parse power/toughness (P/T)
    var powerToughness = ParsePowerToughness(text);
    if (powerToughness == null)
    {
      return null;
    }

    // Parse colors
    var colors = ParseColors(text);

    // Parse creature subtypes
    var subtypes = ParseCreatureSubtypes(text);
    if (subtypes.Count == 0)
    {
      return null;
    }

    // Parse abilities (optional). Convert the "with X" suffixes into the
    // same StaticAbility shape MAST uses for direct keyword abilities so the
    // token's ability list mirrors how the keyword would appear if printed
    // straight onto a permanent.
    var abilityNames = ParseTokenAbilities(text);
    IReadOnlyList<Ability>? tokenAbilities = null;
    if (abilityNames.Count > 0)
    {
      var abilities = new List<Ability>();
      foreach (var name in abilityNames)
      {
        var sa = BuildKeywordStaticAbility(name);
        if (sa is not null)
        {
          abilities.Add(sa);
        }
      }
      if (abilities.Count > 0)
      {
        tokenAbilities = abilities;
      }
    }

    return new CreateTokenEffect
    {
      Count = LiteralQuantity.Of(count),
      Token = new TokenDefinition
      {
        Power = powerToughness.Value.Power,
        Toughness = powerToughness.Value.Toughness,
        Colors = colors,
        Types = ["creature"],
        Subtypes = subtypes,
        Abilities = tokenAbilities,
      },
    };
  }

  /// <summary>
  /// Wraps a keyword name into a <see cref="StaticAbility"/> with the canonical
  /// <c>KeywordSource</c> + effect node, mirroring the shape used when the
  /// same keyword appears directly on a card. Returns null for keywords MAST
  /// does not yet model.
  /// </summary>
  private static StaticAbility? BuildKeywordStaticAbility(string keywordRaw)
  {
    var lower = keywordRaw.ToLowerInvariant().Trim();
    Effect? effect = lower switch
    {
      "flying" => new MagicAST.AST.Effects.Keyword.EvasionEffect
      {
        CanBeBlockedBy = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["flying", "reach"],
        },
      },
      "vigilance" => new MagicAST.AST.Effects.Keyword.VigilanceEffect(),
      "trample" => new MagicAST.AST.Effects.Keyword.TrampleEffect(),
      "haste" => new MagicAST.AST.Effects.Keyword.HasteEffect(),
      "reach" => new MagicAST.AST.Effects.Keyword.ReachEffect(),
      "lifelink" => new MagicAST.AST.Effects.Damage.LifelinkEffect(),
      "indestructible" => new MagicAST.AST.Effects.Keyword.IndestructibleEffect(),
      "deathtouch" => null, // not yet modeled
      _ => null,
    };
    if (effect is null)
    {
      return null;
    }
    var keywordSource = char.ToUpperInvariant(lower[0]) + lower[1..];
    return new StaticAbility { Effect = effect, KeywordSource = keywordSource };
  }

  #endregion

  #region Token Spec Parsing Components

  /// <summary>
  /// Parses article/quantity from token creation text.
  /// Compositional component for token parsing.
  /// </summary>
  private static (string Article, int Count) ParseArticle(string text)
  {
    var lower = text.ToLowerInvariant();

    if (lower.Contains("two "))
    {
      return ("two", 2);
    }

    if (lower.Contains("three "))
    {
      return ("three", 3);
    }

    if (lower.Contains("four "))
    {
      return ("four", 4);
    }

    if (lower.Contains("an "))
    {
      return ("an", 1);
    }

    if (lower.Contains("a "))
    {
      return ("a", 1);
    }

    return ("", 1); // Default to 1
  }

  /// <summary>
  /// Parses power/toughness notation (e.g., "1/1", "2/2", "X/X").
  /// Compositional component reusable across parsers.
  /// </summary>
  private static (string Power, string Toughness)? ParsePowerToughness(string text)
  {
    // Match N/N pattern where N is digit or X
    var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+|X)/(\d+|X)");
    if (!match.Success)
    {
      return null;
    }

    return (match.Groups[1].Value, match.Groups[2].Value);
  }

  /// <summary>
  /// Parses color words from text.
  /// Handles single colors and compound colors (e.g., "white and black").
  /// Compositional component reusable across parsers.
  /// </summary>
  private static List<string> ParseColors(string text)
  {
    var colors = new List<string>();
    var lower = text.ToLowerInvariant();

    // Map color names to abbreviations
    var colorMappings = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };

    foreach (var (name, code) in colorMappings)
    {
      if (lower.Contains(name))
      {
        colors.Add(code);
      }
    }

    // Handle colorless explicitly
    if (lower.Contains("colorless"))
    {
      colors.Clear();
      colors.Add("C");
    }

    return colors;
  }

  /// <summary>
  /// Parses creature subtypes from token creation text.
  /// Looks for common creature types between P/T and "creature token".
  /// Compositional component for token parsing.
  /// </summary>
  private static List<string> ParseCreatureSubtypes(string text)
  {
    var subtypes = new List<string>();

    // Common creature types from data analysis
    var knownTypes = new[]
    {
      "Saproling",
      "Zombie",
      "Spirit",
      "Goblin",
      "Soldier",
      "Human",
      "Elf",
      "Warrior",
      "Wolf",
      "Dragon",
      "Thopter",
      "Servo",
      "Knight",
      "Vampire",
      "Cat",
      "Bat",
      "Bird",
      "Insect",
      "Squirrel",
      "Citizen",
      "Rat",
      "Angel",
      "Detective",
      "Robot",
      "Kraken",
      "Eldrazi",
      "Phyrexian",
      "Germ",
      "Golem",
      "Rebel",
      "Hero",
      "Ally",
      "Orc",
      "Army",
    };

    foreach (var type in knownTypes)
    {
      if (text.Contains(type, StringComparison.OrdinalIgnoreCase))
      {
        subtypes.Add(type);
      }
    }

    return subtypes;
  }

  /// <summary>
  /// Parses token abilities (e.g., "with flying", "with lifelink").
  /// Compositional component for token parsing.
  /// </summary>
  private static List<string> ParseTokenAbilities(string text)
  {
    var abilities = new List<string>();
    var lower = text.ToLowerInvariant();

    // Common token abilities
    if (lower.Contains("with flying"))
    {
      abilities.Add("flying");
    }

    if (lower.Contains("with lifelink"))
    {
      abilities.Add("lifelink");
    }

    if (lower.Contains("with vigilance"))
    {
      abilities.Add("vigilance");
    }

    if (lower.Contains("with deathtouch"))
    {
      abilities.Add("deathtouch");
    }

    if (lower.Contains("with haste"))
    {
      abilities.Add("haste");
    }

    if (lower.Contains("with trample"))
    {
      abilities.Add("trample");
    }

    return abilities;
  }

  #endregion
}
