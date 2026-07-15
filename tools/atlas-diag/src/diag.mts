// atlas-diag — one-off card diagnostics over the committed CardAtlas datasets.
//
// This is a *consumer* of the _08_Reporting dumps (what the frontend eventually
// serves), NOT a Flowthru flow. Flowthru's job ends when the datasets are
// written; this reads them back to answer "what will the frontend consume for
// card X, and does it look right?" — spans sliced against oracle text, ports,
// combos, tier/presence — and (optionally) diffs that against what the live
// GraphQL API actually returns, to bisect data-layer vs seed/endpoint bugs.
//
// Runs under Node's native TS type-stripping (node --experimental-strip-types);
// no build, no deps. Invoked via the nx `card` / `find` targets.

import { readFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

// ── Dump schemas (mirror _08_Reporting/Schemas/CardAtlas.cs SerializedLabels) ──
interface PortRow {
  card: string;
  label: string;
  family: string;
  side: string; // "emit" | "consume" | "" (inferred/declared)
  oracleLineIndex: number;
  spans: number[][] | null; // [[start,end), …] half-open offsets into oracle text
  tier: string; // Green | Amber | Inferred | Declared
  confidence?: number | null;
}
interface MetaRow {
  card: string;
  colorIdentity: string;
  cmc: number;
  typeLine: string;
  portCount: number;
}
interface ComboRow {
  comboId: string;
  cards: string;
  familyRing: string;
  tier: string;
  firable: boolean;
  results: string;
  popularity: number;
}
// card-inputs.json (_02_Intermediate) — the MAST parse input; carries oracle text.
interface CardInput {
  Name: string;
  ManaCost?: string;
  TypeLine?: string;
  OracleText?: string;
  ColorIdentity?: string[];
  CardFaces?: { OracleText?: string }[];
}

// ── tiny ANSI (no-op when piped or NO_COLOR) ──────────────────────────────────
const useColor = process.stdout.isTTY && !process.env.NO_COLOR;
const c = (code: string, s: string) => (useColor ? `\x1b[${code}m${s}\x1b[0m` : s);
const bold = (s: string) => c("1", s);
const dim = (s: string) => c("2", s);
const red = (s: string) => c("31", s);
const green = (s: string) => c("32", s);
const yellow = (s: string) => c("33", s);
const cyan = (s: string) => c("36", s);

// ── arg parsing (flags + positionals; tolerant of nx passthrough) ─────────────
function parseArgs(argv: string[]) {
  const flags: Record<string, string | boolean> = {};
  const positional: string[] = [];
  for (let i = 0; i < argv.length; i++) {
    const a = argv[i];
    if (a.startsWith("--")) {
      const eq = a.indexOf("=");
      if (eq >= 0) flags[a.slice(2, eq)] = a.slice(eq + 1);
      else if (i + 1 < argv.length && !argv[i + 1].startsWith("--")) flags[a.slice(2)] = argv[++i];
      else flags[a.slice(2)] = true;
    } else positional.push(a);
  }
  return { flags, positional };
}

// ── data loading ──────────────────────────────────────────────────────────────
const here = path.dirname(fileURLToPath(import.meta.url));
const WORKSPACE = path.resolve(here, "../../.."); // tools/atlas-diag/src → repo root

function resolveDataRoot(flag?: string | boolean): string {
  if (typeof flag === "string") return path.resolve(flag);
  if (process.env.ATLAS_DATA_DIR) return path.resolve(process.env.ATLAS_DATA_DIR);
  return path.join(WORKSPACE, "tests", "magic-ast-tests", "Data");
}
function loadJson<T>(file: string): T {
  try {
    return JSON.parse(readFileSync(file, "utf8")) as T;
  } catch (e) {
    console.error(red(`✗ cannot read ${path.relative(WORKSPACE, file)}: ${(e as Error).message}`));
    process.exit(2);
  }
}
function loadData(dataRoot: string) {
  const rep = path.join(dataRoot, "_08_Reporting");
  const ports = loadJson<PortRow[]>(path.join(rep, "card-ports.json"));
  const metas = loadJson<MetaRow[]>(path.join(rep, "card-meta.json"));
  const combos = loadJson<ComboRow[]>(path.join(rep, "combo-instances.json"));
  const rawInputs = loadJson<{ Input: CardInput }[]>(
    path.join(dataRoot, "_02_Intermediate", "Datasets", "card-inputs.json"),
  );
  const oracleByName = new Map<string, string>();
  for (const r of rawInputs) if (!oracleByName.has(r.Input.Name)) oracleByName.set(r.Input.Name, oracleTextOf(r.Input));
  return { ports, metas, combos, oracleByName };
}

/** Mirror CardAtlasShared.Project's text resolution so span offsets line up. */
function oracleTextOf(inp: CardInput): string {
  let t = inp.OracleText ?? "";
  if (!t.trim() && inp.CardFaces && inp.CardFaces.length > 0)
    t = inp.CardFaces.map((f) => f.OracleText ?? "").filter((s) => s.length > 0).join("\n\n");
  return t;
}

/** Per-line [start,end) offsets into the full oracle text (split on \n). */
function lineBounds(text: string): { start: number; end: number }[] {
  const res: { start: number; end: number }[] = [];
  let off = 0;
  for (const line of text.split("\n")) {
    res.push({ start: off, end: off + line.length });
    off += line.length + 1; // + the \n
  }
  return res;
}

// ── name resolution (exact, else case-insensitive suggestions) ────────────────
function resolveName(query: string, metas: MetaRow[]): { exact?: string; suggestions: string[] } {
  const exact = metas.find((m) => m.card === query);
  if (exact) return { exact: exact.card, suggestions: [] };
  const q = query.toLowerCase();
  const ci = metas.find((m) => m.card.toLowerCase() === q);
  if (ci) return { exact: ci.card, suggestions: [] };
  const subs = metas.filter((m) => m.card.toLowerCase().includes(q)).map((m) => m.card).slice(0, 12);
  return { suggestions: subs };
}

// ── live API diff ─────────────────────────────────────────────────────────────
interface ApiResult {
  ok: boolean;
  status: number;
  count: number;
  families: string[];
  note?: string;
}
async function apiPorts(url: string, name: string): Promise<ApiResult> {
  const query =
    "query($n:String!){ discover{ atlas{ portRows(where:{card:{eq:$n}}, first:100){ totalCount nodes{ family side tier } } } } }";
  try {
    const res = await fetch(url, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ query, variables: { n: name } }),
    });
    if (res.status !== 200) return { ok: false, status: res.status, count: 0, families: [], note: `HTTP ${res.status}` };
    const json = (await res.json()) as {
      errors?: { message: string }[];
      data?: { discover?: { atlas?: { portRows?: { totalCount: number; nodes: { family: string }[] } } } };
    };
    if (json.errors?.length) return { ok: false, status: 200, count: 0, families: [], note: json.errors[0].message };
    const pr = json.data?.discover?.atlas?.portRows;
    const families = [...new Set((pr?.nodes ?? []).map((n) => n.family))].sort();
    return { ok: true, status: 200, count: pr?.totalCount ?? 0, families };
  } catch (e) {
    return { ok: false, status: 0, count: 0, families: [], note: (e as Error).message };
  }
}

