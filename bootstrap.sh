#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────
# Bootstrap everything that isn't in git: Scryfall bulk data, the pipeline
# raw-input symlinks, and the Python venv for the OracleEmbedding step.
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
RAW="$REPO_ROOT/apps/atlas/Data/_01_Raw/Datasets"

echo "→ Creating data directories"
mkdir -p "$DUMPS" "$RAW"

# ── 1. Scryfall oracle-cards bulk (~165 MB) ──────────────────────────
# Consumed by both the API seeder (dumps/) and the Flowthru pipeline (_01_Raw/).
# We fetch once into dumps/ and symlink into the pipeline's expected location.
if [[ -f "$DUMPS/oracle-cards.json" ]]; then
  echo "✓ dumps/oracle-cards.json already present"
else
  echo "→ Resolving Scryfall oracle-cards bulk download URL..."
  URL=$(curl -s https://api.scryfall.com/bulk-data/oracle-cards \
    | python3 -c "import json,sys; print(json.load(sys.stdin)['download_uri'])")
  echo "→ Downloading $URL"
  curl -L --progress-bar -o "$DUMPS/oracle-cards.json" "$URL"
fi

if [[ -e "$RAW/oracle-cards.json" ]]; then
  echo "✓ _01_Raw/Datasets/oracle-cards.json already present"
else
  echo "→ Symlinking oracle-cards.json into the pipeline's raw inputs"
  ln -sf "$DUMPS/oracle-cards.json" "$RAW/oracle-cards.json"
fi

# ── 2. Scryfall symbology (tiny) ─────────────────────────────────────
# Only the pipeline's RawCardSymbols catalog entry reads this file. The API
# fetches symbology via the HTTP API at seed time, so we don't put it in dumps/.
if [[ -f "$RAW/oracle-card-symbols.json" ]]; then
  echo "✓ _01_Raw/Datasets/oracle-card-symbols.json already present"
else
  echo "→ Downloading Scryfall symbology"
  curl -s -o "$RAW/oracle-card-symbols.json" https://api.scryfall.com/symbology
fi

# ── 3. MTG comprehensive rules (optional, only for RulesProcessing) ──
if [[ ! -f "$RAW/mtg-rules.txt" ]]; then
  echo "ℹ mtg-rules.txt not downloaded (only needed for RulesProcessing flow)."
  echo "  Grab it from https://magic.wizards.com/en/rules if you want to run that flow."
fi

# ── 4. Python venv for the OracleEmbedding step ──────────────────────
VENV="$REPO_ROOT/apps/atlas/.venv"
if [[ -d "$VENV" ]]; then
  echo "✓ Python venv already present at apps/atlas/.venv"
else
  echo "→ Creating Python venv at apps/atlas/.venv"
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
echo "  2. dotnet run --project apps/atlas -- --flows CardProcessing,OracleEmbedding"
echo "       (first run downloads the BERT model ~90 MB; UMAP takes ~2 min)"
echo "  3. dotnet run --project apps/atlas-api"
echo "       (seeds all five tables on first run from Scryfall + dumps/)"
echo "  4. cd apps/atlas-web && pnpm install && pnpm dev"
echo "       (open http://localhost:5173)"
echo "───────────────────────────────────────────────────────────────"
