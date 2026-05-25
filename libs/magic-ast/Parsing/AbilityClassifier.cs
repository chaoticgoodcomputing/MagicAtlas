namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Classification result for an oracle clause.
/// </summary>
public sealed record ClauseClassification
{
  /// <summary>
  /// The classified ability kind.
  /// </summary>
  public required AbilityKind Kind { get; init; }

  /// <summary>
  /// Confidence level of the classification (0.0 to 1.0).
  /// Higher values indicate stronger pattern matches.
  /// </summary>
  public required double Confidence { get; init; }

  /// <summary>
  /// Optional ability word detected (e.g., "Landfall", "Revolt").
  /// </summary>
  public string? AbilityWord { get; init; }

  /// <summary>
  /// For loyalty abilities, the loyalty cost (+N, -N, or 0).
  /// </summary>
  public int? LoyaltyCost { get; init; }
}

/// <summary>
/// Classifies oracle text clauses into ability types based on structural patterns.
/// This pre-classification enables routing to specialized parsers.
/// </summary>
public sealed class AbilityClassifier
{
  /// <summary>
  /// Known ability words that precede em-dashes.
  /// These have no rules meaning but help identify ability patterns.
  /// </summary>
  private static readonly HashSet<string> _abilityWords =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Triggered ability words
      "Landfall",
      "Revolt",
      "Enrage",
      "Delirium",
      "Threshold",
      "Hellbent",
      "Metalcraft",
      "Morbid",
      "Raid",
      "Spell mastery",
      "Ferocious",
      "Formidable",
      "Undergrowth",
      "Addendum",
      "Spectacle",
      "Constellation",
      "Heroic",
      "Inspired",
      "Battalion",
      "Bloodrush",
      "Evolve",
      "Extort",
      "Overload",
      "Populate",
      "Scavenge",
      "Detain",
      "Unleash",
      "Radiance",
      // Static/conditional ability words
      "Kinship",
      "Domain",
      "Converge",
      "Sweep",
      "Grandeur",
      "Channel",
      "Bloodthirst",
      "Imprint",
      "Join forces",
      "Tempting offer",
      "Will of the council",
      "Council's dilemma",
      "Parley",
      "Lieutenant",
      "Eminence",
      "Alliance",
      "Coven",
      "Pack tactics",
      "Magecraft",
      "Fateful hour",
      // Adventure
      "Adventure",
    };

  /// <summary>
  /// Classifies a clause into an ability kind.
  /// </summary>
  /// <param name="clause">The clause to classify.</param>
  /// <returns>The classification result.</returns>
  public ClauseClassification Classify(OracleClause clause)
  {
    var tokens = clause.Tokens.ToList();

    // Empty clause
    if (tokens.Count == 0)
    {
      return new ClauseClassification { Kind = AbilityKind.Unparsed, Confidence = 1.0 };
    }

    // Saga preamble clauses are pre-grouped by ClauseSplitter — they carry
    // their chapters on SagaChapters. Route them straight to the saga parser.
    if (clause.SagaChapters is { Count: > 0 })
    {
      return new ClauseClassification { Kind = AbilityKind.Saga, Confidence = 1.0 };
    }

    // Level-up cluster head: ClauseSplitter has pre-grouped the cost
    // paragraph with its LEVEL N-M stanzas. Route to the level-up parser.
    if (clause.LevelUpStanzas is { Count: > 0 })
    {
      return new ClauseClassification { Kind = AbilityKind.LevelUp, Confidence = 1.0 };
    }

    // Check for ability word pattern: "Word —"
    var abilityWord = TryExtractAbilityWord(clause);

    // Check for loyalty ability: +N: or −N: or 0:
    var loyaltyClassification = TryClassifyAsLoyalty(tokens);
    if (loyaltyClassification != null)
    {
      return loyaltyClassification with { AbilityWord = abilityWord };
    }

    // Check for triggered ability: When/Whenever/At
    if (StartsWithTriggerTiming(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Triggered,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // Check for activated ability: {cost}: or word:
    if (IsActivatedAbilityPattern(tokens, clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // Check for replacement effect: would ... instead
    if (ContainsReplacementPattern(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static, // Replacement effects are a type of static ability
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // Check for modal ability: Choose
    if (StartsWithChoose(tokens))
    {
      // Exception: "Choose a Background" is a named partner-variant keyword
      // (Rule 702.124g, descriptive). Route it to the static keyword parser
      // so it lands as a StaticAbility with KeywordSource="Choose a Background"
      // rather than as an unparsable modal selection.
      var trimmed = clause.RawText.TrimStart();
      if (trimmed.StartsWith("Choose a Background", StringComparison.OrdinalIgnoreCase))
      {
        return new ClauseClassification
        {
          Kind = AbilityKind.Static,
          Confidence = 0.95,
          AbilityWord = abilityWord,
        };
      }
      return new ClauseClassification
      {
        Kind = AbilityKind.Modal,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // Check for single keyword (may need expansion). Skipped when the clause
    // leads with a spell-instruction verb (e.g. "Scry 1.", "Mill 2."): those
    // shapes are imperative resolution steps, not statics, even though they
    // happen to tokenize as <verb> <number> and would otherwise be mistaken
    // for a parameterized keyword.
    if (!StartsWithSpellInstructionVerb(clause.RawText) && IsSingleKeyword(tokens))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static, // Keywords are typically static abilities
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // Spell-style instruction verbs at clause start: imperative effect
    // descriptions consistent with sorcery/instant resolution (Rule 113.3a).
    // Also fires for modal option bodies dispatched through the registry, where
    // each option is effectively a small spell ability nested inside a modal.
    if (StartsWithSpellInstructionVerb(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // Lines beginning with "Until end of turn, ..." are resolution
    // instructions on the spell, not declarative statics — the duration
    // clause is parsed as part of the effect by SpellAbilityParser.
    if (
      clause.RawText.TrimStart()
        .StartsWith("Until end of turn", StringComparison.OrdinalIgnoreCase)
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // "Each player ..." instructional sentences on the spell (Rule 113.3a) —
    // discard prompts, life-loss conditionals, etc. Static doesn't fit; the
    // text is an imperative resolution step.
    if (
      clause.RawText.TrimStart()
        .StartsWith("Each player", StringComparison.OrdinalIgnoreCase)
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // "You may [spell-verb] ..." optional resolution-step phrasing on a spell
    // (Rule 117.7) — e.g., "You may discard a card. If you do, draw two cards."
    // (Abandon Attachments). The opening "You may" is a player-instruction frame
    // around a spell-instruction verb, so the line resolves as a spell effect
    // with IsOptional=true rather than as a declarative static. Mirrors the
    // <c>StartsWithSpellInstructionVerb</c> dispatch but lets the "You may"
    // frame precede the verb.
    if (StartsWithYouMaySpellInstruction(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // "You gain/lose N life." / "You draw N cards." — spell-resolution
    // player-resource instructions whose subject ("You") is the player rather
    // than an imperative verb. The bare-verb <see cref="_spellInstructionVerbs"/>
    // dispatch doesn't catch these because the clause opens with "You";
    // recognise the narrow "You <resource-verb> …" prefix here so modal option
    // bodies like Recuperate's "You gain 6 life." resolve as spell effects
    // rather than defaulting to a mis-tagged static. Tight allowlist of
    // resource verbs (gain/lose life, draw/discard cards) keeps the route
    // from swallowing declarative "You can't …" statics.
    if (StartsWithYouResourceInstruction(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // "Target [filter] blocks this turn if able." — spell-resolution single-target
    // block requirement (e.g., Culling Mark). The static "[subject] blocks each
    // combat if able" recognizer in StaticAbilityParser is the wrong route: this
    // line is an imperative spell instruction (one-shot, with explicit duration),
    // not a continuous static on the named permanent.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S.*?\s+blocks?\s+this\s+turn\s+if\s+able\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // "Target [filter] attacks this turn if able." — spell-resolution single-target
    // attack requirement (e.g., Boiling Blood). Dual of the block-requirement rule
    // above: a one-shot imperative spell instruction with explicit duration, not the
    // continuous static "[subject] attacks each combat if able" that lives on a
    // permanent (Rule 508.1d).
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S.*?\s+attacks?\s+this\s+turn\s+if\s+able\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // "Target [filter] must be blocked this turn if able." — spell-resolution
    // single-target block requirement *on the attacker* (e.g., Irresistible Prey).
    // Rule 509.1c. Distinct from the "Target [filter] blocks this turn if able"
    // rule above: that one compels the named creature to do the blocking; this
    // one compels the defending player's other creatures to block the named
    // creature. Without this rule the line is mis-routed to the static parser
    // (the static "[Self] must be blocked if able" recognizer there is the
    // continuous-permanent variant with no duration).
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S.*?\s+must\s+be\s+blocked\s+this\s+turn\s+if\s+able\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // "[Self] deals N damage to ..." — self-by-name spell-resolution dealDamage.
    // Take Down's modal options ("Take Down deals 4 damage to target creature
    // with flying.") open with the card's own name rather than a recognised
    // imperative verb, so they slip past <c>StartsWithSpellInstructionVerb</c>
    // and would otherwise default to Static. The leading capitalised subject +
    // "deals N damage to ..." frame is unambiguously a spell-resolution effect
    // (Rule 113.3a) — same self-by-name convention used elsewhere (e.g.
    // <see cref="ActivatedAbilityParser"/>'s Denethor burn tail).
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*[A-Z]\S*(?:\s+\S+)*?\s+deals?\s+\S+\s+damage\s+to\s+",
        RegexOptions.None
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // "This spell can't be countered." is a property of the resolving spell;
    // route it to the spell parser so the EffectType lands inside
    // SpellAbility.Effects rather than as a top-level static. Other
    // "This spell ..." phrasings (e.g., "This spell costs {X} less to cast")
    // are static cost-modification effects and remain Static.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*This\s+spell\s+can'?t\s+be\s+countered",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // Ability-word conditional spell-effect: "[AbilityWord] — If <condition>,
    // <spell-verb> …". The ability word itself (e.g. "Fateful hour", Rule 702.95)
    // has no rules meaning — it gates an otherwise normal spell-resolution
    // instruction on a state predicate. We surface the gate as free-text on
    // SpellAbility.Instructions (the documented escape hatch until SpellAbility
    // grows a structured InterveningIf), and route to the spell parser so the
    // trailing effect lands in SpellAbility.Effects instead of stalling on the
    // unimplemented static-conditional path.
    if (
      abilityWord is not null
      && StartsWithIfConditionThenSpellVerb(clause.RawText, abilityWord)
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.85,
        AbilityWord = abilityWord,
      };
    }

    // Garbage / non-oracle characters: when the clause carries glyphs that
    // never appear in printed oracle text (e.g., '@', '#', '$', '%', '^',
    // '&', '*'), there's no meaningful structural pattern to anchor a Static
    // classification on. Route to Unparsed so the fallback parser surfaces
    // a structured "Failed to parse Unparsed ability" diagnostic instead of
    // a mis-tagged "Static ability parser not yet implemented".
    if (ContainsOracleGarbage(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Unparsed,
        Confidence = 1.0,
        AbilityWord = abilityWord,
      };
    }

    // Default to static ability for declarative statements
    return new ClauseClassification
    {
      Kind = AbilityKind.Static,
      Confidence = 0.50,
      AbilityWord = abilityWord,
    };
  }

  /// <summary>
  /// Heuristic: returns true when the clause carries glyphs that don't appear
  /// in printed oracle text. Used to short-circuit classification on garbage
  /// inputs so the fallback parser sees an <see cref="AbilityKind.Unparsed"/>
  /// kind rather than a defaulted Static.
  /// </summary>
  private static bool ContainsOracleGarbage(string text)
  {
    foreach (var ch in text)
    {
      switch (ch)
      {
        case '@':
        case '#':
        case '$':
        case '%':
        case '^':
        case '&':
        case '~':
        case '\\':
          return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Recognises clauses whose first word is a command-style action verb that
  /// reads as a spell resolution instruction. The verb is checked at the
  /// clause's very start; bullets and whitespace are tolerated to support
  /// modal option bodies (<c>"• Return target permanent card ..."</c>).
  /// </summary>
  private static bool StartsWithSpellInstructionVerb(string rawText)
  {
    var trimmed = rawText.TrimStart('•', '•', ' ', '\t');
    foreach (var verb in _spellInstructionVerbs)
    {
      if (
        trimmed.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase)
        || trimmed.Equals(verb, StringComparison.OrdinalIgnoreCase)
      )
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Recognises clauses opened by the "You may [verb] ..." frame, where the
  /// verb is one of the spell-resolution instruction verbs. Distinguished from
  /// the bare <see cref="StartsWithSpellInstructionVerb"/> check because the
  /// imperative is wrapped in a "You may" player-choice (Rule 117.7); the line
  /// is still a spell-resolution step, not a continuous static.
  /// </summary>
  private static bool StartsWithYouMaySpellInstruction(string rawText)
  {
    var trimmed = rawText.TrimStart('•', '•', ' ', '\t');
    if (!trimmed.StartsWith("You may ", StringComparison.OrdinalIgnoreCase))
    {
      return false;
    }
    var rest = trimmed.Substring("You may ".Length);
    foreach (var verb in _spellInstructionVerbs)
    {
      if (rest.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }
    return false;
  }

  /// <summary>
  /// Recognises "You <resource-verb> …" clauses — spell-resolution player-action
  /// instructions whose subject is the player rather than an imperative verb.
  /// Allowlist: gain/lose life, draw/discard cards. Kept narrow on purpose so
  /// declarative "You can't …" / "You may not …" lines stay on the static path.
  /// </summary>
  private static bool StartsWithYouResourceInstruction(string rawText)
  {
    var trimmed = rawText.TrimStart('•', '•', ' ', '\t');
    return Regex.IsMatch(
      trimmed,
      @"^You\s+(gain|lose|draw|discard)\s+",
      RegexOptions.IgnoreCase
    );
  }

  /// <summary>
  /// Recognises clauses of the shape "[AbilityWord] — If <condition>,
  /// <spell-verb> …" — the ability-word conditional spell-effect pattern
  /// (e.g. Spell Snuff's Fateful hour line). The em-dash separates the
  /// ability-word prefix from the body; the leading "If <condition>," in the
  /// body is the gating predicate and the verb that follows must be one of
  /// the spell-resolution instruction verbs for the line to qualify.
  /// </summary>
  private static bool StartsWithIfConditionThenSpellVerb(string rawText, string abilityWord)
  {
    var emDashIndex = rawText.IndexOf('—');
    if (emDashIndex <= 0)
    {
      return false;
    }
    var body = rawText[(emDashIndex + 1)..].TrimStart();
    var ifMatch = Regex.Match(
      body,
      @"^If\s+[^,]+,\s*(?<rest>.+)$",
      RegexOptions.IgnoreCase
    );
    if (!ifMatch.Success)
    {
      return false;
    }
    var rest = ifMatch.Groups["rest"].Value;
    foreach (var verb in _spellInstructionVerbs)
    {
      if (rest.StartsWith(verb + " ", StringComparison.OrdinalIgnoreCase))
      {
        return true;
      }
    }
    return false;
  }

  private static readonly string[] _spellInstructionVerbs =
  [
    "Counter",
    "Destroy",
    "Exile",
    "Return",
    "Deal",
    "Draw",
    "Discard",
    "Search",
    "Mill",
    "Scry",
    "Surveil",
    "Create",
    "Copy",
    "Reveal",
    "Look",
    "Shuffle",
    "Cast",
    "Put",
    "Tap",
    "Untap",
    "Prevent",
  ];

  /// <summary>
  /// Tries to extract an ability word from the clause.
  /// Ability words appear before an em-dash.
  /// </summary>
  private static string? TryExtractAbilityWord(OracleClause clause)
  {
    var emDashIndex = clause.RawText.IndexOf('\u2014');
    if (emDashIndex <= 0)
    {
      return null;
    }

    var prefix = clause.RawText[..emDashIndex].Trim();
    if (_abilityWords.Contains(prefix))
    {
      return prefix;
    }

    return null;
  }

  /// <summary>
  /// Tries to classify a clause as a loyalty ability.
  /// </summary>
  private static ClauseClassification? TryClassifyAsLoyalty(List<Token<OracleToken>> tokens)
  {
    if (tokens.Count < 2)
    {
      return null;
    }

    var first = tokens[0];

    // +N: pattern
    if (first.Kind == OracleToken.LoyaltyUp)
    {
      var loyaltyCost = ParseLoyaltyCost(first.ToStringValue(), positive: true);
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.98,
        LoyaltyCost = loyaltyCost,
      };
    }

    // −N: pattern
    if (first.Kind == OracleToken.LoyaltyDown)
    {
      var loyaltyCost = ParseLoyaltyCost(first.ToStringValue(), positive: false);
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.98,
        LoyaltyCost = loyaltyCost,
      };
    }

    // 0: pattern (number 0 followed by colon)
    if (first.Kind == OracleToken.Number && first.ToStringValue() == "0")
    {
      if (tokens.Count > 1 && tokens[1].Kind == OracleToken.Colon)
      {
        return new ClauseClassification
        {
          Kind = AbilityKind.Activated,
          Confidence = 0.98,
          LoyaltyCost = 0,
        };
      }
    }

    return null;
  }

  /// <summary>
  /// Parses a loyalty cost from the token value.
  /// </summary>
  private static int? ParseLoyaltyCost(string value, bool positive)
  {
    // Remove + or − prefix
    var numStr = value.TrimStart('+', '\u2212', '-');
    if (int.TryParse(numStr, out var cost))
    {
      return positive ? cost : -cost;
    }

    return null;
  }

  /// <summary>
  /// Checks if the clause starts with a trigger timing word.
  /// </summary>
  private static bool StartsWithTriggerTiming(List<Token<OracleToken>> tokens)
  {
    if (tokens.Count == 0)
    {
      return false;
    }

    var first = tokens[0].Kind;
    return first == OracleToken.When || first == OracleToken.Whenever || first == OracleToken.At;
  }

  /// <summary>
  /// Checks if the clause matches an activated ability pattern.
  /// Pattern: {cost}: or word: (but not "When:" etc.)
  /// </summary>
  private static bool IsActivatedAbilityPattern(List<Token<OracleToken>> tokens, string rawText)
  {
    // Look for colon in the raw text
    var colonIndex = rawText.IndexOf(':');
    if (colonIndex < 0)
    {
      return false;
    }

    // Check for mana/tap symbols before colon
    var hasCostToken = false;
    for (var i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];

      if (token.Kind == OracleToken.Colon)
      {
        // Found colon - check if there were any cost-like tokens before it
        // or if this is an activation keyword pattern
        if (i > 0)
        {
          return hasCostToken || IsActivationKeyword(tokens, i);
        }

        return false;
      }

      // Track if we've seen any cost tokens
      if (IsCostToken(token.Kind))
      {
        hasCostToken = true;
      }

      // If we hit a trigger timing word, this isn't an activated ability
      if (
        token.Kind == OracleToken.When
        || token.Kind == OracleToken.Whenever
        || token.Kind == OracleToken.At
      )
      {
        return false;
      }
    }

    return false;
  }

  /// <summary>
  /// Checks if a token kind represents a cost component.
  /// </summary>
  private static bool IsCostToken(OracleToken kind)
  {
    return kind == OracleToken.TapSymbol
      || kind == OracleToken.UntapSymbol
      || kind == OracleToken.GenericMana
      || kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana
      || kind == OracleToken.ColorlessMana
      || kind == OracleToken.HybridMana
      || kind == OracleToken.PhyrexianMana
      || kind == OracleToken.VariableMana
      || kind == OracleToken.EnergySymbol;
  }

  /// <summary>
  /// Checks if the tokens before the colon represent an activation keyword
  /// (e.g., "Cycling", "Equip", "Crew").
  /// </summary>
  private static bool IsActivationKeyword(List<Token<OracleToken>> tokens, int colonIndex)
  {
    // Common activation keywords that appear before colon without mana symbols
    var activationKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
      "Equip",
      "Cycling",
      "Crew",
      "Fortify",
      "Reconfigure",
      "Ninjutsu",
      "Outlast",
      "Megamorph",
      "Morph",
      "Unearth",
      "Suspend",
      "Transfigure",
      "Transmute",
      "Boast",
      "Channel",
    };

    // Look for word token immediately before colon
    if (colonIndex > 0 && tokens[colonIndex - 1].Kind == OracleToken.Word)
    {
      var word = tokens[colonIndex - 1].ToStringValue();
      return activationKeywords.Contains(word);
    }

    return false;
  }

  /// <summary>
  /// Checks if the clause contains a replacement effect pattern.
  /// </summary>
  private static bool ContainsReplacementPattern(List<Token<OracleToken>> tokens)
  {
    var hasWould = false;
    var hasInstead = false;

    foreach (var token in tokens)
    {
      if (token.Kind == OracleToken.Would)
      {
        hasWould = true;
      }
      else if (token.Kind == OracleToken.Instead)
      {
        hasInstead = true;
      }
    }

    return hasWould && hasInstead;
  }

  /// <summary>
  /// Checks if the clause starts with "Choose".
  /// </summary>
  private static bool StartsWithChoose(List<Token<OracleToken>> tokens)
  {
    return tokens.Count > 0 && tokens[0].Kind == OracleToken.Choose;
  }

  /// <summary>
  /// Checks if the clause is a single keyword ability.
  /// </summary>
  private static bool IsSingleKeyword(List<Token<OracleToken>> tokens)
  {
    // Filter out reminder text and punctuation
    var significantTokens = 0;
    foreach (var token in tokens)
    {
      if (
        token.Kind == OracleToken.Word
        || token.Kind == OracleToken.Number
        || token.Kind == OracleToken.GenericMana
        || IsColoredManaToken(token.Kind)
      )
      {
        significantTokens++;
      }
      else if (
        token.Kind != OracleToken.ReminderText
        && token.Kind != OracleToken.Period
        && token.Kind != OracleToken.Comma
      )
      {
        // Has other structural tokens, not a simple keyword
        return false;
      }
    }

    // Single word (possibly with number parameter like "Absorb 2")
    return significantTokens <= 2;
  }

  /// <summary>
  /// Checks if a token is a colored mana token.
  /// </summary>
  private static bool IsColoredManaToken(OracleToken kind)
  {
    return kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana;
  }
}
