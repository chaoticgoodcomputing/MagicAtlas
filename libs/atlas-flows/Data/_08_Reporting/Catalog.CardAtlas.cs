using Flowthru.Data.Catalog;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Catalog wiring for the promoted CardAtlas reporting flow (D1–D4) and the combo-anchor pick surface
/// (upstream-atlas-data-plan §0/§6 P0). The three <b>inputs</b> are file-drops the offline pipeline
/// produces (combos.json / card-inputs.json / parse-records.json — the gitignored corpus); the seven
/// <b>outputs</b> land under <c>{basePath}/_08_Reporting/dumps/</c>, the known directory the atlas-api
/// seeder (plan §2 Option A) reads. Diagnostics; never gates.
/// </summary>
public partial class Catalog
{
  // ── Inputs — file-drops the offline MAST + Commander-Spellbook pipeline provides. ─────────────────

  /// <summary>Lean interaction-triage combos — the Commander Spellbook dump projected to
  /// <see cref="Combo"/> (cards + popularity + results). Supplied as a file-drop; the projection that
  /// produces it is not yet wired into atlas-flows (see plan §0).</summary>
  public IItem<IEnumerable<Combo>> Combos =>
    CreateItem(() =>
      Item.Of<IEnumerable<Combo>>("Combos")
        .Json()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/combos.json")
        .Build()
    );

  /// <summary>Per-card <see cref="MastCardInput"/> records — the narrow projection of the Scryfall bulk
  /// into MagicAST's input contract. Supplied as a file-drop.</summary>
  public IItem<IEnumerable<MastCardInput>> CardInputs =>
    CreateItem(() =>
      Item.Of<IEnumerable<MastCardInput>>("CardInputs")
        .Json()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/card-inputs.json")
        .Build()
    );

  /// <summary>Per-card <see cref="ParseRecord"/> summaries — the corpus parse output the combo-anchor
  /// surface reads (fullyParsed = TotalAbilities == ParsedAbilities). Supplied as a file-drop; the
  /// parse pass that produces it is not yet promoted into atlas-flows (see plan §0).</summary>
  public IItem<IEnumerable<ParseRecord>> ParseRecords =>
    CreateItem(() =>
      Item.Of<IEnumerable<ParseRecord>>("ParseRecords")
        .Json()
        .AtPath($"{_basePath}/_07_ModelOutput/Datasets/parse-records.json")
        .Build()
    );

  // ── Outputs — the five headline dumps (+ two co-products) under _08_Reporting/dumps/. ─────────────

  /// <summary>D1 — per-card deckbuilding metadata (colour identity, mana value, type line, port count).</summary>
  public IItem<IEnumerable<CardMetaRow>> CardMeta =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardMetaRow>>("CardMeta")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/card-meta.json")
        .Build()
    );

  /// <summary>D1 — the card↔port index (one row per card, distinct port label; family + emit/consume side).</summary>
  public IItem<IEnumerable<CardPortRow>> CardPorts =>
    CreateItem(() =>
      Item.Of<IEnumerable<CardPortRow>>("CardPorts")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/card-ports.json")
        .Build()
    );

  /// <summary>D4 — per-combo reconstructed loops (named cards, family-signature, tier, result).</summary>
  public IItem<IEnumerable<ComboInstanceRow>> ComboInstances =>
    CreateItem(() =>
      Item.Of<IEnumerable<ComboInstanceRow>>("ComboInstances")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/combo-instances.json")
        .Build()
    );

  /// <summary>The wide reconstruction-recall measurement (co-produced with D4; measurement, never a gate).</summary>
  public IItem<ExtendedRecallReport> ExtendedRecall =>
    CreateItem(() =>
      Item.Of<ExtendedRecallReport>("ExtendedRecall")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/extended-recall-report.json")
        .Build()
    );

  /// <summary>D2 — the family subway map (stations + realized-combo-annotated directed lines).</summary>
  public IItem<ResourceGraph> ResourceGraph =>
    CreateItem(() =>
      Item.Of<ResourceGraph>("ResourceGraph")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/resource-graph.json")
        .Build()
    );

  /// <summary>D3 — the realized combo-shape catalog (family-signatures with ≥1 reconstructed combo).</summary>
  public IItem<ArchetypeCatalog> ArchetypeCatalog =>
    CreateItem(() =>
      Item.Of<ArchetypeCatalog>("ArchetypeCatalog")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/archetype-catalog.json")
        .Build()
    );

  /// <summary>The combo-anchored pick surface: unparsed hub cards ranked by the combo-popularity value
  /// each gates, with sole-blocker counts, co-star neighborhood, and a block-reason split.</summary>
  public IItem<ComboAnchorReport> ComboAnchorReport =>
    CreateItem(() =>
      Item.Of<ComboAnchorReport>("ComboAnchorReport")
        .Json()
        .AtPath($"{_basePath}/_08_Reporting/dumps/combo-anchor-report.json")
        .Build()
    );
}
