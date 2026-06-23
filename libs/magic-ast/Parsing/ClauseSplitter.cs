namespace MagicAST.Parsing;

using System.Text.RegularExpressions;
using MagicAST.Parsing.Tokens;
using Superpower.Model;
// Use our own TextSpan, not Superpower's
using TextSpan = MagicAST.AST.TextSpan;

/// <summary>
/// Represents a clause (ability segment) extracted from oracle text.
/// Each clause typically represents a single ability.
/// </summary>
public sealed record OracleClause
{
  /// <summary>
  /// The tokens in this clause.
  /// </summary>
  public required TokenList<OracleToken> Tokens { get; init; }

  /// <summary>
  /// The raw text of this clause.
  /// </summary>
  public required string RawText { get; init; }

  /// <summary>
  /// The span of this clause in the original oracle text.
  /// </summary>
  public required TextSpan SourceSpan { get; init; }

  /// <summary>
  /// Whether this clause is a modal option (part of a "Choose" ability).
  /// </summary>
  public bool IsModalOption { get; init; }

  /// <summary>
  /// For a modal-header clause (e.g. "Choose one —"), the option clauses
  /// that belong to it. Null on every non-modal-header clause. Each option
  /// clause has its bullet prefix stripped and is independently tokenized,
  /// so the parser can dispatch each through the normal classifier path.
  /// </summary>
  public IReadOnlyList<OracleClause>? ModalOptions { get; init; }

  /// <summary>
  /// For a saga-preamble clause (the reminder paragraph that starts with
  /// "(As this Saga enters..."), the chapter clauses that follow it. Each
  /// chapter clause carries its <see cref="ChapterNumbers"/> (e.g., [1] or
  /// [1, 2]) and has its "<roman> — " prefix stripped on the body side.
  /// Null on every non-saga-preamble clause.
  /// </summary>
  public IReadOnlyList<OracleClause>? SagaChapters { get; init; }

  /// <summary>
  /// For a saga chapter body clause, the lore-counter numbers that fire it.
  /// E.g. <c>[1]</c> for <c>"I — ..."</c>, <c>[1, 2]</c> for <c>"I, II — ..."</c>.
  /// Null for clauses that aren't saga chapters.
  /// </summary>
  public IReadOnlyList<int>? ChapterNumbers { get; init; }

  /// <summary>
  /// For a level-up cluster head clause (one that <see cref="ClauseSplitter"/>
  /// has pre-grouped), the LEVEL N-M stanzas attached to it. The head clause's
  /// RawText is the "Level up {cost}" line (plus reminder text); the stanzas
  /// carry the LEVEL ranges, P/T strings, and inner-ability sub-clauses.
  /// Null on every non-level-up clause.
  /// </summary>
  public IReadOnlyList<LevelStanzaClause>? LevelUpStanzas { get; init; }

  /// <summary>
  /// For level-up cards, the level range this clause applies to.
  /// </summary>
  public (int Min, int Max)? LevelRange { get; init; }

  /// <summary>
  /// For a Class cluster head clause (one that <see cref="ClauseSplitter"/> has
  /// pre-grouped), the base abilities printed in the top text-box section (CR 716.3)
  /// and the class level bars. The head clause's RawText is the reminder preamble
  /// "(Gain the next level as a sorcery to add its ability.)"; the base-ability and
  /// level-bar bodies hang off these fields. Null on every non-Class clause.
  /// </summary>
  public IReadOnlyList<OracleClause>? ClassBaseAbilities { get; init; }

  /// <summary>
  /// For a Class cluster head clause, the class level bars in oracle-text order.
  /// Null on every non-Class clause.
  /// </summary>
  public IReadOnlyList<ClassLevelClause>? ClassLevels { get; init; }
}

/// <summary>
/// One pre-grouped class level bar from a Class cluster (CR 716.2). Carries the
/// level number, the raw activation-cost text from the bar (e.g. <c>"{1}{R}"</c>),
/// and the ability sub-clauses printed in that bar's text-box section. The parser
/// converts these into a <c>ClassLevel</c> AST node.
/// </summary>
public sealed record ClassLevelClause
{
  /// <summary>The level number this bar advances the Class to (2, 3, …).</summary>
  public required int Level { get; init; }

