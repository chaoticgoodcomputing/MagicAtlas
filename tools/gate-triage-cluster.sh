#!/usr/bin/env bash
#
# META GATE — triage cluster homogeneity (alignment initiative 02).
#
# topYieldClusters are grouped by EXACT normalized template, so members trivially share the
# template. The residual heterogeneity is template *over-collapse*: one normalized template
# lumping oracle lines that bail in DIFFERENT parsers (e.g. "<SUBTYPE> <TYPE>" covering
# "Enchant land" and unrelated lines). A worker handed such a "family" lands a rule correct
# for only part of it. The signal is each cluster's DominantShare (diagnostic-spread
# homogeneity in [0,1], emitted by YieldClusterAnalyzer); a low value means the cluster's
# lines fail in several different ways.
#
# Two uses:
#   audit     bash tools/gate-triage-cluster.sh <report.json>
#             list every heterogeneous cluster; exit 0 (advisory — "exclude these from dispatch").
#   dispatch  bash tools/gate-triage-cluster.sh <report.json> <rank> [<rank> ...]
#             exit nonzero if any NAMED rank (a cluster you intend to dispatch) is heterogeneous.
#
# Env:  MAST_MIN_HOMOGENEITY (default 0.85 — i.e. >15% of failure signals diverging = exclude)
# Exit: 0 = clear / advisory list; 1 = an intended cluster is heterogeneous (HALT that pick);
#       2 = usage / precondition error.

set -uo pipefail

REPORT="${1:-}"
if [ -z "$REPORT" ]; then
  echo "usage: $0 <triage-report.json> [rank ...]" >&2
  exit 2
fi
shift || true
THRESH="${MAST_MIN_HOMOGENEITY:-0.85}"

command -v jq >/dev/null 2>&1 || { echo "FAIL: jq required" >&2; exit 2; }
[ -f "$REPORT" ] || { echo "FAIL: report not found: $REPORT" >&2; exit 2; }
jq -e '.TopYieldClusters | type == "array"' "$REPORT" >/dev/null 2>&1 \
  || { echo "FAIL: malformed report (no TopYieldClusters[])" >&2; exit 2; }
if ! jq -e '.TopYieldClusters[0] | has("DominantShare")' "$REPORT" >/dev/null 2>&1; then
  echo "FAIL: report has no DominantShare — regenerate with: nx run mast:run" >&2
  exit 2
fi

# rank<TAB>share<TAB>template for clusters below the homogeneity threshold.
hetero="$(jq -r --argjson t "$THRESH" \
  '.TopYieldClusters[] | select(.DominantShare < $t) | "\(.Rank)\t\(.DominantShare)\t\(.Template)"' \
  "$REPORT")"

if [ "$#" -eq 0 ]; then
  if [ -z "$hetero" ]; then
    echo "PASS: all clusters >= $THRESH homogeneity."
  else
    n="$(printf '%s\n' "$hetero" | grep -c .)"
    echo "$n heterogeneous cluster(s) (DominantShare < $THRESH) — exclude from dispatch until re-clustered:"
    printf '%s\n' "$hetero" | awk -F'\t' '{printf "  rank %s  share=%.2f  %s\n",$1,$2,$3}'
  fi
  exit 0
fi

bad=""
for rank in "$@"; do
  printf '%s\n' "$hetero" | awk -F'\t' -v r="$rank" '$1==r{found=1} END{exit !found}' && bad="$bad $rank"
done
if [ -n "$bad" ]; then
  echo "FAIL: intended cluster rank(s)$bad are heterogeneous (DominantShare < $THRESH) — do not dispatch:" >&2
  for rank in $bad; do
    printf '%s\n' "$hetero" | awk -F'\t' -v r="$rank" '$1==r{printf "  rank %s  share=%.2f  %s\n",$1,$2,$3}' >&2
  done
  exit 1
fi
echo "PASS: intended cluster rank(s)$(printf ' %s' "$@") all >= $THRESH homogeneity."
exit 0
