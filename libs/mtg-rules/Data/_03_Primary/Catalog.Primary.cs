using Flowthru.Data.Catalog;
using MagicAtlas.Rules.Data._03_Primary.Schemas;

namespace MagicAtlas.Rules.Data;

/// <summary>
/// Primary data catalog entries (Layer 3): the deterministic artifacts this project publishes —
/// the structured rules tree, the glossary dictionary, and the derived type ontology.
/// </summary>
public partial class Catalog
{
  /// <summary>Parsed hierarchical rules structure (<c>rules-structure.json</c>).</summary>
  public IItem<RulesStructure> ParsedRules =>
    CreateItem(() =>
      Item.Of<RulesStructure>("ParsedRules")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/rules-structure.json")
        .Build()
    );

  /// <summary>Parsed glossary as term-definition pairs (<c>glossary.json</c>).</summary>
  public IItem<GlossaryEntries> ParsedGlossary =>
    CreateItem(() =>
      Item.Of<GlossaryEntries>("ParsedGlossary")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/glossary.json")
        .Build()
    );

  /// <summary>
  /// The derived MTG type ontology (<c>type-ontology.json</c>) — card types, the permanent
  /// partition, colors, supertypes, and the 205.3 subtype pools with their owning card types.
  /// Deterministic facts (no copyrighted rules prose), content-hashed for pin-ability. The
  /// canonical ground-truth artifact MAST's ObjectFilter overlap operator is certified against.
  /// </summary>
  public IItem<TypeOntology> TypeOntology =>
    CreateItem(() =>
      Item.Of<TypeOntology>("TypeOntology")
        .Json()
        .AtPath($"{_basePath}/_03_Primary/Datasets/type-ontology.json")
        .Build()
    );
}
