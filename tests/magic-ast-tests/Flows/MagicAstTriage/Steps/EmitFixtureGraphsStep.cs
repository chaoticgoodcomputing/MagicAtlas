using System.Text.Json;
using Flowthru.Step;
using MagicAST;
using MagicAST.AST;
using MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Mermaid;

namespace MagicAtlas.Ast.Tests.Flows.MagicAstTriage.Steps;

/// <summary>
/// Reads every <c>HandParsedCards/**/*.json</c> fixture, emits a per-fixture Mermaid graph to
/// <c>_08_Reporting/fixture-graphs/{Set}/{CardName}.md</c>, and emits the aggregate AST-frequency
/// diagram to <c>_08_Reporting/ast-frequency.md</c>.
/// </summary>
[FlowthruStep]
public static class EmitFixtureGraphsStep
{
    /// <summary>
    /// Creates the step transform. Takes no inputs (reads fixtures from disk) and returns
    /// the number of per-fixture files written as a status value.
    /// </summary>
    /// <param name="fixturesRoot">
    /// Absolute path to the <c>HandParsedCards</c> directory
    /// (e.g. <c>{workspace}/tools/test/magic-ast/Data/HandParsedCards</c>).
    /// </param>
    /// <param name="reportingRoot">
    /// Absolute path to the <c>_08_Reporting</c> directory under the harness
    /// (e.g. <c>{harness}/Data/_08_Reporting</c>).
    /// </param>
    public static Func<Task<FixtureGraphSummary>> Create(
        string fixturesRoot,
        string reportingRoot
    )
    {
        return async () =>
        {
            var fixtures = LoadFixtures(fixturesRoot).ToList();
            var fixtureGraphsDir = Path.Combine(reportingRoot, "fixture-graphs");
            var aggregatePath = Path.Combine(reportingRoot, "ast-frequency.md");

            Directory.CreateDirectory(fixtureGraphsDir);

            var written = 0;
            var cards = new List<CardOutputAST>();

            foreach (var (card, relPath) in fixtures)
            {
                cards.Add(card);

                // Determine output path: mirror the relative path of the fixture
                // e.g. M21/BaneslayerAngel.json → fixture-graphs/M21/BaneslayerAngel.md
                var mdRelPath = Path.ChangeExtension(relPath, ".md");
                var outputPath = Path.Combine(fixtureGraphsDir, mdRelPath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

                var content = MermaidEmitter.RenderFixtureGraph(card);
                await File.WriteAllTextAsync(outputPath, content);
                written++;
            }

            // Aggregate frequency diagram
            var aggregateContent = MermaidEmitter.RenderAggregateGraph(cards);
            await File.WriteAllTextAsync(aggregatePath, aggregateContent);

            return new FixtureGraphSummary
            {
                FixtureCount = written,
                AggregateWritten = true,
                FixtureGraphsDirectory = fixtureGraphsDir,
                AggregateFilePath = aggregatePath,
            };
        };
    }

    // ──────────────────────────────────────────────────────────────────────
    // Fixture loader
    // ──────────────────────────────────────────────────────────────────────

    // Use the same options the NUnit fixture tests use — Strict respects the
    // [JsonPolymorphic] discriminator hierarchies on Ability, Effect, Duration, etc.
    private static readonly JsonSerializerOptions JsonOpts = MagicASTJsonOptions.Strict;

    private static IEnumerable<(CardOutputAST Card, string RelativePath)> LoadFixtures(
        string fixturesRoot
    )
    {
        if (!Directory.Exists(fixturesRoot))
            yield break;

        foreach (
            var filePath in Directory.EnumerateFiles(
                fixturesRoot,
                "*.json",
                SearchOption.AllDirectories
            )
        )
        {
            CardOutputAST? card = null;
            try
            {
                var json = File.ReadAllText(filePath);
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("output", out var outputElement))
                    continue;

                var outputJson = outputElement.GetRawText();
                card = JsonSerializer.Deserialize<CardOutputAST>(outputJson, JsonOpts);
            }
            catch
            {
                // Skip malformed fixtures gracefully
            }

            if (card is null)
                continue;

            var relPath = Path.GetRelativePath(fixturesRoot, filePath);
            yield return (card, relPath);
        }
    }
}

/// <summary>
/// Summary result returned by <see cref="EmitFixtureGraphsStep"/>.
/// </summary>
public sealed record FixtureGraphSummary
{
    /// <summary>Number of per-fixture <c>.md</c> files written.</summary>
    public required int FixtureCount { get; init; }

    /// <summary>Whether the aggregate <c>ast-frequency.md</c> was written.</summary>
    public required bool AggregateWritten { get; init; }

    /// <summary>Absolute path to the <c>fixture-graphs/</c> directory.</summary>
    public required string FixtureGraphsDirectory { get; init; }

    /// <summary>Absolute path to the <c>ast-frequency.md</c> file.</summary>
    public required string AggregateFilePath { get; init; }
}
