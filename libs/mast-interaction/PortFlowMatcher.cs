namespace MagicAST.Interaction;

using MagicAST.AST.References;

/// <summary>
/// ADR-0003 Stage 3 — the <b>structured flow matcher</b>. Selects which flow arm connects an emit to a
/// consume <em>purely from their <see cref="PortStructure"/></em> (the is-a stem + attribute set), then
/// applies that arm's guard. This is the "structure absorbs the structural content of the per-arm switch;
/// the guards stay in code" split (ADR-0003 §5, Decision 1/2): <see cref="SelectArm"/> is the structural
/// half — it reproduces <see cref="PortGraphEngine.FlowFeasible"/>'s <c>(ResourceKind(emit), Role(consume))</c>
/// label switch from the structures alone — while the guards (token-at-creation types, the damage/blink
/// self-source same-card refusals, mana colour, recast type cover) remain the engine's registered
/// implementations, reused verbatim so there is one source of truth.
///
/// <para><b>Shadow mode (the engine is the oracle).</b> <see cref="Captures"/> must return the same
/// accept/reject as <see cref="PortGraphEngine.FlowFeasible"/> for every emit×consume pair —
/// <c>PortFlowMatcherShadowTest</c> proves it over the sentinel corpus. Any divergence is either a matcher
/// bug or an unconverted family (a null Structure), never a silent taxonomy drift.</para>
///
/// <para>Once proven, <see cref="Arms"/> is the compact flow-adjacency the frontend consumes (Stage 5): the
/// legal <c>emit-stem → consume-stem</c> hops plus the facet guards each needs, replacing the lossy
/// family-ring re-expansion that invents edges the engine never drew (Chatterfang→Aang, Barrage→Copper
/// Dragon).</para>
/// </summary>
public sealed class PortFlowMatcher
{
  private readonly PortGraphEngine _engine;

  public PortFlowMatcher(PortGraphEngine engine) => _engine = engine;

  /// <summary>The flow arms — one per <see cref="PortGraphEngine.FlowFeasible"/> switch case, named by the
  /// structural hop it connects.</summary>
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

    return null;
  }

  /// <summary>
  /// Does the emit feed the consume? Structural arm selection (<see cref="SelectArm"/>) then the arm's
  /// guard. Returns false for an unconverted family (either port has a null <see cref="PortNode.Structure"/>)
  /// — the honest "not yet on the structure" answer, which the shadow gate scopes around. Equals
  /// <see cref="PortGraphEngine.FlowFeasible"/> for every structured pair (the equivalence proof).
  /// </summary>
  public bool Captures(PortNode emit, PortNode consume)
  {
    if (emit.Structure is null || consume.Structure is null)
      return false;
    var arm = SelectArm(emit.Structure, consume.Structure);
    return arm switch
    {
      FlowArm.TokenToSac => _engine.TokenSatisfiesAtCreation(emit, consume),
      FlowArm.ManaToPay => PortGraphEngine.ManaColorFeeds(
        PortGraphEngine.ManaColor(emit.Label),
        PortGraphEngine.ManaColor(consume.Label)
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
      null => false,
    };
  }
}
