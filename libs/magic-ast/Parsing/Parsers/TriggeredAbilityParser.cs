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
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.Effects.TokenCopy;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;
using MagicAST.Parsing.Parsers.Triggered;
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

  // Registry-first dispatch (Phase 4) for the trigger-CONDITION side, mirroring the
  // effect-side ITriggeredRule registry. Static so the static ParseTriggerCondition
  // dispatcher can reach it; discovered once at type-init (condition rules don't
  // recurse through parser construction, so no laziness is needed here).
  private static readonly IReadOnlyList<DiscoveredRule<ITriggerConditionRule>> _conditionRules =
    RuleRegistry.Discover<ITriggerConditionRule, TriggerConditionRuleAttribute>("TriggeredAbilityParser");

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
      interveningIf = MagicAST.Parsing.ConditionParser.Parse(trailingIfMatch.Groups["cond"].Value.Trim());
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
        interveningIf = MagicAST.Parsing.ConditionParser.Parse(normalised);
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
        interveningIf = MagicAST.Parsing.ConditionParser.Parse(leadingIfMatch.Groups["cond"].Value.Trim());
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
      "sacrifice",
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
  /// Parses the trigger condition (event + filter) by dispatching to the
  /// priority-ordered set of <see cref="ITriggerConditionRule"/> implementations
  /// discovered by reflection. Each rule lives in its own file under
  /// <c>Parsers/Triggered/Rules/</c> and checks its own guard internally; the
  /// first non-null result wins. Returns null when no rule recognises the trigger.
  /// </summary>
  private static TriggerCondition? ParseTriggerCondition(string triggerText, TriggerTiming timing)
  {
    var lower = triggerText.ToLowerInvariant();

    foreach (var entry in _conditionRules)
    {
      var match = entry.Rule.Match(triggerText, lower, timing);
      if (match is not null)
      {
        return match;
      }
    }

    return null;
  }

  #endregion

  #region Effect Parsing — Dispatcher

  /// <summary>
  /// Per-rule entry for dispatch and telemetry attribution.
  /// </summary>
  private readonly record struct RuleEntry(Triggered.ITriggeredRule Rule, string Name, int Priority);

  private static readonly Lazy<IReadOnlyList<RuleEntry>> _rules =
    new(DiscoverRules, LazyThreadSafetyMode.ExecutionAndPublication);

  private static IReadOnlyList<RuleEntry> DiscoverRules() =>
    RuleRegistry
      .Discover<Triggered.ITriggeredRule, Triggered.TriggeredRuleAttribute>("TriggeredAbilityParser")
      .Select(r => new RuleEntry(r.Rule, r.Name, r.Priority))
      .ToList();

  /// <summary>
  /// Multi-effect dispatch. Tries the two composite-effect orchestration paths
  /// first (Sanctum and Stormfist shapes), then dispatches to single-effect rules
  /// via reflection-discovered <see cref="Triggered.ITriggeredRule"/> implementations.
  /// </summary>
  private static IReadOnlyList<Effect>? ParseEffects(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    var opponentExileThenExile = TryParseOpponentExileCreatureThenExileGraveyardCard(trimmed);
    if (opponentExileThenExile is not null)
    {
      return opponentExileThenExile;
    }

    var counterExileCast = TryParseCounterExileThenCastWithoutPaying(trimmed);
    if (counterExileCast is not null)
    {
      return counterExileCast;
    }

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

    var youDrawAndLose = TryParseYouDrawAndYouLoseLife(trimmed);
    if (youDrawAndLose is not null)
    {
      return youDrawAndLose;
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

    // Multi-sentence effect bundle: "Sentence one. Sentence two." — split on
    // ". " followed by an uppercase letter (the same heuristic the SpellAbilityParser
    // uses). Each sentence is dispatched through the single-rule loop independently.
    // If any sentence fails to parse, the whole bundle falls through to null so the
    // caller can fall back to the FallbackParser rather than silently dropping effects.
    var sentenceBundle = TryParseSentenceBundleEffects(effectText);
    if (sentenceBundle is not null)
    {
      return sentenceBundle;
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

  /// <summary>
  /// Splits a multi-sentence effect body on ". " followed by an uppercase letter
  /// and dispatches each sentence independently through the single-rule chain.
  /// Returns <see langword="null"/> if the text contains fewer than two sentences
  /// (no split needed) or if any sentence fails to parse (preserving fall-through
  /// to the FallbackParser rather than silently producing partial output).
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseSentenceBundleEffects(string effectText)
  {
    var working = effectText.Trim();
    if (working.EndsWith('.'))
    {
      working = working[..^1];
    }

    // Only attempt if there is at least one ". " + uppercase boundary.
    if (!Regex.IsMatch(working, @"\.\s+[A-Z]"))
    {
      return null;
    }

    var sentences = Regex.Split(working, @"\.\s+(?=[A-Z])");
    if (sentences.Length < 2)
    {
      return null;
    }

    var collected = new List<Effect>(sentences.Length);
    foreach (var sentence in sentences)
    {
      var fragment = sentence.Trim();
      if (fragment.Length == 0)
      {
        return null;
      }

      // Dispatch each sentence through the single-rule chain.
      Effect? matched = null;
      foreach (var entry in _rules.Value)
      {
        if (entry.Rule.TryMatch(fragment, out var e) && e is not null)
        {
          matched = e;
          break;
        }
      }

      if (matched is null)
      {
        // At least one sentence is unrecognised — bail so the caller falls back.
        return null;
      }

      collected.Add(matched);
    }

    return collected;
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
      });
    }

    return new List<Effect>
    {
      new CompositeEffect
      {
        Effects = creates,
      },
    };
  }

  /// <summary>
  /// Bundled one-shot counter-exile-recast composite (Transcendent Dragon):
  /// "counter target spell. If that spell is countered this way, exile it instead
  /// of putting it into its owner's graveyard, then you may cast it without paying
  /// its mana cost." Returns the gold's flat two-element list:
  /// <list type="number">
  ///   <item>a <c>counterSpell</c> with <c>ExileInsteadOfGraveyard:true</c> — the
  ///   counter and its zone-change replacement are a single sentence whose follow-up
  ///   references the exiled card, so the "exile instead" rides on the counter
  ///   (CR 406.6 linked-exile setup);</item>
  ///   <item>an <c>optional</c> wrapping <c>castWithoutPaying{Target:It}</c> — "then
  ///   you may cast it".</item>
  /// </list>
  /// Both effects sit inside one triggered ability — the recast is bundled in the
  /// resolving ability with no priority window (ADR 0004 "topology, not annotation").
  /// The "If that spell is countered this way" framing is engine bookkeeping
  /// (the replacement only applies when the counter resolved), not a separate
  /// condition node. CR 701.5 (counter); CR 117.7 ("you may").
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseCounterExileThenCastWithoutPaying(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^counter\s+target\s+spell\.\s+If\s+that\s+spell\s+is\s+countered\s+this\s+way,\s*exile\s+it\s+instead\s+of\s+putting\s+it\s+into\s+its\s+owner's\s+graveyard,\s*then\s+you\s+may\s+cast\s+it\s+without\s+paying\s+its\s+mana\s+cost$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    return new List<Effect>
    {
      new CounterSpellEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["spell"] },
        },
        ExileInsteadOfGraveyard = true,
      },
      new OptionalEffect
      {
        Inner = new CastWithoutPayingEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.It },
        },
      },
    };
  }

  /// <summary>
  /// CR 406.6 linked-exile setup composite: "target opponent exiles a nontoken
  /// creature they control, then they exile a nonland card from their graveyard."
  /// (Azula, Cunning Usurper). The targeted opponent performs two exiles; the
  /// "they" back-references that same opponent. Returns the gold's flat two-element
  /// [exile, exile] list.
  ///
  /// <para>
  /// Both exiles are choices made by the opponent rather than the ability's
  /// controller, so neither carries the "exile target" surface of
  /// <see cref="Triggered.Rules.ExileTargetTriggeredRule"/>. The creature is a
  /// targeted reference ("target opponent ... a nontoken creature they control");
  /// the graveyard card is a Designated reference (the opponent picks one of their
  /// own nonland cards, no "target" keyword). The "exiled with Azula" linkage is
  /// recovered by the separate static permission, not threaded here (ADR 0004
  /// reference-not-resolution). Rule 701.13 (exile); Rule 111 (token); CR 406.6.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseOpponentExileCreatureThenExileGraveyardCard(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^target\s+opponent\s+exiles\s+a\s+nontoken\s+creature\s+they\s+control,\s*then\s+they\s+exile\s+a\s+nonland\s+card\s+from\s+their\s+graveyard$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    return new List<Effect>
    {
      new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Controller = ControllerFilter.Opponent,
            IsToken = false,
          },
        },
      },
      new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Designated,
          Filter = new ObjectFilter
          {
            CardTypes = ["card"],
            Zone = Zone.Graveyard,
            Controller = ControllerFilter.Opponent,
            ExcludedCardTypes = ["land"],
          },
        },
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
  /// Freerunning-style composite: "you draw a card and you lose N life."
  /// Returns a flat two-element list [drawCards(You, 1), loseLife(You, N)].
  ///
  /// <para>
  /// Covers the Merciless Harlequin ETB trigger shape. The "you draw" and
  /// "you lose" halves share the same controller reference. The
  /// <see cref="DrawCardsEffect.IsOptional"/> flag is false (no "you may" prefix).
  /// </para>
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseYouDrawAndYouLoseLife(string effectText)
  {
    var match = Regex.Match(
      effectText,
      @"^you\s+draw\s+(?<draw>a|one|two|three|\d+)\s+cards?\s+and\s+you\s+lose\s+(?<life>\d+|one|two|three)\s+life$",
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
    var you = ObjectReference.You();
    return new List<Effect>
    {
      new DrawCardsEffect { Count = LiteralQuantity.Of(drawCount), Player = you},
      new LoseLifeEffect { Amount = LiteralQuantity.Of(lifeCount), Player = you },
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

    var filter = MagicAST.Parsing.Parsers.Static.StaticRuleHelpers.BuildObjectCountFilter(
      match.Groups["count"].Value.Trim());
    if (filter is null)
    {
      return null;
    }
    return new List<Effect>
    {
      new LoseLifeEffect
      {
        Amount = new CountQuantity { CountOf = filter },
        Player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent },
      },
      new GainLifeEffect
      {
        Amount = new CountQuantity { CountOf = filter },
        Player = ObjectReference.You(),
      },
    };
  }

  #endregion
}
