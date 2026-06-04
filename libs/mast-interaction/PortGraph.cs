namespace MagicAST.Interaction;

using System.Text.Json;
using System.Text.Json.Nodes;
using MagicAST.AST.Costs;
using MagicAST.AST.References;

/// <summary>Which side of a resource flow a port sits on (ADR-0002 §4).</summary>
public enum PortSide
{
  /// <summary>A trigger or cost — consumes a resource/event.</summary>
  Consume,

  /// <summary>An effect — emits a resource.</summary>
  Emit,

  /// <summary>A replacement — intercepts another port's emission (ADR-0001 §4 modifier edge).</summary>
  Intercept,
}

/// <summary>
/// A single-role port (ADR-0002 §4) — one ability sub-tree in one role, its canonical
/// <see cref="PortLabel"/> leaf, its side, and its §8 quantity (<c>null</c> = symbolic, e.g. a
/// variable <c>X</c> or a "that many" count). Supersedes the old <c>Port</c>'s
/// emit/consume/intercept resource sets; a multi-role card (Chatterfang) projects several of these.
/// </summary>
public sealed record PortNode
{
  public required string Card { get; init; }
  public required string Label { get; init; }
  public required PortSide Side { get; init; }

  /// <summary>The §8 magnitude. <c>null</c> = symbolic (variable / calculated), floored to Amber-balance.</summary>
  public int? Quantity { get; init; } = 1;

  /// <summary>
  /// The object the port acts on — the operator's input (§7: the label names, the operator decides).
  /// <c>null</c> for scalar resources (mana) and inert ports. The label is the readable projection of
  /// this; the engine matches on labels but tiers on this filter.
  /// </summary>
  public ObjectFilter? Subject { get; init; }

  /// <summary>
  /// Firability gate (ADR-0002 §8): the port's ability carries a rate limit ("only once each turn")
  /// or a boolean condition (an intervening-if / conditional restriction). A cycle through a gated
  /// port cannot be certified infinite — net(R) is blind to these — so its tier floors to Amber.
  /// </summary>
  public bool Gated { get; init; }

  public required string Identity { get; init; }

  public override string ToString() => $"{Card}::{Label}";
}

/// <summary>
/// A card-defined edge (ADR-0002 §5): the ability's own causality, a trigger/cost (consume) driving
/// an effect (emit) <em>within one ability</em>. Certain by construction; distinct from the
/// rules-defined inter-port edges the engine tiers.
/// </summary>
public sealed record CardDefinedEdge
{
  public required PortNode From { get; init; }
  public required PortNode To { get; init; }
}

/// <summary>A card's projected ports plus the card-defined edges among them.</summary>
public sealed record PortGraph
{
  public IReadOnlyList<PortNode> Ports { get; init; } = [];
  public IReadOnlyList<CardDefinedEdge> CardDefinedEdges { get; init; } = [];
}

/// <summary>
/// ADR-0002 §4 — the generic AST walk that projects a card's <c>Oracle.Abilities</c> into
/// single-role ports via <see cref="PortLabel"/>, joined by card-defined edges. Dispatches on node
/// kind (trigger / cost / effect / replacement); totality (§4) means it projects a port for every
/// structural role — costs at quantity 0, inert keywords — never gating on resource flow. The
/// successor to the hand-coded recognizers (the retired <c>PortProjector</c>); built during (S2a)
/// so the existing engine + golds stay green until the migration (S3).
/// </summary>
public sealed class PortWalk
{
  private readonly TypeOntology _ontology;

  public PortWalk(TypeOntology ontology) => _ontology = ontology;

  public PortGraph Project(string card, JsonNode? oracleAbilities)
  {
    if (oracleAbilities is not JsonArray abilities)
      return new PortGraph();

    var ports = new List<PortNode>();
    var edges = new List<CardDefinedEdge>();

    foreach (var ability in abilities)
    {
      if (ability is not JsonObject)
        continue;

      var consumes = new List<PortNode>();
      var emits = new List<PortNode>();

      var keyword = ability["KeywordSource"]?.ToString();
      Trigger(ability["Trigger"], card, consumes);
      foreach (var cost in ability["Costs"] as JsonArray ?? [])
        Costs(cost, card, consumes);
      foreach (var effect in ability["Effects"] as JsonArray ?? [])
        Effects(effect, card, keyword, consumes, emits);

      // Firability (§8): a rate-limited or gated ability marks all its ports — done before the edges
      // are built so they reference the gated port objects.
      if (IsGated(ability))
      {
        for (var i = 0; i < consumes.Count; i++)
          consumes[i] = consumes[i] with { Gated = true };
        for (var i = 0; i < emits.Count; i++)
          emits[i] = emits[i] with { Gated = true };
      }

      ports.AddRange(consumes);
      ports.AddRange(emits);
      // Card-defined causality: every consume/cost in the ability drives every effect (§5).
      foreach (var from in consumes)
        foreach (var to in emits)
          edges.Add(new CardDefinedEdge { From = from, To = to });
    }

    ResolvePredefinedTokens(card, ports, edges);

    // One port per (card, label) — the Identity is card::label. The §9 resolution can re-derive a port
    // the card already declares (Ruthless Knave sacs Treasures in its own draw ability too); collapse
    // the duplicate so the engine sees one node, not parallel edges.
    var distinctPorts = ports.GroupBy(p => p.Identity).Select(g => g.First()).ToList();
    var distinctEdges = edges
      .GroupBy(e => (e.From.Identity, e.To.Identity))
      .Select(g => g.First())
      .ToList();
    return new PortGraph { Ports = distinctPorts, CardDefinedEdges = distinctEdges };
  }

