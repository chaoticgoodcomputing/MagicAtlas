// ─────────────────────────────────────────────────────────────────────────────
// The Atlas data-access seam.
//
// Every concept view reads its data through the hooks here — never from
// `./mock` directly. The hooks whose backing datasets have a live GraphQL
// surface (resource families/edges, archetypes, ports, headline counts) now
// query the real API through Apollo; the rest still resolve against the mock
// corpus, annotated with the resolver they wait on. Flipping a view onto live
// data is a change *in this file only* — every hook keeps the exact
// `AtlasResult<T>` envelope and data shape the views already consume.
//
// See docs/design/upstream-atlas-data-plan.md for the API + pipeline + MAST
// work these bindings depend on.
//
// LIVE (query the API):
//   useFamilyGraph   → resourceFamilyRows + resourceEdgeRows
//   useStation       → live family graph + portRows (top cards per family)
//   useArchetypes    → archetypeRows(order: realizingCombos DESC)
//   useHeadlineStats → totalCount of cards / combos / ports / families / edges / archetypes
//   useCardNeighbours→ portRows filtered by family-set + side (emit/consume)
//
// STILL MOCK (no resolver yet):
//   useDeckAnalysis  → needs the analyzeDeck resolver (Track C); joining 3.4k
//                      combos client-side is a non-starter, so it stays mock.
//   useOracle        → MAST oracle char-offset spans are dormant ([JsonIgnore]),
//                      so there are no live port spans to highlight yet.
//   useTiers         → the four fidelity tiers are display metadata, not data.
// ─────────────────────────────────────────────────────────────────────────────

import { useMemo } from "react";
import { useQuery } from "@apollo/client";

