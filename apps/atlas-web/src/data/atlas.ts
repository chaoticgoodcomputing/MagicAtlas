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
//   useCardNeighbours→ portRows filtered by family-set + side (emit/consume),
//                      each candidate carrying its live fidelity `tier`
//                      (empty/null → a neutral Amber fallback)
//   useDeckAnalysis  → analyzeDeck(cards) — decklist → coverage/rings/near-miss
//   useOracle        → cardRows.oracleText + portRows.spans, reconstructed into
//                      highlighted segments. Falls back to the hand-authored
//                      ORACLE map until MAST's char offsets are reseeded (spans
//                      are null on every port today → fallback is what renders).
//
// STILL MOCK (no resolver yet):
//   useTiers         → the four fidelity tiers are display metadata, not data.
// ─────────────────────────────────────────────────────────────────────────────

import { useMemo } from "react";
import { useQuery } from "@apollo/client";

import {
  ANALYZE_DECK_QUERY,
  ARCHETYPES_QUERY, FAMILY_CARDS_QUERY, FAMILY_GRAPH_QUERY, HEADLINE_STATS_QUERY, TIER_COUNTS_QUERY,
  ORACLE_SPANS_QUERY,
  PORT_CANDIDATES_QUERY, DECK_PORTS_QUERY, CARD_FORWARD_QUERY,
  CARD_PROFILE_QUERY, CARD_COMBOS_QUERY, CARD_ANCHOR_QUERY, RULINGS_QUERY,
} from "../queries";
import { FLOW_FEEDERS, FLOW_DRAINS, feeds, type PortFacets } from "./flowArms";
import {
  DECKS, FAMCARDS, HEADLINE_STATS, ORACLE,
  TIERS, tierRank,
  edgeKey, ensureFamily,
  type Archetype, type Candidate, type CoverRow, type CoverSide, type Edge,
  type Family, type NearMiss, type NearMissCand, type OracleCard, type OracleSeg,
  type Ring, type Side, type Tier,
} from "./mock";

/** Uniform result envelope so views can render loading/empty without caring
 *  whether the source is mock or a live query. */
export interface AtlasResult<T> {
  data: T;
  loading: boolean;
  error: Error | null;
}

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
// LIVE (with a mock fallback). The card's full newline-preserving oracleText
// plus every port's char-offset spans reconstruct the highlighted segment list
// OracleText.tsx consumes. Each port carries `spans: int[][]` — every [start,end)
// pair is an offset into the *whole* oracleText (oracleLineIndex is carried but
// the offsets are already absolute over the multi-line text, so we build one
// flat segment list over the full string rather than per line).
//
// MAST's char offsets are dormant ([JsonIgnore]) until the pipeline is reseeded,
// so `spans` is null on every port today. When no card resolves, or no port has
// any span yet, we fall back to the hand-authored ORACLE map so the Card
// Explorer / Design System keep rendering. After reseed, spanned cards switch to
// live highlighting automatically; cards MAST hasn't spanned still fall back.

interface OraclePortRow {
  family: string;
  side: string;
  label: string;
  oracleLineIndex: number;
  spans: number[][] | null;
}

/** The resource an intercept (replacement) watches, from its label: a
 *  `replace:token-creation` intercept is *about* tokens even though its own
 *  family is the inert `replacement`. Returns the leading resource segment
 *  (`replace:token-creation` → `token`, `replace:life-gain` → `life`) so the
 *  clause can highlight in that resource's hue. Null when not a `replace:` label. */
function interceptedFamily(label: string): string | null {
  const m = /^replace:([a-z]+)/.exec(label);
  return m ? m[1] : null;
}
interface OracleCardRow { oracleText: string | null; typeLine: string | null; }

/** One resolved span: an absolute [start,end) range over the full oracle text,
 *  tagged with the port family + role it highlights. */
interface SpanEntry { start: number; end: number; role: Side; fam: string; }

