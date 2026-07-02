using Flowthru.Flow;
using MagicAtlas.Rules.Data;
using MagicAtlas.Rules.Data._03_Primary.Schemas;
using MagicAtlas.Rules.Flows.RulesProcessing.Nodes;

namespace MagicAtlas.Rules.Flows.RulesProcessing;

/// <summary>
/// Parses the MTG comprehensive rules text (produced by <c>Ingest</c>) into structured JSON:
/// a hierarchical rules tree plus a flat glossary dictionary. Pure C# — text/regex only, no HTTP.
/// </summary>
public static class RulesProcessingFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("RulesProcessing", pipeline =>
    {
      // Node 1: split raw text into 5 sections
      pipeline.AddStep<string, string, string, string, string, string>(
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
