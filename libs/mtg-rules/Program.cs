using Flowthru.Cli;
using Flowthru.Data.Catalog;
using Flowthru.Hosting;
using MagicAtlas.Rules.Data;
using MagicAtlas.Rules.Flows.Ingest;
using MagicAtlas.Rules.Flows.Ontology;
using MagicAtlas.Rules.Flows.RulesProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Rules;

/// <summary>
/// FlowthruCli entry point for the MTG rules pipeline. Owns the rules text end-to-end: fetch the
/// comprehensive rules .txt over HTTP, parse it into a structured tree + glossary, and derive the
/// deterministic type ontology. The published artifacts (rules-structure.json, glossary.json,
/// type-ontology.json) are vendored by downstream consumers (MAST's tests, MagicAtlas'
/// atlas-flows) — the copyrighted rules text never leaves this project's _01_Raw layer.
/// </summary>
/// <list type="number">
/// <item><b>Ingest</b> — HTTP boundary; fetches the comprehensive rules .txt.</item>
/// <item><b>RulesProcessing</b> — section/rule/glossary extraction from the rules text.</item>
/// <item><b>TypeOntology</b> — derives the deterministic type ontology from the structured rules.</item>
/// </list>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveProjectDirectory())
    );

  /// <summary>
  /// Locate this project's directory from <see cref="AppContext.BaseDirectory"/> (Nx-routed builds
  /// drop outputs under <c>dist/</c>) or the caller's CWD. Walks upward looking for the csproj.
  /// </summary>
  private static string ResolveProjectDirectory()
  {
    const string csproj = "MagicAtlas.Rules.csproj";
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, csproj)))
        return dir.FullName;
      dir = dir.Parent;
    }
    var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (cwd is not null)
    {
      var candidate = Path.Combine(cwd.FullName, "libs", "mtg-rules", csproj);
      if (File.Exists(candidate))
        return Path.GetDirectoryName(candidate)!;
      cwd = cwd.Parent;
    }
    throw new InvalidOperationException(
      $"Could not locate {csproj}. Run from within the workspace, or set CWD to libs/mtg-rules."
    );
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var dataPath = Path.Combine(basePath, "Data");

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    // HttpClient threaded into the Ingest fetch by closure capture (mirrors atlas-flows' Ingest).
    var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MagicAtlas-Rules/0.1");
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/html")
    );
    httpClient.DefaultRequestHeaders.Accept.Add(
      new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain")
    );

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(_ => new Catalog(dataPath));

      flowthru
        .RegisterFlow<Catalog>("Ingest", catalog => IngestFlow.Create(catalog, httpClient))
        .WithDescription("Fetches the MTG comprehensive rules .txt into the _01_Raw layer");

      flowthru
        .RegisterFlow<Catalog>("RulesProcessing", RulesProcessingFlow.Create)
        .WithDescription("Parses the rules text into structured JSON + a glossary dictionary");

      flowthru
        .RegisterFlow<Catalog>("TypeOntology", OntologyFlow.Create)
        .WithDescription("Derives the deterministic MTG type ontology from the structured rules");
    });
  }
}
