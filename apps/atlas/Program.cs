using Flowthru.Core.Cli;
using Flowthru.Core.Services;
using Flowthru.Extensions.Python;
using Flowthru.Extensions.Python.Execution;
using Flowthru.Extensions.Python.Runtime;
using Flowthru.Extensions.Python.Services;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.CardProcessing;
using MagicAtlas.Pipelines.OracleEmbedding;
using MagicAtlas.Pipelines.RulesProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace MagicAtlas;

public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveProjectDirectory())
    );

  // `dotnet run --project apps/atlas` leaves CWD at the caller, and Nx redirects build
  // output to `dist/apps/atlas/net10.0/` (not `bin/Debug/net10.0/`), so we can't rely on a
  // fixed "../../.." walk. Instead, search upward from the assembly location for the csproj.
  private static string ResolveProjectDirectory()
  {
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, "MagicAtlas.csproj"))) return dir.FullName;
      dir = dir.Parent;
    }
    var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (cwd is not null)
    {
      var candidate = Path.Combine(cwd.FullName, "apps", "atlas", "MagicAtlas.csproj");
      if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
      cwd = cwd.Parent;
    }
    throw new InvalidOperationException("Could not locate MagicAtlas.csproj.");
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    // Instantiate the Python subprocess executor directly. Flowthru 0.6.0's AddFlowthru
    // throws if no flows are registered, so we can't spin a temp provider just to resolve
    // IPythonExecutor. Match the sample's "Phase 6 workaround" and pass the executor into
    // OracleEmbedding.Create(...) below.
    var pythonOptions = new PythonRuntimeOptions
    {
      VenvPath = Path.Combine(basePath, ".venv"),
    };
    pythonOptions.ModuleSearchPaths.Add(basePath);
    var executor = new SubprocessPythonExecutor(
      pythonOptions,
      NullLogger<SubprocessPythonExecutor>.Instance
    );

    services.AddFlowthru(flowthru =>
    {
      flowthru.UseConfiguration(opts => opts.ConfigurationPath = basePath);
      flowthru.RegisterCatalog(_ => new Catalog(Path.Combine(basePath, "Data")));
      flowthru.UsePython(python =>
      {
        python.ModuleSearchPaths.Add(basePath);
        python.VenvPath = Path.Combine(basePath, ".venv");
      });

      flowthru
        .RegisterFlow(label: "RulesProcessing", flow: RulesProcessing.Create)
        .WithDescription("Processes MTG comprehensive rules into structured JSON");

      flowthru
        .RegisterFlow(
          label: "CardProcessing",
          flow: CardProcessing.Create,
          configurationSection: "Flowthru:Pipelines:CardProcessing"
        )
        .WithDescription("Processes Scryfall card data and preps for analysis");

      flowthru
        .RegisterFlow(
          label: "OracleEmbedding",
          flow: (Catalog catalog) => OracleEmbedding.Create(catalog, executor)
        )
        .WithDescription("BERT + UMAP (Python): produces dumps/atlas-points.json for the API");
    });
  }
}
