namespace MagicAtlas.Ast.Tests.Data._08_Reporting.Schemas;

using Flowthru.Data.Schema;

/// <summary>
/// The <b>widened-attribute report</b> (ADR-0004 §6). Makes "which ports are BROADER than the card they
/// came from" a query rather than an act of memory — the over-approximation class the dropped-condition
/// report (#33) structurally cannot see.
///
/// <para><b>Three adjacent, non-interchangeable classes.</b> Conflating any two would let one launder
/// another, so the boundaries are structural rather than agreed:</para>
/// <list type="bullet">
/// <item><description><c>known-coarse-projections.json</c> — a discriminator PortWalk DOES dispatch on
/// but projects <b>coarsely</b>, to an <c>emit:&lt;x&gt;</c> no flow arm reads. Unit: a discriminator
/// NAME. Hand-authored, gate-enforced. Loss: <b>resolution</b>.</description></item>
/// <item><description>The over-approximation report (#33) — an AST <c>Condition</c> NODE the projection
/// drops entirely. Unit: a condition node instance on a card. Derived by ablation, diagnostic. Loss: a
/// <b>guard</b> (the port is projected when it should not be projected at all).</description></item>
/// <item><description><b>This report</b> — a narrowing FACET the AST carries that the port does not.
/// Unit: an attribute site on a card. Derived by ablation, diagnostic. Loss: <b>scope</b> (the port is
/// rightly projected, but it names more of the game than the card does).</description></item>
/// </list>
///
/// <para>The partition between the last two is enforced by construction, not by convention: an attribute
/// site is a subtree containing NO polymorphic node, and a <c>Condition</c> is a node, so no condition can
/// ever appear here and no attribute can ever appear there. See
/// <c>MagicAST.Interaction.AttributeConsumption</c>.</para>
///
/// <para>Fully derived — no register, no whitelist, no baseline. Corpus-gated diagnostic (gitignored,
/// never committed); never a gate.</para>
/// </summary>
[FlowthruSchema]
public partial record WidenedAttributeReport
{
  [SerializedLabel("generatedAt")]
  public required string GeneratedAt { get; init; }

  [SerializedLabel("note")]
  public required string Note { get; init; }

  /// <summary>Cards scanned (the parse-ready CSB combo-card union — the D1 CardPorts card set).</summary>
  [SerializedLabel("cardsScanned")]
  public int CardsScanned { get; init; }

  /// <summary>Total attribute sites found across those cards' ASTs (the minuend).</summary>
  [SerializedLabel("attributeSitesTotal")]
  public int AttributeSitesTotal { get; init; }

  /// <summary>Attribute sites the projection CONSUMED — ablating them moves the port graph.</summary>
  [SerializedLabel("attributeSitesConsumed")]
  public int AttributeSitesConsumed { get; init; }

  /// <summary>The facet NAMES the projection demonstrably treats as NARROWING — those whose ablation
  /// somewhere shed a label facet, making the port strictly broader. Derived from the same ablation pass.
  /// A name absent here is an axis the projection narrows on nowhere: a coarse projection
  /// (<c>known-coarse-projections.json</c>'s territory), not a widening, and excluded from the rows
  /// below. Filtering on mere readership instead was measured at 58,306 rows of provenance noise.</summary>
  [SerializedLabel("narrowingFacetNames")]
  public required IReadOnlyList<string> NarrowingFacetNames { get; init; }

  /// <summary>Widened attribute instances — relevant-named facets dropped on a port-bearing ability.</summary>
  [SerializedLabel("widenedCount")]
  public int WidenedCount { get; init; }

  [SerializedLabel("cardsWithWidenedAttributes")]
  public int CardsWithWidenedAttributes { get; init; }

  /// <summary>Distinct (card, port label) pairs at D1 tier <c>Green</c> that are broader than their card
  /// — <b>the answer to "which GREENs over-approximate their scope"</b>.</summary>
  [SerializedLabel("greenPortsWidened")]
  public int GreenPortsWidened { get; init; }

  /// <summary>The same count at tier <c>Amber</c> — already floored, so a widening costs less there;
  /// reported so the GREEN figure is readable against its denominator.</summary>
  [SerializedLabel("amberPortsWidened")]
  public int AmberPortsWidened { get; init; }

  /// <summary>Widened facets grouped by (owner node, attribute name), most frequent first — the
  /// burn-down worklist. One projection slice per group closes every instance of it.</summary>
  [SerializedLabel("byFacet")]
  public required IReadOnlyList<WidenedFacetRow> ByFacet { get; init; }

  /// <summary>Every widened attribute instance, one row each, ranked GREEN-bearing first.</summary>
  [SerializedLabel("widened")]
  public required IReadOnlyList<WidenedAttributeRow> Widened { get; init; }
}

/// <summary>One (owner node, attribute name) group's tally — the actionable unit, because a fix is a
/// projection slice for that facet on that node kind.</summary>
[FlowthruSchema]
public partial record WidenedFacetRow
{
  /// <summary>The discriminator of the node the facet hangs off (<c>tokenCreation</c>, <c>mill</c>, …).</summary>
  [SerializedLabel("ownerNode")]
  public required string OwnerNode { get; init; }

  [SerializedLabel("attributeName")]
  public required string AttributeName { get; init; }

  [SerializedLabel("widenedCount")]
  public int WidenedCount { get; init; }

  [SerializedLabel("cardCount")]
  public int CardCount { get; init; }

  /// <summary>Distinct GREEN (card, label) ports broader than their card because of this facet.</summary>
  [SerializedLabel("greenPorts")]
  public int GreenPorts { get; init; }

  [SerializedLabel("exampleCard")]
  public required string ExampleCard { get; init; }

  /// <summary>A sample of the dropped values, so the group is readable without opening rows.</summary>
  [SerializedLabel("exampleValues")]
  public required IReadOnlyList<string> ExampleValues { get; init; }
}

/// <summary>One widened attribute: the facet the card states, and the ports that ignore it.</summary>
[FlowthruSchema]
public partial record WidenedAttributeRow
{
  [SerializedLabel("card")]
  public required string Card { get; init; }

  [SerializedLabel("ownerNode")]
  public required string OwnerNode { get; init; }

  [SerializedLabel("attributeName")]
  public required string AttributeName { get; init; }

  /// <summary>JSON path from the abilities array — e.g. <c>[1].Effects[0].Event.Controller</c>.</summary>
  [SerializedLabel("path")]
  public required string Path { get; init; }

  /// <summary>The facet's own JSON — the value, exactly as the AST states it.</summary>
  [SerializedLabel("valueJson")]
  public required string ValueJson { get; init; }

  /// <summary>The enclosing ability's oracle text, sliced from its <c>SourceSpan</c> — the printed clause
  /// whose scope the ports fail to carry. Empty when the ability carries no span.</summary>
  [SerializedLabel("oracleClause")]
  public required string OracleClause { get; init; }

  /// <summary>The port labels the enclosing ability projects — each certified without this facet.</summary>
  [SerializedLabel("affectedPorts")]
  public required IReadOnlyList<string> AffectedPorts { get; init; }

  /// <summary>Those of <see cref="AffectedPorts"/> the D1 index tiers <c>Green</c>.</summary>
  [SerializedLabel("greenPorts")]
  public required IReadOnlyList<string> GreenPorts { get; init; }
}
