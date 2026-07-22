using Flowthru.Data.Catalog;
using MagicAtlas.Data._02_Intermediate.Schemas;
using MagicAtlas.Data._07_ModelOutput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Data;

/// <summary>
/// Catalog wiring for the corpus/combo file-drop inputs and the combo-anchor pick surface consumed by
/// this library's <c>CorpusParse</c> / <c>FetchCombos</c> / <c>ComboAnchors</c> flows.
///
/// <para>The CardAtlas D1–D4 reporting flow was DELETED here: it had diverged from the current copy in
/// <c>tests/magic-ast-tests/Flows/CardAtlas</c> (missing the PortNode-role <c>Side</c> fix aeaf18b3 and
/// the ADR-0003 Stage-4 structured facets 86e88db4) and was silently regressing the published dumps. The
/// owner's decision was to make the tests copy the single source of truth. The <c>nx run flowthru:dumps</c>
/// target now drives the <c>mast</c> project, which produces the D1–D4 dumps under
/// <c>tests/magic-ast-tests/Data/_08_Reporting/dumps/</c> and publishes them to the repo-root
/// <c>dumps/</c> the atlas-api seeder reads.</para>
///
/// <para>The three <b>inputs</b> below are file-drops the offline pipeline produces (combos.json /
/// card-inputs.json / parse-records.json — the gitignored corpus). Diagnostics; never gates.</para>
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

  // ── Output — the combo-anchor pick surface (produced by this library's ComboAnchors flow). ─────────

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