  /// <summary>Raw activation-cost text from the bar, verbatim (e.g. <c>"{1}{R}"</c>).</summary>
  public required string CostText { get; init; }

  /// <summary>
  /// Ability clauses granted at this level, in source order. Each is dispatched
  /// through <c>AbilityParserRegistry</c> like any body ability.
  /// </summary>
  public IReadOnlyList<OracleClause> AbilityClauses { get; init; } = [];
}

/// <summary>
/// One pre-grouped LEVEL N-M stanza from a level-up cluster. Carries the
/// raw P/T strings and the inner-ability sub-clauses; the parser converts
/// these into <c>LevelStanza</c> AST nodes.
/// </summary>
public sealed record LevelStanzaClause
{
  /// <summary>Inclusive lower bound.</summary>
  public required int MinLevel { get; init; }

  /// <summary>Inclusive upper bound. Null for open-ended "N+" stanzas.</summary>
  public int? MaxLevel { get; init; }

  /// <summary>Raw power string from the P/T line (e.g. <c>"3"</c> or <c>"*"</c>).</summary>
  public required string PowerText { get; init; }

  /// <summary>Raw toughness string from the P/T line.</summary>
  public required string ToughnessText { get; init; }

  /// <summary>
  /// Inner ability clauses for this stanza, in source order. Each gets
  /// dispatched through <c>AbilityParserRegistry</c> to produce the body
  /// abilities the creature has while in this level range.
  /// </summary>
  public IReadOnlyList<OracleClause> InnerAbilityClauses { get; init; } = [];
}

/// <summary>
/// Splits oracle text into individual ability clauses.
/// Handles paragraph breaks, modal structures, level/chapter markers, and loyalty abilities.
/// </summary>
public sealed class ClauseSplitter
{
  private readonly OracleTokenizer _tokenizer = new();