/** Split `text` at the (already family-tagged) spans into the alternating
 *  plain/highlighted segment list OracleText renders. Overlapping spans keep the
 *  first and skip the rest; out-of-range / empty spans are dropped defensively. */
function segsFromSpans(text: string, entries: SpanEntry[]): OracleSeg[] {
  const clean = entries
    .filter((e) => e.start >= 0 && e.end <= text.length && e.start < e.end)
    .sort((a, b) => a.start - b.start || a.end - b.end);

  const segs: OracleSeg[] = [];
  let cursor = 0;
  for (const e of clean) {
    if (e.start < cursor) continue; // overlaps a kept span — skip defensively
    if (e.start > cursor) segs.push({ t: text.slice(cursor, e.start) });
    segs.push({ t: text.slice(e.start, e.end), role: e.role, fam: e.fam });
    cursor = e.end;
  }
  if (cursor < text.length) segs.push({ t: text.slice(cursor) });
  return segs;
}

/** Build an OracleCard from live rows, or null if the card / its spans are not
 *  available yet (signals the caller to fall back to the mock ORACLE map). Only
 *  canonical resource families are highlighted — non-flow projections (evasion,
 *  modify, replacement, …) are not interaction clauses and would mislabel plain
 *  keyword / rider text. Overlaps keep the earliest-starting span (ties → the
 *  narrower one); genuinely coincident spans (e.g. mana+sacrifice on one cost)
 *  are a data smell the pipeline's cost-span split resolves. */
function oracleFromLive(
  card: OracleCardRow | undefined,
  ports: OraclePortRow[],
  canonical: ReadonlySet<string>,
): OracleCard | null {
  const text = card?.oracleText;
  if (!text) return null; // no live card → fall back

  const entries: SpanEntry[] = [];
  for (const p of ports) {
    if (!p.spans) continue; // span-less port (dormant offsets) — nothing to draw
    // An intercept (replacement) carries the inert `replacement` family, but the
    // clause reads as consuming the resource it watches (a token doubler's "if
    // one or more tokens would be created" is, in flow terms, a token input). Map
    // it to a consume highlight on the intercepted resource so a doubler line
    // shows two clauses — consume:token (the condition) + emit:token (the create).
    let role: Side;
    let fam: string;
    if (p.side === "intercept") {
      const watched = interceptedFamily(p.label);
      if (!watched || !canonical.has(watched)) continue; // intercepts a non-flow event — skip
      role = "consume";
      fam = watched;
    } else {
      if (!canonical.has(p.family)) continue; // non-canonical projection — not an interaction clause
      role = p.side === "emit" ? "emit" : "consume";
      fam = p.family;
    }
    for (const span of p.spans) {
      if (!span || span.length < 2) continue;
      entries.push({ start: span[0], end: span[1], role, fam });
    }
  }
  if (entries.length === 0) return null; // no canonical spans → fall back

  return { type: card.typeLine ?? "", segs: segsFromSpans(text, entries) };
}

export function useOracle(cardName: string): AtlasResult<OracleCard | null> {
  const canonical = useCanonicalFamilies();
  const { data, loading, error } = useQuery(ORACLE_SPANS_QUERY, {
    variables: { card: cardName },
    skip: !cardName,
  });

  const oracle = useMemo<OracleCard | null>(() => {
    const atlas = data?.discover?.atlas;
    const card: OracleCardRow | undefined = atlas?.cardRows?.nodes?.[0];
    const ports: OraclePortRow[] = atlas?.portRows?.nodes ?? [];
    return oracleFromLive(card, ports, canonical) ?? ORACLE[cardName] ?? null;
  }, [data, cardName, canonical]);

  return { data: oracle, loading, error: error ?? null };
}

/** Neutral fallback for ports whose tier is empty/null (pre-reseed, or any port
 *  MAST hasn't tiered): surface them at the middle of the ladder rather than
 *  showing a blank tier. The tier ladder is Green < Amber < Inferred < Declared;
 *  "Amber" is that neutral middle. */
