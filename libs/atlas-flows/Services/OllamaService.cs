using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MagicAtlas.Services;

/// <summary>
/// HttpClient-backed Ollama client. Targets the <c>/api/generate</c> endpoint with
/// <c>stream=false</c> for synchronous responses and uses <c>format</c> + JSON schema for
/// structured calls. One service instance per process — the underlying <see cref="HttpClient"/>
/// is reused across requests.
/// </summary>
/// <remarks>
/// <para>Uses a dedicated <see cref="HttpClient"/> rather than the Scryfall/rules client because
/// (a) its <c>FilesystemHttpCacheHandler</c> would wrongly cache prompt responses, and
/// (b) Ollama benefits from a longer timeout for cold model loads.</para>
/// </remarks>
public sealed class OllamaService : IOllamaService, IDisposable
{
  private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
  private static readonly JsonSchemaExporterOptions SchemaOptions = new()
  {
    TreatNullObliviousAsNonNullable = true,
  };

  private readonly HttpClient _http;
  private readonly OllamaServiceOptions _options;
  private readonly ILogger<OllamaService> _logger;

  public OllamaService(IOptions<OllamaServiceOptions> options, ILogger<OllamaService> logger)
  {
    _options = options.Value;
    _logger = logger;
    _http = new HttpClient
    {
      BaseAddress = new Uri(_options.Endpoint.TrimEnd('/') + "/"),
      Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds),
    };
    _http.DefaultRequestHeaders.UserAgent.ParseAdd("MagicAtlas/0.1");
  }

  public string DefaultModel => _options.DefaultModel;

  public async Task<OllamaHealth> HealthCheckAsync(CancellationToken cancellationToken = default)
  {
    try
    {
      var resp = await _http.GetAsync("api/tags", cancellationToken);
      if (!resp.IsSuccessStatusCode)
      {
        return new OllamaHealth(
          EndpointReachable: false,
          ModelAvailable: false,
          AvailableModels: [],
          Diagnostic: $"GET /api/tags returned HTTP {(int)resp.StatusCode} from {_options.Endpoint}"
        );
      }

      var payload = await resp.Content.ReadFromJsonAsync<TagsResponse>(JsonOptions, cancellationToken);
      var names = payload?.Models?.Select(m => m.Name).ToList() ?? new List<string>();
      var available = names.Contains(_options.DefaultModel);
      var diag = available
        ? $"Endpoint {_options.Endpoint} reachable; model '{_options.DefaultModel}' available."
        : $"Endpoint {_options.Endpoint} reachable but model '{_options.DefaultModel}' is not pulled. "
          + $"Available: [{string.Join(", ", names)}]. Run `ollama pull {_options.DefaultModel}` on the server.";
      return new OllamaHealth(true, available, names, diag);
    }
    catch (Exception ex)
    {
      return new OllamaHealth(
        EndpointReachable: false,
        ModelAvailable: false,
        AvailableModels: [],
        Diagnostic: $"Could not reach Ollama at {_options.Endpoint}: {ex.GetType().Name}: {ex.Message}"
      );
    }
  }

  public async Task<string> GenerateAsync(
    string prompt,
    string? model = null,
    double temperature = 0.0,
    CancellationToken cancellationToken = default)
  {
    var modelName = model ?? _options.DefaultModel;
    var request = new GenerateRequest(modelName, prompt, Stream: false, Format: null,
      new GenerateOptions(Temperature: temperature));
    var resp = await PostGenerateAsync(request, cancellationToken);
    return resp.Response;
  }

  public async Task<T> GenerateStructuredAsync<T>(
    string prompt,
    string? model = null,
    double temperature = 0.0,
    CancellationToken cancellationToken = default) where T : notnull
  {
    var modelName = model ?? _options.DefaultModel;
    var schema = JsonSerializerOptions.Default.GetJsonSchemaAsNode(typeof(T), SchemaOptions);
    EnforceNonEmptyStrings(schema);
    var request = new GenerateRequest(modelName, prompt, Stream: false, Format: schema,
      new GenerateOptions(Temperature: temperature));

    var resp = await PostGenerateAsync(request, cancellationToken);
    var trimmed = resp.Response.Trim();
    try
    {
      return JsonSerializer.Deserialize<T>(trimmed, JsonOptions)
        ?? throw new InvalidOperationException("Ollama returned JSON null for a non-nullable structured request");
    }
    catch (JsonException ex)
    {
      _logger.LogError(ex, "Failed to parse Ollama structured response as {Type}. Raw: {Raw}", typeof(T).Name, trimmed);
      throw;
    }
  }

  private async Task<GenerateResponse> PostGenerateAsync(GenerateRequest request, CancellationToken ct)
  {
    var resp = await _http.PostAsJsonAsync("api/generate", request, JsonOptions, ct);
    if (!resp.IsSuccessStatusCode)
    {
      var body = await resp.Content.ReadAsStringAsync(ct);
      throw new HttpRequestException(
        $"Ollama /api/generate returned HTTP {(int)resp.StatusCode}: {body}"
      );
    }
    var payload = await resp.Content.ReadFromJsonAsync<GenerateResponse>(JsonOptions, ct);
    return payload ?? throw new InvalidOperationException("Ollama /api/generate returned empty body");
  }

  /// <summary>
  /// Walk the auto-generated JSON schema and add <c>minLength: 1</c> to every string property
  /// (and <c>minItems: 1</c> to every array property). Without this, Qwen reliably satisfies
  /// the schema's <c>required</c> constraint by emitting empty strings / empty arrays, which
  /// validates structurally but conveys no content. Observed empirically: 0/588 descriptions
  /// populated until this guard was added.
  /// </summary>
  private static void EnforceNonEmptyStrings(System.Text.Json.Nodes.JsonNode? node)
  {
    if (node is not System.Text.Json.Nodes.JsonObject obj) return;
    if (obj["type"]?.GetValue<string>() == "string")
    {
      obj["minLength"] = 1;
    }
    else if (obj["type"]?.GetValue<string>() == "array")
    {
      obj["minItems"] = 1;
      EnforceNonEmptyStrings(obj["items"]);
    }
    if (obj["properties"] is System.Text.Json.Nodes.JsonObject props)
    {
      foreach (var kv in props)
      {
        EnforceNonEmptyStrings(kv.Value);
      }
    }
  }

  public void Dispose() => _http.Dispose();

  // ── DTOs (private — internal to this client) ──
  // `Think = false`: disables the qwen3 family's hybrid-reasoning mode. Without this, qwen3
  // models route format-constrained JSON output to the `thinking` field and leave `response`
  // empty — observed as 0/588 deserializable structured responses against qwen3:4b.
  private sealed record GenerateRequest(
    string Model,
    string Prompt,
    bool Stream,
    object? Format,
    GenerateOptions Options,
    bool Think = false
  );
  private sealed record GenerateOptions(double Temperature);
  private sealed record GenerateResponse(string Response, bool Done);
  private sealed record TagsResponse(List<TagsModel>? Models);
  private sealed record TagsModel(string Name);
}