// ── `card` — the deep single-card view ────────────────────────────────────────
async function cmdCard(flags: Record<string, string | boolean>, positional: string[]) {
  const name = (flags.name as string) ?? positional[0];
  if (!name) {
    console.error("usage: nx run atlas-diag:card -- --name \"Card Name\"  [--no-api] [--api <url>]");
    process.exit(1);
  }
  const { ports, metas, combos, oracleByName } = loadData(resolveDataRoot(flags.data));
  const { exact, suggestions } = resolveName(name, metas);
  if (!exact) {
    console.log(red(`No card named "${name}" in the dataset.`));
    if (suggestions.length) console.log(dim("Did you mean:\n  ") + suggestions.join("\n  "));
    else console.log(dim("(no substring matches either — check the combo-card union scope)"));
    process.exit(1);
  }

  const meta = metas.find((m) => m.card === exact)!;
  const cardPorts = ports
    .filter((p) => p.card === exact)
    .sort((a, b) => a.oracleLineIndex - b.oracleLineIndex || (a.spans?.[0]?.[0] ?? 0) - (b.spans?.[0]?.[0] ?? 0));
  const text = oracleByName.get(exact) ?? "";
  const bounds = lineBounds(text);
  const spanKey = (s: number[][] | null) => (s ? JSON.stringify(s) : "∅");
  const spanCounts = new Map<string, number>();
  for (const p of cardPorts) spanCounts.set(spanKey(p.spans), (spanCounts.get(spanKey(p.spans)) ?? 0) + 1);

  // header
  console.log(
    "\n" +
      bold(exact) +
      "  " +
      dim(`${meta.colorIdentity || "colorless"} · MV ${meta.cmc} · ${meta.typeLine}`),
  );
  const comboMatches = combos.filter((k) => k.cards.split(" + ").some((n) => n === exact));
  console.log(
    dim(
      `dump: ${cardPorts.length} ports · combos: ${comboMatches.length} · meta.portCount: ${meta.portCount}`,
    ),
  );

  // ports + span slices
  console.log("\n" + bold("Ports (dump)") + dim("  ⚠ flags coarse/duplicated spans"));
  if (!cardPorts.length) console.log(dim("  (none — parse-unready and no backfill, or outside the combo union)"));
  for (const p of cardPorts) {
    const s = p.spans?.[0];
    const b = bounds[p.oracleLineIndex];
    const wholeLine = s && b && s[0] <= b.start && s[1] >= b.end;
    const shared = (spanCounts.get(spanKey(p.spans)) ?? 0) > 1;
    const flagsStr =
      (wholeLine ? " " + yellow("⚠ whole-line") : "") + (shared ? " " + yellow("⚠ shared-span") : "");
    const tierC = p.tier === "Green" ? green(p.tier) : p.tier === "Amber" ? yellow(p.tier) : dim(p.tier);
    const sideC = p.side === "emit" ? cyan("emit   ") : p.side === "consume" ? "consume" : dim("infer  ");
    console.log(
      `  L${p.oracleLineIndex} ${sideC} ${p.label.padEnd(38)} ${tierC.padEnd(useColor ? 14 : 6)} ${dim(
        s ? `[${s[0]},${s[1]}]` : "no-span",
      )}${flagsStr}`,
    );
    if (s) console.log("       " + dim(JSON.stringify(text.slice(s[0], s[1]))));
  }

  // combos
  if (comboMatches.length) {
    const top = comboMatches.sort((a, b) => b.popularity - a.popularity).slice(0, 8);
    console.log("\n" + bold(`Combos (${comboMatches.length}, top by popularity)`));
    for (const k of top) {
      const tierC = k.tier === "Green" ? green(k.tier) : yellow(k.tier);
      console.log(`  ${k.cards.padEnd(48)} ${dim(k.familyRing).padEnd(useColor ? 30 : 22)} ${tierC}  ${dim("pop " + k.popularity)}`);
    }
  }

  // api diff
  if (flags["no-api"]) return;
  const apiUrl = (flags.api as string) ?? process.env.ATLAS_API_URL ?? "http://localhost:55250/graphql";
  const api = await apiPorts(apiUrl, exact);
  console.log("\n" + bold("API diff") + dim(`  (${apiUrl})`));
  if (!api.ok) {
    console.log(
      "  " +
        red(`⚠ API unreachable/error (${api.note}).`) +
        ` dump has ${cardPorts.length} ports; the frontend would show none → bug is downstream of the data (seed/endpoint), not the dump.`,
    );
  } else if (api.count !== cardPorts.length) {
    console.log(
      "  " +
        red(`⚠ MISMATCH — dump ${cardPorts.length} ports, api ${api.count}.`) +
        ` The datasets are ahead of the seeded DB → reseed / promote. (Bug is downstream of the data.)`,
    );
  } else {
    console.log("  " + green(`✓ match — dump and api both ${api.count} ports.`) + dim(` families: ${api.families.join(", ")}`));
  }
}

