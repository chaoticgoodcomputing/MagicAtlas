// ─────────────────────────────────────────────────────────────────────────────
// The Atlas data-access seam.
//
// Every concept view reads its data through the hooks here — never from
// `./mock` directly. Today each hook resolves synchronously against the mock
// corpus; each is annotated with the GraphQL field / endpoint it will bind to
// once the port/family/combo datasets get an API surface. Flipping a view onto
// live data is then a change *in this file only*.
//
// See docs/design/upstream-atlas-data-plan.md for the API + pipeline + MAST
// work these bindings depend on. The `TODO(api:…)` markers below name the
// concrete GraphQL field each hook will target.
// ─────────────────────────────────────────────────────────────────────────────

import {
  ARCHETYPES, CARDPOOL, COVERAGE, DECKS, EDGES, FAM, FAMCARDS, FAMILY_KEYS,
  HEADLINE_STATS, NEARMISS, ORACLE, PORTS, RINGS, TIERS,
  consumersOf, edgeKey, emittersOf,
  type Candidate, type CoverRow, type Edge, type Family, type NearMiss,
  type OracleCard, type Port, type Ring, type Tier,
} from "./mock";

/** Uniform result envelope so views can render loading/empty without caring
 *  whether the source is mock or a live query. */
export interface AtlasResult<T> {
  data: T;
  loading: boolean;
  error: Error | null;
}

const ready = <T,>(data: T): AtlasResult<T> => ({ data, loading: false, error: null });

// ── Family graph (Metro map, Station focus) ──────────────────────────────────
// TODO(api:resourceFamilyRows + resourceEdgeRows): the 17-family lattice and the
// directional, tiered, origin-tagged edges from resource-graph.json.
export function useFamilyGraph(): AtlasResult<{ families: Record<string, Family>; keys: string[]; edges: Edge[] }> {
  return ready({ families: FAM, keys: FAMILY_KEYS, edges: EDGES });
}

/** One family's one-hop neighbourhood + its top cards (Station focus). */
export function useStation(family: string): AtlasResult<{
  family: Family;
  neighbours: { edge: Edge; fam: string; dir: "out" | "in" }[];
  topCards: string[];
}> {
  const neighbours = EDGES.flatMap<{ edge: Edge; fam: string; dir: "out" | "in" }>((e) => {
    if (e.from === family) return [{ edge: e, fam: e.to, dir: "out" }];
    if (e.to === family) return [{ edge: e, fam: e.from, dir: "in" }];
    return [];
  });
  // TODO(api:portRows aggregated by family): top cards per family.
  return ready({ family: FAM[family], neighbours, topCards: FAMCARDS[family] ?? [] });
}

// ── Card ports + oracle spans (Card explorer, Oracle showcase) ───────────────
// TODO(api:portRows with oracle char-offset spans from MAST): today the ORACLE
// segments are hand-authored; MAST must emit [start,end) offsets per port.
export function useOracle(cardName: string): AtlasResult<OracleCard | null> {
  return ready(ORACLE[cardName] ?? null);
}

/** Explorer left/right columns: emitters of what a card consumes, consumers of
 *  what it emits (supergroup matches flagged). */
// TODO(api:candidates(family, side, limit)): server-side ranked candidate lists.
export function useCardNeighbours(cardName: string): AtlasResult<{
  card: (typeof CARDPOOL)[number] | undefined;
  emitters: Candidate[]; // emit what this card consumes  → feed its consume side
  consumers: Candidate[]; // consume what this card emits → drain its emit side
}> {
  const card = CARDPOOL.find((c) => c.card === cardName);
  const emitters = card?.in ? emittersOf(card.in) : [];
  const consumers = card?.out ? consumersOf(card.out) : [];
  return ready({ card, emitters, consumers });
}

// ── Deck resolver (Deck lens, Synergy web) ───────────────────────────────────
export type DeckState = "empty" | "loading" | "sparse" | "full";

// TODO(api:POST /deck/analyze): resolve a decklist to coverage + rings +
// near-miss closers server-side (do not join 95k combos on the client).
export function useDeckAnalysis(state: Exclude<DeckState, "empty" | "loading">): AtlasResult<{
  coverage: CoverRow[];
  rings: Ring[];
  nearMiss: NearMiss[];
  ports: Port[];
}> {
  const cov = state === "full" ? COVERAGE.dense : COVERAGE.sparse;
  return ready({ coverage: cov, rings: RINGS[state], nearMiss: NEARMISS[state], ports: PORTS });
}

export const sampleDeck = (state: "full" | "sparse") => DECKS[state];

// ── Archetypes + tiers + headline (Cover, Design system) ─────────────────────
export function useArchetypes(): AtlasResult<typeof ARCHETYPES> { return ready(ARCHETYPES); }
export function useTiers(): AtlasResult<typeof TIERS> { return ready(TIERS); }
export function useHeadlineStats(): AtlasResult<typeof HEADLINE_STATS> { return ready(HEADLINE_STATS); }

export { edgeKey };
export type { Edge, Tier };
