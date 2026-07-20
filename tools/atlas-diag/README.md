# atlas-diag

One-off card diagnostics over the generated **CardAtlas** datasets — for "why does
card X look wrong in the explorer?" without spinning up the frontend.

It is a **consumer** of the `_08_Reporting` dumps (the same data the API seeds
from and the frontend eventually serves), not a Flowthru flow. Flowthru's job
ends when the datasets are written; this reads them back. Runs under Node's
native TS type-stripping — no build, no deps.

Those dumps are **Derived artifacts** (ADR 0004 §3): gitignored build outputs,
generated on demand, so a clean checkout has none of them. `atlas-diag` does not
fall back to a committed copy — it exits 2 naming the target that produces the
missing file:

```
✗ tests/magic-ast-tests/Data/_08_Reporting/card-ports.json is missing.
  It is a Derived artifact (ADR 0004 §3): gitignored, generated on demand.
  Run `nx run mast:recall-report` first. See docs/design/pipeline-regeneration.md.
```

Against the default `--data` root (`tests/magic-ast-tests/Data`),
`nx run mast:run` produces `card-inputs.json` and `nx run mast:recall-report`
produces the CardAtlas dumps.

## Why this exists — bisecting the data chain

```
OracleParser ─► PortWalk ─► CardPortsStep ─► _08_Reporting/*.json ─► seed ─► Postgres ─► GraphQL ─► frontend
   (parse)      (ports+spans)  (union filter)     THE DUMP           (API / plumbing layer)
```

A card can look wrong in the UI for reasons in different layers. `atlas-diag`
reads the dump **and** queries the live API, then tells you which side is wrong:

- **Coarse/duplicated spans** → a data-layer (parser / PortWalk) issue. Flagged
  with `⚠ whole-line` (span covers the entire oracle line) and `⚠ shared-span`
  (multiple ports point at the identical range).
- **Ports present in the dump but missing from the API** → a downstream
  seed/endpoint issue, not the data. Reported as an `API diff` mismatch.

## Usage

```bash
# Deep single-card view: ports + spans sliced against oracle text, combos,
# tier/presence, and a diff vs the live GraphQL API.
nx run atlas-diag:card -- --name "Chatterfang, Squirrel General"
nx run atlas-diag:card -- --name "Ashnod's Altar" --no-api      # skip the API diff

# Find/filter cards in the dataset
nx run atlas-diag:find -- --query squirrel
nx run atlas-diag:find -- --family sacrifice --side emit --tier Green
```

Flags: `--no-api`, `--api <url>` (default `http://localhost:55250/trax/graphql`, or
`$ATLAS_API_URL`), `--data <Data dir>` (default `tests/magic-ast-tests/Data`, or
`$ATLAS_DATA_DIR`), `--limit N`, `--side`, `--tier`, `--family`.

## Reading the `card` output

- `⚠ whole-line` on `evasion:forestwalk` / replacement lines = the span was not
  refined below the oracle line (keyword + replacement projections still emit the
  whole line; only triggered/activated abilities split cost vs effect).
- `⚠ shared-span` on `pay:mana:black` + `sac:…` = a multi-component cost mints
  several ports off one span instead of `{B}` vs `Sacrifice X Squirrels`.
- `API diff … MISMATCH / unreachable` = the datasets are ahead of the seeded DB
  (or the endpoint is down) — reseed/promote; nothing to fix in MAST.
