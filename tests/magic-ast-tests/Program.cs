using Flowthru.Cli;
using Flowthru.Hosting;
using MagicAtlas.Ast.Tests.Data;
using MagicAtlas.Ast.Tests.Flows.MagicAstSmoke;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Ast.Tests;

/// <summary>
/// Entry point for the MagicAST test harness — a self-contained Flowthru project whose flows
/// will, over time, validate the <c>MagicAST</c> oracle-text parser against gold-standard ASTs.
/// Today it only houses a single smoke flow that proves the wiring works end-to-end.
/// </summary>
public class Program
{
  public static Task<int> Main(string[] args) =>
    FlowthruCli.RunStandaloneAsync(
      args,
      services => ConfigureServices(services, ResolveHarnessDirectory())
    );

  private static string ResolveHarnessDirectory()
  {
    const string csproj = "MagicAtlas.Ast.Tests.csproj";
    var dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir is not null)
    {
      if (File.Exists(Path.Combine(dir.FullName, csproj))) return dir.FullName;
      dir = dir.Parent;
    }
    var cwd = new DirectoryInfo(Directory.GetCurrentDirectory());
    while (cwd is not null)
    {
      var candidate = Path.Combine(cwd.FullName, "tests", "magic-ast-tests", csproj);
      if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
      cwd = cwd.Parent;
    }
    throw new InvalidOperationException(
      $"Could not locate {csproj}. Run from within the workspace, or set CWD to tests/magic-ast-tests."
    );
  }

  private static void ConfigureServices(IServiceCollection services, string basePath)
  {
    var configuration = new ConfigurationBuilder()
      .SetBasePath(basePath)
      .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
      .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
      .Build();
    services.AddSingleton<IConfiguration>(configuration);

    services.AddLogging(logging =>
    {
      logging.AddConsole();
      logging.SetMinimumLevel(LogLevel.Information);
    });

    services.AddFlowthru(flowthru =>
    {
      flowthru.RegisterCatalog(_ => new Catalog());

      flowthru
        .RegisterFlow<Catalog>("MagicAstSmoke", MagicAstSmokeFlow.Create)
        .WithDescription("Placeholder smoke test that runs MagicAST.OracleParser over a fixed input.");
    });
  }
}
