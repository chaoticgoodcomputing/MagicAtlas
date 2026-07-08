#!/usr/bin/env bash
#
# META GATE — judge verdict.
#
# Exits nonzero if the judge's machine-readable verdict file is missing, malformed,
# or contains any non-PASS verdict. The orchestrator runs this at Step 4 (and after
# any back-prop re-judge); the HALT decision stops being a prose judgment call.
#
# Verdict schema:
#   { "items": [ { "target": "<fixture-or-node>",
#                  "verdict": "PASS" | "FAIL",
#                  "citations": ["CR 603.10", ...],
#                  "reason": "..." }, ... ] }
#
# Usage: bash tools/gate-judge-verdict.sh <verdict.json> [<verdict.json> ...]
#   Accepts one OR MANY verdict files — one per judge shard when the judge is
#   sharded across disjoint branch groups (judging is read-only, so it fans out;
#   see the mast-tdd-loop skill). EVERY file must exist, parse, be non-empty, and
#   carry only PASS verdicts; any failure across any shard is a HALT.
# Exit:  0 = all PASS (proceed); 1 = gate failed (HALT); 2 = usage / precondition error.

set -uo pipefail

if [ "$#" -lt 1 ]; then
  echo "usage: $0 <verdict.json> [<verdict.json> ...]" >&2
  exit 2
fi

if ! command -v jq >/dev/null 2>&1; then
  echo "FAIL: jq is not available (required to parse the verdict)" >&2
  exit 2
fi

total=0
for VERDICT in "$@"; do
  if [ ! -f "$VERDICT" ]; then
    echo "FAIL: verdict file not found: $VERDICT" >&2
    exit 1
  fi

  if ! jq -e . "$VERDICT" >/dev/null 2>&1; then
    echo "FAIL: malformed JSON: $VERDICT" >&2
    exit 1
  fi

  if ! jq -e '(.items | type) == "array" and (.items | length) > 0' "$VERDICT" >/dev/null 2>&1; then
    echo "FAIL: .items is missing, not an array, or empty: $VERDICT" >&2
    exit 1
  fi

  nonpass="$(jq -r '.items[] | select(.verdict != "PASS")
    | "  \(.target // "<no target>"): \(.verdict // "<no verdict>") — \(.reason // "")"' "$VERDICT")"
  if [ -n "$nonpass" ]; then
    echo "FAIL: non-PASS verdict(s) in $VERDICT:" >&2
    echo "$nonpass" >&2
    exit 1
  fi

  count="$(jq -r '.items | length' "$VERDICT")"
  total=$((total + count))
done

echo "PASS: $total verdict(s) across $# shard(s), all PASS"
exit 0
