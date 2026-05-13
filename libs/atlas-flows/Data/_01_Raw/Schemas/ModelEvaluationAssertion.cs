using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Declarative test for an embedding model: "the centroid of cards matching <see cref="GroupAPattern"/>
/// should be <see cref="Expect"/> the centroid of cards matching <see cref="GroupBPattern"/>, when
/// compared to <see cref="BaselineGroupPattern"/>." Evaluated against the 5D
/// <c>ClusteringEmbeddings</c> output so the same numbers feed both the clustering step and the
/// eval reports.
/// </summary>
/// <remarks>
/// <para>
/// First-class example: <c>"Flying"</c> and <c>"Menace"</c> are both evasion keywords — their
/// centroids should be closer to each other than either is to <c>"Trample"</c> (a combat-stat
/// keyword that doesn't change blocking). A model that fails this assertion hasn't learned the
/// evasion class.
/// </para>
/// <para>
/// Patterns are interpreted as case-insensitive regex (<c>re.IGNORECASE</c>, <c>re.search()</c>)
/// against the oracle-text fragment string. Use word-boundary anchors (<c>\b</c>) to avoid
/// substring matches like <c>"Flying"</c> matching inside <c>"Flickering"</c>.
/// </para>
/// </remarks>
[FlowthruSchema]
public partial record ModelEvaluationAssertion
{
  /// <summary>Short identifier surfaced in the result report (e.g. <c>"evasion_class"</c>).</summary>
  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Case-insensitive regex defining group A. Matched via <c>re.search()</c> against
  /// each oracle-text fragment.</summary>
  [SerializedLabel("group_a_pattern")]
  public required string GroupAPattern { get; init; }

  /// <summary>Case-insensitive regex defining group B.</summary>
  [SerializedLabel("group_b_pattern")]
  public required string GroupBPattern { get; init; }

  /// <summary>Expected relationship between the centroid distances. One of:
  /// <c>"closer_than"</c> (A↔B distance &lt; A↔baseline distance) or <c>"farther_than"</c>
  /// (A↔B distance &gt; A↔baseline distance).</summary>
  [SerializedLabel("expect")]
  public required string Expect { get; init; }

  /// <summary>Case-insensitive regex defining the baseline group used for the comparison.</summary>
  [SerializedLabel("baseline_group_pattern")]
  public required string BaselineGroupPattern { get; init; }
}
