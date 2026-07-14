using Flowthru.Data.Schema;

namespace MagicAtlas.Data._08_Reporting.Schemas;

/// <summary>
/// The <b>combo-anchored</b> pick surface for the mast-tdd-loop — the demand-side complement to the
/// interaction-triage report's <c>allComboBlockingCards</c>. This ranks the unparsed <b>hub cards</b>
/// by the combo-popularity value each one gates — so the loop can pick a card because reconstructing
/// 1,000 real combos waits on it, not because it shares a lexical template with many others.
///
/// <para>
/// Beyond the raw <c>CardGap</c> overlay it adds the four axes that make a hub actionable:
/// <list type="bullet">
///   <item><see cref="ComboAnchor.SoleBlockerCount"/> — combos where this hub is the ONLY unparsed
///   card, i.e. parsing it alone makes them reconstructable (modulo projection/precision).</item>
///   <item><see cref="ComboAnchor.BlockReason"/> — splits <c>parser-family</c> (dispatchable) from
///   <c>missing-from-corpus</c> (out of scope) and <c>empty-oracle-text</c> (genuinely textless AFTER
///   composing double-faced <c>CardFaces</c> — ≈none in scope, since DFCs parse from their faces).</item>
///   <item><see cref="ComboAnchor.CoStars"/> — the neighborhood: co-stars flagged
///   <c>alsoUnparsed</c> (close together) vs already-parsing (light up free once the hub lands).</item>
///   <item><see cref="ComboAnchor.TopPayoffs"/> — what the blocked combos actually do.</item>
/// </list>
/// </para>
///
/// <para>
/// Promoted verbatim from tests/magic-ast-tests/Data/_08_Reporting/Schemas/ComboAnchorReport.cs.
/// Measurement/pick surface only — never a gate.
/// </para>
/// </summary>
[FlowthruSchema]
public partial record ComboAnchorReport
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  [SerializedLabel("totalCombos")]
  public int TotalCombos { get; init; }

  /// <summary>Distinct unparsed hub cards that gate at least one combo.</summary>
  [SerializedLabel("totalHubs")]
  public int TotalHubs { get; init; }

  /// <summary>Hubs blocked on a genuine parser gap (in corpus, has oracle text) — the mast-tdd-loop's work.</summary>
  [SerializedLabel("parserFamilyHubs")]
  public int ParserFamilyHubs { get; init; }

  [SerializedLabel("parserFamilyMass")]
  public long ParserFamilyMass { get; init; }

  /// <summary>Hubs with no rules text even after composing double-faced <c>CardFaces</c> — genuinely textless (≈none in scope).</summary>
  [SerializedLabel("emptyTextHubs")]
  public int EmptyTextHubs { get; init; }

  [SerializedLabel("emptyTextMass")]
  public long EmptyTextMass { get; init; }

  /// <summary>Hubs outside the commander-legal-paper corpus scope — unactionable here, reported for completeness.</summary>
  [SerializedLabel("missingFromCorpusHubs")]
  public int MissingFromCorpusHubs { get; init; }

  [SerializedLabel("missingFromCorpusMass")]
  public long MissingFromCorpusMass { get; init; }

  /// <summary>The hubs ranked by <see cref="ComboAnchor.PopularityMass"/> — the pick surface.</summary>
  [SerializedLabel("topAnchors")]
  public List<ComboAnchor> TopAnchors { get; init; } = [];
}

/// <summary>One unparsed hub card, with the combo neighborhood it gates.</summary>
[FlowthruSchema]
public partial record ComboAnchor
{
  [SerializedLabel("card")]
  public string Card { get; init; } = "";

  [SerializedLabel("typeLine")]
  public string TypeLine { get; init; } = "";

  /// <summary><c>parser-family</c> | <c>empty-oracle-text</c> | <c>missing-from-corpus</c>.</summary>
  [SerializedLabel("blockReason")]
  public string BlockReason { get; init; } = "";

  [SerializedLabel("blockedComboCount")]
  public int BlockedComboCount { get; init; }

  /// <summary>Combos in which this is the ONLY unparsed card — parse it alone and they become reconstructable.</summary>
  [SerializedLabel("soleBlockerCount")]
  public int SoleBlockerCount { get; init; }

  /// <summary>Sum of the popularity of every combo this hub blocks — the primary ranking key.</summary>
  [SerializedLabel("popularityMass")]
  public long PopularityMass { get; init; }

  [SerializedLabel("maxComboPopularity")]
  public int MaxComboPopularity { get; init; }

  /// <summary>The most popular results the blocked combos produce (e.g. "Infinite ETB").</summary>
  [SerializedLabel("topPayoffs")]
  public List<string> TopPayoffs { get; init; } = [];

  /// <summary>The neighborhood: cards that co-star with this hub in its blocked combos, by shared-combo popularity.</summary>
  [SerializedLabel("coStars")]
  public List<ComboCoStar> CoStars { get; init; } = [];
}

/// <summary>A card that appears alongside a hub in the combos the hub blocks.</summary>
[FlowthruSchema]
public partial record ComboCoStar
{
  [SerializedLabel("card")]
  public string Card { get; init; } = "";

  [SerializedLabel("sharedCombos")]
  public int SharedCombos { get; init; }

  [SerializedLabel("sharedPopularity")]
  public long SharedPopularity { get; init; }

  /// <summary>True if this co-star ALSO doesn't fully parse (close it in the same batch); false = it lights up free once the hub lands.</summary>
  [SerializedLabel("alsoUnparsed")]
  public bool AlsoUnparsed { get; init; }
}
