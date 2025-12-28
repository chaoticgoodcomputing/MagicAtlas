using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Helpers;

namespace MagicAtlas.Pipelines.RulesProcessing.Nodes;

/// <summary>
/// Parses glossary text into term-definition pairs.
/// </summary>
public static class ParseGlossaryNode
{
  public static Func<string, Task<GlossaryEntries>> Create()
  {
    return async (glossaryText) =>
    {
      var terms = new Dictionary<string, string>();

      // Split into individual entries by double newlines
      var entries = glossaryText.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

      foreach (var entry in entries)
      {
        // Skip the "Glossary" header
        if (entry.Trim() == "Glossary")
        {
          continue;
        }

        var lines = entry.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0)
        {
          continue;
        }

        // First line is the term
        var term = TextNormalizer.NormalizeText(lines[0].Trim());

        // Remaining lines are the definition
        if (lines.Length > 1)
        {
          var definition = string.Join(" ", lines.Skip(1).Select(l => l.Trim()));
          terms[term] = TextNormalizer.NormalizeText(definition);
        }
      }

      var glossaryEntries = new GlossaryEntries { Terms = terms };
      return await Task.FromResult(glossaryEntries);
    };
  }
}