  /// <summary>
  /// Splits oracle text into individual clauses.
  /// </summary>
  /// <param name="oracleText">The oracle text to split.</param>
  /// <returns>A sequence of clauses.</returns>
  public IReadOnlyList<OracleClause> Split(string oracleText)
  {
    if (string.IsNullOrWhiteSpace(oracleText))
    {
      return [];
    }

    var clauses = new List<OracleClause>();
    var paragraphs = SplitIntoParagraphs(oracleText).ToList();

    for (var i = 0; i < paragraphs.Count; i++)
    {
      var (paragraphText, paragraphStart) = paragraphs[i];

      // Additional-cost prefix lines ("As an additional cost to cast this
      // spell, [cost].") are spell-casting annotations, not oracle abilities.
      // They belong on Card.Attributes as AdditionalCostsAttribute (populated
      // by AttributeExtractor). Skipping here prevents a spurious UnparsedAbility
      // from appearing in Oracle.Abilities for the cost line.
      if (IsAdditionalCostPrefix(paragraphText))
      {
        continue;
      }

      // Kicker (CR 702.33) is NOT skipped here. It is a static ability whose
      // keyword combinator (KickerKeyword) decomposes the line into a
      // StaticAbility{KeywordSource:"Kicker", AdditionalCastCostEffect} via the
      // Static-classified KeywordListRule path. The line must therefore reach
      // ClauseSplitter's normal paragraph processing — not be dropped — so the
      // keyword ability surfaces in Oracle.Abilities. The separate
      // "if this spell was kicked, ..." line (CR 702.33e / 607) is an ordinary
      // linked ability and parses on its own.

      // Mana-symbol reminder lines (e.g. "({R/W} can be paid with either {R} or {W}.)") are
      // cosmetic oracle annotations that gloss the card's hybrid or Phyrexian mana cost.
      // They carry no oracle-ability semantics (Rule 107.4) and must be dropped here so
      // they do not produce spurious UnparsedAbility nodes in Oracle.Abilities.
      if (IsManaSymbolReminder(paragraphText))
      {
        continue;
      }

      // Cycling-provenance reminder lines (e.g. "(Cycling from Tranquil Thicket.)") are
      // standalone parenthetical annotations that record which cycling ability produced a
      // given game action in certain printed or digital formats. They carry no oracle-ability
      // semantics and must be dropped here so they do not produce spurious UnparsedAbility
      // nodes in Oracle.Abilities.
      if (IsCyclingProvenanceReminder(paragraphText))
      {
        continue;
      }

      // Level-up cost paragraphs ("Level up {cost} (reminder)") start a
      // multi-stanza superstructure. Consume the cost paragraph plus all
      // following LEVEL stanza paragraphs (with their P/T and inner-ability
      // bodies) into one cluster clause carrying LevelUpStanzas.
      if (IsLevelUpCostParagraph(paragraphText))
      {
        var stanzas = new List<LevelStanzaClause>();
        var lookahead = i + 1;
        while (lookahead < paragraphs.Count)
        {
          var stanzaConsumed = TryConsumeLevelStanza(paragraphs, lookahead, out var stanza);
          if (stanzaConsumed == 0 || stanza is null)
          {
            break;
          }
          stanzas.Add(stanza);
          lookahead += stanzaConsumed;
        }

        if (stanzas.Count > 0)
        {
          clauses.Add(
            CreateClause(paragraphText, paragraphStart) with { LevelUpStanzas = stanzas }
          );
          i = lookahead - 1;
          continue;
        }
      }

      // Class preambles (the reminder line "(Gain the next level as a sorcery to
      // add its ability.)") head a Class superstructure (CR 716, "Class Cards").
      // Consume the reminder, the base-ability paragraph(s) printed in the top
      // text-box section (CR 716.3), and the class level bars ("{cost}: Level N"
      // each followed by the abilities granted at that level) into one cluster
      // clause. The reminder carries no game function (CR 207.2) and is kept only
      // as the head clause's RawText anchor.
      if (IsClassPreamble(paragraphText))
      {
        var consumedTo = TryConsumeClassCluster(
          paragraphs,
          i,
          out var baseAbilities,
          out var levels
        );
        if (consumedTo > i && levels.Count > 0)
        {
          clauses.Add(
            CreateClause(paragraphText, paragraphStart) with
            {
              ClassBaseAbilities = baseAbilities,
              ClassLevels = levels,
            }
          );
          i = consumedTo;
          continue;
        }
      }

      // Saga preambles (parenthetical "(As this Saga enters..." paragraphs)
      // may be followed by chapter paragraphs ("I — ...", "I, II — ...").
      // Consume those greedily into a single saga-preamble clause carrying
      // the chapters on its SagaChapters field.
      if (IsSagaPreamble(paragraphText))
      {
        var chapterClauses = new List<OracleClause>();
        var lookahead = i + 1;
        while (lookahead < paragraphs.Count)
        {
          var (chapterText, chapterStart) = paragraphs[lookahead];
          var parsedChapter = TryParseChapterParagraph(chapterText, chapterStart);
          if (parsedChapter is null)
          {
            break;
          }
          chapterClauses.Add(parsedChapter);
          lookahead++;
        }

        if (chapterClauses.Count > 0)
        {
          clauses.Add(
            CreateClause(paragraphText, paragraphStart) with { SagaChapters = chapterClauses }
          );
          i = lookahead - 1;
          continue;
        }
      }

      // Modal headers may be followed by bullet-prefixed option paragraphs.
      // Consume those greedily into a single modal-header clause carrying
      // the options on its ModalOptions field.
      //
      // Two preamble shapes open a modal-bullet group:
      //   • A spell-level "Choose one —" header — IsModalHeader.
      //   • A trigger-level "When [X] dies, choose one —" preamble —
      //     EndsWithModalSelector. The triggered-ability parser unpacks the
      //     attached ModalOptions into a ModalEffect inside the trigger's
      //     Effects list (descriptive parallel to the spell-level shape).
      if (
        (IsModalHeader(paragraphText) || EndsWithModalSelector(paragraphText))
        && !ContainsBullet(paragraphText)
      )
      {
        var lookaheadOptions = new List<OracleClause>();
        var lookahead = i + 1;
        while (lookahead < paragraphs.Count && IsBulletOption(paragraphs[lookahead].Text))
        {
          var (optText, optStart) = paragraphs[lookahead];
          var stripped = StripBullet(optText);
          lookaheadOptions.Add(CreateClause(stripped, optStart + (optText.Length - stripped.Length), isModalOption: true));
          lookahead++;
        }

        if (lookaheadOptions.Count > 0)
        {
          // Emit one combined header clause with the options attached.
          clauses.Add(CreateClause(paragraphText, paragraphStart) with { ModalOptions = lookaheadOptions });
          i = lookahead - 1;
          continue;
        }
      }

      var paragraphClauses = ProcessParagraph(paragraphText, paragraphStart);
      clauses.AddRange(paragraphClauses);
    }

    return clauses;
  }

