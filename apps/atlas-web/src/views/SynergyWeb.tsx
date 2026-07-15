// ─────────────────────────────────────────────────────────────────────────────
// 04 · Deck Synergy Web — the deck as a PORT graph (exploiter view).
//
// LIVE. A pasted decklist → one node per card, built from the card's real
// `portRows` (consume/emit families). Nodes live in a columnar lattice derived
// entirely from the live data:
//   · COLUMN  = the primary consume family's SUPERGROUP (via the GROUPS lattice —
//               e.g. a `sacrifice` consumer sits under the `death` column, since
//               death ⊇ sacrifice). Source cards (no consume) get their own
//               `source` column, placed last.
//   · BAND    = the primary consume family itself (the sub-row within a column);
//               for source cards, their primary emit family.
// Every emit→consume wire is routed out the source column's RIGHT channel, DOWN
// to a staggered bottom bus, ACROSS, then UP the target column's LEFT channel —
// so no wire crosses behind a node. Wiring uses the full consume/emit family
// sets + the existing `subsumes` supergroup logic. Columns, bands and chips all
// drag-reorder (plain Math + React state + SVG pointer events, no d3).
//
// Card names are REAL and full (a live <CardLink> per chip) — no mock short-form
// names that 404 to a not-found card page.
// ─────────────────────────────────────────────────────────────────────────────

import { useEffect, useMemo, useRef, useState, type ReactNode } from "react";
import { famHue, subsumes, GROUPS, tierRank, type Tier } from "../data/mock";
import { sampleDeck, useDeckPorts, type DeckPortRow } from "../data/atlas";
import { SectionHead, SegControl, TierChip } from "../components/primitives";
import { CardLink } from "../components/CardLink";
import { orthoPath, arrowHeadPath, rotate, endAngle, type Pt } from "../lib/ortho";

// ── layout constants ─────────────────────────────────────────────────────────
const VW = 1240;
const CHIP_H = 52;
const TOP = 118;
const ROW_GAP = 66;
const SUB_GAP = 24;
const MARGIN = 76; // left/right gutter for the column band
const SRC_COL = "§src"; // synthetic column key for source (no-consume) cards
const SRC_NONE = "§none"; // band key for a source card with no emit either

// ── keys / helpers (pure) ────────────────────────────────────────────────────
const NOISE = new Set<string>(["unstructured", "unparsed", "other"]);
const collate = (s: string): string => (s === SRC_COL || s === SRC_NONE ? "￿" : s);

/** The supergroup column a consume family belongs to: the `g` whose GROUPS[g]
 *  contains `fam`, else `fam` itself (a family that is its own supergroup). */
function supergroupColumnOf(fam: string): string {
  for (const [g, subs] of Object.entries(GROUPS)) if (subs.includes(fam)) return g;
  return fam;
}

const colLabel = (col: string): string => (col === SRC_COL ? "SOURCE" : col.toUpperCase());
const subLabel = (col: string, sub: string): string =>
  col === SRC_COL
    ? sub === SRC_NONE ? "source" : "emit " + sub
    : "consume " + sub;
const subHue = (col: string, sub: string): string =>
  col === SRC_COL || sub === SRC_NONE ? "#5b5f70" : famHue(sub);

// ── the per-card node model, derived from live ports ─────────────────────────
interface LiveNode {
  card: string;
  in: string | null; // primary consume family (drives column + band)
  out: string | null; // primary emit family (for the emit dot)
  inLabel: string | null; // the port label behind the primary consume
  outLabel: string | null; // the port label behind the primary emit
  consumes: string[]; // every real consume family (for wiring)
  emits: string[]; // every real emit family (for wiring)
  extra: number; // count of secondary families not shown as a dot
  tier: Tier; // best (lowest-rank) tier across the card's ports
  col: string;
  sub: string;
}

interface FamPort { fam: string; tier: Tier; label: string; }

/** Fold the flat live port rows into one node per card. The primary consume /
 *  emit family for each card is the one that actually participates in the most
 *  deck-wide wiring (an in-deck emitter for a consume; an in-deck consumer for
 *  an emit), tie-broken by fidelity then name — so the lattice groups cards by
 *  the family that matters here, not by parse order. */