const FALLBACK_TIER: Tier = "Amber";

/** The four known tiers, used to validate the live `tier` string before we cast
 *  it — an unrecognized value falls back to FALLBACK_TIER too. */
const KNOWN_TIERS = new Set<string>(["Green", "Amber", "Inferred", "Declared"]);

/** Coerce a live port `tier` string (nullable, possibly "" or an unknown value)
 *  into a valid Tier, falling back to the neutral middle so the UI never renders
 *  a blank tier. */
const tierOf = (raw: string | null | undefined): Tier =>
  raw && KNOWN_TIERS.has(raw) ? (raw as Tier) : FALLBACK_TIER;

/** Dedupe candidate portRows into one Candidate per card, keeping ONLY those the
 *  engine's flow matcher (`feeds`) actually connects to one of the focus card's
 *  ports on the pairing side — the ADR-0003 Stage 5 port-level match. This is what
 *  replaces the lossy family-ring re-expansion: a `token` emitter no longer shows
 *  as feeding a `cast` trigger (no flow arm), and a `noncombat` damage emitter no
 *  longer shows as feeding a `combat` self-trigger (the manner/self guard). A
 *  candidate is `via` (a bridge) when the arm crosses families (token→sacrifice),
 *  direct when it stays within one (mana→mana). Sorts best-fidelity, direct-first,
 *  by name. */
function candidatesFrom(
  data: { discover?: { atlas?: { portRows?: { nodes?: { card: string; family: string; tier?: string | null; manner?: string | null; isSelf?: boolean | null }[] } } } } | undefined,
  // The focus card's ports on the side that PAIRS with the candidates: for the
  // emitters column (candidates emit), these are the focus's consume ports; for
  // the consumers column (candidates consume), the focus's emit ports.
  focusPorts: PortFacets[],
  self: string,
  // "emit" ⇒ the candidate is the emitter (left column, feeds the focus); "consume"
  // ⇒ the candidate is the consumer (right column, drained by the focus).
  neighbourSide: "emit" | "consume",
): Candidate[] {
  const nodes = data?.discover?.atlas?.portRows?.nodes ?? [];
  const byCard = new Map<string, { family: string; tier: string | null; linkEmit: string; linkConsume: string; via: boolean }>();
  for (const n of nodes) {
    if (n.card === self) continue; // never list the focus card in its own columns
    const cand: PortFacets = { family: n.family, manner: n.manner, isSelf: n.isSelf ?? false };
    // Keep the candidate only if a real flow arm connects it to a focus port.
    let linkEmit: string | null = null;
    let linkConsume: string | null = null;
    for (const fp of focusPorts) {
      const emit = neighbourSide === "emit" ? cand : fp;
      const consume = neighbourSide === "emit" ? fp : cand;
      if (feeds(emit, consume)) {
        linkEmit = emit.family;
        linkConsume = consume.family;
        break;
      }
    }
    if (linkEmit === null || linkConsume === null) continue; // no flow arm → pruned
    const via = linkEmit !== linkConsume;
    const cur = byCard.get(n.card);
    if (cur === undefined || (cur.via && !via)) // prefer a direct arm over a bridged one
      byCard.set(n.card, { family: n.family, tier: n.tier ?? null, linkEmit, linkConsume, via });
  }
  return [...byCard.entries()]
    .map(([card, r]): Candidate => ({
      card, in: null, out: null, tier: tierOf(r.tier),
      via: r.via, port: r.family, linkEmit: r.linkEmit, linkConsume: r.linkConsume,
    }))
    .sort(
      (a, b) =>
        tierRank[a.tier] - tierRank[b.tier] ||
        Number(a.via) - Number(b.via) ||
        a.card.localeCompare(b.card),
    );
}

