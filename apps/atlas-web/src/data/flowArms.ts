// ADR-0003 Stage 5 — the flow adjacency, in resource-family terms.
//
// This is the SET OF EDGES THE ENGINE ACTUALLY DRAWS (PortGraphEngine.FlowFeasible),
// re-expressed at the resource-family granularity the frontend has. It replaces the
// lossy `resourceEdgeRows` "subway lines" — which are combo-RING co-occurrences
// (FamilyRollupStep splits each reconstructed combo's FamilyRing), NOT direct
// port-to-port flow edges. Re-expanding those rings onto every card of a family
// invents edges the engine never drew (token→cast made "Chatterfang feeds Aang";
// damage→damage with no manner check made "Barrage Ogre feeds Ancient Copper Dragon").
//
// The C# source of truth is PortFlowMatcher (libs/mast-interaction), proven
// equivalent to FlowFeasible over the sentinel corpus (PortFlowMatcherShadowTest).
// Keep this table in sync with PortFlowMatcher.SelectArm's arms.

/** For a CONSUME family (key), the EMIT families that feed it under a real flow arm. */
export const FLOW_FEEDERS: Readonly<Record<string, readonly string[]>> = {
  sacrifice: ["token", "recur"], // token creation / reanimation → a sacrifice cost
  mana: ["mana"], // produced mana → a mana cost (colour compat handled per-port)
  life: ["life"], // a life event → a same-direction life trigger
  dice: ["dice"], // a die roll → a dice-rolled trigger
  damage: ["damage"], // damage dealt → a damage trigger (manner/self handled per-port)
  combat: ["combat"], // an extra combat phase → a re-attack opportunity
  cast: ["cast", "recur", "copy"], // a recast / spell-recursion / spell-copy → a cast driver or trigger
  etb: ["blink", "recur"], // a blink / reanimation re-entry → an Enters trigger
};

/** For an EMIT family (key), the CONSUME families it feeds (the inverse of FLOW_FEEDERS). */
export const FLOW_DRAINS: Readonly<Record<string, readonly string[]>> = (() => {
  const out: Record<string, string[]> = {};
  for (const [consume, emitters] of Object.entries(FLOW_FEEDERS))
    for (const emit of emitters) (out[emit] ??= []).push(consume);
  return out;
})();

/** A port's flow-relevant facets (ADR-0003 structured attributes, served by the API). */
export interface PortFacets {
  family: string;
  manner?: string | null; // combat | noncombat | sacrificed | blink | …
  isSelf?: boolean; // the Subject is self-scoped ("this creature")
}

/**
 * Does an EMIT port feed a CONSUME port? The family-level flow arm plus the per-arm
 * facet guards the engine applies (the ones expressible at this granularity):
 *  • damage — a non-combat emit never feeds a combat-specific trigger (CR 510 vs 120),
 *    and a self-source trigger ("whenever THIS deals damage") fires only on its OWN
 *    card's damage, so no DIFFERENT card feeds it. (`emit` and `consume` are always
 *    different cards in the Explorer's columns.)
 * Other arms match at family granularity — their guards (type Intersects, self-blink
 * same-card) prune only same-card or provably type-disjoint pairs, which the family
 * columns already exclude or tolerate.
 */
export function feeds(emit: PortFacets, consume: PortFacets): boolean {
  if (!FLOW_FEEDERS[consume.family]?.includes(emit.family)) return false;
  if (consume.family === "damage") {
    // CombatFacetFeeds(emitManner, triggerManner): a general "any" trigger takes either;
    // otherwise the manners must match.
    if (consume.manner && consume.manner !== "any" && emit.manner && emit.manner !== consume.manner)
      return false;
    // A self-watching damage trigger is same-card-only — never fed cross-card.
    if (consume.isSelf) return false;
  }
  return true;
}
