using System.Text;
using System.Text.Json.Serialization;
using Flowthru.Step;
using MagicAtlas.Data._00_Config.Schemas;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Services;

namespace MagicAtlas.Flows.TagLabeling.Nodes;

/// <summary>
/// Synthesizes a per-cluster display label by asking Qwen (via <see cref="IOllamaService"/>)
/// to arbitrate the top-K tag candidates and sample lines surfaced by
/// <see cref="ClusterTagAffinity"/>. Output uses the existing <see cref="ClusterLabel"/> shape
/// so reporting / API consumers can swap label sources without code changes.
/// </summary>
/// <remarks>
/// <para>
/// Prompts are deliberately structured: the model receives the candidate set in ranked order
/// plus a handful of representative oracle lines, and is asked for a 2-4 word label, a
/// one-sentence description, and a short keyword list. JSON-schema-constrained output via
/// Ollama's <c>format</c> parameter guarantees parseable structure.
/// </para>
/// <para>
/// Includes a hardcoded sentinel row for the noise bucket (cluster_id = -1) using the same
/// "(noise)" label the c-TF-IDF labeler emits, so downstream consumers see the same row set
/// across both label backends.
/// </para>
/// </remarks>
[FlowthruStep]
public static class LabelClustersWithQwenNode
{
  public static Func<
    (IEnumerable<ClusterTagAffinity>, IEnumerable<ClusterAssignment>, TagLabelingConfig),
    Task<IEnumerable<ClusterLabel>>
  > Create(IOllamaService ollama) =>
    async input =>
    {
      var (affinityRows, assignments, config) = input;
      var modelName = string.IsNullOrWhiteSpace(config.LabelerModel)
        ? ollama.DefaultModel
        : config.LabelerModel;

      var affinityList = affinityRows.ToList();
      Console.Error.WriteLine(
        $"[LabelClustersWithQwen] Labeling {affinityList.Count} clusters via {modelName} "
          + $"(TopK={config.TopKAffinity}, SampleLines={config.MaxSampleLines})"
      );

      var labels = new List<ClusterLabel>(affinityList.Count + 1);
      int processed = 0;
      foreach (var row in affinityList.OrderByDescending(r => r.ClusterSize))
      {
        QwenClusterLabel parsed;
        try
        {
          var prompt = BuildPrompt(row);
          parsed = await ollama.GenerateStructuredAsync<QwenClusterLabel>(
            prompt, model: modelName, temperature: 0.0
          );
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine(
            $"[LabelClustersWithQwen] Cluster {row.ClusterId} (size {row.ClusterSize}) failed: "
              + $"{ex.GetType().Name}: {ex.Message}; falling back to top candidate"
          );
          var fallback = row.CandidateNames.FirstOrDefault() ?? $"Cluster {row.ClusterId}";
          parsed = new QwenClusterLabel
          {
            Label = fallback,
            Description = null,
            Keywords = row.CandidateSlugs.Take(3).ToList(),
          };
        }

        labels.Add(new ClusterLabel
        {
          ClusterId = row.ClusterId,
          Label = parsed.Label?.Trim() ?? "(unlabeled)",
          Description = string.IsNullOrWhiteSpace(parsed.Description) ? null : parsed.Description.Trim(),
          Keywords = System.Text.Json.JsonSerializer.Serialize(parsed.Keywords ?? []),
          Size = row.ClusterSize,
          Source = "qwen",
          SourceVersion = modelName,
        });

        processed++;
        if (processed % 25 == 0 || processed == affinityList.Count)
        {
          Console.Error.WriteLine($"[LabelClustersWithQwen] Labeled {processed}/{affinityList.Count}");
        }
      }

      // Noise sentinel row, matching the c-TF-IDF labeler's contract so downstream consumers
      // see the same row set across backends.
      var noiseSize = assignments.Count(a => a.ClusterId == -1);
      if (noiseSize > 0)
      {
        labels.Add(new ClusterLabel
        {
          ClusterId = -1,
          Label = "(noise)",
          Description = null,
          Keywords = "[]",
          Size = noiseSize,
          Source = "qwen",
          SourceVersion = modelName,
        });
      }

      return labels;
    };

  private static string BuildPrompt(ClusterTagAffinity row)
  {
    var sb = new StringBuilder();
    sb.AppendLine("You are labeling a cluster of Magic: The Gathering oracle-text lines.");
    sb.AppendLine();
    sb.AppendLine("CANDIDATE ARCHETYPES (ranked by similarity to the cluster centroid):");
    for (int i = 0; i < row.CandidateSlugs.Count; i++)
    {
      var marker = row.CandidateSources[i] == "exemplar" ? "*" : " ";
      sb.AppendLine(
        $"  {i + 1}. {marker} {row.CandidateNames[i]} ({row.CandidateSlugs[i]}, score={row.CandidateScores[i]:F3}, source={row.CandidateSources[i]})"
      );
    }
    sb.AppendLine("  (* = curated exemplar; prefer these when their score is comparable.)");
    sb.AppendLine();
    sb.AppendLine($"SAMPLE LINES from this cluster ({row.ClusterSize} total members):");
    foreach (var line in row.SampleLines)
    {
      sb.AppendLine($"  • {line}");
    }
    sb.AppendLine();
    sb.AppendLine("TASK: Produce a JSON object with three fields:");
    sb.AppendLine("  • label: 2-4 word display label (e.g. \"Counterspell\", \"ETB Triggers\", \"Token Creation\")");
    sb.AppendLine("  • description: one-sentence summary of the archetype.");
    sb.AppendLine("  • keywords: a short list (3-6) of single-word or short-phrase keywords.");
    sb.AppendLine();
    sb.AppendLine("Pick the most specific candidate that fits the samples; if no candidate fits well, synthesize a label from the samples.");
    return sb.ToString();
  }

  /// <summary>JSON schema for Ollama's structured-output mode. System.Text.Json builds the schema
  /// from this shape; Qwen returns JSON conforming to it. <see cref="JsonRequiredAttribute"/> is
  /// needed on every field because <c>JsonSchemaExporter</c> defaults to optional (no
  /// <c>required</c> array) — Qwen treats optional fields as skippable, observed empirically as
  /// 0/588 clusters receiving a description on the first labeling pass.</summary>
  public sealed record QwenClusterLabel
  {
    [JsonRequired] public string Label { get; init; } = "";
    [JsonRequired] public string Description { get; init; } = "";
    [JsonRequired] public List<string> Keywords { get; init; } = new();
  }
}
