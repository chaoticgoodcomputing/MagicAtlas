using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MagicAtlas.Harness;

/// <summary>
/// <see cref="DelegatingHandler"/> that caches successful GET responses on disk with a configurable
/// TTL. Each entry is two files keyed by a SHA-256 of the request URI: <c>{hash}.body</c> (raw
/// response bytes) and <c>{hash}.meta.json</c> (URL, fetched-at, status code, content-type). On a
/// cache hit the handler short-circuits the inner handler and synthesises an
/// <see cref="HttpResponseMessage"/> backed by a fresh <see cref="FileStream"/> over the cached
/// body, so streaming consumers (<c>GetStreamAsync</c>, large bulk downloads) keep working without
/// loading the body into memory.
/// </summary>
/// <remarks>
/// Only <c>GET</c> requests are cached. Non-2xx responses are passed through untouched. The cache
/// is keyed by full URI only — no Vary-header handling — which is fine for our use (Scryfall +
/// Wizards endpoints don't vary by request headers in any way that matters here). There is no
/// eviction; <c>.http-cache/</c> is gitignored and bounded in practice by the small set of stable
/// URLs the Ingest flow fetches.
/// </remarks>
public sealed class FilesystemHttpCacheHandler : DelegatingHandler
{
  private readonly string _cacheDirectory;
  private readonly TimeSpan _ttl;
  private readonly ILogger<FilesystemHttpCacheHandler>? _logger;

  public FilesystemHttpCacheHandler(
    HttpMessageHandler inner,
    string cacheDirectory,
    TimeSpan ttl,
    ILogger<FilesystemHttpCacheHandler>? logger = null
  )
    : base(inner)
  {
    _cacheDirectory = cacheDirectory;
    _ttl = ttl;
    _logger = logger;
    Directory.CreateDirectory(_cacheDirectory);
  }

  protected override async Task<HttpResponseMessage> SendAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken
  )
  {
    if (request.Method != HttpMethod.Get || request.RequestUri is null)
      return await base.SendAsync(request, cancellationToken);

    var url = request.RequestUri.ToString();
    var key = ComputeKey(url);
    var bodyPath = Path.Combine(_cacheDirectory, $"{key}.body");
    var metaPath = Path.Combine(_cacheDirectory, $"{key}.meta.json");

    if (TryReadFreshCache(bodyPath, metaPath, out var cachedMeta, out var age))
    {
      _logger?.LogInformation("HTTP cache hit: {Url} (age {Age:c})", url, age);
      return BuildResponseFromCache(bodyPath, cachedMeta!);
    }

    var response = await base.SendAsync(request, cancellationToken);
    if (!response.IsSuccessStatusCode) return response;

    await WriteCacheAsync(response, url, bodyPath, metaPath, cancellationToken);
    return SwapContentForCachedStream(response, bodyPath);
  }

  private bool TryReadFreshCache(
    string bodyPath,
    string metaPath,
    out CacheMeta? meta,
    out TimeSpan age
  )
  {
    meta = null;
    age = TimeSpan.Zero;
    if (!File.Exists(bodyPath) || !File.Exists(metaPath)) return false;
    try
    {
      using var stream = File.OpenRead(metaPath);
      meta = JsonSerializer.Deserialize<CacheMeta>(stream);
    }
    catch (JsonException)
    {
      return false;
    }
    if (meta is null) return false;
    age = DateTimeOffset.UtcNow - meta.FetchedAt;
    return age < _ttl;
  }

  private async Task WriteCacheAsync(
    HttpResponseMessage response,
    string url,
    string bodyPath,
    string metaPath,
    CancellationToken cancellationToken
  )
  {
    await using (var src = await response.Content.ReadAsStreamAsync(cancellationToken))
    await using (var dst = File.Create(bodyPath))
    {
      await src.CopyToAsync(dst, cancellationToken);
    }
    var meta = new CacheMeta(
      url,
      DateTimeOffset.UtcNow,
      (int)response.StatusCode,
      response.Content.Headers.ContentType?.ToString()
    );
    await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(meta), cancellationToken);
  }

  private static HttpResponseMessage SwapContentForCachedStream(
    HttpResponseMessage response,
    string bodyPath
  )
  {
    // Replace the (already-drained) network content with a fresh stream over the cached file so
    // downstream consumers can read the body normally.
    var originalContentType = response.Content.Headers.ContentType;
    response.Content.Dispose();
    var fileStream = File.OpenRead(bodyPath);
    response.Content = new StreamContent(fileStream);
    if (originalContentType is not null)
      response.Content.Headers.ContentType = originalContentType;
    return response;
  }

  private static HttpResponseMessage BuildResponseFromCache(string bodyPath, CacheMeta meta)
  {
    var fileStream = File.OpenRead(bodyPath);
    var response = new HttpResponseMessage((System.Net.HttpStatusCode)meta.StatusCode)
    {
      Content = new StreamContent(fileStream),
    };
    if (!string.IsNullOrWhiteSpace(meta.ContentType))
    {
      response.Content.Headers.TryAddWithoutValidation("Content-Type", meta.ContentType);
    }
    return response;
  }

  private static string ComputeKey(string url)
  {
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(url));
    return Convert.ToHexString(bytes, 0, 8).ToLowerInvariant();
  }

  private sealed record CacheMeta(
    string Url,
    DateTimeOffset FetchedAt,
    int StatusCode,
    string? ContentType
  );
}
