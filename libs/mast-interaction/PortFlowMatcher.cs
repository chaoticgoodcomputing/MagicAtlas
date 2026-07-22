namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 — the <b>structured flow matcher</b>, authoritative since the Stage-4 cutover. Selects which
/// flow arm connects an emit to a consume <em>purely from their <see cref="PortStructure"/></em> (the is-a
/// stem + attribute set), then applies that arm's guard. This is the "structure absorbs the structural
/// content of the per-arm switch; the guards stay in code" split (ADR-0003 §5, Decision 1/2):
/// <see cref="SelectArm"/> is the structural half — it maps <c>(emit-stem, consume-stem + facets)</c> to an
/// arm — while the guards (token-at-creation types, the damage/blink self-source same-card refusals, mana
/// colour, recast type cover) remain the engine's registered implementations, reused verbatim so there is
/// one source of truth. A null <see cref="PortNode.Structure"/> (an unconverted family) yields no arm — the
/// honest "not on the structure" answer, never a silent taxonomy drift.
///
/// <para>The accepted <c>emit-stem → consume-stem</c> hops (plus the facet guards each needs) are also the
/// compact flow-adjacency the frontend consumes (Stage 5), replacing the lossy family-ring re-expansion
/// that invented edges the engine never drew (Chatterfang→Aang, Barrage→Copper Dragon).</para>
/// </summary>
public sealed class PortFlowMatcher
{
  private readonly PortGraphEngine _engine;

  public PortFlowMatcher(PortGraphEngine engine) => _engine = engine;

  /// <summary>The flow arms — one per structural hop the matcher connects.</summary>
  public enum FlowArm
  {
    TokenToSac,
    ManaToPay,
    LifeCostToPay,
    LifeToTrigger,
    DiceToTrigger,
    DamageToTrigger,
    AdditionalCombatToAttacks,
    CastToTrigger,
    BlinkToEtb,
    ReanimateToSac,
    ReanimateToEtb,
    SpellRecursionToCast,
    SpellCopyToCast,
    SacrificeDeathToTrigger,
  }

  /// <summary>
  /// The structural arm selection — reproduces the engine's <c>(ResourceKind(emit), Role(consume))</c>
  /// switch from the two structures alone. Returns null when no flow arm connects the pair (the structural
  /// prune — e.g. a token emit and a cast trigger share no arm, so Chatterfang's <c>deployment:creature</c>
  /// never reaches Aang's <c>cast[role=trigger]</c>). Keying on matching stems is stricter than the label
  /// switch's shared <c>trigger</c> role token, which the switch had to disambiguate with inner
  /// <c>ResourceKind(consume)</c> re-checks — the structure makes that implicit.
  /// </summary>
  public static FlowArm? SelectArm(PortStructure emit, PortStructure consume)
  {
    if (emit.Side != PortSide.Emit || consume.Side != PortSide.Consume)
      return null;

    bool E(string stem) => emit.Stem == stem;
    bool C(string stem) => consume.Stem == stem;

    // token creation → sacrifice fodder (the created creature can be sac'd; type cover in the guard).
    if (E("deployment:creature") && emit.Attr("token") == "true" && C("creature") && consume.Attr("manner") == "sacrificed")
      return FlowArm.TokenToSac;
    // produced mana → a mana cost (colour compatibility in the guard).
    if (E("mana") && C("mana"))
      return FlowArm.ManaToPay;
    // a life-gain event → a life-payment cost (replenishing life spent, mirrors mana → pay:mana; the
    // gain-only direction check is the guard). A distinct consume stem ("paylife", not "life") from the
    // life-TRIGGER consume below keeps the two relations — a subscription vs. an actual resource payment —
    // unambiguous at the structural layer (PayLifeFamily).
    if (E("life") && C("paylife"))
      return FlowArm.LifeCostToPay;
    // a life event → a same-direction life trigger (direction in the guard).
    if (E("life") && C("life"))
      return FlowArm.LifeToTrigger;
    // a dice roll → a dice-rolled trigger.
    if (E("dice") && C("dice"))
      return FlowArm.DiceToTrigger;
    // damage dealt → a damage trigger (manner/recipient/self-source in the guard).
    if (E("damage") && C("damage"))
      return FlowArm.DamageToTrigger;
    // an extra combat phase → a creature's re-attack opportunity (either scope the attacksorblocks consume
    // projects: "this creature" self or the coarse "a creature" — the label oracle accepts both).
    if (
      E("combat")
      && emit.Attr("phase") == "additional"
      && C("combat")
      && (consume.Attr("scope") == "self" || consume.Attr("scope") == "creature")
    )
      return FlowArm.AdditionalCombatToAttacks;
    // a re-cast spell → a "whenever you cast" trigger (type compatibility in the guard).
    if (E("cast") && C("cast") && consume.Attr("role") == "trigger")
      return FlowArm.CastToTrigger;
    // a blinked permanent's re-entry → an Enters trigger (self-blink same-card refusal in the guard).
    if (E("deployment:creature") && emit.Attr("manner") == "blink" && C("deployment:creature") && consume.Attr("event") == "etb")
      return FlowArm.BlinkToEtb;
    // a reanimated self → a sac cost / an Enters trigger (type cover in the guard).
    if (E("recur") && emit.Attr("to") == "battlefield")
    {
      if (C("creature") && consume.Attr("manner") == "sacrificed")
        return FlowArm.ReanimateToSac;
      if (C("deployment:creature") && consume.Attr("event") == "etb")
        return FlowArm.ReanimateToEtb;
    }
    // a return-to-hand → a spell's recast driver. Mirrors the label switch's ("returntohand","cast")
    // entry for BOTH spell-recursion and a bare permanent bounce; the guard's type Intersects prunes a
    // permanent bounce (not an instant/sorcery on the stack), exactly as the engine does.
    if (E("recur") && emit.Attr("to") == "hand" && C("cast") && consume.Attr("role") == "driver")
      return FlowArm.SpellRecursionToCast;
    // a copy → the copied spell's effect driver. Mirrors ("copy","cast") for both permanent and spell
    // copies; the guard rejects a bare permanent copy (no spell effects to re-fire, CR 707.10).
    if (E("copy") && C("cast") && consume.Attr("role") == "driver")
      return FlowArm.SpellCopyToCast;
    // a sacrifice's death event → a dies / LTB / "when sacrificed" trigger (ADR-0003 §5). The emit is the
    // narrowest rung (removal:creature[to=graveyard, manner=sacrificed]); a dies (to=graveyard), bare LTB,
    // or sacrificed-trigger consume captures it by attribute subsumption (the guard). Replaces the retired
    // consume→consume sac→dies label bridge.
    //
    // The pair must share a stem — that stem equality IS the type check, because
    // `SacrificeDeathFeedsTrigger` covers only `to`/`manner` and does no type relation. It is NOT
    // restricted to `removal:creature`: the CR ladder is permanent-general (CR 701.21a sacrifice moves a
    // *permanent* to its graveyard; CR 700.4 dies; CR 603.6d leaves-the-battlefield; CR 603.10b "abilities
    // that trigger when a player sacrifices a permanent"), and hardcoding `creature` pruned an artifact sac
    // outlet from its own artifact-dies rung — a false prune, ruled a real topology gap by the
    // interaction-judge (2026-07-20) against connectivity prediction P1 (ADR-0004 §7, issue #27).
    // CROSS-stem arming (removal:permanent destroy → removal:creature dies, AMBER by CR 110.4) is a
    // separate, larger change: it requires an ObjectFilterRelations type check inside the guard first,
    // without which it re-opens the BridgeFedByIncompatibleToken false loop (CR 111.10). Filed, not bundled.
    if (
      emit.Stem == consume.Stem
      && (emit.Stem == "removal" || emit.Stem.StartsWith("removal:", StringComparison.Ordinal))
    )
      return FlowArm.SacrificeDeathToTrigger;

    return null;
  }

