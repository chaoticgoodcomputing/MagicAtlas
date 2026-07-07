using Flowthru.Data.Schema;

namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

/// <summary>
/// The interaction-triage work-list (the mast-interaction analogue of <c>triage-report.json</c>).
/// Ranks Commander Spellbook combos by popularity and classifies the FIRST blocking layer:
/// <list type="bullet">
///   <item><b>parse-blocked</b> — a combo card doesn't fully parse to an AST yet → routes to the
///   <c>mast-tdd-loop</c> skill. <see cref="TopComboBlockingCards"/> is the popularity-weighted
///   priority overlay ("this card blocks N popular combos — parse it next").</item>
///   <item><b>parse-ready</b> — every card parses, so the combo is a candidate for the interaction
///   loop's novel work (edge fixture → port queries → grammar edge → judge). Whether the engine
///   ALREADY reconstructs it (the L2/L3/L4 split) is the next increment; this first draft lands the
///   parse gate + ranked queues.</item>
/// </list>
/// First-draft join is on card <b>name</b> (the corpus carries no Scryfall <c>oracle_id</c> yet;
/// CSB keys on <c>oracleId</c> — adding it to the corpus is the robustness hardening).
/// </summary>
[FlowthruSchema]
public partial record InteractionTriageReport
{
  [SerializedLabel("generatedAt")]
  public DateTime GeneratedAt { get; init; }

  [SerializedLabel("totalCombos")]
  public int TotalCombos { get; init; }

  [SerializedLabel("parseReady")]
  public int ParseReady { get; init; }

  [SerializedLabel("parseBlocked")]
  public int ParseBlocked { get; init; }

  /// <summary>Parse-ready combos (every card parses), ranked by popularity — the interaction loop's queue.</summary>
  [SerializedLabel("topReconstructionCandidates")]
  public List<ComboWorkItem> TopReconstructionCandidates { get; init; } = [];

  /// <summary>Parse-blocked combos, ranked by popularity — each names the cards that don't yet parse.</summary>
  [SerializedLabel("topParseBlocked")]
  public List<ComboWorkItem> TopParseBlocked { get; init; } = [];

  /// <summary>Cards blocking the most popular combos — the priority overlay for the mast-tdd-loop.</summary>
  [SerializedLabel("topComboBlockingCards")]
  public List<CardGap> TopComboBlockingCards { get; init; } = [];

  /// <summary>
  /// The FULL, untruncated per-card blocking overlay — the machine-facing value
  /// map that the MagicAstTriage flow joins against its yield clusters to compute
  /// each cluster's <see cref="YieldClusterSummary.FusedScore"/> (parse-proximity
  /// weighted by the combo-popularity mass it unblocks). <see cref="TopComboBlockingCards"/>
  /// is the human-readable top slice of this same list.
  /// </summary>
  [SerializedLabel("allComboBlockingCards")]
  public List<CardGap> AllComboBlockingCards { get; init; } = [];
}

/// <summary>One combo on a triage queue — its id, popularity, cards, results, and (if blocked) the blocking cards.</summary>
[FlowthruSchema]
public partial record ComboWorkItem
{
  [SerializedLabel("comboId")]
  public string ComboId { get; init; } = "";

  [SerializedLabel("popularity")]
  public int Popularity { get; init; }

  [SerializedLabel("cards")]
  public List<string> Cards { get; init; } = [];

  [SerializedLabel("results")]
  public List<string> Results { get; init; } = [];

  /// <summary>Cards that don't fully parse (empty for parse-ready combos).</summary>
  [SerializedLabel("blockingCards")]
  public List<string> BlockingCards { get; init; } = [];
}

/// <summary>A card that blocks combos from reconstructing because it doesn't parse (or isn't in the corpus).</summary>
[FlowthruSchema]
public partial record CardGap
{
  [SerializedLabel("card")]
  public string Card { get; init; } = "";

  /// <summary><c>"unparsed"</c> (in corpus, not fully parsed) or <c>"missing-from-corpus"</c> (outside the commander-legal-paper scope).</summary>
  [SerializedLabel("reason")]
  public string Reason { get; init; } = "";

  [SerializedLabel("blockedComboCount")]
  public int BlockedComboCount { get; init; }

  [SerializedLabel("maxComboPopularity")]
  public int MaxComboPopularity { get; init; }

  /// <summary>
  /// Sum of the popularity of every combo this card blocks — the card's total
  /// downstream combo-value mass. Preferred over <see cref="MaxComboPopularity"/>
  /// as the fusion weight because it rewards cards that gate MANY combos, not
  /// just one very popular one. <c>long</c> to avoid overflow when a staple
  /// (e.g. Lotus Petal) blocks hundreds of combos.
  /// </summary>
  [SerializedLabel("popularityMass")]
  public long PopularityMass { get; init; }
}
