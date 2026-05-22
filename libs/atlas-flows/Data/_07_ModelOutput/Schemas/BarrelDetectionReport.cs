using Flowthru.Data.Schema;

namespace MagicAtlas.Data._07_ModelOutput.Schemas;

/// <summary>
/// Diagnostic snapshot of the barrel-detection pass inside
/// <c>ProjectOracleLinesNode</c>. The numerical totals describe what happened to the corpus;
/// the two sample arrays let you eyeball whether the classifier is right.
/// </summary>
/// <remarks>
/// Sample sizes are capped (typically 50 each) — the full classification is implicit in the
/// resulting <c>OracleLines</c> rows. Edit <c>BarrelExampleSampleSize</c> in
/// <c>ProjectOracleLinesNode</c> to take a bigger slice if you're debugging.
/// </remarks>
[FlowthruSchema]
public partial record BarrelDetectionReport
{
  /// <summary>Total oracle-text lines considered (post-parenthetical-strip, pre-classification).</summary>
  [SerializedLabel("total_lines_considered")]
  public required int TotalLinesConsidered { get; init; }

  /// <summary>Lines classified as barrels and dropped from <c>OracleLines</c>.</summary>
  [SerializedLabel("barrel_lines_dropped")]
  public required int BarrelLinesDropped { get; init; }

  /// <summary>
  /// Lines with at least one keyword-matching segment that were NOT classified as a barrel
  /// (i.e. some segment didn't match). The diagnostic to scan for false negatives.
  /// </summary>
  [SerializedLabel("borderline_lines")]
  public required int BorderlineLines { get; init; }

  /// <summary>Synthetic single-keyword lines emitted from <c>card.Keywords</c> metadata.</summary>
  [SerializedLabel("synthetic_keyword_lines_added")]
  public required int SyntheticKeywordLinesAdded { get; init; }

  /// <summary>Sample of lines classified as keyword barrels (for eyeball validation).</summary>
  [SerializedLabel("barrels")]
  public required List<BarrelExample> Barrels { get; init; }

  /// <summary>Sample of borderline lines (contain a keyword segment but aren't barrels).</summary>
  [SerializedLabel("borderlines")]
  public required List<BorderlineExample> Borderlines { get; init; }
}

[FlowthruSchema]
public partial record BarrelExample
{
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("card_name")]
  public required string CardName { get; init; }

  /// <summary>The raw line text (post-parenthetical-strip) that was classified as a barrel.</summary>
  [SerializedLabel("line")]
  public required string Line { get; init; }

  /// <summary>The comma-separated segments and their matched keyword (in vocabulary form).</summary>
  [SerializedLabel("segments")]
  public required List<string> Segments { get; init; }
}

[FlowthruSchema]
public partial record BorderlineExample
{
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("card_name")]
  public required string CardName { get; init; }

  [SerializedLabel("line")]
  public required string Line { get; init; }

  /// <summary>Comma-separated segments that DID match a keyword in vocabulary.</summary>
  [SerializedLabel("matched_segments")]
  public required List<string> MatchedSegments { get; init; }
}
