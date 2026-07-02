using Flowthru.Data.Schema;

namespace MagicAtlas.Rules.Data._03_Primary.Schemas;

/// <summary>
/// The deterministic MTG type ontology, derived from the comprehensive rules. Pure facts — card
/// types (300.1), the permanent partition (110.4), colors (105.1), supertypes (205.4a), and the
/// 205.3 subtype pools each mapped to their owning card type(s). Carries no copyrighted rules
/// prose; content-hashed so a given input rules tree always yields a byte-identical artifact.
/// </summary>
/// <remarks>
/// This is the ground-truth artifact. The overlap operator's lean runtime approximation
/// (closed-pool enumeration + creature-default) is a separate consumer choice certified against
/// this. Per the rules-judge panel: creature types straddle <c>{creature, kindred}</c> and spell
/// types straddle <c>{instant, sorcery}</c> — encoded in each pool's <see cref="SubtypePool.CardTypes"/>.
/// </remarks>
[FlowthruSchema]
public partial record TypeOntology
{
  /// <summary>SHA-256 (lowercase hex) of the canonical body — a pinnable version handle.</summary>
  [SerializedLabel("ontologyHash")]
  public string OntologyHash { get; init; } = "";

  /// <summary>All card types (300.1).</summary>
  [SerializedLabel("cardTypes")]
  public List<string> CardTypes { get; init; } = new();

  /// <summary>The six permanent types (110.4).</summary>
  [SerializedLabel("permanentTypes")]
  public List<string> PermanentTypes { get; init; } = new();

  /// <summary>Card types that are not permanent types (cardTypes − permanentTypes). Kindred
  /// appears here but is partition-neutral per 308.1 — a consumer concern, not a data claim.</summary>
  [SerializedLabel("nonPermanentTypes")]
  public List<string> NonPermanentTypes { get; init; } = new();

  /// <summary>The supertypes (205.4a): basic, legendary, ongoing, snow, world.</summary>
  [SerializedLabel("supertypes")]
  public List<string> Supertypes { get; init; } = new();

  /// <summary>The five colors (105.1).</summary>
  [SerializedLabel("colors")]
  public List<string> Colors { get; init; } = new();

  /// <summary>Card types that have no subtypes (205.3r): conspiracy, phenomenon, scheme, vanguard.</summary>
  [SerializedLabel("noSubtypeCardTypes")]
  public List<string> NoSubtypeCardTypes { get; init; } = new();

  /// <summary>The 205.3 subtype pools, each with its owning card type(s) and members.</summary>
  [SerializedLabel("subtypePools")]
  public List<SubtypePool> SubtypePools { get; init; } = new();

  /// <summary>Flattened convenience index: subtype → the card type(s) it can belong to. A subtype
  /// shared across pools (e.g. "Spacecraft" is both artifact and planar) maps to the union.</summary>
  [SerializedLabel("subtypeToCardTypes")]
  public Dictionary<string, List<string>> SubtypeToCardTypes { get; init; } = new();
}

/// <summary>One 205.3 subtype pool: its name, the card type(s) it belongs to, source rule, members.</summary>
[FlowthruSchema]
public partial record SubtypePool
{
  /// <summary>Pool name as the rules name it ("creature", "land", "artifact", "spell", "planar", ...).</summary>
  [SerializedLabel("name")]
  public string Name { get; init; } = null!;

  /// <summary>Owning card type(s). Creature → {creature, kindred}; spell → {instant, sorcery}; else singleton.</summary>
  [SerializedLabel("cardTypes")]
  public List<string> CardTypes { get; init; } = new();

  /// <summary>Source subrule, e.g. "205.3m".</summary>
  [SerializedLabel("ruleNumber")]
  public string RuleNumber { get; init; } = null!;

  /// <summary>The subtypes in this pool, sorted.</summary>
  [SerializedLabel("subtypes")]
  public List<string> Subtypes { get; init; } = new();
}
