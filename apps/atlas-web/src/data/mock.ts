// ─────────────────────────────────────────────────────────────────────────────
// MagicAtlas — concept sample data.
//
// A faithful port of the constants embedded in the "Atlas Explorer Concepts"
// design canvas. This is the *only* place mock data lives; every view reads
// from here through the hooks in `./atlas.ts`, so swapping to the real GraphQL
// endpoints (see docs/design/upstream-atlas-data-plan.md) is a change to that
// seam, not to the views.
//
// Sample records are the real ones from the design brief where given (Mikaeus,
// the Blight Mound + Phantom Train combo, the token→sacrifice family edge, the
// death·sacrifice·token archetype) and schema-faithful mocks elsewhere.
// ─────────────────────────────────────────────────────────────────────────────

export type Tier = "Green" | "Amber" | "Inferred" | "Declared";
export type Side = "consume" | "emit";

export const TIER_ORDER: Tier[] = ["Green", "Amber", "Inferred", "Declared"];
export const tierRank: Record<Tier, number> = { Green: 0, Amber: 1, Inferred: 2, Declared: 3 };

/** SVG texture channel for a tier — used where hue is already spent on a family. */
export const TIER: Record<Tier, { color: string; dash: string | null; op: number }> = {
  Green: { color: "#3fbf7f", dash: null, op: 1 },
  Amber: { color: "#E0A53C", dash: "14 6", op: 0.92 },
  Inferred: { color: "#9184d9", dash: "6 5", op: 0.8 },
  Declared: { color: "#7f8399", dash: "1 6", op: 0.55 },
};

/** Richer display metadata for the tier legend / design-system reference. */
export interface TierMeta {
  key: Tier;
  name: string; // human display name (Verified / Conditional / …)
  hex: string;
  color: string;
  swatch: string;
  dotBg: string;
  dotShadow: string;
  dotBorder: string;
  dash: string; // css dash label
  opacity: number;
  textureLabel: string;
  vol: string;
  desc: string;
}

export const TIERS: TierMeta[] = [
  {
    key: "Green", name: "Verified", hex: "#3fbf7f", color: "#3fbf7f", swatch: "#3fbf7f",
    dotBg: "#3fbf7f", dotShadow: "0 0 8px rgba(63,191,127,.6)", dotBorder: "none",
    dash: "none", opacity: 1, textureLabel: "solid", vol: "405",
    desc: "Mechanism verified by the engine — fires unconditionally.",
  },
  {
    key: "Amber", name: "Conditional", hex: "#E0A53C", color: "#E0A53C", swatch: "#E0A53C",
    dotBg: "#E0A53C", dotShadow: "0 0 8px rgba(224,165,60,.5)", dotBorder: "none",
    dash: "14 6", opacity: 0.92, textureLabel: "long-dash", vol: "3,062",
    desc: "Mechanism reconstructed but conditional — timing / board-state caveats.",
  },
  {
    key: "Inferred", name: "Inferred", hex: "#9184d9", color: "#b5abfc",
    swatch: "repeating-linear-gradient(45deg,#5d5294,#5d5294 4px,#3a3560 4px,#3a3560 8px)",
    dotBg: "transparent", dotShadow: "none", dotBorder: "1.5px dashed #9184d9",
    dash: "6 5", opacity: 0.8, textureLabel: "short-dash · +conf", vol: "~1,500 target",
    desc: "Ports statistically backfilled — a confidence-scored best guess, never a parse.",
  },
  {
    key: "Declared", name: "Declared", hex: "#7f8399", color: "#9397ab", swatch: "transparent",
    dotBg: "transparent", dotShadow: "none", dotBorder: "1.5px dotted #75798c",
    dash: "1 6", opacity: 0.55, textureLabel: "dotted", vol: "~91k rest",
    desc: "Community-catalogued only — we know the cards and the result, not the mechanism.",
  },
];

// ── Resource families — the seventeen family hues + metro coordinates ────────
export interface Family {
  name: string;
  hue: string;
  cards: number;
  labels: number;
  x: number;
  y: number;
}

