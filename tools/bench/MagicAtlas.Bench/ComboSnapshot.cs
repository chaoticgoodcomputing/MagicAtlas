using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MagicAtlas.Bench;

/// <summary>One card in a pinned combo (its full oracle name + Scryfall oracleId).</summary>
public sealed record SnapshotCard
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = "";

  [JsonPropertyName("oracleId")]
  public string OracleId { get; init; } = "";
}

/// <summary>One pinned Commander Spellbook combo (the lean projection the bench scores against).</summary>
public sealed record SnapshotCombo
{
  [JsonPropertyName("id")]
  public string Id { get; init; } = "";

  [JsonPropertyName("popularity")]
  public int Popularity { get; init; }

  [JsonPropertyName("identity")]
  public string Identity { get; init; } = "";

  [JsonPropertyName("cards")]
  public List<SnapshotCard> Cards { get; init; } = [];

  [JsonPropertyName("results")]
  public List<string> Results { get; init; } = [];
}

/// <summary>
/// The pinned, checksummed Commander Spellbook combo snapshot (alignment initiative 04 §1). It is
/// committed to the repo so the bench is deterministic and OFFLINE — no 510 MB <c>variants.json</c>
/// fetch at bench time. <see cref="Load"/> verifies the snapshot bytes against the committed
/// <c>.sha256</c> sidecar, so a silently-edited snapshot fails loudly rather than skewing the numbers.
/// </summary>
public sealed record ComboSnapshot
{
  [JsonPropertyName("source")]
  public string Source { get; init; } = "";

  [JsonPropertyName("sourceUrl")]
  public string SourceUrl { get; init; } = "";

  [JsonPropertyName("csbVersion")]
  public string CsbVersion { get; init; } = "";

  [JsonPropertyName("csbTimestamp")]
  public string CsbTimestamp { get; init; } = "";

  [JsonPropertyName("eligibleCount")]
  public int EligibleCount { get; init; }

  [JsonPropertyName("combos")]
  public List<SnapshotCombo> Combos { get; init; } = [];

  private static readonly JsonSerializerOptions ReadOptions = new()
  {
    PropertyNameCaseInsensitive = true,
  };

  /// <summary>
  /// Load and checksum-verify the pinned snapshot. The combos are returned in ordinal-sorted id order
  /// so the runner iterates deterministically regardless of the snapshot's on-disk member order.
  /// </summary>
  public static ComboSnapshot Load(string snapshotPath)
  {
    var bytes = File.ReadAllBytes(snapshotPath);

    var checksumPath = snapshotPath + ".sha256";
    if (File.Exists(checksumPath))
    {
      var expected = File.ReadAllText(checksumPath).Trim().ToLowerInvariant();
      var actual = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
      if (!string.Equals(expected, actual, StringComparison.Ordinal))
        throw new InvalidOperationException(
          $"Spellbook combo snapshot checksum mismatch: expected {expected}, got {actual}. "
            + "The pinned snapshot has been altered — refusing to run on unpinned data."
        );
    }

    var snapshot =
      JsonSerializer.Deserialize<ComboSnapshot>(Encoding.UTF8.GetString(bytes), ReadOptions)
      ?? throw new InvalidOperationException($"Could not parse combo snapshot at {snapshotPath}");

    return snapshot with
    {
      Combos = [.. snapshot.Combos.OrderBy(c => c.Id, StringComparer.Ordinal)],
    };
  }
}
