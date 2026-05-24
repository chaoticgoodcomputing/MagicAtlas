namespace MagicAtlas.Services;

/// <summary>
/// Options for <see cref="OllamaService"/>. Bound from <c>Flowthru:Services:Ollama</c> in
/// <c>appsettings.json</c>. The endpoint is configurable so the harness can target a local
/// Ollama daemon, a private deployment, or a remote shared instance without code changes.
/// </summary>
public sealed class OllamaServiceOptions
{
  /// <summary>Base URL of the Ollama API (e.g. <c>https://ollama.speen.us</c>). No trailing slash.</summary>
  public string Endpoint { get; init; } = "http://localhost:11434";

  /// <summary>Default model used by <see cref="IOllamaService.GenerateAsync"/> when no model
  /// override is supplied. Should be a model already pulled on the endpoint.</summary>
  public string DefaultModel { get; init; } = "qwen3:4b";

  /// <summary>Request timeout for /api/generate (seconds). Cluster-labeling prompts are small
  /// but cold-start model load on the server can be several seconds.</summary>
  public int TimeoutSeconds { get; init; } = 120;
}