export const FAM: Record<string, Family> = {
  mana: { name: "mana", hue: "#E8B84B", cards: 1925, labels: 25, x: 560, y: 340 },
  token: { name: "token", hue: "#5FBF73", cards: 1100, labels: 18, x: 400, y: 250 },
  sacrifice: { name: "sacrifice", hue: "#D9534F", cards: 640, labels: 14, x: 500, y: 150 },
  death: { name: "death", hue: "#B95FD9", cards: 980, labels: 20, x: 690, y: 180 },
  counter: { name: "counter", hue: "#4FC3D9", cards: 720, labels: 16, x: 800, y: 290 },
  card: { name: "card", hue: "#5B8DEF", cards: 1500, labels: 22, x: 940, y: 360 },
  damage: { name: "damage", hue: "#EF7A4F", cards: 1300, labels: 19, x: 850, y: 500 },
  life: { name: "life", hue: "#E86FA0", cards: 900, labels: 12, x: 660, y: 560 },
  recursion: { name: "recursion", hue: "#8B7FE8", cards: 610, labels: 13, x: 540, y: 470 },
  tap: { name: "tap", hue: "#46C9A8", cards: 500, labels: 9, x: 300, y: 380 },
  mill: { name: "mill", hue: "#6C6FD9", cards: 300, labels: 7, x: 230, y: 510 },
  exile: { name: "exile", hue: "#9AA6C9", cards: 420, labels: 8, x: 190, y: 320 },
  combat: { name: "combat", hue: "#C98F4F", cards: 760, labels: 11, x: 720, y: 430 },
  cost: { name: "cost", hue: "#A8CF4F", cards: 560, labels: 10, x: 430, y: 410 },
  copy: { name: "copy", hue: "#D96FC9", cards: 480, labels: 9, x: 900, y: 200 },
  discard: { name: "discard", hue: "#A9764F", cards: 380, labels: 8, x: 340, y: 590 },
  untap: { name: "untap", hue: "#7FD0B0", cards: 260, labels: 6, x: 210, y: 170 },
};

export const FAMILY_KEYS = Object.keys(FAM);
export const famHue = (f: string | null | undefined): string => (f && FAM[f]?.hue) || "#75798c";

// ── Synthetic palette for live families absent from FAM ──────────────────────
// The real resource-graph family set (resourceFamilyRows) differs from the
// guessed families above — it carries blink/cast/etb/dice/phase/recur, and
// drops card/mill/exile/cost/discard/recursion. `ensureFamily` merges a live
// family into the palette: known families keep their hand-tuned hue + metro
// coordinates; unknown ones get a deterministic hue (name-hash → HSL) and an
// auto-laid-out coordinate on a ring around the map centre, then are registered
// into FAM so `famHue` (which the views call directly) resolves them too.

const FNV = (s: string): number => {
  let h = 0x811c9dc5;
  for (let i = 0; i < s.length; i++) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 0x01000193);
  }
  return h >>> 0;
};

/** Deterministic, stable hue for a family name not in the hand-tuned palette. */
export const synthFamilyHue = (name: string): string => {
  const h = FNV(name) % 360;
  return `hsl(${h} 58% 60%)`;
};

// Map centre + ring radii used to auto-place unknown families (viewBox 1120×660).
const RING_CX = 560, RING_CY = 330, RING_RX = 250, RING_RY = 200;

// Hand-placed metro coordinates for the live families with no tuned FAM entry.
// The real resource-graph set is exactly 17:
//   mana · etb · tap · cast · token · counter · sacrifice · death · phase · recur
//   · damage · life · untap · copy · blink · combat · dice
// The first group already has tuned FAM coords above; these six fill the gaps so
// no two stations overlap on the 1120×660 viewBox (the tuned card/mill/exile/
// cost/discard/recursion entries are NOT in the live set and never render, so
// their coordinates are free real estate here). A quick pairwise check keeps
// every station ≥ ~135 units from its nearest neighbour.
const SYNTH_COORDS: Record<string, { x: number; y: number }> = {
  cast: { x: 1000, y: 360 },
  etb: { x: 470, y: 470 },
  recur: { x: 300, y: 540 },
  phase: { x: 180, y: 470 },
  blink: { x: 130, y: 300 },
  dice: { x: 1000, y: 550 },
};

// Minimum centre-to-centre separation used by the collision-aware fallback.
const MIN_SEP = 78;

/** Collision-aware ring placement for a family with no hand-tuned coordinate:
 *  sweep the map on a golden-angle spiral, widening the ring each lap, until we
 *  find a slot at least MIN_SEP from every already-registered station. Pure and
 *  deterministic in `name` + the current FAM occupancy. */