  /// <summary>
  /// Does the emit feed the consume? Structural arm selection (<see cref="SelectArm"/>) then the arm's
  /// guard. Returns false for an unconverted family (either port has a null <see cref="PortNode.Structure"/>)
  /// — the honest "not yet on the structure" answer.
  /// </summary>
  public bool Captures(PortNode emit, PortNode consume) => CapturingArm(emit, consume) is not null;

  /// <summary>
  /// The <see cref="FlowArm"/> that connects this emit→consume, or <c>null</c> when none does. Identical
  /// decision to <see cref="Captures"/>, but returns <em>which</em> arm fired — the fine structural
  /// mechanism the engine tags onto the formed edge (<see cref="PortEdge.Arm"/>, ADR-0004 issue #34).
  /// Keeping this the single source of the arm means the tag and the edge's existence can never disagree.
  /// </summary>
  public PortFlowMatcher.FlowArm? CapturingArm(PortNode emit, PortNode consume)
  {
    if (emit.Structure is null || consume.Structure is null)
      return null;
    var arm = SelectArm(emit.Structure, consume.Structure);
    var applies = arm switch
    {
      FlowArm.TokenToSac => _engine.TokenSatisfiesAtCreation(emit, consume),
      FlowArm.ManaToPay => PortGraphEngine.ManaColorFeeds(
        PortGraphEngine.ManaColor(emit),
        PortGraphEngine.ManaColor(consume)
      ),
      FlowArm.LifeCostToPay => PortGraphEngine.LifeGainFeedsCost(emit),
      FlowArm.LifeToTrigger => PortGraphEngine.LifeFlowFeasible(emit, consume),
      FlowArm.DiceToTrigger => true,
      FlowArm.DamageToTrigger => _engine.DamageSatisfiesTrigger(emit, consume),
      FlowArm.AdditionalCombatToAttacks => true,
      FlowArm.CastToTrigger => _engine.CastSatisfiesTrigger(emit, consume),
      FlowArm.BlinkToEtb => _engine.BlinkSatisfiesEnter(emit, consume),
      FlowArm.ReanimateToSac => _engine.RecastSatisfies(emit, consume),
      FlowArm.ReanimateToEtb => _engine.RecastSatisfies(emit, consume),
      FlowArm.SpellRecursionToCast => _engine.SpellRecursionSatisfiesCast(emit, consume),
      FlowArm.SpellCopyToCast => _engine.SpellCopyReFiresEffects(emit, consume),
      FlowArm.SacrificeDeathToTrigger => _engine.SacrificeDeathFeedsTrigger(emit, consume),
      _ => false, // null (no arm) or any unnamed enum value — no flow
    };
    return applies ? arm : null;
  }
}