/** The live canonical resource-family set — the resource-graph stations
 *  (`resourceFamilyRows`), which the pipeline builds only for families in its
 *  `ResourceFamilies.Canonical` taxonomy. Deriving it here (rather than
 *  hardcoding) keeps the API the single source of truth: retune the taxonomy
 *  upstream and every canonical gate in the UI follows with no frontend edit.
 *  While the graph loads, this is the family skeleton (a rendering placeholder,
 *  not an authority). */
export function useCanonicalFamilies(): ReadonlySet<string> {
  const graph = useFamilyGraph();
  return useMemo(() => new Set(graph.data.keys), [graph.data.keys]);
}

/** The resource families a card touches on one side (dedup, in port order),
 *  restricted to the live canonical set. Non-canonical projections (evasion,
 *  modify, replacement, …) are dropped — they carry no flow, so they must not
 *  drive the columns or the interaction highlight. */
export function canonicalFamilies(
  ports: CardPort[],
  side: Side,
  canonical: ReadonlySet<string>,
): string[] {
  const out: string[] = [];
  for (const p of ports)
    if (p.side === side && canonical.has(p.family) && !out.includes(p.family))
      out.push(p.family);
  return out;
}

const uniqStr = (xs: string[]): string[] => [...new Set(xs)];

/** Explorer explore/exploit columns, resource-flow model. A card's canonical
 *  emit/consume families are matched to neighbours two ways, unioned:
 *   • direct — the same family on the opposite side (a mana emitter ↔ a mana
 *     payer);
 *   • flow-bridge — a combo-adjacent family from the live resource-edge graph,
 *     so a token emitter reaches sacrifice outlets via the `token→sacrifice`
 *     hop even though no card carries a `consume:token` port. Bridged candidates
 *     are flagged `via` (and carry the bridge family in `port`).
 *  Left = emitters that FEED this card's consume families ∪ their edge
 *  predecessors; right = consumers that DRAIN its emit families ∪ their edge
 *  successors. Non-canonical projections never participate. Returns the card's
 *  own canonical families per side so the caller can label the columns. */
export function useCardNeighbours(
  ports: CardPort[],
  self: string,
): AtlasResult<{
  emitters: Candidate[]; // feed this card's consume side (left)
  consumers: Candidate[]; // drain this card's emit side (right)
  inFams: string[]; // canonical consume families of this card
  outFams: string[]; // canonical emit families of this card
}> {
  const graph = useFamilyGraph();
  const canonical = useMemo(() => new Set(graph.data.keys), [graph.data.keys]);

  const inFams = useMemo(() => canonicalFamilies(ports, "consume", canonical), [ports, canonical]);
  const outFams = useMemo(() => canonicalFamilies(ports, "emit", canonical), [ports, canonical]);

  // The focus card's own ports (with facets) per side — the `feeds` check pairs a
  // candidate against these, so a match respects manner/self, not just family.
  const consumeFacets = useMemo<PortFacets[]>(
    () => ports.filter((p) => p.side === "consume").map((p) => ({ family: p.family, manner: p.manner, isSelf: p.isSelf })),
    [ports],
  );
  const emitFacets = useMemo<PortFacets[]>(
    () => ports.filter((p) => p.side === "emit").map((p) => ({ family: p.family, manner: p.manner, isSelf: p.isSelf })),
    [ports],
  );

  // Left query set: the EMIT families that feed this card's consume families under
  // a real flow arm (PortFlowMatcher / FLOW_FEEDERS) — NOT the lossy ring
  // predecessors. Right query set: the CONSUME families this card's emit families
  // feed (FLOW_DRAINS).
  const feederFams = useMemo(
    () => uniqStr(inFams.flatMap((f) => [...(FLOW_FEEDERS[f] ?? [])])),
    [inFams],
  );
  const drainFams = useMemo(
    () => uniqStr(outFams.flatMap((f) => [...(FLOW_DRAINS[f] ?? [])])),
    [outFams],
  );

  const emitQ = useQuery(PORT_CANDIDATES_QUERY, {
    variables: { families: feederFams, side: "emit" },
    skip: feederFams.length === 0,
  });
  const consumeQ = useQuery(PORT_CANDIDATES_QUERY, {
    variables: { families: drainFams, side: "consume" },
    skip: drainFams.length === 0,
  });

  const emitters = useMemo(
    () => (feederFams.length ? candidatesFrom(emitQ.data, consumeFacets, self, "emit") : []),
    [emitQ.data, feederFams, consumeFacets, self],
  );
  const consumers = useMemo(
    () => (drainFams.length ? candidatesFrom(consumeQ.data, emitFacets, self, "consume") : []),
    [consumeQ.data, drainFams, emitFacets, self],
  );

  return {
    data: { emitters, consumers, inFams, outFams },
    loading: graph.loading || emitQ.loading || consumeQ.loading,
    error: graph.error ?? emitQ.error ?? consumeQ.error ?? null,
  };
}

