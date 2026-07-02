#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────
# Bootstrap the ONE bit that isn't in git and can't be pulled by a pipeline or
# the running app: the Python venv (sentence-transformers + UMAP). Everything
# else now self-fetches over HTTP — a fresh clone needs no browser or curl.
#
# What's handled by the Flowthru pipelines directly (no manual step):
#   • atlas-flows: RawCards / RawCardSymbols auto-fetch over HTTP (with
#     Flowthru's conditional-GET cache under tests/atlas-flow-test/.http-cache/).
#   • mtg-rules: the comprehensive-rules text auto-fetches over HTTP.
#   • magic-ast-tests: the Commander Spellbook variants.json dump loads as a
#     Flowthru HTTP catalog item (CsbVariantsRaw), cached the same way.
#
# What's handled by the running API (no manual step):
#   • Cards, Rulings, Sets, Symbology — AtlasSeeder streams all four from
#     Scryfall's HTTP API on first startup when the DB tables are empty.
#   • atlas_points — produced by running the OracleEmbedding Flowthru flow
#     (see the "Run the pipeline" section below).
#
# Idempotent: re-running won't recreate the venv if it already exists.
# ─────────────────────────────────────────────────────────────────────
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Python venv for the OracleEmbedding step ─────────────────────────
# Lives alongside the lib's pyproject.toml so the Flowthru host (configured by
# tests/atlas-flow-test/Program.cs) can resolve both module search paths and the venv
# from a single libs/atlas-flows location.
VENV="$REPO_ROOT/libs/atlas-flows/.venv"
if [[ -d "$VENV" ]]; then
  echo "✓ Python venv already present at libs/atlas-flows/.venv"
else
  echo "→ Creating Python venv at libs/atlas-flows/.venv"
  python3 -m venv "$VENV"
fi

# sentence-transformers pulls torch (~2 GB) — warn the user.
if ! "$VENV/bin/python" -c "import sentence_transformers, umap, pandas, pyarrow" 2>/dev/null; then
  echo "→ Installing Python dependencies (sentence-transformers + torch ≈ 2 GB; this takes a few minutes)"
  "$VENV/bin/pip" install --quiet --upgrade pip
  "$VENV/bin/pip" install --quiet \
    pandas \
    pyarrow \
    sentence-transformers \
    umap-learn
  echo "✓ Python deps installed"
else
  echo "✓ Python deps already installed"
fi

echo ""
echo "───────────────────────────────────────────────────────────────"
echo "Bootstrap complete. To bring the atlas online end-to-end:"
echo ""
echo "  1. docker compose -f apps/atlas-api/docker-compose.yml up -d"
echo "  2. dotnet run --project tests/atlas-flow-test"
echo "       (auto-fetches the Scryfall oracle bulk + symbology over HTTP;"
echo "        first run downloads the BERT model ~90 MB; UMAP takes ~2 min)"
echo "  3. dotnet run --project apps/atlas-api"
echo "       (seeds all five tables on first run — cards/rulings/sets/symbology"
echo "        stream from Scryfall over HTTP, atlas_points from atlas-flow-test)"
echo "  4. cd apps/atlas-web && pnpm install && pnpm dev"
echo "       (open http://localhost:5173)"
echo "───────────────────────────────────────────────────────────────"
