namespace MagicAST.Parsing;

using MagicAST.AST.References;
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
  /// Optional ability word detected (e.g., "Landfall", "Revolt"). Only a real
  /// CR 207.2c ability word lands here — it is emitted on the parsed ability's
  /// <see cref="MagicAST.AST.Abilities.Ability.AbilityWord"/>.
  /// </summary>
  public string? AbilityWord { get; init; }

  /// <summary>
  /// A printed italic label that LOOKS like an ability word ("Word — …") but is
  /// NOT a CR 207.2c ability word — it is the card's own flavor label (e.g.
  /// Displacer Kitten's "Avoidance"), mechanically inert. Detected so the body
  /// after the em-dash strips and classifies generically, but never emitted as a
  /// CR <see cref="AbilityWord"/> (which would assert a non-existent ability word).
  /// </summary>
  public string? PrintedLabel { get; init; }

  /// <summary>
  /// The em-dash prefix to strip from the body, whether it is a real ability word
  /// (<see cref="AbilityWord"/>) or a printed flavor label (<see cref="PrintedLabel"/>).
  /// </summary>
  public string? DashPrefix => AbilityWord ?? PrintedLabel;

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
      "Rally",
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
      // EOE (Edge of Eternities) ability words
      "Void",
      // Bloom Tender / Eventide ability word (CR 207.2c list)
      "Vivid",
      // Duskmourn: House of Horror ability word (CR 207.2c list).
      // "Eerie — Whenever an enchantment you control enters and whenever you fully
      // unlock a Room, [effect]." — fires on enchantment ETB and Room full-unlock.
      "Eerie",
      // Exhaust (CR 702.177): a special kind of activated ability. "Exhaust — [Cost]:
      // [Effect]" means "[Cost]: [Effect]. Activate only once." The prefix is stripped
      // and the ability word emitted so the parser can inject the OnlyOnce restriction.
      "Exhaust",
      // Grant an Advantage: the CR 207.2c ability word for the dice-advantage
      // replacement (Adventures in the Forgotten Realms / Commander Legends: Battle
      // for Baldur's Gate). "Grant an Advantage — If you would roll one or more dice,
      // instead roll that many dice plus one and ignore the lowest roll." (Pixie
      // Guide; the word also fronts Barbarian Class's identical line). The prefix is
      // stripped and the word emitted on AbilityWord so DiceAdvantageReplacementRule
      // matches the body. (FLAG: the abridged rules-structure.json dataset shipped in
      // libs/mtg-rules does not include the CR 207.2c ability-word list, so the word
      // itself could not be string-verified there — only the underlying "ignore the
      // lowest roll" mechanic, CR 706.6, is present and verified.)
      "Grant an Advantage",
    };

  /// <summary>
  /// Printed italic labels that take the "Word — …" shape of an ability word but
  /// are NOT CR 207.2c ability words — they are a single card's own flavor label,
  /// mechanically inert. Detected so the body strips/classifies like an ability
  /// word, but routed to <see cref="ClauseClassification.PrintedLabel"/> rather
  /// than <see cref="ClauseClassification.AbilityWord"/> so the AST never asserts
  /// a non-existent ability word.
  /// </summary>
  private static readonly HashSet<string> _printedLabels =
    new(StringComparer.OrdinalIgnoreCase)
    {
      // Displacer Kitten's printed label; not on the CR 207.2c ability-word list.
      "Avoidance",
      // Sephiroth, Fabled SOLDIER's printed label for the transform-into trigger.
      // Not on the CR 207.2c ability-word list; mechanically inert flavor label.
      "Super Nova",
      // d20 results-table cohort (AFR / Forgotten Realms) printed flavor labels.
      // Each fronts a "[Label] — When/Whenever … roll a d20." trigger that opens a
      // results table (CR 706.3). None is a CR 207.2c ability word — they are
      // single-card flavor labels, mechanically inert, so they re-home to
      // PrintedLabel and the body still classifies as the triggered ability it is.
      "Siege Monster",
      "Sneak Attack",
      "Wild Magic Surge",
    };

  /// <summary>
  /// Classifies a clause into an ability kind. A detected "Word — …" prefix that
  /// is a printed flavor label (not a CR 207.2c ability word) is re-homed off
  /// <see cref="ClauseClassification.AbilityWord"/> onto
  /// <see cref="ClauseClassification.PrintedLabel"/> so the AST never asserts a
  /// non-existent ability word, while the body still strips and classifies
  /// generically off <see cref="ClauseClassification.DashPrefix"/>.
  /// </summary>
  /// <param name="clause">The clause to classify.</param>
  /// <returns>The classification result.</returns>
  public ClauseClassification Classify(OracleClause clause)
  {
    var classification = ClassifyCore(clause);
    if (IsPrintedLabel(classification.AbilityWord))
    {
      return classification with
      {
        AbilityWord = null,
        PrintedLabel = classification.AbilityWord,
      };
    }
    return classification;
  }

  /// <summary>
  /// Core classification: routes a clause to an ability kind and threads the
  /// detected em-dash prefix (real or flavor) on <see cref="ClauseClassification.AbilityWord"/>.
  /// <see cref="Classify"/> post-processes the flavor case.
  /// </summary>
  private ClauseClassification ClassifyCore(OracleClause clause)
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

    // Class cluster head: ClauseSplitter has pre-grouped the reminder preamble
    // with the base abilities and class level bars (CR 716, "Class Cards").
    // Route straight to the Class parser.
    if (clause.ClassLevels is { Count: > 0 })
    {
      return new ClauseClassification { Kind = AbilityKind.Class, Confidence = 1.0 };
    }

    // Check for ability word pattern: "Word —"
    var abilityWord = TryExtractAbilityWord(clause);

    // Check for loyalty ability: +N: or −N: or 0:
    var loyaltyClassification = TryClassifyAsLoyalty(tokens);
    if (loyaltyClassification != null)
    {
      return loyaltyClassification with { AbilityWord = abilityWord };
    }

    // Parenthetical-wrapped activated ability: "({T}: Add {B} or {R}.)" on dual
    // lands and basic land subtypes. The whole clause tokenizes as a single
    // ReminderText token; the inner content is a normal {cost}: effect pattern.
    if (IsParentheticalActivatedAbility(tokens, clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Activated,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // Standalone Siege type-reminder line:
    // "(As a Siege enters, choose an opponent to protect it. You and others can
    //  attack it. When it's defeated, exile it, then cast it transformed.)"
    // The whole clause tokenizes as a single ReminderText token (so it is not a
    // parenthetical-wrapped activated ability — its inner text opens with "As",
    // not "{cost}:"). Although the reminder body contains "When it's defeated",
    // the clause does not start with a trigger-timing word, so StartsWithTriggerTiming
    // (which checks tokens[0]) does not fire. Route it to the Static path so
    // SiegeReminderStaticRule can emit a StaticAbility{ Effects: [SiegeEffect],
    // Reminder } — a marker that preserves the no-game-function reminder text
    // (CR 207.2) and structurally anchors the Siege mechanic (Rule 310). Mirrors
    // the "Start your engines!" interception below: a raw-text prefix check that
    // bypasses the heuristic. Without it the clause still defaults to Static at
    // 0.50, but the explicit intercept records intent and keeps the routing stable.
    if (IsSiegeReminderLine(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // A "When/Whenever … this turn, …" whose *trigger condition* (the text before
    // the comma) is bounded to "this turn" is not a printed triggered ability — it
    // is a spell that, on resolution, creates a delayed triggered ability (CR 603.7),
    // e.g. Glimpse of Nature. Route it to the spell parser (which builds a
    // createDelayedTrigger). The pre-comma scope distinguishes it from a printed
    // trigger whose *effect* merely mentions "this turn" (e.g. "When ~ enters,
    // target creature can't block this turn"). ADR 0002/0004.
    if (StartsWithTriggerTiming(tokens) && TriggerConditionMentionsThisTurn(clause.RawText))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // Check for triggered ability: When/Whenever/At — either at clause start,
    // or after an ability-word prefix ("Landfall — Whenever ..."). Ability words
    // (Rule 207.2c) have no rules meaning; peeling the prefix before the
    // trigger-timing check makes this classification generic across all ability
    // words (Landfall, Threshold, Delirium, Revolt, Enrage, Fateful hour, etc.)
    // without any per-word special-casing.
    if (StartsWithTriggerTiming(tokens) || (abilityWord is not null && BodyAfterAbilityWordStartsWithTriggerTiming(clause.RawText)))
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
      var trimmed = clause.RawText.TrimStart();

      // Exception: "Choose a Background" is a named partner-variant keyword
      // (Rule 702.124g, descriptive). Route it to the static keyword parser
      // so it lands as a StaticAbility with KeywordSource = KeywordAbility.ChooseABackground
      // rather than as an unparsable modal selection.
      if (trimmed.StartsWith("Choose a Background", StringComparison.OrdinalIgnoreCase))
      {
        return new ClauseClassification
        {
          Kind = AbilityKind.Static,
          Confidence = 0.95,
          AbilityWord = abilityWord,
        };
      }

      // Exception: "Choose target [type]..." is a targeting instruction on a
      // spell effect (e.g. "Choose target artifact or enchantment. Its owner
      // shuffles it into their library." — Unravel the Aether). This is NOT a
      // modal selection (mode choice); it resolves as a single spell effect
      // (Rule 701.19, 701.20). Route to the spell parser so
      // ShuffleIntoLibraryRule can handle the two-sentence form via
      // IMultiSpellRule.TryMatchMulti.
      if (Regex.IsMatch(trimmed, @"^Choose\s+target\s+", RegexOptions.IgnoreCase))
      {
        return new ClauseClassification
        {
          Kind = AbilityKind.Spell,
          Confidence = 0.90,
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

    // "Start your engines!" — Aetherdrift keyword (Rule 702.178). The oracle
    // text always appears as "Start your engines! (reminder...)" which tokenizes
    // as three Word tokens (Start, your, engines) plus optional reminder text.
    // IsSingleKeyword uses significantTokens <= 2 and would miss this three-word
    // keyword, routing it to the Static default at 0.50 confidence where the
    // StaticAbilityParser then fails on the reminder's "If" clause. Intercept it
    // here with the same pattern used for "Choose a Background" (Rule 702.124g)
    // — a raw-text prefix check that bypasses the heuristic.
    if (clause.RawText.TrimStart().StartsWith("Start your engines", StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
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

    // "Each player can't cast more than N spell(s) each turn." — a declarative
    // permanent static that caps cast events per player per turn (Eidolon of
    // Rhetoric, Arcane Laboratory). A rules-of-the-game-modifying continuous
    // effect (CR 611.1), NOT an imperative spell-resolution step. The generic
    // "Each player ..." → Spell route immediately below would mis-route it to the
    // spell parser (where it falls through), so intercept the "can't cast" cap
    // shape here and send it to the static path.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Each\s+player\s+can'?t\s+cast\s+more\s+than\s+\S+\s+spell",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "Each opponent can cast spells only any time they could cast a sorcery." —
    // a declarative permanent static that restricts the timing window in which
    // opponents may cast spells (Teferi, Time Raveler; Rule 116.1b: you can cast
    // an instant at any time you could cast a sorcery, but a sorcery only during
    // your own main phase while the stack is empty). This is a continuous
    // static effect (CR 611.1) that modifies the rules of the game — NOT an
    // imperative spell-resolution step. Without this intercept the generic
    // "Each opponent ..." → Spell route below mis-routes it to the spell parser
    // where it falls through to UnparsedAbility.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Each\s+opponent\s+can\s+cast\s+spells\s+only\s+any\s+time\s+they\s+could\s+cast\s+a\s+sorcery",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.97,
        AbilityWord = abilityWord,
      };
    }

    // "Each player ..." / "Each opponent ..." instructional sentences on the
    // spell (Rule 113.3a) — life-loss, discard prompts, etc. Static doesn't
    // fit; the text is an imperative resolution step.
    var trimmedRaw = clause.RawText.TrimStart();
    if (
      trimmedRaw.StartsWith("Each player", StringComparison.OrdinalIgnoreCase) ||
      trimmedRaw.StartsWith("Each opponent", StringComparison.OrdinalIgnoreCase)
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.80,
        AbilityWord = abilityWord,
      };
    }

    // "You may cast this spell as though it had flash. If you cast it any time
    // a sorcery couldn't have been cast, the controller of the permanent it
    // becomes sacrifices it at the beginning of the next cleanup step." —
    // Armor of Thorns / conditional flash grant with cleanup sacrifice
    // consequence (Rule 702.8e). Although the clause opens with "You may cast"
    // which would normally route to the spell parser via StartsWithYouMaySpellInstruction,
    // this two-sentence paragraph is a permanent static property of the card —
    // NOT a one-shot resolution step — so it belongs on the static path.
    // Intercept before the generic "You may <verb>" gate below.
    if (clause.RawText.TrimStart().StartsWith(
      "You may cast this spell as though it had flash",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "Play with the top card of your library revealed." — a continuous static
    // game-modification (CR 701.18c: "Some effects instruct a player to 'play'
    // with a certain aspect of the game changed … 'Play' in this sense means to
    // play the Magic game."; CR 401.5–401.6 govern how the revealed top card
    // interacts with spells in flight). This is a persistent, non-optional static
    // (no "You may" preamble) — the controller must keep the top card visible as
    // long as the source is on the battlefield (CR 604.2). Intercept before any
    // other clause that could misroute an imperative starting with "Play".
    if (clause.RawText.TrimStart().StartsWith(
      "Play with the top card of your library revealed",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "You may look at the top card of your library any time." A continuous
    // static permission ("any time" timing grant) that persists as long as the
    // source permanent is on the battlefield (Rule 604.2 — static abilities
    // create continuous effects). "Look" is in _spellInstructionVerbs because it also heads one-shot
    // spell effects ("Look at target player's hand."), but the "any time" phrase
    // marks this specific line as a declarative static, not a resolution step.
    // Intercept here before StartsWithYouMaySpellInstruction swallows it.
    if (clause.RawText.TrimStart().StartsWith(
      "You may look at the top card of your library any time",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "You may cast this card from your graveyard [as long as …]." — the
    // written-out conditional graveyard-recursion permission (Gravecrawler, CR
    // 601.3e). Although the clause opens with "You may cast", which the generic
    // gate below routes to the spell parser, this is a permanent static property
    // of the card (a continuous cast-from-zone permission, CR 604.2) — NOT a
    // one-shot spell-resolution step — so it belongs on the static path where
    // CastFromGraveyardConditionalRule emits the AlternativeCastEffect. Intercept
    // before StartsWithYouMaySpellInstruction swallows it (mirrors the
    // "You may cast this spell as though it had flash" interception above).
    if (clause.RawText.TrimStart().StartsWith(
      "You may cast this card from your graveyard",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "You may cast this card from exile." — an unconditional cast-from-exile
    // permission (Eternal Scourge, CR 601.3e). Same static-permission semantics
    // as the graveyard form above. Also covers the combined form "You may cast
    // this card from your graveyard or from exile." (Squee, the Immortal) which
    // begins with this prefix. Intercept before StartsWithYouMaySpellInstruction
    // swallows it (parallels the "You may cast this card from your graveyard"
    // intercept above).
    if (clause.RawText.TrimStart().StartsWith(
      "You may cast this card from exile",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "You may cast spells as though they had flash." — Vedalken Orrery and
    // similar global flash-grant static abilities (CR 702.8a: "Flash is a static
    // ability … 'Flash' means 'You may play this card any time you could cast an
    // instant.'"). This is a continuous static permission on the permanent — NOT
    // a one-shot spell-resolution step — so it belongs on the static path where
    // CastSpellsAsFlashRule emits a TimingModificationEffect with AppliesTo
    // covering all spells you cast. Intercept before StartsWithYouMaySpellInstruction
    // swallows it (parallels the "You may cast this spell as though it had flash"
    // and "You may cast this card from your graveyard" intercepts above).
    //
    // Also handles "You may cast noncreature spells as though they had flash." —
    // Valley Floodcaller and similar cards that grant flash to noncreature spells
    // only. Same static-permission semantics; routed to CastNoncreatureSpellsAsFlashRule.
    if (clause.RawText.TrimStart().StartsWith(
      "You may cast spells as though they had flash",
      StringComparison.OrdinalIgnoreCase) ||
      clause.RawText.TrimStart().StartsWith(
      "You may cast noncreature spells as though they had flash",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
        AbilityWord = abilityWord,
      };
    }

    // "You may cast [filter] spells from the top of your library." — Mystic Forge
    // and similar play-from-library static permissions. Although "Cast" is in
    // _spellInstructionVerbs (so StartsWithYouMaySpellInstruction would route it
    // to the spell parser), "from the top of your library" marks this as a
    // continuous static permission on the permanent (CR 604.2), not a one-shot
    // resolution step. Intercept here before StartsWithYouMaySpellInstruction
    // swallows it (parallels the "You may cast this card from your graveyard"
    // intercept above).
    if (clause.RawText.TrimStart().Contains(
      "from the top of your library",
      StringComparison.OrdinalIgnoreCase) &&
      clause.RawText.TrimStart().StartsWith(
      "You may cast",
      StringComparison.OrdinalIgnoreCase))
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Static,
        Confidence = 0.95,
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

    // "Target player/opponent discards/draws/gains/loses/mills ..." — spell-resolution
    // player-targeting resource instructions (e.g., "Target player discards their hand,
    // then draws four cards." — Wheel and Deal; "Target opponent draws a card." — Bargain;
    // "Target opponent reveals their hand. You choose ..." — Thought Erasure / Thoughtseize /
    // Coercion reveal-choose-discard family). Both "Target player" and "Target opponent" mark
    // a one-shot imperative spell effect (Rule 113.3a) addressed to a targeted player, not a
    // continuous static. Without this check these lines fall through to the Static default and
    // stall in StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+(player|opponent)\s+(discards?|draws?|gains?|loses?|mills?|returns?|exiles?|sacrifices?|reveals?)\s+",
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

    // "Target creature you control fights target creature ..." — fight keyword
    // action (CR 701.14). The "fights" verb marks this as a one-shot imperative
    // spell-resolution instruction, not a continuous static. Without this route
    // the clause defaults to Static and stalls in StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S.*?\s+fights\s+target\s+",
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

    // "Target player mills N cards." / "Target opponent mills N cards." — Rule 701.17
    // mill keyword action targeting a specific player or opponent. The "Target" subject
    // and imperative "mills" verb mark this as a one-shot spell-resolution instruction
    // (Rule 113.3a), not a declarative static. Without this rule the clause defaults to
    // Static and stalls at StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+(player|opponent)\s+mills?\s+\S+",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Target [filter] gets +N/+M ..." — spell-resolution single-target P/T modifier,
    // optionally combined with keyword grants and/or a duration clause
    // ("until end of turn"). The "Target" subject marks this as a one-shot imperative
    // spell effect (Rule 113.3a), not a declarative static. Uses "Target\s+\S+" so
    // it fires on "Target creature" but not on static shapes like "Equipped creature".
    // Also matches variable-X forms like "+X/+0" where one or both values may be a
    // spell variable (X/Y/Z) rather than a literal digit sequence.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S+.*?\s+gets\s+[+-](?:\d+|[XYZ])/[+-](?:\d+|[XYZ])",
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

    // "Switch target creature's power and toughness until end of turn." — spell-resolution
    // single-target P/T-switch instruction (CR 613.4d — layer 7d: "Effects that switch a
    // creature's power and toughness are applied."). "Switch" is not in
    // <see cref="_spellInstructionVerbs"/> (it's too generic a word to allowlist bare), so
    // without this narrow, fully-anchored intercept the line defaults to Static and stalls
    // in StaticAbilityParser. Distinct from the "Target [filter] gets +N/+M" rule above —
    // this is a swap operation, not an additive modifier. (Twisted Image.)
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Switch\s+target\s+creature's\s+power\s+and\s+toughness\s+until\s+end\s+of\s+turn\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Target [filter] gains [keyword] until end of turn." — spell-resolution single-target
    // keyword grant (e.g., Jump, Unnatural Speed, Lace with Moonglove). The "Target"
    // subject marks this as a one-shot imperative spell effect (Rule 113.3a, Rule 613.1c),
    // not a declarative static like "Enchanted creature has flying." The "until end of
    // turn" duration further confirms this is a transient resolution instruction.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+\S+.*?\s+gains\s+\S+.*?\s+until\s+end\s+of\s+turn",
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

    // "One or more target creatures become [color] until end of turn." — a one-or-more
    // targeting spell (CR 115.1) applying a layer-5 color-changing continuous effect
    // (CR 105.3 / CR 613.1e) to each target for the turn (Dwarven Song). The "target"
    // keyword plus the "until end of turn" duration mark this as a one-shot imperative
    // spell effect (Rule 113.3a), not a declarative static. Fully anchored on the whole
    // "One or more target creatures become [color] until end of turn" phrase so it cannot
    // capture any sibling clause; the trailing period is optional. Without this the line
    // defaults to Static, where StaticAbilityParser stalls on the complex targeting.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*One\s+or\s+more\s+target\s+creatures?\s+become\s+(white|blue|black|red|green|colorless)\s+until\s+end\s+of\s+turn\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // Mass P/T-modification spell shapes with "until end of turn" — one-shot imperative
    // spell effects (Rule 113.3a). The "until end of turn" duration is the distinguishing
    // marker between these spell forms and their permanent-static counterparts (e.g.,
    // Glorious Anthem's "Creatures you control get +1/+1." which has no duration).
    //
    // Handled subjects:
    //   "Creatures you control get ..."           (e.g., Charge, Bar the Door)
    //   "Creatures your opponents control get ..."  (e.g., Cower in Fear, Make Obsolete)
    //   "All creatures get ..."                   (e.g., Shrivel, Infest, Languish)
    //   "Attacking creatures get ..."             (e.g., Army of Allah)
    //   "Blocking creatures get ..."              (e.g., Piety, Hold the Line)
    //
    // Without these rules the classifier defaults to Static, routing these lines
    // to StaticAbilityParser where all spell-form mass anthems stall.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*(Creatures\s+you\s+control|Creatures\s+your\s+opponents\s+control|All\s+creatures|Attacking\s+creatures|Blocking\s+creatures)"
        + @"\s+get\s+[+\-]\d+/[+\-]\d+\s+until\s+end\s+of\s+turn",
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

    // "All creatures able to block target creature this turn do so." — Lure-type
    // one-shot spell effect (Rule 509.1c). The "target creature" and "this turn"
    // markers together distinguish the spell form from the static-ability forms
    // ("...this creature..." and "...enchanted creature...") which carry no duration
    // and are handled by StaticAbilityParser. Without this rule the line defaults to
    // Static and stalls there.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*All\s+creatures\s+able\s+to\s+block\s+target\s+creature\s+this\s+turn\s+do\s+so",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Target creature deals damage to itself equal to its power." — spell-resolution
    // self-damage pattern (Repentance, Justice Strike). The subject is "Target creature",
    // not the spell itself, and there is no numeric amount token between "deals" and
    // "damage" — the amount is a derived reference ("equal to its power"). Without this
    // rule the line defaults to Static and stalls in StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+creature\s+deals\s+damage\s+to\s+itself\b",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Target creature you control deals damage equal to its power to target creature ..."
    // — bite mechanic (CR 701.14 adjacent): one-directional power-based damage spell
    // (Rabid Bite, Tail Swipe). No numeric amount between "deals" and "damage" — the
    // amount is a derived reference ("equal to its power"). Without this route the clause
    // defaults to Static and stalls in StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+creature\s+you\s+control\s+deals\s+damage\s+equal\s+to\s+its\s+power\s+to\s+target\b",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
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
    //
    // Guard: skip when the clause carries a QuotedText token — that token
    // means the "deals damage" text is inside a nested ability grant
    // (e.g. "[filter] has \"{T}: This creature deals 1 damage to any target.\"").
    // Those lines are "[filter] has [quoted ability]" static grants handled by
    // StaticAbilityParser.TryParseGrantedAbility, not direct spell effects.
    if (
      !clause.Tokens.Any(t => t.Kind == OracleToken.QuotedText)
      && Regex.IsMatch(
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

    // "If this spell was cast from your hand and you've cast another spell named …" —
    // Approach of the Second Sun's self-conditional win pattern. The "If this spell
    // was cast from [zone]" preamble introduces a spell-resolution conditional
    // (CR 601.2 — casting from hand; CR 104.2b — effect may say you win the game).
    // Without this interception the classifier defaults to Static and the
    // StaticAbilityParser stalls ("Static ability parser not yet implemented").
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*If\s+this\s+spell\s+was\s+cast\s+from\s+your\s+hand\b",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Creatures target player/opponent controls gain [keyword] until end of turn." —
    // spell-resolution keyword grant to all creatures owned by a targeted player or
    // opponent (e.g. Savage Alliance mode 1). The "target player/opponent" marks this
    // as a one-shot imperative spell effect (Rule 113.3a) not a declarative static.
    // Without this route the clause defaults to Static and stalls there.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Creatures\s+target\s+(player|opponent)\s+controls\s+gain\s+\S+.*?\s+until\s+end\s+of\s+turn\.?\s*$",
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

    // "Creatures [without <keyword>] can't block this turn." — one-shot blocking
    // restriction applied by a spell (Falter, Cosmotronic Wave). The "this turn"
    // duration marks this as an imperative spell-resolution instruction (Rule 509.1c),
    // not the declarative static "This creature can't block." that lives on a
    // permanent. Without this route the clause defaults to Static and stalls in
    // StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Creatures(\s+without\s+\S+)?\s+can'?t\s+block\s+this\s+turn\.?\s*$",
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

    // "Target creature can't be blocked this turn." — spell-resolution single-target
    // evasion grant (Artful Dodge, Slip Through Space). The "this turn" duration marks
    // this as an imperative spell-resolution instruction (Rule 509.1b), not the
    // declarative static "[Self/Enchanted creature] can't be blocked" that lives on a
    // permanent. Without this route the clause defaults to Static and stalls in
    // StaticAbilityParser.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Target\s+creature\s+can'?t\s+be\s+blocked\s+this\s+turn\.?\s*$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
        AbilityWord = abilityWord,
      };
    }

    // "Double target [filter]'s life total." — Beacon of Immortality's doubling
    // keyword action (CR 701.10d). A one-shot imperative spell-resolution instruction
    // (Rule 113.3a), not a declarative static. "Double" alone is not added to
    // _spellInstructionVerbs because "Double strike" (the keyword) also starts with
    // "Double " — the targeted form is distinguished by the "target" qualifier.
    if (
      Regex.IsMatch(
        clause.RawText,
        @"^\s*Double\s+target\s+",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new ClauseClassification
      {
        Kind = AbilityKind.Spell,
        Confidence = 0.90,
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
  /// True when the entire clause is a parenthetical-wrapped activated ability,
  /// e.g. "({T}: Add {B} or {R}.)" on dual lands and basic land subtypes.
  /// The clause tokenises as a single <see cref="OracleToken.ReminderText"/>
  /// token; the inner content opens with a mana/tap symbol followed by a colon.
  /// </summary>
  private static bool IsParentheticalActivatedAbility(
    List<Token<OracleToken>> tokens,
    string rawText
  )
  {
    if (tokens.Count != 1 || tokens[0].Kind != OracleToken.ReminderText)
    {
      return false;
    }
    var inner = rawText.Trim();
    if (inner.StartsWith('(') && inner.EndsWith(')'))
    {
      inner = inner[1..^1].Trim();
    }
    return Regex.IsMatch(inner, @"^\{[^}]+\}:", RegexOptions.None);
  }

  /// <summary>
  /// True when the entire clause is the standalone Siege type-reminder line that
  /// appears on every "Battle — Siege" front face (the March-of-the-Machine
  /// Invasions): "(As a Siege enters, …)". It tokenizes as a single
  /// <see cref="OracleToken.ReminderText"/> token; the inner text opens with
  /// "As a Siege enters" and there is no host ability. CR 207.2 (italic
  /// no-game-function text); Rule 310 (battles/sieges). Routed to the Static
  /// path so <c>SiegeReminderStaticRule</c> emits a marker that preserves the
  /// reminder rather than dropping it.
  /// </summary>
  private static bool IsSiegeReminderLine(string rawText)
  {
    var trimmed = rawText.Trim();
    return Regex.IsMatch(
      trimmed,
      @"^\(As a Siege enters,",
      RegexOptions.IgnoreCase
    );
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
        // Handles keyword-action verbs that appear as standalone lines:
        // "Investigate." — the period follows immediately with no space.
        // The reminder text "(…)" may follow the period; that's also valid.
        || trimmed.StartsWith(verb + ".", StringComparison.OrdinalIgnoreCase)
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
    // Mana production on the spell body (e.g. "Add {R}{R}{R}." — Infernal Plunge).
    // Without this entry the classifier defaults to Static and routes to the
    // unimplemented static-ability parser. "Add" is an imperative resolution step
    // (Rule 701.21 — it produces mana that goes into the mana pool).
    "Add",
    // "Roll [N] dice" (CR 706) is an imperative die-roll resolution step — a spell body
    // opener (Pair o' Dice Lost: "Roll two six-sided dice. …"). Without this it defaults to
    // Static and stalls in the unimplemented static parser.
    "Roll",
    // Keyword actions (Rule 701) — these are imperative resolution steps, not
    // statics. Without an explicit entry here, a bare "Investigate." line would
    // be classified as IsSingleKeyword → Static, which sends it to the static
    // parser where it stalls. All keyword actions with standalone oracle lines
    // belong here so they route to SpellAbilityParser.
    "Investigate",
    "Populate",
    "Proliferate",
    "Regenerate",
    "Bolster",
    "Manifest",
    "Detain",
    "Explore",
    "Adapt",
    "Amass",
    "Connive",
    "Train",
    "Exert",
    "Venture",
    // Target-redirection imperative (Rule 115.7): "Change the target of …" — one-shot
    // spell-resolution instruction, not a declarative static.
    "Change",
    // Control-change imperative (Rule 701.8 / CR 613.1a — Layer 2): "Gain control of …"
    // is a one-shot resolution instruction on spells (Act of Treason, Threaten, etc.),
    // not a declarative static. The imperative form "Gain" (second person) differs from
    // the third-person "gains" that appears on permanent-static oracle text.
    "Gain",
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
    // Both real ability words and printed flavor labels take the "Word \u2014 \u2026" shape
    // and are detected here so the body strips/classifies generically. The public
    // Classify wrapper re-homes a detected printed label off AbilityWord onto
    // PrintedLabel (it is not a CR 207.2c ability word).
    if (_abilityWords.Contains(prefix) || _printedLabels.Contains(prefix))
    {
      return prefix;
    }

    return null;
  }

  /// <summary>
  /// True when <paramref name="prefix"/> is a printed flavor label (not a CR
  /// 207.2c ability word) \u2014 used by <see cref="Classify"/> to re-home it.
  /// </summary>
  private static bool IsPrintedLabel(string? prefix) =>
    prefix is not null && _printedLabels.Contains(prefix);

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
  /// True when the trigger condition — the text before the first comma — is bounded
  /// to "this turn", the signature of a delayed triggered ability created by a
  /// resolving spell (CR 603.7) rather than a printed trigger. Scoped pre-comma so a
  /// printed trigger whose *effect* says "this turn" is not misread.
  ///
  /// <para>
  /// Not every "this turn" in a trigger condition is the delayed-trigger boundary.
  /// In the death-watch-with-provenance family — "Whenever a creature dealt damage by
  /// [source] this turn <i>dies</i>, …" (Sengir Bats/Vampire, Predator Ooze, Blood
  /// Cultist, Zurgo Helmsmasher, …) — "this turn" is a backward-looking provenance
  /// qualifier on the subject ("dealt damage by [source] this turn", a
  /// <see cref="MagicAST.AST.References.DealtDamageByPredicate"/>), and the real
  /// trigger event verb ("dies") <i>follows</i> it. The same shape appears for
  /// "creatures that entered this turn <i>attack</i>" triggers. These are printed
  /// triggers (CR 603.2), not delayed triggers (CR 603.7). The distinguishing
  /// structural fact: in a delayed trigger "this turn" terminates the condition
  /// (e.g. Glimpse of Nature's "you cast a creature spell this turn"; Graceful
  /// Reprieve's "target creature dies this turn"), whereas here an event verb sits
  /// <i>after</i> the last "this turn". Detect that trailing event verb and decline
  /// the delayed-trigger classification so the printed trigger routes correctly.
  /// </para>
  /// </summary>
  private static bool TriggerConditionMentionsThisTurn(string raw)
  {
    var comma = raw.IndexOf(',');
    var condition = comma >= 0 ? raw[..comma] : raw;

    var lastThisTurn = condition.LastIndexOf("this turn", StringComparison.OrdinalIgnoreCase);
    if (lastThisTurn < 0)
    {
      return false;
    }

    // Text following the final "this turn" within the condition. When an event verb
    // (dies / attacks / enters / …) appears here, "this turn" was a provenance
    // qualifier on the subject and the condition's real event verb terminates it —
    // a printed trigger (CR 603.2), not a spell-created delayed trigger (CR 603.7).
    var afterThisTurn = condition[(lastThisTurn + "this turn".Length)..];
    if (Regex.IsMatch(
      afterThisTurn,
      @"\b(dies|die|attacks|attack|blocks|block|enters|enter)\b",
      RegexOptions.IgnoreCase))
    {
      return false;
    }

    return true;
  }

  /// <summary>
  /// After stripping the ability-word prefix (e.g. "Landfall — "), checks whether
  /// the remaining body text starts with a trigger-timing word (When/Whenever/At).
  /// Used to classify ability-word-prefixed triggered abilities correctly without
  /// per-word special-casing — covers Landfall, Threshold, Delirium, Revolt,
  /// Enrage, Fateful hour, and any future ability words generically.
  /// </summary>
  private static bool BodyAfterAbilityWordStartsWithTriggerTiming(string rawText)
  {
    var emDashIndex = rawText.IndexOf('—');
    if (emDashIndex < 0)
    {
      return false;
    }
    var body = rawText[(emDashIndex + 1)..].TrimStart();
    return body.StartsWith("When ", StringComparison.OrdinalIgnoreCase)
      || body.StartsWith("Whenever ", StringComparison.OrdinalIgnoreCase)
      || body.StartsWith("At ", StringComparison.OrdinalIgnoreCase)
      // CR 603.6: "As [this permanent] transforms into [Name]" is a static trigger
      // (fires on the transform event, timing = When). Oracle uses "As" instead of
      // "When" for the transform-into shape; detected here so the classifier routes
      // the body to the triggered path rather than defaulting to static.
      || body.StartsWith("As this creature transforms into ", StringComparison.OrdinalIgnoreCase)
      || body.StartsWith("As this permanent transforms into ", StringComparison.OrdinalIgnoreCase);
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

    // Check for mana/tap symbols before colon, or non-mana cost verbs
    // ("Sacrifice …" / "Discard …") which are written as Word tokens.
    var hasCostToken = false;
    var hasNonManaCostVerb = false;
    for (var i = 0; i < tokens.Count; i++)
    {
      var token = tokens[i];

      if (token.Kind == OracleToken.Colon)
      {
        // Found colon - check if there were any cost-like tokens before it
        // or if this is an activation keyword pattern
        if (i > 0)
        {
          return hasCostToken || hasNonManaCostVerb || IsActivationKeyword(tokens, i);
        }

        return false;
      }

      // Track if we've seen any cost tokens
      if (IsCostToken(token.Kind))
      {
        hasCostToken = true;
      }

      // Track non-mana cost verbs (Rule 701.9 Discard, Rule 701.21 Sacrifice,
      // Rule 122 Remove counters, Rule 118 Pay life). These appear as Word tokens
      // before the colon and are unambiguously costs rather than effect verbs
      // because they precede the colon separator.
      if (token.Kind == OracleToken.Word)
      {
        var word = token.ToStringValue();
        if (word.Equals("Sacrifice", StringComparison.OrdinalIgnoreCase)
          || word.Equals("Discard", StringComparison.OrdinalIgnoreCase)
          || word.Equals("Remove", StringComparison.OrdinalIgnoreCase))
        {
          hasNonManaCostVerb = true;
        }

        // "Exile <filter>:" is an exile-as-cost activated ability (Food Chain,
        // Scavenge; CR 118.8a: exile is a cost action when it precedes the colon).
        // Scoped to the clause's first token so "Exile target creature." in an
        // effect body is not mistaken for a cost verb.
        if (i == 0 && word.Equals("Exile", StringComparison.OrdinalIgnoreCase))
        {
          hasNonManaCostVerb = true;
        }

        // "Pay N life" — a life-payment cost (Rule 118.9: "Pay N life" appears
        // before the colon of an activated ability). Scoped to the clause's first
        // token so "Pay" inside an effect clause is not mistaken for a cost verb.
        if (i == 0 && word.Equals("Pay", StringComparison.OrdinalIgnoreCase))
        {
          hasNonManaCostVerb = true;
        }

        // A leading "Tap …" / "Untap …" spelled out as a Word (not the {T}/{Q}
        // symbol) is a cost verb when a colon follows — e.g. Whirler Rogue's
        // "Tap two untapped artifacts you control: Target creature can't be
        // blocked this turn." (CR 602.5 / 118.3 — tapping permanents as an
        // activation cost). Scoped to the clause's first token so the same verb
        // used imperatively inside an effect ("…, then tap target creature.")
        // is not mistaken for a cost. The {T} symbol form is already covered by
        // IsCostToken (TapSymbol/UntapSymbol).
        if (i == 0
          && (word.Equals("Tap", StringComparison.OrdinalIgnoreCase)
            || word.Equals("Untap", StringComparison.OrdinalIgnoreCase)))
        {
          hasNonManaCostVerb = true;
        }

        // A leading "Return …" is a return-to-hand cost when a colon follows —
        // e.g. Quirion Ranger's "Return a Forest you control to its owner's hand:
        // Untap target creature." (CR 602 — return-to-hand is a valid activation
        // cost; Grinning Ignus is the canonical self-bounce form). Scoped to
        // the clause's first token so "Return target creature to its owner's hand."
        // (no colon — spell effect) is not mistaken for an activated ability cost.
        if (i == 0 && word.Equals("Return", StringComparison.OrdinalIgnoreCase))
        {
          hasNonManaCostVerb = true;
        }

        // "Put a -1/-1 counter on this creature:" — placing counters as an
        // activation cost (Devoted Druid, Quillspike). CR 122.1: counter
        // placement is a valid cost action. Scoped to the clause's first token
        // so "Put a +1/+1 counter on target creature." (no colon — a spell/triggered
        // effect) is not mistaken for an activated ability cost.
        if (i == 0 && word.Equals("Put", StringComparison.OrdinalIgnoreCase))
        {
          hasNonManaCostVerb = true;
        }
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
