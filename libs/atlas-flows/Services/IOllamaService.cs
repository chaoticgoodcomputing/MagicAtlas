namespace MagicAtlas.Services;

/// <summary>
/// Wraps an Ollama HTTP endpoint for use as a Flowthru effect. Steps that need an LLM call
/// depend on this interface; <c>Program.cs</c> registers the concrete <see cref="OllamaService"/>
/// in DI and attaches an <c>AddFlowServiceInspector&lt;IOllamaService&gt;</c> preflight probe so
/// the flow fails fast if the endpoint is unreachable or the configured model isn't pulled.
/// </summary>
/// <remarks>
/// The default model and endpoint come from <see cref="OllamaServiceOptions"/>; per-call
/// overrides are available where a step wants to swap models (e.g. cluster labeling vs.
/// future tag-description generation).
/// </remarks>
public interface IOllamaService
{
  /// <summary>The default model resolved at construction. Exposed for diagnostics.</summary>
  string DefaultModel { get; }

  /// <summary>Verify endpoint reachability and that the requested model is available.
  /// Used by the preflight inspector to fail fast before any compute runs.</summary>
  Task<OllamaHealth> HealthCheckAsync(CancellationToken cancellationToken = default);

  /// <summary>Free-form text generation. Returns the model's raw response string.</summary>
  Task<string> GenerateAsync(
    string prompt,
    string? model = null,
    double temperature = 0.0,
    CancellationToken cancellationToken = default
  );

  /// <summary>
  /// Structured generation. Asks Ollama to constrain output to a JSON schema derived from
  /// <typeparamref name="T"/>, then deserializes into <typeparamref name="T"/>. Use this when
  /// the caller needs typed fields rather than a free-form string.
  /// </summary>
  Task<T> GenerateStructuredAsync<T>(
    string prompt,
    string? model = null,
    double temperature = 0.0,
    CancellationToken cancellationToken = default
  ) where T : notnull;
}

/// <summary>Result of <see cref="IOllamaService.HealthCheckAsync"/>.</summary>
/// <param name="EndpointReachable">true iff <c>/api/tags</c> responded with HTTP 200.</param>
/// <param name="ModelAvailable">true iff <see cref="OllamaServiceOptions.DefaultModel"/> appears
/// in the endpoint's model list.</param>
/// <param name="AvailableModels">All model names the endpoint reports. Empty when unreachable.</param>
/// <param name="Diagnostic">Human-readable detail, suitable for surfacing in a preflight failure.</param>
public sealed record OllamaHealth(
  bool EndpointReachable,
  bool ModelAvailable,
  IReadOnlyList<string> AvailableModels,
  string Diagnostic
);