function placeClear(name: string): { x: number; y: number } {
  const base = (FNV(name) % 360) * (Math.PI / 180);
  for (let step = 0; step < 400; step++) {
    const ang = base + step * 0.6180339887 * Math.PI * 2;
    const grow = Math.floor(step / 12);
    const rx = RING_RX + grow * 26;
    const ry = RING_RY + grow * 20;
    const x = Math.round(RING_CX + Math.cos(ang) * rx);
    const y = Math.round(RING_CY + Math.sin(ang) * ry);
    if (x < 40 || x > 1080 || y < 40 || y > 620) continue;
    let clear = true;
    for (const f of Object.values(FAM)) {
      if (Math.hypot(f.x - x, f.y - y) < MIN_SEP) { clear = false; break; }
    }
    if (clear) return { x, y };
  }
  return {
    x: Math.round(RING_CX + Math.cos(base) * RING_RX),
    y: Math.round(RING_CY + Math.sin(base) * RING_RY),
  };
}

/**
 * Return the palette entry for a live family, registering a synthesized one into
 * FAM on first sight so it is shared across every `famHue` lookup. Known live
 * families use a hand-tuned metro coordinate; any truly unknown family falls
 * back to collision-aware ring placement so no two stations ever overlap.
 * Idempotent; hue is always deterministic (name-hash → HSL).
 */
export function ensureFamily(name: string): Family {
  const existing = FAM[name];
  if (existing) return existing;
  const { x, y } = SYNTH_COORDS[name] ?? placeClear(name);
  const synth: Family = { name, hue: synthFamilyHue(name), cards: 0, labels: 0, x, y };
  FAM[name] = synth;
  return synth;
}

/** Families g whose consume-supergroup subsumes `fam` (incl. fam itself). */
export const supergroupsOf = (fam: string): string[] => {
  const sups = [fam];
  for (const [g, subs] of Object.entries(GROUPS)) if (subs.includes(fam)) sups.push(g);
  return sups;
};

// ── Super/subgroup lattice — key = supergroup consume family ─────────────────
//    "a creature dies" ⊇ "a creature is sacrificed"; card advantage ⊇ self-mill.
export const GROUPS: Record<string, string[]> = {
  death: ["sacrifice"],
  card: ["mill"],
};

/** Does supergroup `sup` subsume subgroup `sub`? (reflexive) */
export const subsumes = (sup: string, sub: string): boolean =>
  sup === sub || (GROUPS[sup] || []).includes(sub);

// ── Metro edges: [from, to, combos, tier, engine, origin] ────────────────────
export interface Edge {
  from: string;
  to: string;
  combos: number;
  tier: Tier;
  engine: boolean;
  origin: "rules" | "card";
}

const RAW_EDGES: [string, string, number, Tier, boolean, "rules" | "card"][] = [
  ["token", "sacrifice", 2574, "Green", true, "card"],
  ["sacrifice", "death", 1980, "Green", true, "rules"],
  ["mana", "token", 1450, "Green", false, "card"],
  ["card", "mana", 1300, "Green", true, "card"],
  ["tap", "mana", 990, "Green", false, "card"],
  ["death", "recursion", 610, "Green", true, "rules"],
  ["counter", "token", 540, "Green", false, "card"],
  ["recursion", "sacrifice", 700, "Green", true, "card"],
  ["untap", "tap", 480, "Green", true, "rules"],
  ["token", "death", 940, "Amber", false, "rules"],
  ["sacrifice", "mana", 880, "Amber", true, "card"],
  ["death", "card", 720, "Amber", false, "rules"],
  ["copy", "token", 620, "Amber", false, "card"],
  ["cost", "mana", 560, "Amber", false, "card"],
  ["life", "card", 210, "Amber", false, "card"],
  ["mana", "counter", 430, "Inferred", false, "card"],
  ["mill", "card", 330, "Inferred", false, "rules"],
  ["counter", "combat", 150, "Inferred", false, "rules"],
  ["combat", "damage", 360, "Declared", false, "rules"],
  ["damage", "life", 290, "Declared", false, "rules"],
  ["exile", "recursion", 180, "Declared", false, "card"],
  ["token", "combat", 240, "Declared", false, "rules"],
];

export const EDGES: Edge[] = RAW_EDGES.map(([from, to, combos, tier, engine, origin]) => ({
  from, to, combos, tier, engine, origin,
}));

