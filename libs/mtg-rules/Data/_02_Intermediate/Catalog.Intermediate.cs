using Flowthru.Data.Catalog;

namespace MagicAtlas.Rules.Data;

/// <summary>
/// Intermediate data catalog (Layer 2): the five major sections the raw rules text splits into
/// (<c>SplitRulesIntoMajorSections</c>). Each is a gitignored text build artifact reproducing WotC
/// prose. Only <see cref="RulesText"/> and <see cref="GlossaryText"/> feed downstream parsing;
/// <see cref="Intro"/>, <see cref="TableOfContents"/>, and <see cref="Credits"/> are retained for
/// provenance and split-point debugging.
/// </summary>
public partial class Catalog
{
  /// <summary>Front matter before the "Contents" table.</summary>
  public IItem<string> Intro =>
    CreateItem(() =>
      Item.Of<string>("Intro")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/intro.txt")
        .Build()
    );

  /// <summary>The table-of-contents block (up to "1. Game Concepts").</summary>
  public IItem<string> TableOfContents =>
    CreateItem(() =>
      Item.Of<string>("TableOfContents")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/table-of-contents.txt")
        .Build()
    );

  /// <summary>
  /// The numbered rules body (from "1. Game Concepts" up to the Glossary). Parsed into the
  /// hierarchical <c>RulesStructure</c> by <c>ParseRulesIntoHierarchy</c>.
  /// </summary>
  public IItem<string> RulesText =>
    CreateItem(() =>
      Item.Of<string>("RulesText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/rules-text.txt")
        .Build()
    );

  /// <summary>
  /// The glossary section (between "Glossary" and "Credits"). Parsed into term-definition pairs
  /// by <c>ParseGlossaryIntoDictionary</c>.
  /// </summary>
  public IItem<string> GlossaryText =>
    CreateItem(() =>
      Item.Of<string>("GlossaryText")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/glossary-text.txt")
        .Build()
    );

  /// <summary>The trailing credits section.</summary>
  public IItem<string> Credits =>
    CreateItem(() =>
      Item.Of<string>("Credits")
        .Text()
        .AtPath($"{_basePath}/_02_Intermediate/Datasets/credits.txt")
        .Build()
    );
}