import {
  ARCHETYPES_QUERY, FAMILY_CARDS_QUERY, FAMILY_GRAPH_QUERY, HEADLINE_STATS_QUERY,
  PORT_CANDIDATES_QUERY,
} from "../queries";
import {
  CARDPOOL, COVERAGE, DECKS, FAMCARDS, GROUPS, HEADLINE_STATS, NEARMISS, ORACLE,
  PORTS, RINGS, TIERS,
  edgeKey, ensureFamily, supergroupsOf,
  type Archetype, type Candidate, type CoverRow, type Edge, type Family,
  type NearMiss, type OracleCard, type Port, type Ring, type Tier,
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
// resourceFamilyRows { family cards labels } + resourceEdgeRows
// { fromFamily toFamily realizingCombos bestTier engine origin } → the metro map.
//
// The live family set differs from the hand-guessed FAM palette (it carries
// blink/cast/etb/dice/phase/recur and drops card/mill/exile/…). `ensureFamily`
// merges each live family into the palette — known families keep their tuned
// hue + coordinates, unknown ones get a deterministic hue + ring-placed coords.
//
// The metro view seeds its drag positions from `keys` exactly once, so the key
// set must be identical on the loading and loaded renders. We therefore return a
// stable skeleton of the real family set while the query is in flight, then let
// the live rows refine each family's counts and add the edges. The skeleton also
// registers every family into FAM up front so the views' direct `famHue` calls
// resolve synthesized families immediately.
const FAMILY_SKELETON = [
  "mana", "token", "sacrifice", "death", "counter", "damage", "life", "tap",
  "combat", "copy", "untap", "blink", "cast", "dice", "etb", "phase", "recur",
];

interface FamilyGraph { families: Record<string, Family>; keys: string[]; edges: Edge[]; }

const skeletonGraph = (): FamilyGraph => {
  const families: Record<string, Family> = {};
  const keys: string[] = [];
  for (const name of FAMILY_SKELETON) {
    families[name] = { ...ensureFamily(name), cards: 0, labels: 0 };
    keys.push(name);
  }
  return { families, keys, edges: [] };
};

// Built once at module load so FAM carries every synthesized family before any
// view renders (the views call famHue(...) against FAM directly).
const SKELETON = skeletonGraph();

interface FamilyRow { family: string; cards: number; labels: number; }
interface EdgeRow {
  fromFamily: string; toFamily: string; realizingCombos: number;
  bestTier: string; engine: boolean; origin: string | null;
}

const toEdge = (e: EdgeRow): Edge => ({
  from: e.fromFamily,
  to: e.toFamily,
  combos: e.realizingCombos,
  tier: e.bestTier as Tier,
  engine: e.engine,
  origin: e.origin === "card" ? "card" : "rules",
});

export function useFamilyGraph(): AtlasResult<FamilyGraph> {
  const { data, loading, error } = useQuery(FAMILY_GRAPH_QUERY);

  const graph = useMemo<FamilyGraph>(() => {
    const atlas = data?.discover?.atlas;
    const familyRows: FamilyRow[] | undefined = atlas?.resourceFamilyRows?.nodes;
    const edgeRows: EdgeRow[] | undefined = atlas?.resourceEdgeRows?.nodes;
    if (!familyRows || familyRows.length === 0) return SKELETON;

    const families: Record<string, Family> = {};
    const keys: string[] = [];
    for (const r of familyRows) {
      families[r.family] = { ...ensureFamily(r.family), name: r.family, cards: r.cards, labels: r.labels };
      keys.push(r.family);
    }
    const edges = (edgeRows ?? []).map(toEdge);
    return { families, keys, edges };
  }, [data]);

  return { data: graph, loading, error: error ?? null };
}

/** One family's one-hop neighbourhood + its top cards (Station focus). */
export function useStation(family: string): AtlasResult<{
  family: Family;
  neighbours: { edge: Edge; fam: string; dir: "out" | "in" }[];
  topCards: string[];
}> {
  const graph = useFamilyGraph();
  // Top cards per family from portRows; distinct card names, capped for the rail.
  const { data: cardData } = useQuery(FAMILY_CARDS_QUERY, { variables: { family } });

  const { families, edges } = graph.data;

  const neighbours = useMemo(
    () => edges.flatMap<{ edge: Edge; fam: string; dir: "out" | "in" }>((e) => {
      if (e.from === family) return [{ edge: e, fam: e.to, dir: "out" }];
      if (e.to === family) return [{ edge: e, fam: e.from, dir: "in" }];
      return [];
    }),
    [edges, family],
  );

  const topCards = useMemo<string[]>(() => {
    const nodes: { card: string }[] | undefined = cardData?.discover?.atlas?.portRows?.nodes;
    if (nodes && nodes.length) {
      const seen = new Set<string>();
      for (const n of nodes) { if (!seen.has(n.card)) seen.add(n.card); if (seen.size >= 8) break; }
      return [...seen];
    }
    return FAMCARDS[family] ?? []; // fallback until the family's ports land
  }, [cardData, family]);

  return {
    data: { family: families[family], neighbours, topCards },
    loading: graph.loading,
    error: graph.error,
  };
}

// ── Card ports + oracle spans (Card explorer, Oracle showcase) ───────────────
// STILL MOCK. MAST emits port structure but its oracle char-offset spans are
// dormant ([JsonIgnore]); until they land there is no live span data to
// highlight, so the hand-authored ORACLE segments stand in.
export function useOracle(cardName: string): AtlasResult<OracleCard | null> {
  return ready(ORACLE[cardName] ?? null);
}

/** Live portRows have no tier yet; candidates surface at the neutral middle. */
const CANDIDATE_TIER: Tier = "Amber";

/** Dedupe portRows into one Candidate per card, preferring an exact-family port
 *  over a super/subgroup one, and flagging lattice matches as `via`. */
function candidatesFrom(
  data: { discover?: { atlas?: { portRows?: { nodes?: { card: string; family: string }[] } } } } | undefined,
  queriedFam: string,
): Candidate[] {
  const nodes = data?.discover?.atlas?.portRows?.nodes ?? [];
  const byCard = new Map<string, string>(); // card → chosen port family
  for (const n of nodes) {
    const cur = byCard.get(n.card);
    if (cur === undefined || (cur !== queriedFam && n.family === queriedFam)) {
      byCard.set(n.card, n.family);
    }
  }
  return [...byCard.entries()]
    .map(([card, port]): Candidate => ({
      card, in: null, out: null, tier: CANDIDATE_TIER,
      via: port !== queriedFam, port,
    }))
    .sort((a, b) => Number(a.via) - Number(b.via) || a.card.localeCompare(b.card));
}

/** Explorer left/right columns: emitters of what a card consumes, consumers of
 *  what it emits (supergroup matches flagged). The card's own consume/emit
 *  families come from the mock CARDPOOL; the candidate lists are live portRows.
 *
 *  - emitters: ports on the EMIT side whose family is the card's consume family
 *    or a subgroup of it (a subgroup emit satisfies the supergroup consume).
 *  - consumers: ports on the CONSUME side whose family is the card's emit family
 *    or a supergroup of it.
 *  `via` marks a candidate matched through the super/subgroup lattice rather
 *  than the family itself. (Mock CARDPOOL families with no live equivalent —
 *  e.g. "card" — resolve to empty lists.) */
// TODO(api:portRows for the queried card): source the card's own in/out ports
// live too, replacing the CARDPOOL lookup.
export function useCardNeighbours(cardName: string): AtlasResult<{
  card: (typeof CARDPOOL)[number] | undefined;
  emitters: Candidate[]; // emit what this card consumes  → feed its consume side
  consumers: Candidate[]; // consume what this card emits → drain its emit side
}> {
  const card = CARDPOOL.find((c) => c.card === cardName);
  const inFam = card?.in ?? null; // family this card consumes
  const outFam = card?.out ?? null; // family this card emits

  // Emitters: EMIT-side ports in {inFam} ∪ subgroups(inFam).
  const emitFamilies = inFam ? [inFam, ...(GROUPS[inFam] ?? [])] : [];
  const emitQ = useQuery(PORT_CANDIDATES_QUERY, {
    variables: { families: emitFamilies, side: "emit" },
    skip: !inFam,
  });

  // Consumers: CONSUME-side ports in {outFam} ∪ supergroups(outFam).
  const consumeFamilies = outFam ? supergroupsOf(outFam) : [];
  const consumeQ = useQuery(PORT_CANDIDATES_QUERY, {
    variables: { families: consumeFamilies, side: "consume" },
    skip: !outFam,
  });

  const emitters = useMemo(
    () => (inFam ? candidatesFrom(emitQ.data, inFam) : []),
    [emitQ.data, inFam],
  );
  const consumers = useMemo(
    () => (outFam ? candidatesFrom(consumeQ.data, outFam) : []),
    [consumeQ.data, outFam],
  );

  return {
    data: { card, emitters, consumers },
    loading: emitQ.loading || consumeQ.loading,
    error: emitQ.error ?? consumeQ.error ?? null,
  };
}

// ── Deck resolver (Deck lens, Synergy web) ───────────────────────────────────
export type DeckState = "empty" | "loading" | "sparse" | "full";

// STILL MOCK. Needs the analyzeDeck resolver (Track C) to resolve a decklist to
// coverage + rings + near-miss closers server-side — joining the combo corpus on
// the client is a non-starter.
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
interface ArchetypeRow {
  signature: string; families: string; familyCount: number; realizingCombos: number;
  bestTier: string; greenFraction: number; exampleCards: string;
}

export function useArchetypes(): AtlasResult<Archetype[]> {
  const { data, loading, error } = useQuery(ARCHETYPES_QUERY);

  const archetypes = useMemo<Archetype[]>(() => {
    const rows: ArchetypeRow[] | undefined = data?.discover?.atlas?.archetypeRows?.nodes;
    if (!rows) return [];
    return rows.map((r): Archetype => ({
      sig: r.signature,
      combos: r.realizingCombos,
      tier: r.bestTier as Tier,
      fam: (r.families.split(",")[0] ?? "").trim(),
    }));
  }, [data]);

  return { data: archetypes, loading, error: error ?? null };
}

// Display metadata, not corpus data — stays local.
export function useTiers(): AtlasResult<typeof TIERS> { return ready(TIERS); }

const fmt = (n: number | undefined): string => (n ?? 0).toLocaleString();

export function useHeadlineStats(): AtlasResult<typeof HEADLINE_STATS> {
  const { data, loading, error } = useQuery(HEADLINE_STATS_QUERY);

  const stats = useMemo(() => {
    const a = data?.discover?.atlas;
    if (!a) return HEADLINE_STATS; // mock placeholder until counts arrive
    return [
      { value: fmt(a.cardRows?.totalCount), label: "cards parsed" },
      { value: fmt(a.comboRows?.totalCount), label: "combos reconstructed" },
      { value: `${fmt(a.resourceFamilyRows?.totalCount)} · ${fmt(a.resourceEdgeRows?.totalCount)}`, label: "families · lines" },
      { value: fmt(a.archetypeRows?.totalCount), label: "archetypes realized" },
    ];
  }, [data]);

  return { data: stats, loading, error: error ?? null };
}

export { edgeKey };
export type { Edge, Tier };