function buildNodes(rows: DeckPortRow[]): LiveNode[] {
  // card → deduped consume / emit family ports (best tier per family).
  const cons = new Map<string, Map<string, FamPort>>();
  const emit = new Map<string, Map<string, FamPort>>();
  const order: string[] = [];

  const put = (m: Map<string, Map<string, FamPort>>, card: string, p: FamPort) => {
    let f = m.get(card);
    if (!f) { f = new Map(); m.set(card, f); }
    const cur = f.get(p.fam);
    if (!cur || tierRank[p.tier] < tierRank[cur.tier]) f.set(p.fam, p);
  };

  for (const r of rows) {
    if (NOISE.has(r.family)) continue;
    if (!cons.has(r.card) && !emit.has(r.card)) order.push(r.card);
    const port: FamPort = { fam: r.family, tier: r.tier, label: r.label };
    if (r.side === "emit") put(emit, r.card, port);
    else if (r.side === "consume") put(cons, r.card, port);
    // side-less inferred/backfill ports don't carry a direction — skip for wiring.
  }

  // Deck-wide family sets, for the "does anything feed / drain this?" score.
  const deckEmits = new Set<string>();
  const deckCons = new Set<string>();
  for (const f of emit.values()) for (const k of f.keys()) deckEmits.add(k);
  for (const f of cons.values()) for (const k of f.keys()) deckCons.add(k);

  // How many distinct deck emit families satisfy this consume family (direct or
  // via a subgroup — a `sacrifice` emit feeds a `death` consume).
  const fed = (c: string): number => [...deckEmits].filter((e) => subsumes(c, e)).length;
  // How many distinct deck consume families this emit family satisfies.
  const drain = (e: string): number => [...deckCons].filter((c) => subsumes(c, e)).length;

  const pickPrimary = (ports: FamPort[], score: (f: string) => number): FamPort | null => {
    if (!ports.length) return null;
    return [...ports].sort(
      (a, b) =>
        score(b.fam) - score(a.fam) ||
        tierRank[a.tier] - tierRank[b.tier] ||
        a.fam.localeCompare(b.fam),
    )[0];
  };

  const nodes: LiveNode[] = [];
  for (const card of order) {
    const cPorts = [...(cons.get(card)?.values() ?? [])];
    const ePorts = [...(emit.get(card)?.values() ?? [])];
    if (!cPorts.length && !ePorts.length) continue; // only noise/side-less → skip

    const pc = pickPrimary(cPorts, fed);
    const pe = pickPrimary(ePorts, drain);
    const consumes = cPorts.map((p) => p.fam);
    const emits = ePorts.map((p) => p.fam);
    const shown = (pc ? 1 : 0) + (pe ? 1 : 0);
    const tier = [...cPorts, ...ePorts].reduce<Tier>(
      (best, p) => (tierRank[p.tier] < tierRank[best] ? p.tier : best),
      "Declared",
    );

    const inFam = pc?.fam ?? null;
    const outFam = pe?.fam ?? null;
    nodes.push({
      card,
      in: inFam,
      out: outFam,
      inLabel: pc?.label ?? null,
      outLabel: pe?.label ?? null,
      consumes,
      emits,
      extra: consumes.length + emits.length - shown,
      tier,
      col: inFam ? supergroupColumnOf(inFam) : SRC_COL,
      sub: inFam ? inFam : (outFam ?? SRC_NONE),
    });
  }
  return nodes;
}

// ── lattice order (columns → bands → cards), derived + drag-reorderable ───────
interface SynOrder {
  cols: string[];
  subs: Record<string, string[]>;
  cards: Record<string, string[]>; // key = col + "|" + sub → card names
}

