using Flowthru.Flow;
using MagicAtlas.Rules.Data;
using MagicAtlas.Rules.Data._03_Primary.Schemas;
using MagicAtlas.Rules.Flows.Ontology.Nodes;

namespace MagicAtlas.Rules.Flows.Ontology;

/// <summary>
/// Derives the deterministic MTG type ontology from the structured rules tree (produced by
/// <c>RulesProcessing</c>). Pure C# over <c>ParsedRules</c> — consumes the parsed rules as input
/// rather than re-deriving them, so it can run standalone against any vendored
/// <c>rules-structure.json</c> for offline determinism checks.
/// </summary>
public static class OntologyFlow
{
  public static BuiltFlow Create(Catalog catalog)
  {
    return FlowBuilder.CreateFlow("TypeOntology", pipeline =>
    {
      pipeline.AddStep<RulesStructure, TypeOntology>(
        label: "BuildTypeOntology",
        transform: BuildTypeOntologyNode.Create(),
        inputs: catalog.ParsedRules,
        outputs: catalog.TypeOntology
      );
    });
  }
}
