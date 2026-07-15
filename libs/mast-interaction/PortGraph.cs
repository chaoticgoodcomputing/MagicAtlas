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
  /// The oracle-text span of the ability this port was projected from (MAST
  /// provenance — upstream-atlas-data-plan §4), when the projected ability JSON
  /// carries a <c>SourceSpan</c>. <c>null</c> when absent (combat-presence and
  /// predefined-token ports have no ability span; and until MAST span serialization
  /// is enabled the projected JSON omits it). Additive and null-safe; not part of
  /// <see cref="Identity"/>, so it never affects port de-duplication.
  /// </summary>
  public MagicAST.AST.TextSpan? SourceSpan { get; init; }

  /// <summary>
  /// The 0-based oracle-text line index of the projected ability, when the ability
  /// JSON carries an <c>OracleLineIndex</c>; else 0.
  /// </summary>
  public int OracleLineIndex { get; init; }

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

  public PortGraph Project(
    string card,
    JsonNode? oracleAbilities,
    JsonNode? manaCostSymbols = null,
    JsonNode? cardProfile = null
  )
  {
    var ports = new List<PortNode>();
    var edges = new List<CardDefinedEdge>();

    // Combat-damage as a structural CARD PROPERTY (CR 510, combat-damage-modeling decision 1): a creature
    // that can attack (a creature card, power not provably 0, no Defender) deals combat damage as a
    // consequence of attacking — there is NO effect/emit for it (the modeling blocker), so it is projected
    // here from the card's combat presence, not from an ability. The SOURCE is the creature itself
    // (self), the recipient is `any` (combat damage hits the blocker creature or the defending player), and
    // it is GATED: attacking is a once-per-combat event the engine can't freely re-fire within a turn (no
    // extra-combat modeling), so any loop through it floors to AMBER — never a false GREEN. Projected only
    // when the caller supplies a cardProfile (the bench corpus); other callers omit it (no combat presence).
    ProjectCombatPresence(card, cardProfile, ports, edges);

    if (oracleAbilities is JsonArray abilities)
      foreach (var ability in abilities)
        if (ability is JsonObject ao)
          ProjectAbility(ao, card, ports, edges, manaCostSymbols);

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
  /// Project a creature's structural <b>combat-damage emit</b> (CR 510, combat-damage-modeling decision 1).
  /// "Dealing combat damage" is an implicit consequence of attacking with NO effect to read — so it is
  /// projected from the card's combat presence: a CREATURE (CR 301.7 — a Vehicle isn't one until crewed, too
  /// indirect to claim here), power not provably 0 (CR 208 — a 0-power creature deals no combat damage),
  /// and no Defender (CR 702.3b — can't attack). The SOURCE is self ("this creature"); the recipient is
  /// <c>any</c> (combat damage hits the blocking creature OR the defending player, CR 510.1). GATED: attacking
  /// is once per combat and the engine models no extra-combat phases, so any loop through it floors to AMBER
  /// — never a false GREEN (the soundness floor that lets the combat arm be active without over-certifying).
  /// <para>The <paramref name="cardProfile"/> (a <c>{ Types, Power, HasDefender }</c> object) is supplied
  /// only by callers that know the card's type line + P/T (the combo-recall bench). Callers that omit it
  /// (the sentinel snapshot, census, union) project no combat presence — keeping the combat emit scoped to
  /// where it is measured.</para>
  /// </summary>
  private void ProjectCombatPresence(string card, JsonNode? cardProfile, List<PortNode> ports, List<CardDefinedEdge> edges)
  {
    if (cardProfile is not JsonObject p)
      return;
    var types = (p["Types"] as JsonArray)?.Select(t => t?.ToString()) ?? [];
    if (!types.Any(t => string.Equals(t, "creature", StringComparison.OrdinalIgnoreCase)))
      return; // only creatures have a standing combat presence
    if (p["HasDefender"]?.GetValue<bool>() == true)
      return; // Defender can't attack (CR 702.3b)
    if (p["Power"] is JsonValue pv && pv.TryGetValue<int>(out var pow) && pow == 0)
      return; // provably 0 power deals no combat damage; variable/unknown power stays present (conservative)

    var self = new ObjectFilter { IsSelf = true };
    var combatDamage =
      Port(card, PortLabel.DealDamageEmit(PortLabel.DamageCombat, "any"), PortSide.Emit, subject: self)
        with
      {
        Gated = true,
      };
    ports.Add(combatDamage);

    // Extra-combat re-attack (CR 500.8): an additional combat phase lets this creature attack AGAIN → it
    // deals combat damage again. Project an `attacksorblocks:self` consume (satisfied by an
    // emit:additionalcombat through the extra-combat arm) and a card-defined edge re-driving the
    // combat-damage emit. This CLOSES the Breath-of-Fury / Aggravated-Assault infinite-combat loop
    // (combat-damage → additional-combat → re-attack → combat-damage), with an attack-roll creature's roll
    // as the offshoot. CRUCIAL: combatDamage is ALSO projected as a standalone (seed) emit, so the turn's
    // free first combat fires it unconditionally — this only ADDS a re-drive path. A loop through it stays
    // AMBER (combatDamage is Gated); a non-extra-combat combo (Captain Rex) is untouched (its combat-damage
    // hop was already Gated→Amber; the unfed attacks co-cost only re-confirms that floor, never prunes).
    var attacks = Port(card, PortLabel.AttacksConsume(), PortSide.Consume, subject: self) with { Gated = true };
    ports.Add(attacks);
    edges.Add(new CardDefinedEdge { From = attacks, To = combatDamage });
  }

  /// <summary>
  /// Project ONE ability (a card's own top-level ability, or — recursively — an ability GRANTED by a
  /// continuous effect, CR 113.6) into single-role ports + their card-defined edges, appended to the
  /// card-level <paramref name="ports"/>/<paramref name="edges"/> accumulators. A granted ability's ports
  /// belong to the GRANTING card (CR 611 — the grant gives the affected permanent that ability; for
  /// reconstruction it is attributed to the granter, whose grant is what creates the looping object — e.g.
  /// Captain Rex Nebula's Crash Land on the Vehicle it makes). Each ability is its own unit so its trigger
  /// drives its own effects (§5), not the granter's outer trigger.
  /// </summary>
  private void ProjectAbility(
    JsonObject ability,
    string card,
    List<PortNode> ports,
    List<CardDefinedEdge> edges,
    JsonNode? manaCostSymbols
  )
  {
      // A CLASS ability (CR 716) is a CONTAINER — a base section (always active) plus leveled sections
      // gained by paying a level-up cost. It has no Trigger/Costs/Effects of its own; its flow lives in
      // the nested abilities. Recurse each (base + per-level) as its own ProjectAbility unit (like
      // becomesPermanent's GainedAbilities), so the base + level bodies project their ports — e.g. Barbarian
      // Class's base dice-advantage replacement and its Level-2 "whenever you roll" trigger become real graph
      // nodes. The level-up mana cost gates the leveled abilities, but the parse layer models level bodies as
      // reachable and §8 firability already floors any loop through a gated emit, so a per-level cost gate is
      // not needed for soundness here. (Saga/Modal/LevelUp containers stay un-recursed until a combo needs
      // their bodies — same non-inert principle as the becomesPermanent scope note.)
      if (string.Equals(ability["Kind"]?.ToString(), "class", StringComparison.Ordinal))
      {
        foreach (var ba in ability["BaseAbilities"] as JsonArray ?? [])
          if (ba is JsonObject bao)
            ProjectAbility(bao, card, ports, edges, manaCostSymbols);
        foreach (var lvl in ability["Levels"] as JsonArray ?? [])
          foreach (var la in lvl?["Abilities"] as JsonArray ?? [])
            if (la is JsonObject lao)
              ProjectAbility(lao, card, ports, edges, manaCostSymbols);
        return;
      }

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
        Effects(effect, card, keyword, manaCostSymbols, consumes, emits, ports, edges);

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

      // Oracle-text provenance (upstream-atlas-data-plan §4): thread the projecting
      // ability's SourceSpan + OracleLineIndex (populated by the MAST parser) onto
      // every port it produced, so a port traces back to the exact oracle substring.
      // Null-safe: when the ability JSON carries no span (the default until MAST span
      // serialization is enabled) this is a no-op and the ports keep null/0.
      // Clause-accurate provenance (upstream-atlas-data-plan §4): consume ports trace to
      // the TRIGGER-half span, emit ports to the EFFECT-half span. Each falls back to the
      // whole-ability span when its half is absent — activated-ability cost/cast/combat
      // consumes have no Trigger node, spell effects carry no per-effect span, etc.
      var abilitySpan = ReadSpan(ability);
      var abilityLine = ability["OracleLineIndex"]?.GetValue<int>() ?? 0;
      var rawTrigger = ability["Trigger"] is JsonObject trigObj ? ReadSpan(trigObj) : null;
      MagicAST.AST.TextSpan? rawEffect = null;
      foreach (var eff in ability["Effects"] as JsonArray ?? [])
        if (eff is JsonObject effObj && ReadSpan(effObj) is { } es)
        {
          rawEffect = es;
          break;
        }

      // A child span only helps if it genuinely NARROWS the ability — an unstructured
      // effect (or a trigger with no parsed span) re-carries the whole-ability span, which
      // tells us nothing. When the specific span is absent we DERIVE the half from the
      // boundary between trigger and effect: emit region = after the trigger; consume/cost
      // region (activated, no trigger) = before the effect.
      bool Narrows(MagicAST.AST.TextSpan? child) =>
        child is { } c && abilitySpan is { } a && (c.Start != a.Start || c.Length != a.Length);
      var triggerNarrows = Narrows(rawTrigger);
      var effectNarrows = Narrows(rawEffect);

      MagicAST.AST.TextSpan? consumeSpan =
        rawTrigger is { } trg ? trg
        : effectNarrows && abilitySpan is { } ac && rawEffect is { } reC
          ? MagicAST.AST.TextSpan.FromBounds(ac.Start, reC.Start)
          : abilitySpan;

      MagicAST.AST.TextSpan? emitSpan =
        effectNarrows ? rawEffect
        : triggerNarrows && abilitySpan is { } ae && rawTrigger is { } rtE
          ? MagicAST.AST.TextSpan.FromBounds(rtE.End, ae.End)
          : abilitySpan;

      if (consumeSpan is not null || emitSpan is not null || abilityLine != 0)
      {
        for (var i = 0; i < consumes.Count; i++)
          consumes[i] = consumes[i] with { SourceSpan = consumeSpan, OracleLineIndex = abilityLine };
        for (var i = 0; i < emits.Count; i++)
          emits[i] = emits[i] with { SourceSpan = emitSpan, OracleLineIndex = abilityLine };
      }

      ports.AddRange(consumes);
      ports.AddRange(emits);
      // Card-defined causality: every consume/cost in the ability drives every effect (§5).
      foreach (var from in consumes)
        foreach (var to in emits)
          edges.Add(new CardDefinedEdge { From = from, To = to });
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
    else if (ev == "SpellCast")
      // "Whenever you cast a [noncreature] spell" (CR 603.2) — consumes a spell-cast event (Displacer
      // Kitten). The watched-spell filter (its `!creature` exclusion, controller) rides as the subject so
      // the cast flow arm tiers the connection by spell-type (ADR-0002 §7). Non-null (broadest "a spell"
      // when unqualified) — a cast event is type-scoped, never a null-default-GREEN scalar.
      consumes.Add(Port(card, PortLabel.CastTrigger(filter ?? AnySpell, _ontology), PortSide.Consume, subject: filter ?? AnySpell));
    else if (ev == "DiceRolled")
      // "Whenever you roll one or more dice" (CR 706.2) — consumes a die-roll event. The rolling player
      // (the controller) rides as the subject so the dice flow arm tiers by player (You↔You GREEN). A
      // result threshold (DieResultThreshold) gates firability, not the flow connection. Non-null
      // (controller-scoped) — a roll trigger watches YOUR rolls, never a null-default-GREEN scalar.
      consumes.Add(Port(card, PortLabel.RollDiceTrigger(Roller), PortSide.Consume, subject: Roller));
    else if (DamageTriggerFacets(ev) is { } facets)
      // "Whenever [source] deals [combat] damage [to recipient]" (CR 120 general / CR 510 combat) —
      // consumes a damage event. The watched SOURCE (the trigger's Filter — "this Vehicle", "a creature
      // you control") rides as the NON-NULL subject so the damage flow arm tiers it; a self-watching
      // trigger ("this …") matches same-card-only in the arm. The combat facet (combat/noncombat/any) and
      // recipient class (from the event name) ride in the label, gating feasibility (a non-combat emit
      // never feeds a combat trigger; a player-recipient emit never feeds a creature-recipient trigger).
      consumes.Add(
        Port(
          card,
          PortLabel.DamageTrigger(facets.Combat, facets.Recipient),
          PortSide.Consume,
          subject: filter ?? AnySource
        )
      );
    else
      // Coarse fallback (totality): the event name as the role, plus the watched filter as the NON-NULL
      // subject. Threading the filter (not null) is load-bearing: when a flow arm reads a coarse trigger
      // consume (e.g. the extra-combat arm reads attacksorblocks), a null subject would hit AddRulesEdge's
      // scalar null-default-GREEN branch — a false GREEN (re-attacking is a CHOICE, CR 508.1a, never
      // guaranteed). The filter ("this creature" → {creature, IsSelf}) tiers it AMBER (Overlaps, not
      // Subsumes a producer's {Controller:You}), matching the explicit ?? Any… floors the semantic triggers use.
      consumes.Add(Port(card, Coarse(ev, filter), PortSide.Consume, subject: filter));
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
    List<PortNode> emits,
    List<PortNode> ports,
    List<CardDefinedEdge> edges
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
        Effects(sub, card, keyword, manaCostSymbols, consumes, emits, ports, edges);
        if (gated)
          for (var i = before; i < emits.Count; i++)
            emits[i] = emits[i] with { Gated = true };
      }
      return;
    }

    // A CONDITIONAL effect ("If [condition], [Then] [else Else]", CR 603 in-ability gate) is a hard
    // firability gate on its branches (ADR-0002 §8): the branch fires only when the condition holds, so a
    // loop through a Then/Else emit can't be certified infinite — its ports are marked Gated → the cycle
    // floors to Amber. Recurse into BOTH branches by totality (§4) so their flow ports project (Captain Rex
    // Nebula's Crash Land: "if the result equals this Vehicle's mana value, … it deals that much damage" —
    // the gated dealDamage emit that closes the deal→roll→deal self-loop at a sound Amber). The condition
    // itself is descriptive (the engine doesn't evaluate it); only the gate matters here.
    if (effectType == "conditional")
    {
      foreach (var branch in new[] { e["Then"], e["Else"] })
        foreach (var sub in InnerEffects(branch))
        {
          var beforeE = emits.Count;
          var beforeC = consumes.Count;
          Effects(sub, card, keyword, manaCostSymbols, consumes, emits, ports, edges);
          for (var i = beforeE; i < emits.Count; i++)
            emits[i] = emits[i] with { Gated = true };
          for (var i = beforeC; i < consumes.Count; i++)
            consumes[i] = consumes[i] with { Gated = true };
        }
      return;
    }

    // A die-roll RESULTS-TABLE (rollResultsTable, CR 706.3) is a result-gated fan-out: each row's effects
    // fire only when the die result lands in that row's inclusive range. Like a conditional (§8), a specific
    // row's emit is a hard firability gate (you can't certify a given row fires every iteration) → Gated →
    // a loop through it floors to Amber. Recurse every row's effects by totality (§4) so their flow ports
    // project (e.g. a "10—19 | create a token" row's emit:token; the roll itself is the sibling rollDie
    // effect in the composite, which projects emit:rolldice independently → the offshoot the dice arm reads).
    if (effectType == "rollResultsTable")
    {
      foreach (var row in e["Rows"] as JsonArray ?? [])
        foreach (var sub in row?["Effects"] as JsonArray ?? [])
        {
          var beforeE = emits.Count;
          var beforeC = consumes.Count;
          Effects(sub, card, keyword, manaCostSymbols, consumes, emits, ports, edges);
          for (var i = beforeE; i < emits.Count; i++)
            emits[i] = emits[i] with { Gated = true };
          for (var i = beforeC; i < consumes.Count; i++)
            consumes[i] = consumes[i] with { Gated = true };
        }
      return;
    }

    // A "becomes a [type] permanent … and gains [abilities]" continuous grant (becomesPermanent, CR
    // 611/113.6) gives the affected permanent one or more abilities. Project each GRANTED ability as its
    // own port unit, attributed to the GRANTING card — the grant is what creates the looping object
    // (Captain Rex Nebula's "Crash Land" on the Vehicle it makes; a granted ability's own trigger drives
    // its own effects, §5, so it forms an independent ProjectAbility unit). The grant effect itself still
    // projects its coarse inert emit (totality, §4) below.
    // SCOPE: only becomesPermanent recurses today — it is the sole node whose grant carries a flow-relevant
    // TRIGGERED ability (Crash Land). The sibling grants (becomesCreature's Keyrune flying, gainAbility's
    // dredge/keywords) grant only inert keywords in the corpus, so recursing them would add inert ports
    // with no recall benefit (broad snapshot churn for zero arms) — deferred per the non-inert principle;
    // they stay coarse-whitelisted until a combo needs a granted triggered ability through them.
    if (effectType == "becomesPermanent")
      foreach (var ga in e["GainedAbilities"] as JsonArray ?? [])
        if (ga is JsonObject gao)
          ProjectAbility(gao, card, ports, edges, manaCostSymbols);

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
    if (
      effectType == "returnToHand"
      && string.Equals(e["Target"]?["Kind"]?.ToString(), "Self", StringComparison.OrdinalIgnoreCase)
    )
    {
      // Cast-recursion (Displacer Kitten family) — the MORE-SPECIFIC Self-bounce shape, matched first.
      // A noncreature permanent that returns ITSELF to hand ({mana}: "Return this Aura to its owner's hand"
      // — Mourning/Conviction; also Reiterate's buyback self-return) becomes a card in hand that is RE-CAST
      // as a spell, and that cast genuinely fires a "whenever you cast a spell" trigger (CR 601/603.2 —
      // distinct from a spell-COPY, uncast per CR 707.10). Project the recast FAITHFULLY as emit:cast with a
      // NON-NULL Subject = the recast spell's filter (broadest "a spell" — the card type is not threaded
      // into the walk, so the operator tiers the trigger's `!creature` exclusion as a sound AMBER, never a
      // null-default GREEN — anti-pattern 3). Attach the card's OWN mana cost as the recast pay:mana
      // CO-COST (the structural twin of the aristocrat recast), so §8 mana-balance floors a loop whose
      // recast mana it can't itself refill (the lands Peregrine untaps are an outside-the-loop enabler) —
      // the honest AMBER. Returns early, so a Self-bounce never also projects the coarse returntohand below.
      foreach (var (label, quantity) in RecastManaCost(null, manaCostSymbols))
        consumes.Add(Port(card, label, PortSide.Consume, quantity));
      return Port(card, PortLabel.CastEmit(AnySpell, _ontology), PortSide.Emit, subject: AnySpell);
    }
    if (effectType == "returnToHand")
    {
      // "Return target [card] to [its owner's] hand." The NON-self shapes (the Self-bounce recast above
      // already returned). Two distinct cases:
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
      // permanent. Consumed by the ("copy","cast") arm (PortGraphEngine.SpellCopyReFiresEffects): a stack
      // spell-copy re-fires the copied spell's effects into a type-compatible cast:spell:self driver
      // (CR 707.10), never a cast trigger. (adding-a-flow-arm.md projection↔connection split.)
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
    if (effectType is "additionalCombatPhase" or "additionalCombatAndMainPhase")
      // An additional combat phase (CR 500.8 — Aggravated Assault, Breath of Fury, Combat Celebrant) lets
      // YOUR creatures attack again. Subject {Controller:You} (your creatures) so the extra-combat arm into
      // an attacksorblocks:self consume tiers AMBER (Overlaps, not Subsumes the specific attacker) — never a
      // null-default GREEN. additionalCombatAndMainPhase collapses to the same emit: the extra MAIN phase is
      // irrelevant to the combat loop. Drives the extra-combat arm (emit:additionalcombat → attacksorblocks).
      return Port(card, PortLabel.AdditionalCombatEmit(), PortSide.Emit, subject: new ObjectFilter { Controller = ControllerFilter.You });
    if (effectType == "rollDie")
    {
      // A die-roll event (CR 706). The rolling player (the controller) rides as the subject so a
      // "whenever you roll" trigger tiers by player. Count (null ≡ 1) is the number of dice rolled —
      // the emit quantity, since rolling N dice is N roll events (CR 706.2). Feeds the dice flow arm.
      var count = (int?)(e["Count"]?.GetValue<int>()) ?? 1;
      return Port(card, PortLabel.RollDiceEmit(Roller), PortSide.Emit, count, Roller);
    }
    if (
      effectType == "returnToBattlefield"
      && e["Target"]?["Filter"]?["ExiledWith"]?["Kind"]?.ToString() == "Self"
    )
    {
      // A "return [the just-exiled thing] to the battlefield" (Target.Filter.ExiledWith:Self) is a BLINK:
      // the permanent re-enters as a NEW object (CR 603.6e/400.7), re-firing its ETB. Project emit:blink
      // (feeds the blink arm → an etb consume), so the ACTIVATED/triggered "exile target creature, then
      // return it" outlets the narrow composite BlinkPort missed — Emiel the Blessed, Eldrazi Displacer
      // (they parse as a FLAT [exile, returnToBattlefield] pair, not a composite) — become real repeatable
      // blink outlets the engine can bond to a dice-ETB creature's self-ETB (Swarming Goblins, etc.). The
      // Subject floors to {creature} (the standalone-return blink outlets exile creatures; the exile's exact
      // filter lives on the sibling effect, not threaded here) — NON-NULL, so it never hits the scalar
      // null-default GREEN (anti-pattern 3), and BlinkSatisfiesEnter's same-card guard keeps it sound.
      // CRITICAL: a return WITHOUT ExiledWith:Self — Persist/Undying's self-return (Target:Self from the
      // graveyard) and reanimation (return a card from a graveyard) — falls through to the coarse
      // emit:returntobattlefield, PRESERVING the §8-B one-shot-self-removal carve-out that keys on it.
      var blinked = new ObjectFilter { CardTypes = ["creature"] };
      return Port(card, PortLabel.BlinkEmit(blinked, _ontology), PortSide.Emit, subject: blinked);
    }
    if (effectType == "dealDamage")
    {
      // A "deals N damage to [target]" event (CR 119/120). The combat facet is non-combat by default
      // (an explicit damage effect, CR 120) unless IsCombat marks it combat (CR 510) — so it feeds a
      // bare "deals damage" trigger and a non-combat trigger, but NEVER a combat-specific trigger (the
      // soundness the damage arm enforces). The SOURCE (who deals it — Self/"this", a target creature)
      // rides as the NON-NULL subject so the operator tiers the arm; a self-source self-watching pair
      // is matched same-card-only by the arm. The recipient class rides in the label, gating feasibility.
      var source = SourceFilter(e["Source"]);
      var combat = e["IsCombat"]?.GetValue<bool>() == true ? PortLabel.DamageCombat : PortLabel.DamageNoncombat;
      var recipient = DamageRecipientFacet(e["Target"]);
      return Port(card, PortLabel.DealDamageEmit(combat, recipient), PortSide.Emit, Qty(e["Amount"]), source);
    }
    return effectType switch
    {
      // Inert effects (no flow) are still ports, by totality (§4) — edge-sparse, never dropped.
      "modifyPT" => Port(card, "modify:pt", PortSide.Emit),
      "switchPT" => Port(card, "switch:pt", PortSide.Emit),
      "setBasePT" => Port(card, "set:pt", PortSide.Emit),
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

  /// <summary>
  /// Read an ability's <c>SourceSpan</c> (MAST oracle-text provenance) from its JSON,
  /// tolerating either PascalCase (<c>Start</c>/<c>Length</c>) or camelCase serialization.
  /// <c>null</c> when the ability carries no span (the default until MAST span
  /// serialization is enabled) or the span is malformed — never fabricated.
  /// </summary>
  private static MagicAST.AST.TextSpan? ReadSpan(JsonObject ability)
  {
    if (ability["SourceSpan"] is not JsonObject span)
      return null;
    var start = (span["Start"] ?? span["start"])?.GetValue<int>();
    var length = (span["Length"] ?? span["length"])?.GetValue<int>();
    if (start is null || length is null)
      return null;
    return new MagicAST.AST.TextSpan(start.Value, length.Value);
  }

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

  /// <summary>The die-roll Subject — the rolling player (the controller). "Whenever you roll" watches the
  /// controller's own rolls (CR 706.2), and a "roll N dice" effect is the controller rolling, so both the
  /// emit and the trigger carry this controller scope; You↔You overlap tiers the dice flow arm.</summary>
  private static readonly ObjectFilter Roller = new() { Controller = ControllerFilter.You };

  /// <summary>The cast consume's Subject — "this instant or sorcery spell" (CR 601.2). The card type is
  /// not threaded into the walk, so this is the broadest faithful spell type (a Kind:spell ability is an
  /// instant/sorcery on-cast effect); NON-NULL so the spell-recast arm never hits the scalar null-default
  /// GREEN (adding-a-flow-arm anti-pattern 3). IsSelf marks it the spell's own identity.</summary>
  private static readonly ObjectFilter SpellSelf = new()
  {
    CardTypes = ["instant", "sorcery"],
    IsSelf = true,
  };

  /// <summary>The broadest cast-spell subject (CR 601) — a NON-null floor (CardTypes:[spell], you control)
  /// so a cast trigger / recast emit never hits the scalar null-default GREEN in <see cref="PortGraphEngine"/>
  /// (adding-a-flow-arm anti-pattern 3). Used when the watched/recast spell's type is unqualified (a bare
  /// "whenever you cast a spell") or not threaded into the walk (a self-bounce recast); the operator tiers
  /// the trigger's `!creature` exclusion against it as a sound AMBER until a parse-layer sharpen earns more.</summary>
  private static readonly ObjectFilter AnySpell = new() { CardTypes = ["spell"], Controller = ControllerFilter.You };

  /// <summary>The broadest damage-source subject — a NON-null floor (an empty filter Overlaps any source)
  /// so a damage trigger/emit with no stated source never hits the scalar null-default GREEN in
  /// <see cref="PortGraphEngine"/> (adding-a-flow-arm anti-pattern 3). Most damage triggers DO carry a
  /// source filter ("this …", "a creature you control"), so this floor is the rare unqualified case.</summary>
  private static readonly ObjectFilter AnySource = new();

  /// <summary>The damage SOURCE (who deals the damage) → an <see cref="ObjectFilter"/> subject the operator
  /// tiers. <c>Self</c>/<c>It</c> → <c>{IsSelf:true}</c> ("this object" — the source itself, the common case:
  /// a creature/Vehicle dealing its own damage); a <c>Target</c>/filtered source carries its embedded filter;
  /// absent/unknown → the broadest <see cref="AnySource"/>. NEVER null (a null source would hit the scalar
  /// null-default GREEN). The arm's same-card guard handles the <c>IsSelf</c> object-identity question the
  /// operator can't see.</summary>
  private static ObjectFilter SourceFilter(JsonNode? source) =>
    source?["Kind"]?.ToString() switch
    {
      null => AnySource,
      "Self" or "It" => new ObjectFilter { IsSelf = true },
      _ => Filter(source?["Filter"]) ?? AnySource,
    };

  /// <summary>The recipient-class facet of a damage event's target (who/what takes the damage) — the
  /// label facet that prunes a player-recipient emit from feeding a creature-recipient trigger (CR 510.1:
  /// combat damage is assigned to players, planeswalkers, battles, or creatures). <c>any</c> is the
  /// permissive floor (an unqualified or "any target" recipient overlaps everything).</summary>
  private static string DamageRecipientFacet(JsonNode? target) =>
    (target?["Kind"]?.ToString()) switch
    {
      "Opponent" or "EachOpponent" => "opponent",
      "AnyTarget" => "any",
      "You" or "EachPlayer" or "ThatPlayer" => "player",
      _ => DamageRecipientFromFilter(Filter(target?["Filter"])),
    };

  /// <summary>The recipient facet from a target's type filter — <c>player</c>/<c>creature</c>/
  /// <c>planeswalker</c> when the filter names that card type, else the permissive <c>any</c>.</summary>
  private static string DamageRecipientFromFilter(ObjectFilter? f)
  {
    var types = f?.CardTypes;
    if (types is null || types.Count == 0)
      return "any";
    if (types.Any(t => string.Equals(t, "creature", StringComparison.OrdinalIgnoreCase)))
      return "creature";
    if (types.Any(t => string.Equals(t, "planeswalker", StringComparison.OrdinalIgnoreCase)))
      return "planeswalker";
    if (types.Any(t => string.Equals(t, "player", StringComparison.OrdinalIgnoreCase)))
      return "player";
    return "any";
  }

  /// <summary>
  /// The combat facet + recipient class of a damage TRIGGER event (CR 120 general / CR 510 combat), or
  /// <c>null</c> when <paramref name="ev"/> is not a source-perspective damage trigger. Only the
  /// SOURCE-perspective events ("[source] deals damage …") are projected here — their <c>Filter</c> is the
  /// watched source, which rides as the port Subject. The recipient-perspective events ("a player/creature
  /// is dealt damage", <c>PlayerDealtDamage</c>/<c>CreatureDealtDamage</c>/<c>DamageDealt</c>) watch the
  /// RECIPIENT, a different keying, and stay coarse for now.
  /// </summary>
  private static (string Combat, string Recipient)? DamageTriggerFacets(string ev) =>
    ev switch
    {
      "DealsCombatDamage" => ("combat", "any"),
      "DealsCombatDamageToPlayer" => ("combat", "player"),
      "DealsCombatDamageToPlayerOrPlaneswalker" => ("combat", "playerorpw"),
      "DealsCombatDamageToCreature" => ("combat", "creature"),
      "NoncombatDamageDealt" => ("noncombat", "any"),
      "DealsDamage" => ("any", "any"),
      "DealsDamageToOpponent" => ("any", "opponent"),
      "DealsDamageToOpponents" => ("any", "opponent"),
      _ => null,
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
