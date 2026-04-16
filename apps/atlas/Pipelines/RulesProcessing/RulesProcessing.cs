using Flowthru.Core.Flows;
using MagicAtlas.Data;
using MagicAtlas.Pipelines.RulesProcessing.Nodes;

namespace MagicAtlas.Pipelines.RulesProcessing;

public static class RulesProcessing
{
  public static Flow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow(pipeline =>
    {
      // Node 1: Split raw text into 5 sections
      pipeline.AddStep(
        label: "SplitRulesIntoMajorSections",
        transform: SplitSectionsNode.Create(),
        input: catalog.RawRules,
        output: (
          catalog.Intro,
          catalog.TableOfContents,
          catalog.RulesText,
          catalog.GlossaryText,
          catalog.Credits
        )
      );

      // Node 2: Parse rules into hierarchical structure
      pipeline.AddStep(
        label: "ParseRulesIntoHierarchy",
        transform: ParseRulesNode.Create(),
        input: catalog.RulesText,
        output: catalog.ParsedRules
      );

      // Node 3: Parse glossary into term-definition pairs
      pipeline.AddStep(
        label: "ParseGlossaryIntoDictionary",
        transform: ParseGlossaryNode.Create(),
        input: catalog.GlossaryText,
        output: catalog.ParsedGlossary
      );
    });
  }
}