  private static bool ContainsBullet(string text) => text.Contains('•');

  private static bool IsBulletOption(string text) => text.StartsWith("•");

  /// <summary>
  /// Recognises "As an additional cost to cast this spell, [cost]." lines.
  /// These lines are spell-casting annotations (CR 117.12), not oracle abilities.
  /// AttributeExtractor parses the cost and emits AdditionalCostsAttribute instead.
  /// Stripping them here prevents a spurious UnparsedAbility in Oracle.Abilities.
  /// </summary>
  private static bool IsAdditionalCostPrefix(string text) =>
    text.StartsWith("As an additional cost to cast this spell,", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Recognises standalone mana-symbol reminder lines added to cards with hybrid,
  /// Phyrexian, monocolored-hybrid, and snow mana costs. These lines are cosmetic
  /// oracle annotations — not abilities — and must be dropped at clause-split time
  /// so they do not produce spurious <c>UnparsedAbility</c> nodes.
  ///
  /// <para>
  /// Examples:
  /// <code>
  ///   ({R/W} can be paid with either {R} or {W}.)
  ///   ({W/P} can be paid with either {W} or 2 life.)
  ///   ({G/P} can be paid with either {G} or 2 life.)
  ///   ({S} can be paid with one mana from a snow source.)
  /// </code>
  /// </para>
  ///
  /// <para>
  /// The pattern is deliberately narrow to avoid stripping genuine keyword
  /// reminders such as:
  /// <list type="bullet">
  ///   <item>Echo: <c>(At the beginning of your upkeep, if this...)</c></item>
  ///   <item>Bestow: <c>(If you cast this card for its bestow cost...)</c></item>
  ///   <item>Persist: <c>(When this creature dies, if it had no -1/-1 counters...)</c></item>
  /// </list>
  /// Those lines begin with <c>(At the beginning</c>, <c>(If you cast</c>, or
  /// <c>(When this creature</c> — none of which open with a mana symbol.
  /// </para>
  ///
  /// <para>
  /// Rule 107.4: Mana symbols represent amounts of mana that must be paid,
  /// or produced, or cost alterations (for hybrid and Phyrexian variants).
  /// These reminder lines exist solely to gloss the hybrid/Phyrexian cost
  /// printed on the card face; they carry no oracle-rule semantics of their own.
  /// </para>
  /// </summary>
  private static readonly System.Text.RegularExpressions.Regex ManaSymbolReminderPattern =
    new(
      @"^\s*\(\s*\{[^}]+\}\s+can\s+be\s+paid\s+with\b[^)]*\)\s*\.?\s*$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

  private static bool IsManaSymbolReminder(string text) =>
    ManaSymbolReminderPattern.IsMatch(text);

  /// <summary>
  /// Recognises standalone cycling-provenance reminder lines that appear as separate
  /// oracle text paragraphs in certain printed or digital formats. These lines record
  /// which cycling ability produced a given game action — for example:
  /// <code>
  ///   (Cycling from Tranquil Thicket.)
  ///   (cycling from Forgotten Cave.)
  ///   (Cycling from Shoreline Ranger.)
  /// </code>
  ///
  /// <para>
  /// The pattern is deliberately narrow: it requires the line to be a complete
  /// parenthetical that contains "cycling from" followed by one or more non-empty words
  /// (the card name). It does not match lines where "cycling from" appears mid-sentence
  /// in a genuine oracle ability, such as:
  /// <list type="bullet">
  ///   <item>"Return up to two target cards with cycling from your graveyard to your hand."</item>
  /// </list>
  /// Those lines do not start with "(" and thus do not match this regex.
  /// </para>
  /// </summary>
  private static readonly System.Text.RegularExpressions.Regex CyclingProvenanceReminderPattern =
    new(
      @"^\s*\(.*[Cc]ycling\s+from\b.*\)\s*\.?\s*$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );

  private static bool IsCyclingProvenanceReminder(string text) =>
    CyclingProvenanceReminderPattern.IsMatch(text);

  /// <summary>
  /// Recognises the head paragraph of a Level Up cluster: a line that opens
  /// with "Level up " (case-insensitive). Reminder text in parens is allowed
  /// on the same line; it's part of the cost paragraph.
  /// </summary>
  private static bool IsLevelUpCostParagraph(string text) =>
    text.StartsWith("Level up ", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Attempts to consume one LEVEL N-M (or N+) stanza starting at
  /// <paramref name="startIndex"/> in the paragraph list. Returns the number
  /// of paragraphs consumed; 0 means no stanza was consumed and the caller
  /// should stop looking for further stanzas.
  /// </summary>
  /// <remarks>
  /// Layout per stanza:
  /// <code>
  ///   LEVEL 1-2           ← header paragraph
  ///   3/3                 ← P/T paragraph
  ///   This creature has flying.   ← zero or more inner-ability paragraphs
  ///                                  (stops on the next LEVEL header or EOF)
  /// </code>
  /// </remarks>
  private int TryConsumeLevelStanza(
    IReadOnlyList<(string Text, int Start)> paragraphs,
    int startIndex,
    out LevelStanzaClause? stanza
  )
  {
    stanza = null;
    if (startIndex >= paragraphs.Count)
    {
      return 0;
    }

    var headerRange = TryParseLevelRange(paragraphs[startIndex].Text);
    if (!headerRange.HasValue)
    {
      return 0;
    }
    int min = headerRange.Value.Min;
    int? max = headerRange.Value.Max == int.MaxValue ? (int?)null : headerRange.Value.Max;

    // Next paragraph: P/T line.
    if (startIndex + 1 >= paragraphs.Count)
    {
      return 0;
    }
    var ptText = paragraphs[startIndex + 1].Text;
    var pt = TryParsePowerToughnessLine(ptText);
    if (!pt.HasValue)
    {
      return 0;
    }

    int consumed = 2;
    var innerClauses = new List<OracleClause>();
    for (var j = startIndex + 2; j < paragraphs.Count; j++)
    {
      // Stop on next stanza header.
      if (TryParseLevelRange(paragraphs[j].Text).HasValue)
      {
        break;
      }
      innerClauses.Add(CreateClause(paragraphs[j].Text, paragraphs[j].Start));
      consumed++;
    }

    stanza = new LevelStanzaClause
    {
      MinLevel = min,
      MaxLevel = max,
      PowerText = pt.Value.Power,
      ToughnessText = pt.Value.Toughness,
      InnerAbilityClauses = innerClauses,
    };
    return consumed;
  }

  /// <summary>
  /// Parses a P/T-only paragraph like <c>"3/3"</c> or <c>"*+1/2"</c> into
  /// (power, toughness) raw strings. Returns null if the paragraph isn't a
  /// pure P/T line.
  /// </summary>
  private static (string Power, string Toughness)? TryParsePowerToughnessLine(string text)
  {
    var trimmed = text.Trim();
    var slash = trimmed.IndexOf('/');
    if (slash <= 0 || slash >= trimmed.Length - 1)
    {
      return null;
    }
    var power = trimmed[..slash].Trim();
    var toughness = trimmed[(slash + 1)..].Trim();
    if (string.IsNullOrEmpty(power) || string.IsNullOrEmpty(toughness))
    {
      return null;
    }
    // Reject paragraphs that aren't pure P/T (e.g. "3/3 with flying").
    // Either side may contain '+', '*', '-', digits — but no spaces, no
    // alpha words.
    if (HasInnerWhitespace(power) || HasInnerWhitespace(toughness))
    {
      return null;
    }
    return (power, toughness);
  }

  private static bool HasInnerWhitespace(string s) =>
    s.Any(char.IsWhiteSpace);

  /// <summary>
  /// Class preamble — the reminder paragraph that introduces the level-up
  /// mechanic on "Enchantment — Class" cards (CR 716). Always wrapped in
  /// parentheses; the printed wording is "(Gain the next level as a sorcery to
  /// add its ability.)". Matched on the "Gain the next level" opening after the
  /// leading paren so a future minor reminder-wording change still anchors.
  /// </summary>
  private static bool IsClassPreamble(string text) =>
    Regex.IsMatch(
      text.Trim(),
      @"^\(\s*Gain the next level as a sorcery",
      RegexOptions.IgnoreCase
    );

  /// <summary>
  /// Recognises a class level bar paragraph: "{cost}: Level N" (CR 716.2 /
  /// 107.16 — a level bar pairs an activation cost with a level number).
  /// Returns the (level number, raw cost text) on success; null otherwise.
  /// </summary>
  private static (int Level, string CostText)? TryParseClassLevelBar(string text)
  {
    var m = Regex.Match(
      text.Trim(),
      @"^(?<cost>\{[^}]+\}(?:\{[^}]+\})*)\s*:\s*Level\s+(?<n>\d+)\s*$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    if (!int.TryParse(m.Groups["n"].Value, out var level))
    {
      return null;
    }
    return (level, m.Groups["cost"].Value);
  }

  /// <summary>
  /// Consumes a Class cluster starting at the reminder preamble paragraph
  /// <paramref name="headIndex"/>. Gathers the base-ability paragraph(s) printed
  /// before the first level bar (CR 716.3) and each "{cost}: Level N" bar with the
  /// ability paragraphs printed in its section. Returns the index of the LAST
  /// paragraph consumed (so the caller sets <c>i = result</c>); equals
  /// <paramref name="headIndex"/> when no level bar was found.
  /// </summary>
  private int TryConsumeClassCluster(
    IReadOnlyList<(string Text, int Start)> paragraphs,
    int headIndex,
    out IReadOnlyList<OracleClause> baseAbilities,
    out IReadOnlyList<ClassLevelClause> levels
  )
  {
    var baseList = new List<OracleClause>();
    var levelList = new List<ClassLevelClause>();
    baseAbilities = baseList;
    levels = levelList;

    // Base abilities: every paragraph after the reminder, up to the first level bar.
    var j = headIndex + 1;
    while (j < paragraphs.Count && TryParseClassLevelBar(paragraphs[j].Text) is null)
    {
      baseList.Add(CreateClause(paragraphs[j].Text, paragraphs[j].Start));
      j++;
    }

    // Level bars: each "{cost}: Level N" followed by its granted-ability paragraphs
    // (up to the next bar or end of text).
    var lastConsumed = headIndex;
    while (j < paragraphs.Count)
    {
      var bar = TryParseClassLevelBar(paragraphs[j].Text);
      if (bar is null)
      {
        break;
      }
      var abilityClauses = new List<OracleClause>();
      var k = j + 1;
      while (k < paragraphs.Count && TryParseClassLevelBar(paragraphs[k].Text) is null)
      {
        abilityClauses.Add(CreateClause(paragraphs[k].Text, paragraphs[k].Start));
        k++;
      }
      levelList.Add(
        new ClassLevelClause
        {
          Level = bar.Value.Level,
          CostText = bar.Value.CostText,
          AbilityClauses = abilityClauses,
        }
      );
      lastConsumed = k - 1;
      j = k;
    }

    // If we found level bars, the base-ability paragraphs were legitimately part of
    // the cluster. If no bar was found at all, the reminder stands alone — signal
    // "nothing consumed" so the caller falls through to normal processing.
    return levelList.Count > 0 ? lastConsumed : headIndex;
  }

  /// <summary>
  /// Saga preamble — the reminder paragraph that introduces the lore-counter
  /// mechanic. Always wrapped in parentheses; always starts with "As this Saga
  /// enters". Sometimes follows immediately with "Sacrifice after [Roman]" on
  /// the same line, or "...and after your draw step, add a lore counter."
  /// </summary>
  private static bool IsSagaPreamble(string text) =>
    text.StartsWith("(As this Saga enters", StringComparison.OrdinalIgnoreCase)
    || text.StartsWith("(As this saga enters", StringComparison.OrdinalIgnoreCase);

  /// <summary>
  /// Parses a saga chapter paragraph like <c>"I — Effect."</c> or
  /// <c>"I, II — Effect."</c> into an OracleClause whose RawText is the body
  /// (post-em-dash) and whose ChapterNumbers field carries the chapter counts.
  /// Returns null if the paragraph isn't a chapter header.
  /// </summary>
  private OracleClause? TryParseChapterParagraph(string text, int paragraphStart)
  {
    // Match: roman numerals (possibly comma-separated) followed by em-dash + body.
    // Em-dash is U+2014; tolerate hyphen-with-spaces as a fallback.
    var emDashIndex = text.IndexOf('—');
    if (emDashIndex < 0)
    {
      return null;
    }

    var prefix = text[..emDashIndex].Trim();
    var body = text[(emDashIndex + 1)..].TrimStart();

    var numbers = TryParseChapterNumbers(prefix);
    if (numbers is null || numbers.Count == 0)
    {
      return null;
    }

    var bodyStart = paragraphStart + (text.Length - body.Length);
    return CreateClause(body, bodyStart) with { ChapterNumbers = numbers };
  }

  /// <summary>
  /// Parses chapter prefixes like <c>"I"</c>, <c>"III"</c>, <c>"I, II"</c>,
  /// <c>"II, III"</c> into a list of lore-counter numbers.
  /// </summary>
  private static IReadOnlyList<int>? TryParseChapterNumbers(string prefix)
  {
    var parts = prefix.Split(',');
    var result = new List<int>(parts.Length);
    foreach (var part in parts)
    {
      var trimmed = part.Trim();
      var value = ParseRoman(trimmed);
      if (value <= 0)
      {
        return null;
      }
      result.Add(value);
    }
    return result;
  }

  private static int ParseRoman(string roman)
  {
    // Saga chapters realistically run I-V. Wider Roman parsing isn't needed.
    var values = new Dictionary<char, int>
    {
      ['I'] = 1,
      ['V'] = 5,
      ['X'] = 10,
    };

    int total = 0;
    int previous = 0;
    foreach (var ch in roman.ToUpperInvariant())
    {
      if (!values.TryGetValue(ch, out var current))
      {
        return -1;
      }
      total += current;
      if (previous != 0 && previous < current)
      {
        // Subtractive notation: IV, IX. Adjust: we counted both, but the
        // previous should have been negative.
        total -= 2 * previous;
      }
      previous = current;
    }
    return total;
  }

  private static string StripBullet(string text)
  {
    var stripped = text.TrimStart('•');
    return stripped.TrimStart();
  }

  /// <summary>
  /// Splits oracle text into paragraphs by newline characters.
  /// </summary>
  private static IEnumerable<(string Text, int Start)> SplitIntoParagraphs(string text)
  {
    var start = 0;
    var lines = text.Split('\n');

    foreach (var line in lines)
    {
      var trimmed = line.Trim();
      if (!string.IsNullOrEmpty(trimmed))
      {
        yield return (trimmed, start);
      }

      start += line.Length + 1; // +1 for the newline
    }
  }

  /// <summary>
  /// Processes a single paragraph into one or more clauses.
  /// Handles modal structures and level/chapter markers.
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessParagraph(string paragraphText, int paragraphStart)
  {
    // Check for modal structure: "Choose one —" followed by bullets
    if (IsModalHeader(paragraphText))
    {
      return ProcessModalParagraph(paragraphText, paragraphStart);
    }

    // Check for level-up structure
    var levelRange = TryParseLevelRange(paragraphText);
    if (levelRange.HasValue)
    {
      return ProcessLevelParagraph(paragraphText, paragraphStart, levelRange.Value);
    }

    // Standard single-clause paragraph
    return [CreateClause(paragraphText, paragraphStart)];
  }

  /// <summary>
  /// Checks if a paragraph starts a modal structure.
  /// </summary>
  private static bool IsModalHeader(string text)
  {
    var lower = text.ToLowerInvariant();
    return lower.StartsWith("choose one")
      || lower.StartsWith("choose two")
      || lower.StartsWith("choose three")
      || lower.StartsWith("choose any number")
      || lower.StartsWith("choose up to");
  }

  /// <summary>
  /// Checks if a paragraph ENDS with a modal selector phrase that opens an
  /// inline modal-bullet group, e.g.
  /// <c>"When Ao dies, choose one —"</c>. These preambles are not modal headers
  /// in their own right (the leading text classifies as Triggered/Activated);
  /// the modal choice is one step inside the ability's resolution. Detecting
  /// the trailing selector lets <see cref="ClauseSplitter"/> still consume the
  /// following bullet lines into the same clause's <c>ModalOptions</c>, which
  /// the triggered/activated parser unpacks into a <c>ModalEffect</c>.
  /// </summary>
  private static bool EndsWithModalSelector(string text)
  {
    // Strip a trailing em-dash (and optional whitespace/period) so we can match
    // the selector phrase by its tail.
    var trimmed = text.TrimEnd();
    if (trimmed.EndsWith('—'))
    {
      trimmed = trimmed[..^1].TrimEnd();
    }
    var lower = trimmed.ToLowerInvariant();
    return lower.EndsWith("choose one")
      || lower.EndsWith("choose two")
      || lower.EndsWith("choose three")
      || lower.EndsWith("choose any number")
      || System.Text.RegularExpressions.Regex.IsMatch(lower, @"choose up to \w+$");
  }

  /// <summary>
  /// Processes a modal paragraph (inline bullets) into one combined modal-header clause.
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessModalParagraph(
    string paragraphText,
    int paragraphStart
  )
  {
    OracleClause? headerClause = null;
    var options = new List<OracleClause>();

    // Split by bullet points
    var parts = paragraphText.Split('•');

    // First part is the header (e.g., "Choose one —")
    if (parts.Length > 0)
    {
      var header = parts[0].Trim();
      if (!string.IsNullOrEmpty(header))
      {
        headerClause = CreateClause(header, paragraphStart);
      }
    }

    // Remaining parts are modal options
    var offset = parts[0].Length + 1; // +1 for the bullet
    for (var i = 1; i < parts.Length; i++)
    {
      var option = parts[i].Trim();
      if (!string.IsNullOrEmpty(option))
      {
        options.Add(CreateClause(option, paragraphStart + offset, isModalOption: true));
      }

      offset += parts[i].Length + 1;
    }

    if (headerClause is null)
    {
      return options;
    }

    return options.Count > 0
      ? [headerClause with { ModalOptions = options }]
      : [headerClause];
  }

  /// <summary>
  /// Attempts to parse a level range from level-up card text.
  /// </summary>
  private static (int Min, int Max)? TryParseLevelRange(string text)
  {
    var lower = text.ToLowerInvariant();

    // Match patterns like "LEVEL 1-2", "LEVEL 3-4", "LEVEL 5+"
    if (!lower.StartsWith("level "))
    {
      return null;
    }

    var levelPart = text[6..].Trim();

    // "5+" pattern
    if (levelPart.EndsWith("+"))
    {
      if (int.TryParse(levelPart[..^1], out var min))
      {
        return (min, int.MaxValue);
      }
    }

    // "1-2" pattern
    var dashIndex = levelPart.IndexOf('-');
    if (dashIndex > 0)
    {
      var minStr = levelPart[..dashIndex];
      var maxStr = levelPart[(dashIndex + 1)..];
      if (int.TryParse(minStr, out var min) && int.TryParse(maxStr, out var max))
      {
        return (min, max);
      }
    }

    return null;
  }

  /// <summary>
  /// Processes a level paragraph, extracting the level range.
  /// </summary>
  private IReadOnlyList<OracleClause> ProcessLevelParagraph(
    string paragraphText,
    int paragraphStart,
    (int Min, int Max) levelRange
  )
  {
    // Remove the "LEVEL X-Y" prefix
    var colonIndex = paragraphText.IndexOf(':');
    var contentStart = colonIndex >= 0 ? colonIndex + 1 : 0;
    var content = paragraphText[contentStart..].Trim();

    if (string.IsNullOrEmpty(content))
    {
      return [];
    }

    var clause = CreateClause(content, paragraphStart + contentStart);
    return [clause with { LevelRange = levelRange }];
  }

  /// <summary>
  /// Creates a clause from text, tokenizing it.
  /// </summary>
  private OracleClause CreateClause(string text, int startOffset, bool isModalOption = false)
  {
    var tokenResult = _tokenizer.TryTokenize(text);

    TokenList<OracleToken> tokens;
    if (tokenResult.HasValue)
    {
      tokens = tokenResult.Value;
    }
    else
    {
      // Empty token list for failed tokenization
      tokens = new TokenList<OracleToken>([]);
    }

    return new OracleClause
    {
      Tokens = tokens,
      RawText = text,
      SourceSpan = new TextSpan(startOffset, text.Length),
      IsModalOption = isModalOption,
    };
  }
}