/** Third-degree hop — one card's forward (emit-side) canonical families: what it
 *  emits, i.e. what it feeds NEXT. Given a neighbour reached as `A → B`, this is
 *  the `C` in `A → B → C`. Lazy: pass `null` to skip the query (the Explorer
 *  fires it only when a neighbour row is expanded), so the third degree costs one
 *  query per opened row, not per listed card. */
export function useCardForward(name: string | null): AtlasResult<string[]> {
  const graph = useFamilyGraph();
  const canonical = useMemo(() => new Set(graph.data.keys), [graph.data.keys]);
  const q = useQuery(CARD_FORWARD_QUERY, { variables: { name }, skip: !name });
  const fams = useMemo(() => {
    const nodes: { family: string }[] = q.data?.discover?.atlas?.portRows?.nodes ?? [];
    return uniqStr(nodes.map((n) => n.family).filter((f) => canonical.has(f)));
  }, [q.data, canonical]);
  return { data: fams, loading: graph.loading || q.loading, error: q.error ?? null };
}

// ── Card profile page (views/CardExplorer.tsx) ───────────────────────────────────
// LIVE. One card's full record by name + its live ports. Everything the card
// page renders (header/imagery/oracle/ports/price/meta) flows from here; the
// combo, anchor and ruling panels layer on the sibling hooks below.

export interface CardPort {
  family: string;
  /** The port's structural role. `intercept` (a replacement) is a real third
   *  role beyond emit/consume — kept distinct so the Ports listing is honest;
   *  it matches neither column side, so it never joins the explore/exploit set. */
  side: Side | "intercept";
  tier: Tier;
  confidence: number | null;
  label: string;
  /** ADR-0003 provenance: the 0-based oracle line this port projects from, and
   *  the absolute `[start, end]` character spans of the projecting clause within
   *  the oracle text. Lets the Explorer group ports by clause and tint the span. */
  lineIndex: number;
  spans: number[][] | null;
  /** ADR-0003 structured facets — the frontend's port-level flow match keys on
   *  these (via `feeds`) instead of re-expanding lossy family-ring edges. */
  stem: string | null;
  manner: string | null;
  isSelf: boolean;
}

export interface CardProfile {
  id: string;
  oracleId: string | null;
  name: string;
  typeLine: string | null;
  manaCost: string | null;
  oracleText: string | null;
  imageUriNormal: string | null;
  imageUriLarge: string | null;
  priceUsd: number | null;
  edhrecRank: number | null;
  scryfallUri: string | null;
  colors: string[];
  keywords: string[];
  ports: CardPort[];
}

interface CardProfileCardRow {
  id: string; oracleId: string | null; name: string;
  typeLine: string | null; manaCost: string | null; oracleText: string | null;
  imageUriNormal: string | null; imageUriLarge: string | null;
  priceUsd: number | null; edhrecRank: number | null; scryfallUri: string | null;
  colors: string[] | null; keywords: string[] | null;
}
interface CardProfilePortRow {
  family: string; side: string; tier: string | null;
  confidence: number | null; label: string;
  oracleLineIndex: number | null; spans: number[][] | null;
  stem: string | null; manner: string | null; isSelf: boolean | null;
}