export const SPARSE_EDGES = new Set([
  "token>sacrifice", "sacrifice>death", "mana>token",
  "card>mana", "tap>mana", "death>recursion", "counter>token",
]);
export const edgeKey = (e: Edge) => `${e.from}>${e.to}`;

// ── Realized archetypes ──────────────────────────────────────────────────────
export interface Archetype { sig: string; combos: number; tier: Tier; fam: string; }
export const ARCHETYPES: Archetype[] = [
  { sig: "death · sacrifice · token", combos: 994, tier: "Green", fam: "death" },
  { sig: "mana · token · card", combos: 612, tier: "Green", fam: "mana" },
  { sig: "tap · untap · mana", combos: 430, tier: "Green", fam: "tap" },
  { sig: "copy · token · sacrifice", combos: 388, tier: "Amber", fam: "copy" },
  { sig: "recursion · death · card", combos: 301, tier: "Green", fam: "recursion" },
  { sig: "counter · token · combat", combos: 210, tier: "Amber", fam: "counter" },
  { sig: "mill · card · recursion", combos: 150, tier: "Inferred", fam: "mill" },
  { sig: "life · card · damage", combos: 96, tier: "Declared", fam: "life" },
];

// ── Top cards per family (Station Focus rail) ────────────────────────────────
export const FAMCARDS: Record<string, string[]> = {
  mana: ["Sol Ring", "Mana Vault", "Basalt Monolith", "Priest of Titania"],
  token: ["Chatterfang, Squirrel General", "Bitterblossom", "Ophiomancer", "Scute Swarm"],
  sacrifice: ["Ashnod's Altar", "Viscera Seer", "Carrion Feeder", "Phyrexian Altar"],
  death: ["Mikaeus, the Unhallowed", "Grave Pact", "Dictate of Erebos", "Butcher of Malakir"],
  counter: ["Aang, A Lot to Learn", "Hardened Scales", "Winding Constrictor", "The Ozolith"],
  card: ["Skullclamp", "Midnight Reaper", "Read the Bones", "Grim Haruspex"],
  combat: ["Impact Tremors", "Purphoros, God of the Forge", "Warstorm Surge", "Terror of the Peaks"],
  recursion: ["Reveillark", "Karmic Guide", "Sun Titan", "Mikaeus, the Unhallowed"],
  copy: ["Kiki-Jiki, Mirror Breaker", "Dualcaster Mage", "Splinter Twin", "Twinflame"],
  damage: ["Blood Artist", "Zulaport Cutthroat", "Poison-Tip Archer", "Bastion of Remembrance"],
  life: ["Aetherflux Reservoir", "Vito, Thorn of the Dusk Rose", "Sanguine Bond", "Exquisite Blood"],
  tap: ["Cryptolith Rite", "Earthcraft", "Song of Freyalise", "Citanul Hierophants"],
  untap: ["Village Bell-Ringer", "Intruder Alarm", "Derevi, Empyrial Tactician", "Sword of Feast and Famine"],
  mill: ["Altar of Dementia", "Hedron Crab", "Bruvac the Grandiloquent", "Ruin Crab"],
  exile: ["Leonin Relic-Warder", "Fiend Hunter", "Sun Titan", "Skyclave Apparition"],
  cost: ["Training Grounds", "Biomancer's Familiar", "Zirda, the Dawnwaker", "Heartstone"],
  discard: ["Bone Miser", "Waste Not", "Megrim", "Liliana's Caress"],
};

// ── Oracle port spans (hard-coded until MAST emits source offsets) ───────────
export interface OracleSeg { t: string; role?: Side; fam?: string; }
export interface OracleCard { type: string; segs: OracleSeg[]; }