  /// <summary>
  /// ADR-0002 §9 — a created predefined token (CR 111.10) is an object with its OWN ports. For each
  /// distinct created token in the <see cref="PredefinedTokens"/> registry, project its intrinsic
  /// activated ability: the self-sacrifice (plus tap / generic-mana / discard) costs that consume the
  /// token, each driving the resource it emits (a Treasure → <c>emit:mana:any</c>). The card's
  /// <c>emit:token</c> port then flows into that self-sacrifice (the engine's token-flow arm), and the
  /// emitted resource feeds a cost elsewhere — closing e.g. Chatterfang's <c>{B}</c> via Pitiless's
  /// Treasure. Attributed to the creating card (CR 111.2 — a token's creator controls it).
  /// </summary>
  private void ResolvePredefinedTokens(
    string card,
    List<PortNode> ports,
    List<CardDefinedEdge> edges
  )
  {
    var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    foreach (
      var emit in ports.Where(p => p.Side == PortSide.Emit && p.Subject?.IsToken == true).ToList()
    )
    {
      var subtype = emit.Subject!.Subtypes?.FirstOrDefault();
      if (
        subtype is null
        || !resolved.Add(subtype) // resolve each token subtype once per card
        || !PredefinedTokens.Registry.TryGetValue(subtype, out var spec)
      )
        continue;

      var emitPort = Port(card, spec.Emit, PortSide.Emit);
      var costs = new List<PortNode>();
      if (spec.Sacrifices)
      {
        var self = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
          IsToken = true,
        };
        costs.Add(
          Port(card, PortLabel.SacrificeCost(self, _ontology), PortSide.Consume, subject: self)
        );
      }
      if (spec.Taps)
        costs.Add(Port(card, "tap:self", PortSide.Consume));
      if (spec.GenericMana > 0)
        costs.Add(Port(card, "pay:mana", PortSide.Consume, spec.GenericMana));
      if (spec.Discards)
        costs.Add(Port(card, "pay:discard", PortSide.Consume));

      ports.Add(emitPort);
      ports.AddRange(costs);
      foreach (var c in costs)
        edges.Add(new CardDefinedEdge { From = c, To = emitPort });
    }
  }

  private void Trigger(JsonNode? trigger, string card, List<PortNode> consumes)
  {
    if (trigger is not JsonObject t)
      return;
    var eventNode = t["Event"];
    var filter = Filter(t["Filter"]);

    // A structured (phase) event — "at the beginning of [phase]". Never ToString the object (that
    // leaked raw JSON into labels); derive a clean token from its part, else a generic trigger.
    if (eventNode is JsonObject phase)
    {
      var part = (phase["Part"] ?? phase["part"])?.ToString();
      consumes.Add(
        Port(card, part is not null ? $"at:{part.ToLowerInvariant()}" : "trigger", PortSide.Consume)
      );
      return;
    }

    var ev = (eventNode as JsonValue)?.ToString();
    if (ev is null)
      return;
    if (ev == "Dies" && filter is not null)
      consumes.Add(
        Port(card, PortLabel.DeathTrigger(filter, _ontology), PortSide.Consume, subject: filter)
      );
    else if (ev == "Enters" && filter is not null)
      consumes.Add(
        Port(card, PortLabel.EntersTrigger(filter, _ontology), PortSide.Consume, subject: filter)
      );
    else
      // Coarse fallback (totality): the event name as the role, plus the subject if any.
      consumes.Add(Port(card, Coarse(ev, filter), PortSide.Consume));
  }

  private void Costs(JsonNode? cost, string card, List<PortNode> consumes)
  {
    if (cost is not JsonObject c)
      return;
    switch (c["CostType"]?.ToString())
    {
      case "sacrifice" when Filter(c["Filter"]) is { } fodder:
      {
        // CR 701.21a: you sacrifice only what you control — the operator sees a controlled fodder.
        var controlled = fodder with { Controller = fodder.Controller ?? ControllerFilter.You };
        consumes.Add(
          Port(
            card,
            PortLabel.SacrificeCost(fodder, _ontology),
            PortSide.Consume,
            Qty(c["Quantity"]),
            controlled
          )
        );
        break;
      }
      case "mana":
        var symbols =
          c["Symbols"].Deserialize<List<ManaSymbol>>(MagicAST.MagicASTJsonOptions.Strict) ?? [];
        foreach (var (label, quantity) in PortLabel.PayMana(symbols))
          consumes.Add(Port(card, label, PortSide.Consume, quantity));
        break;
      case "tap":
        consumes.Add(Port(card, "tap:self", PortSide.Consume));
        break;
      case { } other:
        consumes.Add(Port(card, $"pay:{other.ToLowerInvariant()}", PortSide.Consume, Qty(c["Quantity"])));
        break;
    }
  }

  private void Effects(
    JsonNode? effect,
    string card,
    string? keyword,
    List<PortNode> consumes,
    List<PortNode> emits
  )
  {
    if (effect is not JsonObject e)
      return;
    if (e["EffectType"]?.ToString() == "replacement")
    {
      // Intercept side: the replaced event (ADR-0002 §3, CR 614). Scope rides only if the event carries it.
      var eventType = e["Event"]?["EventType"]?.ToString() ?? "event";
      consumes.Add(
        Port(
          card,
          PortLabel.Replacement(eventType),
          PortSide.Intercept,
          subject: new ObjectFilter { IsToken = true }
        )
      );
      // Emit side: the replacement's own effect (Chatterfang's added Squirrels).
      if (EmitPort(e["Replacement"], card, keyword) is { } inner)
        emits.Add(inner);
      return;
    }
    if (EmitPort(effect, card, keyword) is { } emit)
      emits.Add(emit);
  }

  private PortNode? EmitPort(JsonNode? effect, string card, string? keyword)
  {
    if (effect is not JsonObject e)
      return null;
    var effectType = e["EffectType"]?.ToString();
    if (effectType == "createToken")
    {
      var token = TokenFilter(e["Token"], e["Player"]);
      return Port(
        card,
        PortLabel.CreateTokenEmit(token, _ontology),
        PortSide.Emit,
        Qty(e["Count"]),
        token
      );
    }
    return effectType switch
    {
      // Inert effects (no flow) are still ports, by totality (§4) — edge-sparse, never dropped.
      "modifyPT" => Port(card, "modify:pt", PortSide.Emit),
      "evasion" => Port(card, $"evasion:{keyword?.ToLowerInvariant() ?? "evasion"}", PortSide.Emit),
      { } other => Port(card, $"emit:{other.ToLowerInvariant()}", PortSide.Emit),
      _ => null,
    };
  }

  // --- helpers ---

  private static PortNode Port(
    string card,
    string label,
    PortSide side,
    int? quantity = 1,
    ObjectFilter? subject = null
  ) =>
    new()
    {
      Card = card,
      Label = label,
      Side = side,
      Quantity = quantity,
      Subject = subject,
      Identity = $"{card}::{label}",
    };

  // Restrictions that gate firability (ADR-0002 §8): a rate limit or a board-state condition. Timing
  // restrictions (OnlyAsSorcery, OnlyDuringYourTurn) don't block a loop within a turn, so they don't gate.
  private static readonly HashSet<string> GatingRestrictions = new(StringComparer.Ordinal)
  {
    "OnlyOnceEachTurn",
    "Conditional",
    "OnlyIfNoUntappedLands",
  };

  /// <summary>Firability gate (ADR-0002 §8): an intervening-if or a rate-limit/conditional restriction.</summary>
  private static bool IsGated(JsonNode ability)
  {
    if (ability["InterveningIf"] is not null)
      return true;
    if (ability["Restrictions"] is JsonArray restrictions)
      foreach (var r in restrictions)
        if (r is not null && GatingRestrictions.Contains(r.ToString()))
          return true;
    return false;
  }

  private static string Coarse(string eventName, ObjectFilter? filter)
  {
    var role = eventName.ToLowerInvariant();
    return filter is null ? role : $"{role}:{PortLabel.Subject(filter, EmptyOntology) ?? "object"}";
  }

  // Subject for the coarse trigger fallback uses no lift (the precise roles thread the real ontology).
  private static readonly TypeOntology EmptyOntology = new();

  private static ObjectFilter? Filter(JsonNode? node) =>
    node?.Deserialize<ObjectFilter>(MagicAST.MagicASTJsonOptions.Strict);

  /// <summary>Translate a created token's spec (<c>Types</c>/<c>Subtypes</c> + creator) into the filter the subject projection reads.</summary>
  private static ObjectFilter TokenFilter(JsonNode? token, JsonNode? player) =>
    new()
    {
      CardTypes = StrList(token?["Types"]),
      Subtypes = StrList(token?["Subtypes"]),
      IsToken = true,
      Controller = player?["Kind"]?.ToString() == "You" ? ControllerFilter.You : null,
    };

  /// <summary>The §8 quantity: literal/fixed → its value; variable/calculated → <c>null</c> (symbolic); absent → 1.</summary>
  private static int? Qty(JsonNode? quantity) =>
    quantity is null ? 1
    : quantity["QuantityType"]?.ToString() switch
    {
      "literal" or "fixed" => quantity["Value"]?.GetValue<int>(),
      _ => null,
    };

  private static IReadOnlyList<string>? StrList(JsonNode? node) =>
    node is JsonArray arr ? arr.Where(x => x is not null).Select(x => x!.ToString()).ToList() : null;
}
