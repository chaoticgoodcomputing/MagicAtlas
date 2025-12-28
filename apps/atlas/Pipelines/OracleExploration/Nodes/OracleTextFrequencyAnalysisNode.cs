using MagicAtlas.Data._05_ModelInput.Schemas;
using MagicAtlas.Data._08_Reporting.Schemas;

namespace MagicAtlas.Pipelines.OracleExploration.Nodes;

/// <summary>
/// Generates a frequency count for oracle text entries, determining how many times an exact oracle
/// text entry occurs in the data.
/// </summary>
public static class OracleTextFrequencyAnalysisNode
{
  public class Params
  {
    /// <summary>
    /// Number of top duplicate oracle texts to keep.
    /// </summary>
    public int KeepTopN { get; set; } = 100;
  }

  public static Func<
    IEnumerable<EmbeddingModelOracleInput>,
    Task<IEnumerable<OracleTextDuplicateCount>>
  > Create(Params? parameters = null)
  {
    parameters ??= new Params();

    return async (input) =>
    {
      var countDict = new Dictionary<string, int>();

      // Iterate through all oracle text entries and create a frequency count
      foreach (var entry in input)
      {
        var oracleText = entry.Text;
        if (countDict.ContainsKey(oracleText))
        {
          countDict[oracleText]++;
        }
        else
        {
          countDict[oracleText] = 1;
        }
      }

      // Flatten to expected result
      var result = countDict
        .Select(kvp => new OracleTextDuplicateCount
        {
          OracleText = kvp.Key,
          DuplicateCount = kvp.Value,
        })
        .OrderByDescending(otdc => otdc.DuplicateCount)
        .Take(parameters.KeepTopN)
        .ToList();

      return await Task.FromResult(result);
    };
  }
}