export const ORACLE: Record<string, OracleCard> = {
  "Midnight Reaper": { type: "Creature — Zombie", segs: [
    { t: "Whenever another nontoken creature you control dies", role: "consume", fam: "death" }, { t: ", " },
    { t: "draw a card", role: "emit", fam: "card" },
    { t: ". Whenever this happens and your life total is less than 1, you lose the game… (Midnight Reaper deals 1 damage to you.)" }] },
  "Grave Pact": { type: "Enchantment", segs: [
    { t: "Whenever a creature you control dies", role: "consume", fam: "death" }, { t: ", " },
    { t: "each other player sacrifices a creature", role: "emit", fam: "sacrifice" }, { t: "." }] },
  "Ashnod's Altar": { type: "Artifact", segs: [
    { t: "Sacrifice a creature", role: "consume", fam: "sacrifice" }, { t: ": " },
    { t: "Add {C}{C}", role: "emit", fam: "mana" }, { t: "." }] },
  "Skullclamp": { type: "Artifact — Equipment", segs: [
    { t: "Equipped creature gets +1/-1. Whenever equipped creature dies", role: "consume", fam: "death" }, { t: ", " },
    { t: "draw two cards", role: "emit", fam: "card" }, { t: ". Equip {1}." }] },
  "Carrion Feeder": { type: "Creature — Zombie", segs: [
    { t: "Can't block. Sacrifice a creature", role: "consume", fam: "sacrifice" }, { t: ": " },
    { t: "Put a +1/+1 counter on Carrion Feeder", role: "emit", fam: "counter" }, { t: "." }] },
  "Zulaport Cutthroat": { type: "Creature — Human Rogue Ally", segs: [
    { t: "Whenever Zulaport Cutthroat or another creature you control dies", role: "consume", fam: "death" }, { t: ", " },
    { t: "each opponent loses 1 life and you gain that much life", role: "emit", fam: "life" }, { t: "." }] },
  "Impact Tremors": { type: "Enchantment", segs: [
    { t: "Whenever a creature enters the battlefield under your control", role: "consume", fam: "token" }, { t: ", " },
    { t: "Impact Tremors deals 1 damage to each opponent", role: "emit", fam: "damage" }, { t: "." }] },
  "Chatterfang, Squirrel General": { type: "Legendary Creature — Squirrel", segs: [
    { t: "If one or more tokens would be created under your control, those tokens plus that many Squirrel tokens are created instead. " },
    { t: "{X}{B}, sacrifice X Squirrels", role: "consume", fam: "sacrifice" }, { t: ": " },
    { t: "Target creature gets -X/-X", role: "emit", fam: "damage" }, { t: "." }] },
  "Pitiless Plunderer": { type: "Creature — Human Pirate", segs: [
    { t: "Whenever another creature you control dies", role: "consume", fam: "death" }, { t: ", " },
    { t: "create a Treasure token", role: "emit", fam: "mana" }, { t: "." }] },
  "Warren Soultrader": { type: "Creature — Devil", segs: [
    { t: "Sacrifice another creature", role: "consume", fam: "sacrifice" }, { t: ": " },
    { t: "Create a 1/1 Devil creature token. Add one mana of any color", role: "emit", fam: "token" }, { t: "." }] },
  "Blight Mound": { type: "Artifact Creature — Construct", segs: [
    { t: "Whenever another creature dies", role: "consume", fam: "death" },
    { t: ", put a spore counter on Blight Mound. Remove three spore counters: " },
    { t: "create a 1/1 Saproling; it deals damage", role: "emit", fam: "damage" }, { t: "." }] },
  "Sol Ring": { type: "Artifact", segs: [
    { t: "{T}: " }, { t: "Add {C}{C}", role: "emit", fam: "mana" }, { t: "." }] },
};

// ── Card Explorer corpus: {card, in(consume), out(emit), tier, conf?} ────────
export interface PoolCard { card: string; in: string | null; out: string | null; tier: Tier; conf?: number; }

export const CARDPOOL: PoolCard[] = [
  { card: "Grave Pact", in: "death", out: "sacrifice", tier: "Green" },
  { card: "Dictate of Erebos", in: "death", out: "sacrifice", tier: "Green" },
  { card: "Fleshbag Marauder", in: null, out: "sacrifice", tier: "Amber" },
  { card: "Merciless Executioner", in: null, out: "sacrifice", tier: "Amber" },
  { card: "Mikaeus, the Unhallowed", in: "death", out: "death", tier: "Inferred", conf: 0.87 },
  { card: "Ashnod's Altar", in: "sacrifice", out: "mana", tier: "Green" },
  { card: "Carrion Feeder", in: "sacrifice", out: "counter", tier: "Green" },
  { card: "Viscera Seer", in: "sacrifice", out: "life", tier: "Green" },
  { card: "Woe Strider", in: "sacrifice", out: "card", tier: "Amber" },
  { card: "Blood Artist", in: "death", out: "life", tier: "Green" },
  { card: "Zulaport Cutthroat", in: "death", out: "life", tier: "Green" },
  { card: "Poison-Tip Archer", in: "death", out: "life", tier: "Amber" },
  { card: "Skullclamp", in: "death", out: "card", tier: "Green" },
  { card: "Grim Haruspex", in: "death", out: "card", tier: "Green" },
  { card: "Midnight Reaper", in: "death", out: "card", tier: "Green" },
  { card: "Pitiless Plunderer", in: "death", out: "mana", tier: "Green" },
  { card: "The Locust God", in: "card", out: "token", tier: "Green" },
  { card: "Chasm Skulker", in: "card", out: "token", tier: "Amber" },
  { card: "Nadir Kraken", in: "card", out: "token", tier: "Declared" },
  { card: "Psychosis Crawler", in: "card", out: "damage", tier: "Amber" },
  { card: "Chatterfang, Squirrel General", in: null, out: "token", tier: "Green" },
  { card: "Bitterblossom", in: null, out: "token", tier: "Green" },
  { card: "Impact Tremors", in: "token", out: "damage", tier: "Green" },
  { card: "Purphoros, God of the Forge", in: "token", out: "damage", tier: "Green" },
  { card: "Sol Ring", in: null, out: "mana", tier: "Green" },
];

