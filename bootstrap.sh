#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────
# Bootstrap the bits that aren't in git and can't be pulled by the pipeline
# itself: the Python venv (sentence-transformers + UMAP), and the atlas-api's
# Scryfall oracle-cards seed file at dumps/oracle-cards.json.
#
# What's NO LONGER in this script (handled by the Flowthru pipeline directly):
#   • Pipeline's RawCards / RawCardSymbols / RawRules — the atlas-flows lib
#     now auto-fetches all three over HTTP, with Flowthru's conditional-GET
#     caching under tests/atlas-flow-test/.http-cache/.
#
# What's NOT in this script (because the running API handles it):
#   • Rulings, Sets, and card-Symbology — AtlasSeeder fetches these from
#     Scryfall's HTTP API on first API startup when the DB tables are empty.
#   • atlas_points — produced by running the OracleEmbedding Flowthru flow
#     (see the "Run the pipeline" section below).
#
# Idempotent: re-running won't re-download if files already exist.
# ─────────────────────────────────────────────────────────────────────
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DUMPS="$REPO_ROOT/dumps"

echo "→ Creating data directories"
mkdir -p "$DUMPS"

# ── 1. Scryfall oracle-cards bulk (~165 MB) for atlas-api seeder ─────
# The atlas-api's AtlasSeeder reads dumps/oracle-cards.json on first start
# to populate its card table. The Flowthru pipeline auto-fetches its own
# copy through Flowthru.Extensions.Http and doesn't share this file.
# TODO: migrate the API seeder to HTTP-direct too, and remove this step.
if [[ -f "$DUMPS/oracle-cards.json" ]]; then
  echo "✓ dumps/oracle-cards.json already present"
else
  echo "→ Resolving Scryfall oracle-cards bulk download URL..."
  URL=$(curl -s https://api.scryfall.com/bulk-data/oracle-cards \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['download_uri'])")
  echo "→ Downloading $URL"
  curl -L --progress-bar -o "$DUMPS/oracle-cards.json" "$URL"
fi

# ── 2. Python venv for the OracleEmbedding step ──────────────────────
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
echo "       (auto-fetches Scryfall bulk + symbology + MTG rules over HTTP;"
echo "        first run downloads the BERT model ~90 MB; UMAP takes ~2 min)"
echo "  3. dotnet run --project apps/atlas-api"
echo "       (seeds all five tables on first run from Scryfall + atlas-flow-test outputs)"
echo "  4. cd apps/atlas-web && pnpm install && pnpm dev"
echo "       (open http://localhost:5173)"
echo "───────────────────────────────────────────────────────────────"