// ── `find` — filter/search the dataset ────────────────────────────────────────
function cmdFind(flags: Record<string, string | boolean>, positional: string[]) {
  const { ports, metas } = loadData(resolveDataRoot(flags.data));
  const query = ((flags.query as string) ?? (flags.name as string) ?? positional[0] ?? "").toLowerCase();
  const family = flags.family as string | undefined;
  const side = flags.side as string | undefined;
  const tier = flags.tier as string | undefined;
  const limit = Number(flags.limit ?? 40);

  // cards satisfying the port filters (family/side/tier), if any given
  let allow: Set<string> | null = null;
  if (family || side || tier) {
    allow = new Set(
      ports
        .filter(
          (p) =>
            (!family || p.family === family) &&
            (!side || p.side === side) &&
            (!tier || p.tier === tier),
        )
        .map((p) => p.card),
    );
  }
  let rows = metas.filter((m) => (!query || m.card.toLowerCase().includes(query)) && (!allow || allow.has(m.card)));
  rows = rows.sort((a, b) => a.card.localeCompare(b.card));
  const filterDesc = [
    query && `name~"${query}"`,
    family && `family=${family}`,
    side && `side=${side}`,
    tier && `tier=${tier}`,
  ]
    .filter(Boolean)
    .join(" · ");
  console.log(bold(`${rows.length} cards`) + dim(filterDesc ? `  (${filterDesc})` : "  (all)") + dim(rows.length > limit ? ` — showing ${limit}` : ""));
  for (const m of rows.slice(0, limit)) {
    let matched = "";
    if (allow) {
      const labels = ports
        .filter((p) => p.card === m.card && (!family || p.family === family) && (!side || p.side === side) && (!tier || p.tier === tier))
        .map((p) => p.label);
      matched = dim("  " + [...new Set(labels)].join(", "));
    }
    console.log(
      `  ${m.card.padEnd(42)} ${dim(`${(m.colorIdentity || "—").padEnd(5)} MV${String(m.cmc).padEnd(2)} ${m.portCount}p`)}${matched}`,
    );
  }
  if (rows.length > limit) console.log(dim(`  … +${rows.length - limit} more (raise --limit)`));
}

// ── main ──────────────────────────────────────────────────────────────────────
const { flags, positional } = parseArgs(process.argv.slice(2));
const command = positional.shift();
switch (command) {
  case "card":
    await cmdCard(flags, positional);
    break;
  case "find":
    cmdFind(flags, positional);
    break;
  default:
    console.error(
      "atlas-diag — card diagnostics over the CardAtlas dumps\n\n" +
        '  nx run atlas-diag:card -- --name "Chatterfang, Squirrel General"\n' +
        '  nx run atlas-diag:find -- --family sacrifice --side emit\n' +
        '  nx run atlas-diag:find -- --query squirrel\n\n' +
        "flags: --no-api, --api <url>, --data <Data dir>, --limit N, --tier, --side",
    );
    process.exit(command ? 1 : 0);
}