export const EXPLORER_CARDS = ["Midnight Reaper", "Grave Pact", "Ashnod's Altar", "Zulaport Cutthroat", "Impact Tremors"];

export interface Candidate extends PoolCard { via: boolean; port: string; }

/** Cards that emit what `fam` consumes (supergroup matches flagged `via`). */
export function emittersOf(fam: string): Candidate[] {
  return CARDPOOL
    .filter((c) => c.out != null && subsumes(fam, c.out))
    .map((c) => ({ ...c, via: c.out !== fam, port: c.out as string }))
    .sort((a, b) => tierRank[a.tier] - tierRank[b.tier]);
}

/** Cards that consume what `fam` emits (supergroup matches flagged `via`). */
export function consumersOf(fam: string): Candidate[] {
  return CARDPOOL
    .filter((c) => c.in != null && subsumes(c.in, fam))
    .map((c) => ({ ...c, via: c.in !== fam, port: c.in as string }))
    .sort((a, b) => tierRank[a.tier] - tierRank[b.tier]);
}

// ── Deck Synergy web: port grain ─────────────────────────────────────────────
export const PORT_PKG = ["Ramp", "Aristocrats", "Card draw", "ETB / drain"];
export interface Port { card: string; in: string | null; out: string | null; pkg: number; }

export const PORTS: Port[] = [
  { card: "Sol Ring", in: null, out: "mana", pkg: 0 },
  { card: "Pitiless Plunderer", in: "death", out: "mana", pkg: 0 },
  { card: "Chatterfang", in: null, out: "token", pkg: 1 },
  { card: "Grave Pact", in: "death", out: "sacrifice", pkg: 1 },
  { card: "Blight Mound", in: "death", out: "damage", pkg: 1 },
  { card: "Ashnod's Altar", in: "sacrifice", out: "mana", pkg: 1 },
  { card: "Carrion Feeder", in: "sacrifice", out: "counter", pkg: 1 },
  { card: "Warren Soultrader", in: "sacrifice", out: "token", pkg: 1 },
  { card: "Skullclamp", in: "death", out: "card", pkg: 2 },
  { card: "Midnight Reaper", in: "death", out: "card", pkg: 2 },
  { card: "Impact Tremors", in: "token", out: "damage", pkg: 3 },
  { card: "Zulaport Cutthroat", in: "death", out: "life", pkg: 3 },
];

export const SYNERGY_SPARSE = new Set([
  "Chatterfang", "Grave Pact", "Ashnod's Altar", "Carrion Feeder", "Skullclamp", "Impact Tremors",
]);

// ── Deck Lens: decklists, rings, near-miss, coverage rows ────────────────────
export const DECKS: Record<"full" | "sparse", string> = {
  full: `1x Chatterfang, Squirrel General
1x Pitiless Plunderer
1x Ashnod's Altar
1x Blight Mound
1x Phantom Train
1x Carrion Feeder
1x Warren Soultrader
1x Sol Ring
1x Skullclamp
1x Grave Pact
1x Dictate of Erebos
1x Mycloth, Sylvan Emissary
1x Poison-Tip Archer
… 87 more`,
  sparse: `1x Blight Mound
1x Phantom Train
1x Ashnod's Altar
1x Grave Pact
… 9 more`,
};