export function useCardProfile(name: string): AtlasResult<CardProfile | null> {
  const { data, loading, error } = useQuery(CARD_PROFILE_QUERY, {
    variables: { name },
    skip: !name,
  });

  const profile = useMemo<CardProfile | null>(() => {
    const atlas = data?.discover?.atlas;
    const row: CardProfileCardRow | undefined = atlas?.cardRows?.nodes?.[0];
    if (!row) return null;
    const portRows: CardProfilePortRow[] = atlas?.portRows?.nodes ?? [];
    const ports: CardPort[] = portRows.map((p) => ({
      family: p.family,
      side: p.side === "emit" ? "emit" : p.side === "intercept" ? "intercept" : "consume",
      tier: tierOf(p.tier),
      confidence: p.confidence,
      label: p.label,
      lineIndex: p.oracleLineIndex ?? 0,
      spans: p.spans ?? null,
      stem: p.stem ?? null,
      manner: p.manner ?? null,
      isSelf: p.isSelf ?? false,
    }));
    return {
      id: row.id, oracleId: row.oracleId, name: row.name,
      typeLine: row.typeLine, manaCost: row.manaCost, oracleText: row.oracleText,
      imageUriNormal: row.imageUriNormal, imageUriLarge: row.imageUriLarge,
      priceUsd: row.priceUsd, edhrecRank: row.edhrecRank, scryfallUri: row.scryfallUri,
      colors: row.colors ?? [], keywords: row.keywords ?? [],
      ports,
    };
  }, [data]);

  return { data: profile, loading, error: error ?? null };
}

// ── Card combos (views/CardExplorer.tsx) ─────────────────────────────────────────
// LIVE. Named combos the card is in. `cards` is a " + "-joined string filtered
// by substring `contains`, so we re-check the exact name after splitting to drop
// substring false-positives (a shorter name embedded in another card's name).

export interface CardCombo {
  comboId: string;
  cards: string[];
  familyRing: string;
  tier: Tier;
  popularity: number;
}

interface CardComboRow {
  comboId: string; cards: string; familyRing: string; tier: string; popularity: number;
}

const splitComboCards = (cards: string): string[] =>
  cards.split(" + ").map((c) => c.trim()).filter(Boolean);

export function useCardCombos(name: string): AtlasResult<CardCombo[]> {
  const { data, loading, error } = useQuery(CARD_COMBOS_QUERY, {
    variables: { name },
    skip: !name,
  });

  const combos = useMemo<CardCombo[]>(() => {
    const rows: CardComboRow[] | undefined = data?.discover?.atlas?.comboRows?.nodes;
    if (!rows) return [];
    return rows
      .map((r): CardCombo => ({
        comboId: r.comboId,
        cards: splitComboCards(r.cards),
        familyRing: r.familyRing,
        tier: r.tier as Tier,
        popularity: r.popularity,
      }))
      // Drop substring false-positives: keep only combos that actually list the
      // exact card name as one of their parts.
      .filter((c) => c.cards.includes(name));
  }, [data, name]);

  return { data: combos, loading, error: error ?? null };
}

// ── Card anchor (views/CardExplorer.tsx) ─────────────────────────────────────────
// LIVE. Present only when the card is a blocker: how many combos it blocks / is
// sole blocker for, plus its co-stars (cards it most often blocks alongside).

export interface CardCoStar {
  card: string; sharedCombos: number; sharedPopularity: number; alsoUnparsed: boolean;
}
export interface CardAnchor {
  card: string;
  blockedComboCount: number;
  soleBlockerCount: number;
  popularityMass: number;
  maxComboPopularity: number;
  coStars: CardCoStar[];
}