function buildOrder(nodes: LiveNode[]): SynOrder {
  const colCount: Record<string, number> = {};
  for (const n of nodes) colCount[n.col] = (colCount[n.col] ?? 0) + 1;
  const cols = [...new Set(nodes.map((n) => n.col))].sort(
    (a, b) => colCount[b] - colCount[a] || collate(a).localeCompare(collate(b)),
  );

  const subs: Record<string, string[]> = {};
  const cards: Record<string, string[]> = {};
  for (const col of cols) {
    const inCol = nodes.filter((n) => n.col === col);
    const subCount: Record<string, number> = {};
    for (const n of inCol) subCount[n.sub] = (subCount[n.sub] ?? 0) + 1;
    const sl = [...new Set(inCol.map((n) => n.sub))].sort(
      (a, b) => subCount[b] - subCount[a] || collate(a).localeCompare(collate(b)),
    );
    subs[col] = sl;
    for (const sub of sl) {
      cards[col + "|" + sub] = inCol.filter((n) => n.sub === sub).map((n) => n.card);
    }
  }
  return { cols, subs, cards };
}

// ── placed geometry ──────────────────────────────────────────────────────────
interface Placed extends LiveNode { x: number; y: number; }
interface Band { col: string; sub: string; x: number; yLabel: number; yMid: number; count: number; }
interface EdgeR { s: Placed; t: Placed; kind: "direct" | "super"; fam: string; }

type Drag =
  | { kind: "col"; col: string; px: number; py: number }
  | { kind: "sub"; col: string; sub: string; px: number; py: number }
  | { kind: "chip"; card: string; col: string; sub: string; px: number; py: number }
  | null;

/** A colored (emit/consume) or hollow (null) presence dot. */
function dot(fam: string | null): ReactNode {
  return fam ? (
    <span style={{ width: 7, height: 7, borderRadius: "50%", background: famHue(fam), display: "inline-block" }} />
  ) : (
    <span
      style={{
        width: 7, height: 7, borderRadius: "50%",
        border: "1px solid #5b5f70", boxSizing: "border-box", display: "inline-block",
      }}
    />
  );
}

/** The chip's clause line — the live port labels behind the primary in/out. */
function portClause(p: Placed): ReactNode {
  return (
    <span style={{ fontFamily: "var(--font-mono)", color: "#9aa0b4" }}>
      {p.inLabel ? p.inLabel : "—"}
      <span style={{ color: "#75798c" }}> ▸ </span>
      {p.outLabel ? p.outLabel : "—"}
      {p.extra > 0 && <span style={{ color: "#5b5f70" }}>{`  +${p.extra}`}</span>}
    </span>
  );
}

// ── parse a pasted decklist → bare card names (mirrors DeckLens.parseDeck) ────
function parseDeck(text: string): string[] {
  return text
    .split("\n")
    .map((l) => l.trim())
    .filter((l) => l.length > 0 && !l.startsWith("…") && !l.startsWith("..."))
    .map((l) => l.replace(/^\d+\s*x?\s+/i, "").trim())
    .filter((l) => l.length > 0);
}

const clamp = (v: number, lo: number, hi: number): number => Math.max(lo, Math.min(hi, v));