export interface Ring { cards: string; ring: string; tier: Tier; pop: number; conf?: number; }
export const RINGS: Record<"full" | "sparse", Ring[]> = {
  full: [
    { cards: "Chatterfang + Pitiless Plunderer + Ashnod's Altar", ring: "token → sacrifice → mana", tier: "Green", pop: 994 },
    { cards: "Blight Mound + Phantom Train", ring: "token → sacrifice → death", tier: "Amber", pop: 23 },
    { cards: "Carrion Feeder + Warren Soultrader", ring: "sacrifice → death → token", tier: "Inferred", conf: 0.61, pop: 311 },
  ],
  sparse: [
    { cards: "Blight Mound + Phantom Train", ring: "token → sacrifice → death", tier: "Amber", pop: 23 },
  ],
};

export interface NearMissCand { name: string; evidence: string; price: string; score: number | string; }
export interface NearMiss { missing: string; ring: string; resultTier: Tier; cands: NearMissCand[]; }

const cand = (a: [string, string, string, number | string]): NearMissCand => ({ name: a[0], evidence: a[1], price: a[2], score: a[3] });
export const NEARMISS: Record<"full" | "sparse", NearMiss[]> = {
  full: [
    { missing: "a sacrifice outlet", ring: "token → ? → death", resultTier: "Green", cands: [
      cand(["Viscera Seer", "in 41% of decks", "$0.25", 92]),
      cand(["Carrion Feeder", "seen w/ 67 combos", "$1.10", 78]),
      cand(["Ashnod's Altar", "seen w/ 90 combos", "$28", "62"]),
    ] },
    { missing: "a death payoff", ring: "sacrifice → death → ?", resultTier: "Amber", cands: [
      cand(["Mikaeus, the Unhallowed", "blocks 1,115 combos", "$18", 88]),
      cand(["Zulaport Cutthroat", "in 33% of decks", "$3.50", 71]),
    ] },
  ],
  sparse: [
    { missing: "a token producer", ring: "? → sacrifice → death", resultTier: "Green", cands: [
      cand(["Chatterfang", "seen w/ 90 combos", "$6.20", 84]),
      cand(["Bitterblossom", "in 22% of decks", "$9", "58"]),
    ] },
  ],
};

// Coverage-chart rows: [fam, emit{own,subs?,note?}, consume{own,subs?}]
export interface CoverSide { own: number; subs?: [string, number][]; note?: string; }
export type CoverRow = [string, CoverSide, CoverSide];

export const COVERAGE: Record<"dense" | "sparse", CoverRow[]> = {
  dense: [
    ["mana", { own: 22 }, { own: 8 }],
    ["token", { own: 14 }, { own: 4 }],
    ["sacrifice", { own: 3, note: "death" }, { own: 17 }],
    ["death", { own: 6, subs: [["sacrifice", 5]] }, { own: 6, subs: [["sacrifice", 4]] }],
    ["counter", { own: 5 }, { own: 3 }],
    ["combat", { own: 7 }, { own: 2 }],
    ["card", { own: 9 }, { own: 8, subs: [["mill", 2]] }],
    ["recursion", { own: 6 }, { own: 5 }],
  ],
  sparse: [
    ["token", { own: 5 }, { own: 1 }],
    ["sacrifice", { own: 1, note: "death" }, { own: 6 }],
    ["death", { own: 3, subs: [["sacrifice", 2]] }, { own: 2, subs: [["sacrifice", 2]] }],
    ["mana", { own: 2 }, { own: 1 }],
  ],
};

// ── Cover / headline stats ───────────────────────────────────────────────────
export const HEADLINE_STATS = [
  { value: "31,284", label: "cards parsed" },
  { value: "95,001", label: "combos reconstructed" },
  { value: "17 · 45", label: "families · lines" },
  { value: "51 / 3,286", label: "archetypes realized" },
];

// ── Card imagery (mock uses Scryfall named-image; prod → CardRow.imageUri*) ───
export const cardImage = (name: string): string =>
  `https://api.scryfall.com/cards/named?exact=${encodeURIComponent(name)}&format=image&version=normal`;

/** Inject each family hue as a CSS variable (`--fam-<name>`) on :root. */
export function applyFamilyVars(): void {
  if (typeof document === "undefined") return;
  const root = document.documentElement;
  for (const [key, f] of Object.entries(FAM)) root.style.setProperty(`--fam-${key}`, f.hue);
}