interface CardAnchorRow {
  card: string; blockedComboCount: number; soleBlockerCount: number;
  popularityMass: number; maxComboPopularity: number; coStars: CardCoStar[] | null;
}

export function useCardAnchor(name: string): AtlasResult<CardAnchor | null> {
  const { data, loading, error } = useQuery(CARD_ANCHOR_QUERY, {
    variables: { name },
    skip: !name,
  });

  const anchor = useMemo<CardAnchor | null>(() => {
    const row: CardAnchorRow | undefined = data?.discover?.atlas?.comboAnchorRows?.nodes?.[0];
    if (!row) return null;
    return { ...row, coStars: row.coStars ?? [] };
  }, [data]);

  return { data: anchor, loading, error: error ?? null };
}

// ── Card rulings (views/CardExplorer.tsx) ────────────────────────────────────────
// LIVE. Scryfall rulings for the card's oracle id, oldest first.

export interface CardRuling { id: string; source: string; publishedAt: string; comment: string; }

interface CardRulingRow { id: string; source: string; publishedAt: string; comment: string; }

export function useCardRulings(oracleId: string | null | undefined): AtlasResult<CardRuling[]> {
  const { data, loading, error } = useQuery(RULINGS_QUERY, {
    variables: { oracleId },
    skip: !oracleId,
  });

  const rulings = useMemo<CardRuling[]>(() => {
    const rows: CardRulingRow[] | undefined = data?.discover?.atlas?.rulingRows?.nodes;
    return rows ?? [];
  }, [data]);

  return { data: rulings, loading, error: error ?? null };
}

// ── Deck resolver (Deck lens) ────────────────────────────────────────────────
// LIVE. discover.atlas.analyzeDeck(cards) resolves a decklist server-side to
// directional port coverage + the complete rings it already makes + the
// near-miss closers one card away. The resolver rows map almost 1:1 onto the
// view's CoverRow/Ring/NearMiss shapes; the only reshaping is:
//   · coverage row `note` → the emit side's `note` (where the chart reads it),
//     and `subs: {family,count}[]` → the view's `[family, count][]` tuples;
//   · rings deduped by (cards, ring) keeping the highest-pop instance, and
//     `confidence` → `conf`;
//   · nearMiss capped to the strongest few closers.

/** How many near-miss closers to surface (the resolver returns the full ranked
 *  list; the UI only wants the top handful). */
const NEAR_MISS_CAP = 8;

interface DeckCoverSideRow { own: number; subs: { family: string; count: number }[]; }
interface DeckCoverageRow {
  family: string; note: string | null;
  emit: DeckCoverSideRow; consume: DeckCoverSideRow;
}
interface DeckRingRow { cards: string; ring: string; tier: string; pop: number; confidence: number | null; }
interface DeckNearMissRow {
  missing: string; ring: string; resultTier: string;
  cands: { name: string; evidence: string; price: string; score: number }[];
}
interface DeckAnalysisRow {
  coverage: DeckCoverageRow[]; rings: DeckRingRow[]; nearMiss: DeckNearMissRow[];
}

const toCoverSide = (s: DeckCoverSideRow, note?: string | null): CoverSide => ({
  own: s.own,
  ...(s.subs.length ? { subs: s.subs.map((x): [string, number] => [x.family, x.count]) } : {}),
  ...(note ? { note } : {}),
});

const EMPTY_DECK = { coverage: [] as CoverRow[], rings: [] as Ring[], nearMiss: [] as NearMiss[] };

