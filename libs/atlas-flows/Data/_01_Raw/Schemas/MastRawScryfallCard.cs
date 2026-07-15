using Flowthru.Data.Schema;

namespace MagicAtlas.Data._01_Raw.Schemas;

/// <summary>
/// Narrow projection of Scryfall's card object — only the fields MagicAST's parser
/// consumes. Intentionally rejects the full ~50-field Scryfall schema, and is kept
/// distinct from this project's richer <see cref="RawScryfallCard"/> so the promoted
/// corpus-parse chain (FetchScryfallBulk → ProjectToCardInput → ParseCorpus) stays a
/// faithful copy of the tests/magic-ast-tests source.
/// </summary>
/// <remarks>
/// Field names are snake_case to match Scryfall's API; Flowthru's
/// <c>[SerializedLabel]</c> aliases let us expose PascalCase C# names while
/// reading the snake_case JSON.
///
/// <para>Promoted from tests/magic-ast-tests/Data/_01_Raw/Schemas/MastRawScryfallCard.cs
/// (upstream-atlas-data-plan §0/§6 P0) so the shippable library — not the test assembly —
/// can regenerate card-inputs.json / parse-records.json from the Scryfall bulk.</para>
/// </remarks>
[FlowthruSchema]
public partial record MastRawScryfallCard
{
  [SerializedLabel("id")]
  public string Id { get; init; } = "";

  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("mana_cost")]
  public string? ManaCost { get; init; }

  [SerializedLabel("type_line")]
  public string TypeLine { get; init; } = "";

  [SerializedLabel("oracle_text")]
  public string? OracleText { get; init; }

  [SerializedLabel("power")]
  public string? Power { get; init; }

  [SerializedLabel("toughness")]
  public string? Toughness { get; init; }

  [SerializedLabel("loyalty")]
  public string? Loyalty { get; init; }

  [SerializedLabel("colors")]
  public List<string>? Colors { get; init; }

  [SerializedLabel("color_identity")]
  public List<string>? ColorIdentity { get; init; }

  [SerializedLabel("color_indicator")]
  public List<string>? ColorIndicator { get; init; }

  [SerializedLabel("keywords")]
  public List<string>? Keywords { get; init; }

  [SerializedLabel("layout")]
  public string? Layout { get; init; }

  /// <summary>
  /// Format-legality map. Read by <c>ProjectToCardInputStep</c> to filter the
  /// corpus to commander-legal cards. Scryfall values: "legal", "not_legal",
  /// "restricted", "banned".
  /// </summary>
  [SerializedLabel("legalities")]
  public Dictionary<string, string>? Legalities { get; init; }

  /// <summary>
  /// Game-platform list: "paper", "mtgo", "arena". Used to exclude
  /// digital-only printings.
  /// </summary>
  [SerializedLabel("games")]
  public List<string>? Games { get; init; }

  [SerializedLabel("card_faces")]
  public List<MastRawScryfallCardFace>? CardFaces { get; init; }
}

/// <summary>One face of a multi-faced card (split, transform, flip, etc.).</summary>
[FlowthruSchema]
public partial record MastRawScryfallCardFace
{
  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("mana_cost")]
  public string? ManaCost { get; init; }

  [SerializedLabel("type_line")]
  public string TypeLine { get; init; } = "";

  [SerializedLabel("oracle_text")]
  public string? OracleText { get; init; }

  [SerializedLabel("power")]
  public string? Power { get; init; }

  [SerializedLabel("toughness")]
  public string? Toughness { get; init; }

  [SerializedLabel("loyalty")]
  public string? Loyalty { get; init; }

  [SerializedLabel("colors")]
  public List<string>? Colors { get; init; }

  [SerializedLabel("color_indicator")]
  public List<string>? ColorIndicator { get; init; }
}
