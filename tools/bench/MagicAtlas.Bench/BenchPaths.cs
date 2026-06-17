using System.Reflection;

namespace MagicAtlas.Bench;

/// <summary>
/// Resolves the bench's input/output paths. The snapshot, the linked gold corpus, and the ontology are
/// all copied next to the built assembly (see the <c>.csproj</c> <c>CopyToOutputDirectory</c> items), so
/// they resolve identically whether the bench runs via <c>dotnet run</c> or under the <c>dotnet test</c>
/// runner — no dependency on the (differing) current working directory.
/// </summary>
public static class BenchPaths
{
  private static string OutputDir =>
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

  /// <summary>The pinned, checksummed Commander Spellbook combo snapshot.</summary>
  public static string SnapshotPath =>
    Path.Combine(OutputDir, "Data", "spellbook-combos.snapshot.json");

  /// <summary>The hand-parsed gold corpus root (the eval corpus).</summary>
  public static string FixturesRoot =>
    Path.Combine(OutputDir, "Fixtures", "HandParsedCards");

  /// <summary>The type ontology the interaction engine binds to (provenance: mtg-rules).</summary>
  public static string OntologyPath => Path.Combine(OutputDir, "Data", "type-ontology.json");

  /// <summary>
  /// The committed baseline report path, resolved in the SOURCE tree (not the build output) so the
  /// ratchet reads/writes the version-controlled artifact. The build redirects output to
  /// <c>dist/tools/bench/MagicAtlas.Bench/net10.0</c> (Directory.Build.props' <c>BaseOutputPath</c>),
  /// so this walks up to the repo root (the dir holding <c>MagicAtlas.slnx</c>) and rejoins the known
  /// source path.
  /// </summary>
  public static string BaselineReportPath =>
    Path.Combine(RepoRoot, "tools", "bench", "MagicAtlas.Bench", "bench-report.json");

  /// <summary>
  /// The explicit per-combo expected-tier whitelist — the GATE for the combo-recall test (it replaced the
  /// moving-baseline ratchet). Resolved in the SOURCE tree (like <see cref="BaselineReportPath"/>), since
  /// it is the version-controlled contract the test asserts against, not a build artifact.
  /// </summary>
  public static string ExpectedTiersPath =>
    Path.Combine(RepoRoot, "tools", "bench", "MagicAtlas.Bench", "combo-expected-tiers.json");

  private static string RepoRoot
  {
    get
    {
      var dir = new DirectoryInfo(OutputDir);
      while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MagicAtlas.slnx")))
        dir = dir.Parent;
      return dir?.FullName
        ?? throw new InvalidOperationException(
          "Could not locate the repo root (MagicAtlas.slnx) above " + OutputDir
        );
    }
  }
}