export function useDeckAnalysis(cards: string[]): AtlasResult<{
  coverage: CoverRow[];
  rings: Ring[];
  nearMiss: NearMiss[];
}> {
  const { data, loading, error } = useQuery(ANALYZE_DECK_QUERY, {
    variables: { cards },
    skip: cards.length === 0,
  });

  const result = useMemo(() => {
    const a: DeckAnalysisRow | undefined = data?.discover?.atlas?.analyzeDeck;
    if (!a) return EMPTY_DECK;

    // note rides on the EMIT side, which is where the coverage chart reads it.
    const coverage: CoverRow[] = a.coverage.map((r): CoverRow => [
      r.family, toCoverSide(r.emit, r.note), toCoverSide(r.consume),
    ]);

    // Collapse rings that share the same card pair + family ring (the resolver
    // can emit several combos behind one visible line); keep the most-popular.
    const seen = new Set<string>();
    const rings: Ring[] = [];
    for (const r of a.rings) {
      const k = `${r.cards}|${r.ring}`;
      if (seen.has(k)) continue;
      seen.add(k);
      rings.push({
        cards: r.cards, ring: r.ring, tier: r.tier as Tier, pop: r.pop,
        ...(r.confidence != null ? { conf: r.confidence } : {}),
      });
    }

    const nearMiss: NearMiss[] = a.nearMiss.slice(0, NEAR_MISS_CAP).map((nm): NearMiss => ({
      missing: nm.missing,
      ring: nm.ring,
      resultTier: nm.resultTier as Tier,
      cands: nm.cands.map((c): NearMissCand => ({
        name: c.name, evidence: c.evidence, price: c.price, score: c.score,
      })),
    }));

    return { coverage, rings, nearMiss };
  }, [data]);

  return { data: result, loading, error: error ?? null };
}

export const sampleDeck = (state: "full" | "sparse") => DECKS[state];

// ── Deck ports (Deck Synergy web) ────────────────────────────────────────────
// LIVE. Every port for a set of card names (a pasted decklist), one row per
// (card, family, side). The Synergy view aggregates these into per-card
// consume/emit family sets and wires the deck as an emit→consume port graph,
// so every node carries a real full card name (a live CardLink, never a mock
// short-form). `tier` is coerced defensively; `side` is passed through verbatim
// ("emit" | "consume" | "" for a side-less inferred/backfill port).

export interface DeckPortRow {
  card: string;
  family: string;
  side: string; // "emit" | "consume" | "" (side-less inferred port)
  tier: Tier;
  confidence: number | null;
  label: string;
}

interface RawDeckPortRow {
  card: string; family: string; side: string | null;
  tier: string | null; confidence: number | null; label: string | null;
}

export function useDeckPorts(cards: string[]): AtlasResult<DeckPortRow[]> {
  const { data, loading, error } = useQuery(DECK_PORTS_QUERY, {
    variables: { cards },
    skip: cards.length === 0,
  });

  const ports = useMemo<DeckPortRow[]>(() => {
    const rows: RawDeckPortRow[] | undefined = data?.discover?.atlas?.portRows?.nodes;
    if (!rows) return [];
    return rows.map((r): DeckPortRow => ({
      card: r.card,
      family: r.family,
      side: r.side ?? "",
      tier: tierOf(r.tier),
      confidence: r.confidence,
      label: r.label ?? "",
    }));
  }, [data]);

  return { data: ports, loading, error: error ?? null };
}

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
// The four tier tokens are static design metadata, but their `vol` (legend
// volume) is live: the count of ports at each fidelity. Falls back to the
// authored placeholder volumes only while the query is in flight.
export function useTiers(): AtlasResult<typeof TIERS> {
  const { data, loading, error } = useQuery(TIER_COUNTS_QUERY);

  const tiers = useMemo(() => {
    const a = data?.discover?.atlas;
    if (!a) return TIERS;
    const count: Record<Tier, number | undefined> = {
      Green: a.green?.totalCount,
      Amber: a.amber?.totalCount,
      Inferred: a.inferred?.totalCount,
      Declared: a.declared?.totalCount,
    };
    return TIERS.map((t) => ({ ...t, vol: fmt(count[t.key]) }));
  }, [data]);

  return { data: tiers, loading, error: error ?? null };
}

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
