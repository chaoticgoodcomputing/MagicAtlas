using Flowthru.Flow;
using MagicAtlas.Data;
using MagicAtlas.Data._03_Primary.Schemas;
using MagicAtlas.Flows.RulesProcessing.Nodes;

namespace MagicAtlas.Flows.RulesProcessing;

/// <summary>
/// Parses the MTG comprehensive rules text file into structured JSON: a hierarchical rules
/// tree plus a flat glossary dictionary. Pure C# — text/regex processing only, no Python.
/// </summary>
public static class RulesProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog, HttpClient httpClient)
  {
    return FlowBuilder.CreateFlow("RulesProcessing", pipeline =>
    {
      // Source step: resolve the current MTG rules .txt URL by scraping
      // https://magic.wizards.com/en/rules, fetch the body, and park it in RawRules.
      pipeline.AddStep<string>(
        label: "FetchRulesText",
        transform: FetchRulesTextNode.Create(httpClient),
        outputs: catalog.RawRules
      );

      // Node 1: split raw text into 5 sections
      pipeline.AddStep<
        string,
        string,
        string,
        string,
        string,
        string
      >(
        label: "SplitRulesIntoMajorSections",
        transform: SplitSectionsNode.Create(),
        inputs: catalog.RawRules,
        outputs: (
          catalog.Intro,
          catalog.TableOfContents,
          catalog.RulesText,
          catalog.GlossaryText,
          catalog.Credits
        )
      );

      // Node 2: parse rules into hierarchical structure
      pipeline.AddStep<string, RulesStructure>(
        label: "ParseRulesIntoHierarchy",
        transform: ParseRulesNode.Create(),
        inputs: catalog.RulesText,
        outputs: catalog.ParsedRules
      );

      // Node 3: parse glossary into term-definition pairs
      pipeline.AddStep<string, GlossaryEntries>(
        label: "ParseGlossaryIntoDictionary",
        transform: ParseGlossaryNode.Create(),
        inputs: catalog.GlossaryText,
        outputs: catalog.ParsedGlossary
      );
    });
  }
}
