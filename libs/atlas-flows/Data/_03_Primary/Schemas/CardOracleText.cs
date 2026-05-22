using Flowthru.Data.Schema;

namespace MagicAtlas.Data._03_Primary.Schemas;

/// <summary>
/// Card-level oracle text with reminder parentheticals intact — the input shape the FineTune
/// flow's training-pair builder needs to extract reminder-text → glossary paraphrase pairs.
/// Sibling to <see cref="OracleLine"/>, which is line-level with parentheticals stripped.
/// </summary>
/// <remarks>
/// Arrow-friendly flat projection of <see cref="CardCoreData"/>: only the three scalar fields
/// the training-pair builder needs are carried across the Python boundary. The nested lists
/// (Types, Subtypes, Keywords, Colors, etc.) on CardCoreData would break the marshaller, so
/// we project them away here.
/// </remarks>
[FlowthruSchema]
public partial record CardOracleText
{
  [SerializedLabel("card_id")]
  public required Guid CardId { get; init; }

  [SerializedLabel("name")]
  public required string Name { get; init; }

  /// <summary>Full oracle text, with reminder-text parentheticals preserved.</summary>
  [SerializedLabel("oracle_text")]
  public required string OracleText { get; init; }
}
