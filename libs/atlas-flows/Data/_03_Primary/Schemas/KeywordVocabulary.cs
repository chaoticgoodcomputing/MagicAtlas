using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Sorted, distinct set of Scryfall keyword strings observed across every
/// <see cref="CardCoreData"/> in the filtered corpus, in their canonical Scryfall casing
/// (typically capitalized: "Flying", "Ward", "Annihilator").
/// </summary>
/// <remarks>
/// Used by <c>ProjectOracleLinesNode</c> to classify oracle-text lines as keyword barrels
/// (every comma-separated segment is a keyword), and by the keyword-cluster reports to identify
/// which synthetic lines represent which keyword anchor.
/// </remarks>
[FlowthruSchema]
public partial record KeywordVocabulary
{
  [SerializedLabel("keywords")]
  public List<string> Keywords { get; init; } = new();
}
