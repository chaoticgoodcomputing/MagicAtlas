namespace MagicAST.Interaction;

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
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
  /// Firability gate (ADR-0002 §8): the port's ability carries a <b>hard</b> rate limit ("only once each
  /// turn") or a boolean condition (an intervening-if / conditional restriction). A cycle through a gated
  /// port cannot be certified infinite — net(R) is blind to these — so its tier floors to Amber. This
  /// gate is never discharged (distinct from <see cref="TapGated"/>).
  /// </summary>
  public bool Gated { get; init; }

  /// <summary>
  /// A <b>tap</b> rate limit (ADR-0002 §8): the port's ability has a <c>{T}</c> cost, so it fires only
  /// once per untap (CR 107.5). Distinct from <see cref="Gated"/> because it is <em>dischargeable</em>:
  /// a loop that untaps the permanent each iteration (Blasting Station — "untap this whenever a creature
  /// enters" — fed by the loop's own creature tokens) renews the tap, so the cycle stays firable
  /// (<see cref="PortCycle.TapRenewed"/>). Absent such renewal it floors to Amber like any rate limit.
  /// </summary>
  public bool TapGated { get; init; }

  /// <summary>
  /// A dies-trigger whose intervening-if requires the dying object to have had a counter of this kind
  /// (Basri's Lieutenant — "if it had a +1/+1 counter on it"; CR 603.10 look-back). <c>null</c> when the
  /// ability has no such gate. The §8 counter-gate prune uses this: a loop whose own tokens enter without
  /// the counter, and which has no in-loop counter source, can never re-satisfy the gate.
  /// </summary>
  public string? RequiresCounter { get; init; }

  public required string Identity { get; init; }

  /// <summary>
  /// Copy-token inheritance (copy-inheritance-scope.md, Decision 1/2): the parsed
  /// <see cref="MagicAST.AST.Effects.TokenCopy.CopyModification"/>s an <c>emit:copy</c> applies to the
  /// copy it creates — Kiki's <c>abilityAdder:haste</c>, Helm's <c>supertypeRemover:[Legendary]</c>,
  /// a <c>typeAdder</c>/<c>powerToughnessOverride</c>. Read by <see cref="PortGraphEngine"/>'s graft
  /// pass to adjust the cloned ports' type facets. Modifications never <em>add</em> an ability the
  /// partner lacks (they remove/override/grant inert keywords), so they cannot widen the graft — the
  /// soundness note in Decision 1. <c>null</c> for every non-copy port.
  /// </summary>
  public IReadOnlyList<MagicAST.AST.Effects.TokenCopy.CopyModification>? CopyMods { get; init; }

  /// <summary>
  /// Copy-token inheritance: a port that was <b>grafted</b> onto a synthesized copy identity by
  /// <see cref="PortGraphEngine"/> (CR 707.2 — the copy carries the copied card's abilities). The
  /// copier that created the copy; <c>null</c> for an ordinary projected port. Lets the generalized
  /// <see cref="PortGraphEngine"/> tap-renewal recognise "this untap lives on a copy <c>Grafter</c>
  /// made," so an inherited untap renews the copier's tap (Decision 4a).
  /// </summary>
  public string? Grafter { get; init; }

  /// <summary>
  /// Copy-token inheritance: the original card this grafted port was <b>copied from</b> (CR 707.2 — the
  /// copy takes the copiable values of that card). <c>null</c> for an ordinary projected port. A grafted
  /// copy belongs to BOTH its <see cref="Grafter"/> (the copier that created it) and this copied card, so
  /// a reconstruction spanning the copy genuinely spans both combo cards.
  /// </summary>
  public string? CopiedFrom { get; init; }

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

  public PortGraph Project(string card, JsonNode? oracleAbilities, JsonNode? manaCostSymbols = null)
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

      // A SPELL ability (an instant/sorcery's one-shot effect, CR 601) is cast by paying its mana cost,
      // and the spell then produces its effects (CR 601.2f→608) — so the card's own mana cost is a
      // pay:mana CO-COST that DRIVES the spell's emits, exactly like the aristocrat-recursion recast
      // co-cost (PortGraph.cs:432). The faithful parse-layer truth: "this spell costs {N} to cast" is
      // what the card says, and casting is the cause of the effect. Threading it as a consume is what
      // lets a self-refueling spell loop close — a flicker spell (Ghostly Flicker) blinks a permanent
      // whose ETB refunds the mana to recast it (the mana-untap blink family) — and feeds the §8 mana
      // balance the same machinery already runs for activated/recast abilities. A spell with no mana cost
      // (an alternative-only card) simply adds no consume.
      if (string.Equals(ability["Kind"]?.ToString(), "spell", StringComparison.Ordinal))
        foreach (var (label, quantity) in SpellCastCost(manaCostSymbols))
          consumes.Add(Port(card, label, PortSide.Consume, quantity));

      foreach (var effect in ability["Effects"] as JsonArray ?? [])
        Effects(effect, card, keyword, manaCostSymbols, consumes, emits);

      // Spell-recast (CR 601.2): a `Kind:spell` ability is the on-cast effect of an instant/sorcery — its
      // effects fire because the spell was CAST. Project a cast:spell:self consume (NON-NULL spell-type
      // Subject) so a spell-recursion emit (Archaeomancer returning Ghostly Flicker to hand) can refuel
      // the recast, re-firing the spell's effects (the spell-recast flow arm). Only when the ability has a
      // flow emit (an inert spell has no loop to enable); the recast carries the card's OWN mana cost as a
      // pay:mana co-cost (threaded via manaCostSymbols, the same source the aristocrat recast reads) so the
      // §8 mana-balance floors the loop honestly when the recast mana isn't refunded.
      if (string.Equals(ability["Kind"]?.ToString(), "spell", StringComparison.Ordinal) && emits.Count > 0)
      {
        consumes.Add(Port(card, PortLabel.CastConsume(), PortSide.Consume, subject: SpellSelf));
        foreach (var (label, quantity) in RecastManaCost(null, manaCostSymbols))
          consumes.Add(Port(card, label, PortSide.Consume, quantity));
      }

      // Firability (§8): a hard-gated ability marks all its ports Gated; a tap ability marks them
      // TapGated (the dischargeable rate limit). Done before the edges are built so they reference the
      // marked port objects.
      var hardGated = IsGated(ability);
      var tapGated = HasTapCost(ability);
      if (hardGated || tapGated)
      {
        for (var i = 0; i < consumes.Count; i++)
          consumes[i] = consumes[i] with { Gated = hardGated, TapGated = tapGated };
        for (var i = 0; i < emits.Count; i++)
          emits[i] = emits[i] with { Gated = hardGated, TapGated = tapGated };
      }

      // §8 counter-gate: a dies-trigger whose intervening-if requires the dying object to have had a
      // counter ("if it had a +1/+1 counter on it") marks its consume ports so the engine can prune a
      // loop whose own tokens can never carry it (Basri's Lieutenant).
      var requiresCounter = CounterRequirement(ability);
      if (requiresCounter is not null)
        for (var i = 0; i < consumes.Count; i++)
          consumes[i] = consumes[i] with { RequiresCounter = requiresCounter };

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

      // The token's intrinsic emit inherits the creation count (2 Treasures → 2 mana) so the §8
      // balance sums the real per-iteration yield, not 1.
      var emitPort = Port(card, spec.Emit, PortSide.Emit, emit.Quantity);
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

      // A token that TAPS but is not consumed (no self-sac) is a persistent, reused producer — its tap
      // is a rate limit (CR 107.5), so tap-gate it (ADR-0002 §8). A token that SACRIFICES itself for the
      // mana (a Treasure) is re-created fresh each iteration, so its tap is not a cross-iteration limit
      // (gating it would wrongly floor the Chatterfang × Pitiless Treasure-fed loop).
      if (spec.Taps && !spec.Sacrifices)
      {
        emitPort = emitPort with { TapGated = true };
        for (var i = 0; i < costs.Count; i++)
          costs[i] = costs[i] with { TapGated = true };
      }

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
    else if (ev == "GainsLife")
      // "Whenever [a player] gains life" — consumes a life-gain event (CR 119). The watched player
      // (filter Controller) rides as the subject so the operator tiers the flow (ADR-0002 §7). Non-null
      // (broadest player if unqualified) — life is player-scoped, never a null-default-GREEN scalar.
      consumes.Add(Port(card, PortLabel.LifeGainTrigger(filter), PortSide.Consume, subject: filter ?? AnyPlayer));
    else if (ev == "LosesLife")
      consumes.Add(Port(card, PortLabel.LifeLossTrigger(filter), PortSide.Consume, subject: filter ?? AnyPlayer));
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
    JsonNode? manaCostSymbols,
    List<PortNode> consumes,
    List<PortNode> emits
  )
  {
    if (effect is not JsonObject e)
      return;
    var effectType = e["EffectType"]?.ToString();

    // A blink (flicker) is an exile-then-return-the-just-exiled action (CR 603.6e/400.7), stated as a
    // `composite` of [exile, returnToBattlefield(ExiledWith:Self)], optionally wrapped in `optional`
    // ("you may", CR 117.7). Recognise the WHOLE composite as one emit:blink (the re-entered permanent
    // refuels its own ETB and re-enters untapped) BEFORE the generic optional/composite recursion, so the
    // exile and return don't project as two opaque inert ports the blink arm can't read.
    if (effectType is "optional" or "composite")
    {
      var inner = effectType == "optional" ? e["Inner"] : e;
      if (BlinkPort(inner, card) is { } blink)
      {
        // The "you may" floors firability to AMBER (the controller may decline — a loop through it can't
        // be certified infinite, ADR-0002 §8). Marked Gated so PortCycle.Firable floors it.
        emits.Add(effectType == "optional" ? blink with { Gated = true } : blink);
        return;
      }
      // Not a blink — recurse into the inner effect(s) so nested flow ports (a composite of token+mana)
      // still project by totality (§4), carrying the optional's gate to each.
      var gated = effectType == "optional";
      foreach (var sub in InnerEffects(inner))
      {
        var before = emits.Count;
        Effects(sub, card, keyword, manaCostSymbols, consumes, emits);
        if (gated)
          for (var i = before; i < emits.Count; i++)
            emits[i] = emits[i] with { Gated = true };
      }
      return;
    }

    if (effectType == "replacement")
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
      if (EmitPort(e["Replacement"], card, keyword, manaCostSymbols, consumes) is { } inner)
        emits.Add(inner);
      return;
    }
    if (EmitPort(effect, card, keyword, manaCostSymbols, consumes) is { } emit)
      emits.Add(emit);
  }

  private PortNode? EmitPort(
    JsonNode? effect,
    string card,
    string? keyword,
    JsonNode? manaCostSymbols,
    List<PortNode> consumes
  )
  {
    if (effect is not JsonObject e)
      return null;
    var effectType = e["EffectType"]?.ToString();
    if (
      effectType == "alternativeCast"
      && string.Equals(e["FromZone"]?.ToString(), "Graveyard", StringComparison.OrdinalIgnoreCase)
    )
    {
      // Aristocrat recursion (aristocrat-recursion-scope.md, Decision 1/2a). A cast-from-graveyard
      // permission (CR 601.3e) projects as the EXISTING emit:returntobattlefield:self label — the card
      // re-enters the battlefield (refueling a sac), and the §8-B carve-out (which keys on this label for
      // Persist/Undying) retains the self-death recursion cycle. The Subject is NON-NULL (the card's own
      // self-filter), never a null-default GREEN (anti-pattern 3). The recast carries a pay:mana CO-COST
      // = the card's OWN mana cost (AlternativeCastEffect.Cost is null for Gravecrawler ⇒ "cast for its
      // own mana cost"), pushed into this ability's consumes so the §8 mana-balance machinery tiers it.
      var self = new ObjectFilter { IsSelf = true, CardTypes = ["creature"], Controller = ControllerFilter.You };
      foreach (var (label, quantity) in RecastManaCost(e["Cost"], manaCostSymbols))
        consumes.Add(Port(card, label, PortSide.Consume, quantity));
      return Port(card, PortLabel.ReturnToBattlefieldEmit(), PortSide.Emit, subject: self);
    }
    if (effectType == "returnToHand")
    {
      // "Return target [card] to [its owner's] hand." Two distinct shapes:
      //  - SPELL-RECURSION (Archaeomancer/Izzet Chronarch): an instant/sorcery card returned FROM A
      //    GRAVEYARD to hand → it can be recast (CR 601.2), re-firing its effects. Project the recursion
      //    emit:returntohand:spell the spell-recast arm reads, Subject = the returned-card filter.
      //  - BOUNCE (Boomerang): a battlefield permanent to its owner's hand → NOT a spell-recast (a creature
      //    recast is a re-entry, not a spell-effect re-fire). Coarse emit:returntohand no arm reads.
      var returned = Filter(e["Target"]?["Filter"]);
      if (returned is not null && IsSpellRecursion(returned))
        return Port(card, PortLabel.SpellRecursionEmit(returned, _ontology), PortSide.Emit, subject: returned);
      return Port(card, PortLabel.ReturnToHandEmit(returned, _ontology), PortSide.Emit, subject: returned);
    }
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
    if (effectType == "addMana")
    {
      // A mana-producing effect (ADR-0002 §3b): project the color-axis emit:mana:<color> with its
      // count (so the colour-aware mana-flow arm feeds a cost AND the §8 balance can sum it), instead
      // of the opaque emit:addmana fallback that no flow edge reads.
      var (color, count) = ParseAddedMana(e);
      return Port(card, PortLabel.ManaEmit(color), PortSide.Emit, count);
    }
    if (effectType == "putCounters")
    {
      // A counter-adder (ADR-0002 §8): "put a +1/+1 counter on [target]". The §8 counter-gate prune
      // reads this as a potential in-loop counter SOURCE — a loop with one can re-satisfy a "had a
      // counter" dies-gate, so it is NOT pruned. Scope: self vs a target/other creature.
      var counterType = e["CounterType"]?.ToString() ?? "counter";
      var self = e["Target"]?["Kind"]?.ToString() == "Self";
      return Port(
        card,
        $"emit:counter:{counterType.ToLowerInvariant()}:{(self ? "self" : "target")}",
        PortSide.Emit,
        Qty(e["Count"])
      );
    }
    if (effectType == "copy")
    {
      var copyTarget = Filter(e["Target"]?["Filter"]) ?? new ObjectFilter { CardTypes = ["creature"] };

      // A SPELL-copy ("copy target instant or sorcery spell", Target.Zone:Stack — Dualcaster, Reiterate,
      // Narset's Reversal) is a DIFFERENT resource from a permanent token-copy (CR 707.10 — a copy of a
      // spell is put on the STACK, isn't cast, and carries no ETB/untap to graft onto a permanent loop).
      // Project it FAITHFULLY but DISTINCTLY (emit:copy:spell, NON-NULL Subject = the copied-spell filter)
      // so the copy-inheritance permanent graft (which keys on the bare emit:copy) never grafts a spell as a
      // permanent. No flow arm consumes emit:copy:spell yet — the sound spell-copy refuel needs a
      // copy-of-spell graft shape (sibling of copy-inheritance), STOP-and-reported for human review; this is
      // the parse-layer prerequisite (PortLabel.SpellCopyEmit docstring; adding-a-flow-arm.md projection↔connection split).
      if (copyTarget.Zone == MagicAST.AST.References.Zone.Stack)
        return Port(card, PortLabel.SpellCopyEmit(copyTarget, _ontology), PortSide.Emit, subject: copyTarget);

      // Copy-token inheritance (copy-inheritance-scope.md, Decision 1/2 + §6 Track A): "create a token
      // that's a copy of target nonlegendary creature you control" (Kiki). The copy emit is projected
      // FAITHFULLY — a real label plus a NON-NULL Subject = the copy TARGET filter, the discriminator the
      // graft operator tiers on (Decision 3). The Subject is NEVER null: a null subject would hit the
      // scalar null-default GREEN in AddRulesEdge (adding-a-flow-arm anti-pattern 3) and graft
      // unconditionally. The floor is {CardTypes:[creature]} — the broadest a copy can ever be — never
      // narrower-by-omission. The parsed modifications (abilityAdder:haste, supertypeRemover, …) ride as
      // CopyMods so the engine's graft pass can apply them to the cloned ports.
      var mods = e["Modifications"]?.Deserialize<List<MagicAST.AST.Effects.TokenCopy.CopyModification>>(
        MagicAST.MagicASTJsonOptions.Strict
      );
      return Port(card, "emit:copy", PortSide.Emit, subject: copyTarget) with { CopyMods = mods };
    }
    if (effectType == "untap")
    {
      // Carry the untap's SCOPE: "untap this" (ObjectReference.Self) → emit:untap:self; "untap target X"
      // → emit:untap (a different permanent). The §8 tap-renewal carve-out discharges a tap gate for a
      // SELF-untap on the same card, OR — copy-inheritance Decision 4b — for a TARGET-untap whose target
      // filter subsumes the tap-gated source (Corridor Monitor's "untap target artifact or creature"
      // renews Kiki). So a non-self untap carries its TARGET FILTER as the port Subject, the discriminator
      // the renewal operator tiers on (the projection sharpen in §6 Track A).
      var self = e["Target"]?["Kind"]?.ToString() == "Self";
      if (self)
        return Port(card, "emit:untap:self", PortSide.Emit);
      var target = Filter(e["Target"]?["Filter"]);
      return Port(card, "emit:untap", PortSide.Emit, subject: target);
    }
    if (effectType == "gainLife")
    {
      // A life-gain event (CR 119). The gaining player rides as the subject so a "whenever you gain
      // life" trigger tiers by the player axis (ADR-0002 §3/§7). Feeds the life flow arm.
      var who = PlayerFilter(e["Player"]);
      return Port(card, PortLabel.LifeGainEmit(who), PortSide.Emit, Qty(e["Amount"]), who);
    }
    if (effectType == "loseLife")
    {
      var who = PlayerFilter(e["Player"]);
      return Port(card, PortLabel.LifeLossEmit(who), PortSide.Emit, Qty(e["Amount"]), who);
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

  /// <summary>
  /// Recognise a <b>blink</b> (flicker): a <c>composite</c> whose effects are an <c>exile</c> of a target
  /// permanent followed by a <c>returnToBattlefield</c> of the JUST-EXILED card (the linked
  /// <c>ExiledWith:Self</c> reference, CR 406.6 / ADR 0004). The two ordered effects ARE one game action
  /// — the permanent leaves and re-enters as a new object (CR 603.6e/400.7). Project a single
  /// <c>emit:blink</c> carrying the EXILE TARGET filter as its NON-NULL Subject (the blinked permanent —
  /// what re-enters; what the operator tiers the re-entry/renewal on). <c>null</c> when the effects are
  /// not this exile-then-return-self pair (a plain exile, a reanimate-from-graveyard, an unrelated
  /// composite) — those keep their own (coarse) projection, never a false blink.
  /// </summary>
  private PortNode? BlinkPort(JsonNode? inner, string card)
  {
    if (inner is not JsonObject c || c["EffectType"]?.ToString() != "composite")
      return null;
    if (c["Effects"] is not JsonArray effects)
      return null;

    JsonObject? exile = null;
    JsonObject? ret = null;
    foreach (var sub in effects)
      switch ((sub as JsonObject)?["EffectType"]?.ToString())
      {
        case "exile":
          exile ??= sub as JsonObject;
          break;
        case "returnToBattlefield":
          ret ??= sub as JsonObject;
          break;
      }
    if (exile is null || ret is null)
      return null;

    // The return must be of the just-exiled card (Zone:Exile + ExiledWith:Self) — this is what makes it a
    // blink rather than a generic reanimation. Without this link the composite is some other exile+return.
    var retFilter = ret["Target"]?["Filter"];
    if (
      retFilter?["Zone"]?.ToString() != "Exile"
      || retFilter["ExiledWith"]?["Kind"]?.ToString() != "Self"
    )
      return null;

    var blinked = Filter(exile["Target"]?["Filter"]);
    if (blinked is null)
      return null; // an under-specified blink target — no Subject to tier; don't manufacture a blink port

    return Port(card, PortLabel.BlinkEmit(blinked, _ontology), PortSide.Emit, Qty(exile["Target"]?["Quantity"]), blinked);
  }

  /// <summary>
  /// A <b>spell-recursion</b> return-to-hand (vs a battlefield bounce): the returned card is an
  /// <b>instant or sorcery</b> (a spell card) drawn from a NON-battlefield zone (a graveyard — the
  /// canonical Archaeomancer case — or exile/library; never the battlefield, where instants/sorceries
  /// can't exist). Returning it to hand makes it recastable (CR 601.2), re-firing its effects. A bounce
  /// (Boomerang's "return target permanent to hand", Zone:Battlefield or no instant/sorcery type) is NOT
  /// this — it re-casts a creature/permanent, not a spell effect — so it keeps the coarse projection.
  /// </summary>
  private static bool IsSpellRecursion(ObjectFilter returned) =>
    returned.CardTypes is { } types
    && types.Any(t =>
      string.Equals(t, "instant", StringComparison.OrdinalIgnoreCase)
      || string.Equals(t, "sorcery", StringComparison.OrdinalIgnoreCase)
    )
    && returned.Zone != Zone.Battlefield;

  /// <summary>The child effects of an <c>optional.Inner</c> or a <c>composite</c> (its <c>Effects</c>
  /// array), for the generic non-blink recursion. A single non-composite inner is yielded as itself.</summary>
  private static IEnumerable<JsonNode?> InnerEffects(JsonNode? inner)
  {
    if (inner is JsonObject o && o["EffectType"]?.ToString() == "composite" && o["Effects"] is JsonArray arr)
      return arr;
    return inner is null ? [] : [inner];
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
  // Declared in PortWalkProjection (single source of truth) so the exhaustiveness ratchet can see it.
  private static readonly IReadOnlySet<string> GatingRestrictions = PortWalkProjection.GatingRestrictions;

  /// <summary>A <b>hard</b> firability gate (ADR-0002 §8): an intervening-if, a rate-limit/conditional
  /// restriction, or a <b>self-bounce</b> cost ("Return this … to its owner's hand" — the source leaves
  /// the battlefield and must be recast, a mana cost the loop doesn't model, so a loop through it can't
  /// be certified infinite — Grinning Ignus). Never discharged. (A <c>{T}</c> tap cost is the separate,
  /// dischargeable <see cref="HasTapCost"/> gate.)</summary>
  private static bool IsGated(JsonNode ability)
  {
    if (ability["InterveningIf"] is not null)
      return true;
    if (ability["Restrictions"] is JsonArray restrictions)
      foreach (var r in restrictions)
        if (r is not null && GatingRestrictions.Contains(r.ToString()))
          return true;
    if (ability["Costs"] is JsonArray costs)
      foreach (var c in costs)
        if (c?["CostType"]?.ToString() == "returnToHand")
          return true;
    return false;
  }

  /// <summary>The counter kind a dies-trigger's intervening-if requires PRESENT on the dying object
  /// ("if it had a +1/+1 counter on it" → <c>"+1/+1"</c>), or <c>null</c>. The absent form ("had no
  /// +1/+1 counters", Persist/Undying) is about returning the source, not a loop-repeat gate, so it
  /// doesn't count here.</summary>
  private static string? CounterRequirement(JsonNode ability)
  {
    if (
      ability["InterveningIf"] is JsonObject iv
      && iv["ConditionType"]?.ToString() == "triggeringObjectCounter"
      && iv["Present"]?.GetValue<bool>() == true
    )
      return iv["CounterType"]?.ToString();
    return null;
  }

  /// <summary>A <c>{T}</c> tap cost — a once-per-untap rate limit (CR 107.5), dischargeable by an
  /// untapper in the loop (ADR-0002 §8, <see cref="PortNode.TapGated"/>).</summary>
  private static bool HasTapCost(JsonNode ability)
  {
    if (ability["Costs"] is JsonArray costs)
      foreach (var c in costs)
        if (c?["CostType"]?.ToString() == "tap")
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

  /// <summary>The affected player of a life effect → an <see cref="ObjectFilter"/> subject the operator
  /// can tier. <c>You</c> → controller You; <c>Opponent</c>/<c>EachOpponent</c> → controller Opponent
  /// (so "each opponent loses life" REASONS to GREEN against a "whenever an opponent loses life" trigger,
  /// not GREEN-by-accident); a <c>Target</c>/other player carries its embedded filter. Crucially this is
  /// NEVER null: life is player-scoped, not a fungible scalar, so a null subject would wrongly hit the
  /// mana-style scalar null-default (GREEN) in <c>AddRulesEdge</c> — a false-positive vector. An
  /// unqualified player (e.g. <c>target player</c>) therefore floors to the broadest player filter, which
  /// the operator tiers as a sound AMBER against an opponent-scoped trigger until the PARSE layer sharpens
  /// the target to opponent-scoped (the GREEN ceiling lives in parse, never papered over here).</summary>
  /// <summary>The broadest player subject (CR 109) — a non-null floor so life ports never hit the
  /// scalar null-default GREEN in <see cref="PortGraphEngine"/>; tiers as a sound AMBER against a
  /// controller-scoped trigger.</summary>
  private static readonly ObjectFilter AnyPlayer = new() { CardTypes = ["player"] };

  /// <summary>The cast consume's Subject — "this instant or sorcery spell" (CR 601.2). The card type is
  /// not threaded into the walk, so this is the broadest faithful spell type (a Kind:spell ability is an
  /// instant/sorcery on-cast effect); NON-NULL so the spell-recast arm never hits the scalar null-default
  /// GREEN (adding-a-flow-arm anti-pattern 3). IsSelf marks it the spell's own identity.</summary>
  private static readonly ObjectFilter SpellSelf = new()
  {
    CardTypes = ["instant", "sorcery"],
    IsSelf = true,
  };

  private static ObjectFilter PlayerFilter(JsonNode? player) =>
    player?["Kind"]?.ToString() switch
    {
      "You" => new ObjectFilter { Controller = ControllerFilter.You },
      "Opponent" or "EachOpponent" => new ObjectFilter { Controller = ControllerFilter.Opponent },
      _ => Filter(player?["Filter"]) ?? AnyPlayer,
    };

  /// <summary>Translate a created token's spec (<c>Types</c>/<c>Subtypes</c> + creator) into the filter the subject projection reads.</summary>
  private static ObjectFilter TokenFilter(JsonNode? token, JsonNode? player) =>
    new()
    {
      CardTypes = StrList(token?["Types"]),
      Subtypes = StrList(token?["Subtypes"]),
      IsToken = true,
      Controller = player?["Kind"]?.ToString() == "You" ? ControllerFilter.You : null,
    };

  /// <summary>
  /// The recast's <c>pay:mana</c> co-cost (aristocrat-recursion-scope §2a / Decision 1). When the
  /// <c>alternativeCast</c> carries an explicit alternative <see cref="MagicAST.AST.Effects.CardFlow.AlternativeCastEffect.Cost"/>
  /// that is a mana cost (Escape/Flashback "by paying {…}"), read its symbols; otherwise the card is cast
  /// for its OWN mana cost (Gravecrawler — <c>Cost</c> null, CR 601.3e), so read the card's mana-cost
  /// attribute symbols threaded in via <paramref name="manaCostSymbols"/>. Both flow through
  /// <see cref="PortLabel.PayMana"/> so each coloured pip becomes its own per-colour <c>pay:mana</c>
  /// requirement the §8 per-colour balance can floor. Empty when no mana cost is available (the recast
  /// then carries no mana co-cost — the §8 balance is conservative and won't floor it; never invents a cost).
  /// </summary>
  private static IReadOnlyList<(string Label, int Quantity)> RecastManaCost(
    JsonNode? alternativeCost,
    JsonNode? manaCostSymbols
  )
  {
    // An explicit alternative mana cost on the permission (e.g. Escape's "{2}{B}{B}") takes precedence.
    var altSymbols = alternativeCost?["Symbols"] ?? alternativeCost?["ManaCost"]?["Symbols"];
    var node = altSymbols ?? manaCostSymbols;
    if (node is not JsonArray)
      return [];
    var symbols = node.Deserialize<List<ManaSymbol>>(MagicAST.MagicASTJsonOptions.Strict) ?? [];
    return PortLabel.PayMana(symbols);
  }

  /// <summary>
  /// A SPELL ability's cast cost (CR 601.2f) — the card's own mana cost, threaded in via
  /// <paramref name="manaCostSymbols"/>, projected as per-colour <c>pay:mana[:colour]</c> consumes via
  /// <see cref="PortLabel.PayMana"/> (the same per-pip shape as an activated mana cost). These DRIVE the
  /// spell's emits (every consume drives every emit, §5), so a self-refueling spell loop (a flicker spell
  /// re-cast by the mana its blink target's ETB refunds) closes, and the §8 per-colour balance can floor
  /// it. Empty when the card carries no mana-cost attribute (an alternative-only / X-only card) — never
  /// invents a cost.
  /// </summary>
  private static IReadOnlyList<(string Label, int Quantity)> SpellCastCost(JsonNode? manaCostSymbols)
  {
    if (manaCostSymbols is not JsonArray)
      return [];
    var symbols = manaCostSymbols.Deserialize<List<ManaSymbol>>(MagicAST.MagicASTJsonOptions.Strict) ?? [];
    return PortLabel.PayMana(symbols);
  }

  /// <summary>The §8 quantity: literal/fixed → its value; variable/calculated → <c>null</c> (symbolic); absent → 1.</summary>
  private static int? Qty(JsonNode? quantity) =>
    quantity is null ? 1
    : quantity["QuantityType"]?.ToString() switch
    {
      "literal" or "fixed" => quantity["Value"]?.GetValue<int>(),
      _ => null,
    };

  /// <summary>
  /// Parse an <c>addMana</c> effect's produced mana into (colour, count): <c>{C}{C}</c> →
  /// (colorless, 2), <c>{G}</c> → (green, 1), "any color" → (any, n). A mixed-colour add (rare) and a
  /// generic <c>{N}</c> fall back to <c>any</c>. The count is the §8 quantity the balance sums.
  /// </summary>
  private static (string Color, int Count) ParseAddedMana(JsonObject e)
  {
    var mana = e["Mana"]?.ToString() ?? "";
    var symbols = Regex.Matches(mana, @"\{([^}]+)\}").Select(m => m.Groups[1].Value).ToList();
    if (e["AnyColor"]?.GetValue<bool>() == true)
      return ("any", Math.Max(1, symbols.Count));
    if (symbols.Count == 0)
      return ("any", 1);
    var count = 0;
    var colors = new HashSet<string>(StringComparer.Ordinal);
    foreach (var s in symbols)
      if (int.TryParse(s, out var n))
      {
        count += n;
        colors.Add("any");
      }
      else
      {
        count += 1;
        colors.Add(ManaColorName(s));
      }
    return (colors.Count == 1 ? colors.First() : "any", count);
  }

  private static string ManaColorName(string symbol) =>
    symbol.ToUpperInvariant() switch
    {
      "W" => "white",
      "U" => "blue",
      "B" => "black",
      "R" => "red",
      "G" => "green",
      "C" => "colorless",
      _ => "any",
    };

  private static IReadOnlyList<string>? StrList(JsonNode? node) =>
    node is JsonArray arr ? arr.Where(x => x is not null).Select(x => x!.ToString()).ToList() : null;
}
