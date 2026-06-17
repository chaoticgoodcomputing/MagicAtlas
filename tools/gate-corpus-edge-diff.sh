#!/usr/bin/env bash
# Corpus-edge-diff gate — the deterministic defense against the OVERFIT FAIL class (mast-tdd-loop).
#
# A new/edited parser rule can silently mislabel a SIBLING corpus card (drop a filter, flip a derived
# kind). It is invisible to the worker's targeted suite (the sibling has no gold) AND to the per-combo
# bench (33 pinned combos). It WAS caught ~4x (Mindcrank, Rings, Hapatra) only by judge diligence. This
# gate mechanizes that: it diffs the per-card port-projection signatures of card-edges.json (the union
# interaction graph over ~2,900 cards) between the batch base and the merged tip, and HALTs if ANY card
# that was NOT a dispatch target changed its interaction footprint — unless named in the carve-out.
#
# Usage:
#   tools/gate-corpus-edge-diff.sh <baseline-sigs.json> <current-card-edges.json> <dispatched-cards-csv> [carve-out.json]
#     baseline-sigs.json   signatures snapshot from the batch BASE (tools/corpus-edge-signatures.py at base)
#     current-card-edges   card-edges.json regenerated after merge (nx run mast:interaction-triage)
#     dispatched-cards-csv comma-separated card NAMES this batch targeted (legitimately may change)
#     carve-out.json       named cross-card reprojections, default tests/.../edge-diff-expected.json
# Exit 0 = clean; nonzero = HALT (an un-whitelisted non-target card's edges changed — likely an overfit).
set -euo pipefail

BASELINE="${1:?baseline sigs json}"
CARDEDGES="${2:?current card-edges.json}"
DISPATCHED="${3:-}"
CARVEOUT="${4:-tests/magic-ast-tests/Fixtures/edge-diff-expected.json}"
HERE="$(cd "$(dirname "$0")/.." && pwd)"

CUR="$(mktemp)"
trap 'rm -f "$CUR"' EXIT
python3 "$HERE/tools/corpus-edge-signatures.py" "$CARDEDGES" > "$CUR"

python3 - "$BASELINE" "$CUR" "$DISPATCHED" "$CARVEOUT" <<'PY'
import json, sys
baseline, current, dispatched_csv, carveout_path = sys.argv[1:5]
base = json.load(open(baseline))
cur = json.load(open(current))
dispatched = {c.strip() for c in dispatched_csv.split(",") if c.strip()}
carve = set()
try:
    co = json.load(open(carveout_path))
    carve = {e["card"] for e in co.get("entries", [])}
except FileNotFoundError:
    pass

changed = sorted(c for c in set(base) | set(cur) if base.get(c) != cur.get(c))
unexpected = [c for c in changed if c not in dispatched and c not in carve]

print(f"corpus-edge-diff: {len(changed)} card(s) changed | "
      f"{len([c for c in changed if c in dispatched])} dispatched(expected) | "
      f"{len([c for c in changed if c in carve])} carve-out | {len(unexpected)} UNEXPECTED")
if unexpected:
    print("\nHALT — these NON-TARGET cards changed their interaction footprint (likely an overfit "
          "mislabeling a sibling). Investigate the merged rules; if a change is a legitimate cross-card "
          "reprojection, add a named entry to " + carveout_path + " (card + reason), never silently:")
    for c in unexpected[:60]:
        print(f"  - {c}  ({'NEW' if c not in base else 'REMOVED' if c not in cur else 'CHANGED'})")
    if len(unexpected) > 60:
        print(f"  … and {len(unexpected) - 60} more")
    sys.exit(1)
print("PASS: every changed card was a dispatch target or a named carve-out.")
PY
