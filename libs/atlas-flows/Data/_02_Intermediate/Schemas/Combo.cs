using Flowthru.Data.Schema;

namespace MagicAtlas.Data._02_Intermediate.Schemas;

/// <summary>
/// Lean interaction-triage combo — the projection of a Commander Spellbook variant down to what
/// triage/reconstruction actually uses: the cards (name + Scryfall <c>oracleId</c>), the popularity
/// ranking signal, the color identity, and the produced results (prose). Each combo is a candidate the
/// interaction engine should be able to reconstruct.
///
/// <para>Promoted from tests/magic-ast-tests/Data/_02_Intermediate/Schemas/Combo.cs so the CardAtlas
/// reporting flow reads the same shape from this shippable library.</para>
/// </summary>
[FlowthruSchema]
public partial record Combo
{
  [SerializedLabel("id")]
  public string Id { get; init; } = "";

  [SerializedLabel("popularity")]
  public int Popularity { get; init; }

  [SerializedLabel("identity")]
  public string Identity { get; init; } = "";

  [SerializedLabel("cards")]
  public List<ComboCard> Cards { get; init; } = [];

  /// <summary>Produced features (e.g. "Infinite mana") — human context, not the reconstruction target.</summary>
  [SerializedLabel("results")]
  public List<string> Results { get; init; } = [];
}

/// <summary>A card in a combo, keyed by the Scryfall <c>oracleId</c> we join to the parse corpus.</summary>
[FlowthruSchema]
public partial record ComboCard
{
  [SerializedLabel("name")]
  public string Name { get; init; } = "";

  [SerializedLabel("oracleId")]
  public string OracleId { get; init; } = "";
}
