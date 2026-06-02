namespace MagicAST.AST.References;

using System.Text.Json.Serialization;

/// <summary>
/// magic-ast-local consumer model for the MTG type ontology published by the <c>mtg-rules</c>
/// project as <c>type-ontology.json</c>. Per ADR-0008 the serialized artifact is the cross-language
/// contract — this is a thin model bound to it, NOT a project reference to mtg-rules. Carries the
/// derived type facts the relation operators read: card types (300.1), the permanent partition
/// (110.4), colors (105.1), supertypes (205.4a), and the 205.3 subtype pools with their owning
/// card type(s) — creature → <c>{creature, kindred}</c>, spell → <c>{instant, sorcery}</c>.
/// </summary>
public sealed record TypeOntology
{
  /// <summary>Content hash + CR version pin of the source ontology.</summary>
  [JsonPropertyName("ontologyHash")]
  public string? OntologyHash { get; init; }

  /// <summary>All card types (300.1).</summary>
  [JsonPropertyName("cardTypes")]
  public IReadOnlyList<string> CardTypes { get; init; } = [];

  /// <summary>The six permanent types (110.4).</summary>
  [JsonPropertyName("permanentTypes")]
  public IReadOnlyList<string> PermanentTypes { get; init; } = [];

  /// <summary>Card types that are not permanent types (cardTypes − permanentTypes).</summary>
  [JsonPropertyName("nonPermanentTypes")]
  public IReadOnlyList<string> NonPermanentTypes { get; init; } = [];

  /// <summary>The supertypes (205.4a).</summary>
  [JsonPropertyName("supertypes")]
  public IReadOnlyList<string> Supertypes { get; init; } = [];

  /// <summary>The five colors (105.1).</summary>
  [JsonPropertyName("colors")]
  public IReadOnlyList<string> Colors { get; init; } = [];

  /// <summary>Card types that have no subtypes (205.3r).</summary>
  [JsonPropertyName("noSubtypeCardTypes")]
  public IReadOnlyList<string> NoSubtypeCardTypes { get; init; } = [];

  /// <summary>The 205.3 subtype pools, each with its owning card type(s) and members.</summary>
  [JsonPropertyName("subtypePools")]
  public IReadOnlyList<SubtypePool> SubtypePools { get; init; } = [];

  /// <summary>Flattened index: subtype → the card type(s) it can belong to (the union across pools).</summary>
  [JsonPropertyName("subtypeToCardTypes")]
  public IReadOnlyDictionary<string, IReadOnlyList<string>> SubtypeToCardTypes { get; init; } =
    new Dictionary<string, IReadOnlyList<string>>();
}

/// <summary>One 205.3 subtype pool: its name, owning card type(s), source rule, and members.</summary>
public sealed record SubtypePool
{
  [JsonPropertyName("name")]
  public string Name { get; init; } = "";

  [JsonPropertyName("cardTypes")]
  public IReadOnlyList<string> CardTypes { get; init; } = [];

  [JsonPropertyName("ruleNumber")]
  public string RuleNumber { get; init; } = "";

  [JsonPropertyName("subtypes")]
  public IReadOnlyList<string> Subtypes { get; init; } = [];
}
