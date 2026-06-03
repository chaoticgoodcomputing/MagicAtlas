namespace MagicAST.Tests.Infrastructure;

/// <summary>
/// Shared test-data locations for the consolidated MAST test project.
///
/// Hand-authored, source-of-truth golds live under <c>Fixtures/</c> (parser fixtures, keyword
/// expansions, operator conformance, interaction reconstructions). The Flowthru triage flows own
/// <c>Data/</c> (mostly gitignored generated layers).
///
/// The vendored MTG <c>type-ontology.json</c> the <c>ObjectFilter</c> operators and the interaction
/// engine bind to is NOT hand-authored — it is the derived artifact published by the upstream
/// <c>mtg-rules</c> project, copied into the Flowthru <c>_01_Raw/Datasets/Curated/</c> input layer.
/// Run <c>nx run mast:seed-ontology</c> to regenerate it from <c>mtg-rules</c> and re-copy it.
/// </summary>
public static class TestData
{
  /// <summary>The single vendored <c>type-ontology.json</c> (provenance: <c>mtg-rules</c>; see class doc).</summary>
  public static string OntologyPath =>
    Path.Combine(
      TestContext.CurrentContext.TestDirectory,
      "Data",
      "_01_Raw",
      "Datasets",
      "Curated",
      "type-ontology.json"
    );
}