export default function SynergyWeb() {
  const [text, setText] = useState<string>(sampleDeck("full"));
  const [cards, setCards] = useState<string[]>(() => parseDeck(sampleDeck("full")));
  const [sample, setSample] = useState<"sparse" | "full">("full");
  const [order, setOrder] = useState<SynOrder | null>(null);
  const [drag, setDrag] = useState<Drag>(null);
  const svgRef = useRef<SVGSVGElement | null>(null);

  const { data: ports, loading } = useDeckPorts(cards);
  const nodes = useMemo(() => buildNodes(ports), [ports]);

  // A fresh lattice whenever the resolved node set changes (its identity is the
  // sorted card list, so drag-reorder survives unrelated re-renders).
  const nodeKey = useMemo(() => nodes.map((n) => n.card).sort().join("|"), [nodes]);
  useEffect(() => {
    setOrder(nodes.length ? buildOrder(nodes) : null);
    setDrag(null);
  }, [nodeKey]); // eslint-disable-line react-hooks/exhaustive-deps

  const analyze = (): void => setCards(parseDeck(text));
  const clear = (): void => { setText(""); setCards([]); };
  const pick = (v: "sparse" | "full"): void => {
    const t = sampleDeck(v);
    setSample(v);
    setText(t);
    setCards(parseDeck(t));
  };

  const nodeByCard = useMemo(() => {
    const m = new Map<string, LiveNode>();
    for (const n of nodes) m.set(n.card, n);
    return m;
  }, [nodes]);

  // ── column geometry (dynamic — the count is data-driven) ────────────────────
  const cols = order?.cols ?? [];
  const nSlots = cols.length;
  const usable = VW - 2 * MARGIN;
  const CHIP_W = clamp(Math.floor(usable / Math.max(1, nSlots)) - 26, 150, 224);
  const step = nSlots > 1 ? (usable - CHIP_W) / (nSlots - 1) : 0;
  const slotX = (k: number): number => MARGIN + CHIP_W / 2 + step * k;
  const colSlot = useMemo(() => {
    const m: Record<string, number> = {};
    cols.forEach((c, k) => (m[c] = k));
    return m;
  }, [cols]);
  const nearestSlot = (px: number): number => {
    let k = 0, best = 1e9;
    for (let j = 0; j < nSlots; j++) {
      const d = Math.abs(slotX(j) - px);
      if (d < best) { best = d; k = j; }
    }
    return k;
  };

  // ── place cards; record bands ───────────────────────────────────────────────
  const placed: Placed[] = [];
  const bands: Band[] = [];
  if (order) {
    cols.forEach((col) => {
      const x = slotX(colSlot[col]);
      let y = TOP;
      (order.subs[col] ?? []).forEach((sub) => {
        const list = (order.cards[col + "|" + sub] ?? []).filter((c) => nodeByCard.has(c));
        if (!list.length) return;
        bands.push({ col, sub, x, yLabel: y - CHIP_H / 2 - 6, yMid: y, count: list.length });
        list.forEach((card) => {
          placed.push({ ...(nodeByCard.get(card) as LiveNode), x, y });
          y += ROW_GAP;
        });
        y += SUB_GAP;
      });
    });
  }

  // ── dynamic canvas height + bottom bus (below the tallest column) ───────────
  const contentBottom = placed.reduce((b, p) => Math.max(b, p.y + CHIP_H / 2), TOP);
  const GRID_BOTTOM = contentBottom + 26;
  const VH = GRID_BOTTOM + 48 + 44;

  // ── channels (midpoints between adjacent column edges; ±24 at the ends) ─────
  const colLeft = cols.map((_, k) => slotX(k) - CHIP_W / 2);
  const colRight = cols.map((_, k) => slotX(k) + CHIP_W / 2);
  const chL: number[] = [];
  const chR: number[] = [];
  for (let k = 0; k < nSlots; k++) {
    chL[k] = k === 0 ? colLeft[0] - 24 : (colRight[k - 1] + colLeft[k]) / 2;
    chR[k] = k === nSlots - 1 ? colRight[k] + 24 : (colRight[k] + colLeft[k + 1]) / 2;
  }

  // ── edges: one best wire per (source, target) card pair ─────────────────────
  const deg: Record<string, number> = {};
  placed.forEach((p) => (deg[p.card] = 0));
  const edges: EdgeR[] = [];
  placed.forEach((s) => {
    if (!s.emits.length) return;
    placed.forEach((t) => {
      if (s.card === t.card || !t.consumes.length) return;
      let best: { fam: string; kind: "direct" | "super" } | null = null;
      for (const c of t.consumes) {
        for (const e of s.emits) {
          if (c === e) { best = { fam: e, kind: "direct" }; break; }
        }
        if (best) break;
      }
      if (!best) {
        outer: for (const c of t.consumes) {
          for (const e of s.emits) {
            if (subsumes(c, e)) { best = { fam: e, kind: "super" }; break outer; }
          }
        }
      }
      if (best) {
        edges.push({ s, t, kind: best.kind, fam: best.fam });
        deg[s.card]++;
        deg[t.card]++;
      }
    });
  });
  const supN = edges.filter((e) => e.kind === "super").length;

  // ── drag commit / start / move ─────────────────────────────────────────────
  const svgCoords = (e: { clientX: number; clientY: number }): Pt => {
    const svg = svgRef.current;
    if (!svg) return [0, 0];
    const r = svg.getBoundingClientRect();
    return [((e.clientX - r.left) / r.width) * VW, ((e.clientY - r.top) / r.height) * VH];
  };
  const startDrag = (e: React.PointerEvent, d: Drag) => {
    e.stopPropagation();
    svgRef.current?.setPointerCapture(e.pointerId);
    setDrag(d);
  };
  const onMove = (e: React.PointerEvent) => {
    if (!drag) return;
    const [px, py] = svgCoords(e);
    setDrag({ ...drag, px, py });
  };
  const onUp = (e: React.PointerEvent) => {
    if (!drag || !order) return;
    const [px, py] = svgCoords(e);
    if (drag.kind === "col") {
      const k = nearestSlot(px);
      const arr = order.cols.filter((c) => c !== drag.col);
      arr.splice(Math.min(k, arr.length), 0, drag.col);
      setOrder({ ...order, cols: arr });
    } else if (drag.kind === "sub") {
      const others = bands.filter((z) => z.col === drag.col && z.sub !== drag.sub);
      let ti = 0;
      others.forEach((z) => { if (z.yMid < py) ti++; });
      const rest = (order.subs[drag.col] ?? []).filter((s) => s !== drag.sub);
      rest.splice(Math.min(ti, rest.length), 0, drag.sub);
      setOrder({ ...order, subs: { ...order.subs, [drag.col]: rest } });
    } else if (drag.kind === "chip") {
      const key = drag.col + "|" + drag.sub;
      const sib = placed.filter((q) => q.col === drag.col && q.sub === drag.sub && q.card !== drag.card);
      let ti = 0;
      sib.forEach((q) => { if (q.y < py) ti++; });
      const rest = (order.cards[key] ?? []).filter((c) => c !== drag.card);
      rest.splice(Math.min(ti, rest.length), 0, drag.card);
      setOrder({ ...order, cards: { ...order.cards, [key]: rest } });
    }
    setDrag(null);
  };

  // ── drop-indicator geometry ─────────────────────────────────────────────────
  const colBottom = (col: string): number => {
    let b = -1e9;
    placed.forEach((p) => { if (p.col === col) b = Math.max(b, p.y + CHIP_H / 2); });
    return b > -1e9 ? b : VH - 90;
  };
  const subInsertY = (col: string, sub: string, py: number): number => {
    const others = bands.filter((z) => z.col === col && z.sub !== sub);
    if (!others.length) return TOP - 12;
    let ti = 0;
    others.forEach((z) => { if (z.yMid < py) ti++; });
    return ti >= others.length
      ? others[others.length - 1].yMid + ROW_GAP / 2
      : others[ti].yMid - ROW_GAP / 2;
  };
  const chipInsertY = (p: Placed, py: number): number => {
    const sib = placed.filter((q) => q.col === p.col && q.sub === p.sub && q.card !== p.card);
    if (!sib.length) return p.y;
    let ti = 0;
    sib.forEach((q) => { if (q.y < py) ti++; });
    return ti >= sib.length ? sib[sib.length - 1].y + ROW_GAP / 2 : sib[ti].y - ROW_GAP / 2;
  };

  // ── background grid ─────────────────────────────────────────────────────────
  const grid: ReactNode[] = [];
  for (let gx = 0; gx <= VW; gx += 42)
    grid.push(<line key={`gx${gx}`} x1={gx} y1={0} x2={gx} y2={VH} stroke="#161826" strokeWidth={1} />);
  for (let gy = 0; gy <= VH; gy += 42)
    grid.push(<line key={`gy${gy}`} x1={0} y1={gy} x2={VW} y2={gy} stroke="#161826" strokeWidth={1} />);

  const showGraph = !loading && cards.length > 0 && placed.length > 0;
  const showEmpty = cards.length === 0;
  const showLoading = loading && cards.length > 0;
  const showNoPorts = !loading && cards.length > 0 && placed.length === 0;

  return (
    <div className="view-grid">
      <SectionHead kicker="04 · Exploit" title="Deck Synergy Web">
        The deck as a live port graph — one node per card, grouped supergroup columns × consume-family
        rows, wired emit→consume through a collision-free bus. Drag columns, bands, or cards to reorder.
      </SectionHead>

      {/* decklist input (same parse rules as Deck Lens) */}
      <div className="panel">
        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", gap: 8, marginBottom: 8 }}>
          <h5 style={{ margin: 0, color: "var(--atlas-muted)" }}>Decklist</h5>
          <SegControl<"sparse" | "full">
            options={[{ value: "sparse", label: "Sparse" }, { value: "full", label: "Dense" }]}
            value={sample}
            onChange={pick}
          />
        </div>
        <textarea
          className="input"
          value={text}
          spellCheck={false}
          rows={7}
          onChange={(e) => setText(e.target.value)}
          aria-label="Decklist"
        />
        <div style={{ display: "flex", gap: 8, marginTop: 10 }}>
          <button type="button" className="btn btn-primary" onClick={analyze}>Analyze</button>
          <button type="button" className="btn btn-secondary" onClick={clear}>Clear</button>
        </div>
      </div>

      <div className="panel panel-svg">
        <svg
          ref={svgRef}
          viewBox={`0 0 ${VW} ${VH}`}
          width="100%"
          role="img"
          aria-label="deck synergy port graph"
          style={{ touchAction: "none", userSelect: "none" }}
          onPointerMove={onMove}
          onPointerUp={onUp}
          onPointerCancel={() => setDrag(null)}
        >
          {grid}

          {showEmpty && (
            <text x={VW / 2} y={VH / 2} textAnchor="middle" fill="#75798c" fontSize={16} fontFamily="var(--font-mono)">
              No deck loaded
            </text>
          )}

          {showNoPorts && (
            <text x={VW / 2} y={VH / 2} textAnchor="middle" fill="#75798c" fontSize={15} fontFamily="var(--font-mono)">
              No parsed ports for these cards yet
            </text>
          )}

          {showLoading &&
            Array.from({ length: 18 }, (_, i) => {
              const a = (i / 18) * Math.PI * 2;
              return (
                <circle key={i} cx={VW / 2 + Math.cos(a) * 200} cy={VH / 2 + Math.sin(a) * 150} r={7} fill="#23252f">
                  <animate attributeName="opacity" values="0.25;1;0.25" dur="1.2s" begin={`${i * 0.05}s`} repeatCount="indefinite" />
                </circle>
              );
            })}

          {showGraph && (
            <>
              {/* ── edges: right channel → bottom bus → left channel ── */}
              {edges.map((e, i) => {
                const hue = famHue(e.fam);
                const sk = colSlot[e.s.col];
                const tk = colSlot[e.t.col];
                const sRight = colRight[sk];
                const tLeft = colLeft[tk];
                const sCh = chR[sk];
                const tCh = chL[tk];
                const busY = GRID_BOTTOM + (i % 8) * 6;
                const pts: Pt[] = [
                  [sRight, e.s.y],
                  [sCh, e.s.y],
                  [sCh, busY],
                  [tCh, busY],
                  [tCh, e.t.y],
                  [tLeft, e.t.y],
                ];
                const ang = endAngle(pts);
                const c2 = pts[pts.length - 2];
                const p1 = pts[pts.length - 1];
                const L = Math.hypot(p1[0] - c2[0], p1[1] - c2[1]) || 1;
                const tt = Math.max(0.2, (L - 9) / L);
                const ax = c2[0] + (p1[0] - c2[0]) * tt;
                const ay = c2[1] + (p1[1] - c2[1]) * tt;
                return (
                  <g key={`${e.s.card}»${e.t.card}»${e.kind}`}>
                    <path
                      d={orthoPath(pts)}
                      fill="none"
                      stroke={hue}
                      strokeWidth={e.kind === "super" ? 1.5 : 2}
                      strokeLinejoin="round"
                      strokeDasharray={e.kind === "super" ? "3 4" : undefined}
                      opacity={e.kind === "super" ? 0.5 : 0.9}
                    >
                      <title>
                        {`${e.s.card}  emit:${e.fam}  →  ${e.t.card}  consume:${e.t.in ?? "—"}` +
                          (e.kind === "super" ? `   (${e.fam} ⊂ ${e.t.in} — counted via supergroup)` : "")}
                      </title>
                    </path>
                    <circle cx={sRight} cy={e.s.y} r={2.4} fill={hue} opacity={0.9} />
                    <path
                      d={arrowHeadPath(4)}
                      transform={rotate(ax, ay, ang)}
                      fill={hue}
                      opacity={e.kind === "super" ? 0.65 : 0.95}
                    />
                    {e.kind === "super" && (
                      <text
                        x={(sCh + tCh) / 2} y={busY - 3} textAnchor="middle"
                        fontSize={10} fill={famHue(e.t.in)} fontFamily="var(--font-mono)"
                      >
                        ⊃
                      </text>
                    )}
                  </g>
                );
              })}

              {/* ── column headers (drag left/right to reorder supergroups) ── */}
              {cols.map((col) => {
                const x = slotX(colSlot[col]);
                return (
                  <g
                    key={`hdr${col}`}
                    style={{ cursor: "grab" }}
                    opacity={drag?.kind === "col" && drag.col === col ? 0.28 : 1}
                    onPointerDown={(e) => {
                      const [px, py] = svgCoords(e);
                      startDrag(e, { kind: "col", col, px, py });
                    }}
                  >
                    <rect x={x - CHIP_W / 2} y={46} width={CHIP_W} height={40} rx={6} fill="#12131f" stroke="#23252f" strokeWidth={1} />
                    <text x={x} y={60} textAnchor="middle" fontSize={8} fontFamily="var(--font-mono)" fill="#5b5f70" letterSpacing="0.1em">
                      ⣿ SUPERGROUP
                    </text>
                    <text x={x} y={77} textAnchor="middle" fontSize={12} fontFamily="var(--font-mono)" fill="#cfd3e5">
                      {colLabel(col)}
                    </text>
                  </g>
                );
              })}

              {/* ── band labels (drag up/down within a supergroup) ── */}
              {bands.map((b) => (
                <text
                  key={`band${b.col}-${b.sub}`}
                  x={b.x - CHIP_W / 2 + 2}
                  y={b.yLabel}
                  fontSize={7.5}
                  fontFamily="var(--font-mono)"
                  fill={subHue(b.col, b.sub)}
                  letterSpacing="0.06em"
                  style={{ cursor: "grab" }}
                  opacity={drag?.kind === "sub" && drag.col === b.col && drag.sub === b.sub ? 0.3 : 1}
                  onPointerDown={(e) => {
                    const [px, py] = svgCoords(e);
                    startDrag(e, { kind: "sub", col: b.col, sub: b.sub, px, py });
                  }}
                >
                  {"⣿ " + subLabel(b.col, b.sub).toUpperCase()}
                </text>
              ))}

              {/* ── card chips (drag to reorder within a band) ── */}
              {placed.map((p) => {
                const x0 = p.x - CHIP_W / 2;
                const y0 = p.y - CHIP_H / 2;
                const dragging = drag?.kind === "chip" && drag.card === p.card;
                const dy = dragging ? (drag as Extract<Drag, { kind: "chip" }>).py - p.y : 0;
                return (
                  <g
                    key={`chip${p.card}`}
                    transform={dy ? `translate(0,${dy})` : undefined}
                    opacity={dragging ? 0.9 : 1}
                    style={{ cursor: "grab" }}
                    onPointerDown={(e) => {
                      const [px, py] = svgCoords(e);
                      startDrag(e, { kind: "chip", card: p.card, col: p.col, sub: p.sub, px, py });
                    }}
                  >
                    <rect
                      x={x0} y={y0} width={CHIP_W} height={CHIP_H} rx={7}
                      fill="#141522"
                      stroke={dragging ? "#9184d9" : "#2c2f3d"}
                      strokeWidth={dragging ? 1.8 : 1.2}
                    >
                      <title>
                        {`${p.card} · consume ${p.in ?? "—"} → emit ${p.out ?? "—"} · ${deg[p.card]} links · drag to reorder`}
                      </title>
                    </rect>
                    <foreignObject x={x0} y={y0} width={CHIP_W} height={CHIP_H} style={{ pointerEvents: "none" }}>
                      <div
                        style={{
                          height: "100%", boxSizing: "border-box", padding: "5px 9px",
                          fontFamily: "Inter, system-ui, sans-serif", overflow: "hidden",
                        }}
                      >
                        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 6, marginBottom: 2 }}>
                          <span
                            style={{
                              fontSize: 10.5, fontWeight: 600, color: "#e9e9ed",
                              whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis", pointerEvents: "auto",
                            }}
                          >
                            <CardLink name={p.card} />
                          </span>
                          <span style={{ display: "flex", alignItems: "center", gap: 5, flex: "none" }}>
                            <span style={{ display: "flex", alignItems: "center", gap: 3 }}>
                              {dot(p.in)}
                              <span style={{ color: "#75798c", fontSize: 9 }}>▸</span>
                              {dot(p.out)}
                            </span>
                            <TierChip tier={p.tier} />
                          </span>
                        </div>
                        <div
                          style={{
                            fontSize: 9, lineHeight: 1.45, color: "#cfd3e5",
                            display: "-webkit-box", WebkitLineClamp: 2, WebkitBoxOrient: "vertical", overflow: "hidden",
                          }}
                        >
                          {portClause(p)}
                        </div>
                      </div>
                    </foreignObject>
                  </g>
                );
              })}

              {/* ── ghost + drop-indicator overlay ── */}
              {drag && (
                <g pointerEvents="none">
                  {drag.kind === "col" &&
                    (() => {
                      const cb = colBottom(drag.col);
                      const ix = slotX(nearestSlot(drag.px)) - CHIP_W / 2 - 11;
                      return (
                        <>
                          <rect
                            x={drag.px - CHIP_W / 2} y={46} width={CHIP_W} height={cb - 46 + 8} rx={8}
                            fill="color-mix(in srgb, #9184d9 15%, transparent)"
                            stroke="#9184d9" strokeWidth={1.5} strokeDasharray="5 3"
                          />
                          <line x1={ix} y1={42} x2={ix} y2={cb + 8} stroke="#b5abfc" strokeWidth={3} strokeLinecap="round" />
                          <text x={ix} y={36} textAnchor="middle" fontSize={9} fontFamily="var(--font-mono)" fill="#b5abfc">
                            {"▸ slot " + (nearestSlot(drag.px) + 1)}
                          </text>
                        </>
                      );
                    })()}

                  {drag.kind === "sub" &&
                    (() => {
                      const band = bands.find((z) => z.col === drag.col && z.sub === drag.sub);
                      const bx = band ? band.x : slotX(colSlot[drag.col] ?? 0);
                      const iy = subInsertY(drag.col, drag.sub, drag.py);
                      return (
                        <line x1={bx - CHIP_W / 2 - 6} y1={iy} x2={bx + CHIP_W / 2 + 6} y2={iy} stroke="#b5abfc" strokeWidth={3} strokeLinecap="round" />
                      );
                    })()}

                  {drag.kind === "chip" &&
                    (() => {
                      const p = placed.find((q) => q.card === drag.card);
                      if (!p) return null;
                      const iy = chipInsertY(p, drag.py);
                      return (
                        <line x1={p.x - CHIP_W / 2} y1={iy} x2={p.x + CHIP_W / 2} y2={iy} stroke="#b5abfc" strokeWidth={3} strokeLinecap="round" />
                      );
                    })()}
                </g>
              )}

              {/* footnote: node / wire / super-edge counts */}
              <text x={16} y={VH - 12} fontSize={10} fontFamily="var(--font-mono)" fill="#5b5f70">
                {`${placed.length} cards · ${edges.length} wires · ${supN} via supergroup`}
              </text>
            </>
          )}
        </svg>
      </div>
    </div>
  );
}
