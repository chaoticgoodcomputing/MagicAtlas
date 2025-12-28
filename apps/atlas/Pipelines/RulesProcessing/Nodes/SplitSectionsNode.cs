namespace MagicAtlas.Pipelines.RulesProcessing.Nodes;

/// <summary>
/// Splits raw MTG rules text into five major sections.
/// </summary>
public static class SplitSectionsNode
{
  public static Func<
    string,
    Task<(string intro, string toc, string rules, string glossary, string credits)>
  > Create()
  {
    return async (rawText) =>
    {
      var content = rawText;

      // Split intro (everything before "Contents")
      var (intro, remainder) = SplitAt(content, "Contents");
      content = remainder;

      // Split TOC (everything before first occurrence of "1. Game Concepts" after position 100)
      var tocEndIndex = content.IndexOf("1. Game Concepts", 100, StringComparison.Ordinal);
      var (toc, rulesAndRest) =
        tocEndIndex != -1 ? (content[..tocEndIndex].Trim(), content[tocEndIndex..]) : (content, "");
      content = rulesAndRest;

      // Split rules (everything before "Glossary")
      var (rules, glossaryAndRest) = SplitAt(content, "\nGlossary\n");
      content = glossaryAndRest;

      // Split glossary (everything before "Credits")
      var (glossary, credits) = SplitAt(content, "\nCredits\n");

      return await Task.FromResult(
        (intro: intro, toc: toc, rules: rules, glossary: glossary, credits: credits)
      );
    };
  }

  private static (string before, string after) SplitAt(string text, string delimiter)
  {
    var index = text.IndexOf(delimiter, StringComparison.Ordinal);
    if (index == -1)
    {
      return (text.Trim(), "");
    }

    return (text[..index].Trim(), text[index..]);
  }
}
