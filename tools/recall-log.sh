#!/usr/bin/env bash
# recall-log.sh — append the current wide-recall numbers to a git-tracked history and print the delta.
#
# The mast-tdd-loop optimizes L2 parse coverage, but the PRODUCT is trustworthy reconstructed combos.
# extended-recall-report.json carries that number (green/amber/missed + recallAtGreen +
# popularity-weighted recall) — but it lives under gitignored Data/_08_Reporting, so per-batch deltas
# were historically invisible. This appends a one-line snapshot to docs/judgments/recall-log.jsonl
# (tracked) each batch and prints the delta vs the previous entry, so the batch report can quote the
# number that actually moves. Regenerate the source first with `nx run mast:recall-report`.
#
# Usage: bash tools/recall-log.sh <label>          # e.g. bash tools/recall-log.sh b11
#        bash tools/recall-log.sh <label> --dry     # print delta, do NOT append
set -euo pipefail

LABEL="${1:?usage: recall-log.sh <label> [--dry]}"
DRY="${2:-}"
ROOT="$(git -C "$(dirname "$0")/.." rev-parse --show-toplevel)"
REPORT="$ROOT/tests/magic-ast-tests/Data/_08_Reporting/extended-recall-report.json"
LOG="$ROOT/docs/judgments/recall-log.jsonl"

[ -f "$REPORT" ] || { echo "recall-log: $REPORT not found — run 'nx run mast:recall-report' first" >&2; exit 1; }

DATE="$(date -u +%Y-%m-%d)"
cur="$(jq -c --arg d "$DATE" --arg l "$LABEL" '{
  date:$d, label:$l,
  green, amber, missed, reconstructed, projectionReadyCombos,
  recallAtGreen, popularityWeightedRecall
}' "$REPORT")"

prev=""
[ -f "$LOG" ] && prev="$(tail -n 1 "$LOG" 2>/dev/null || true)"

fmt_delta() {  # $1 field, $2 pct(1|0)
  local f="$1" pct="$2" c p d
  c="$(jq -r ".$f" <<<"$cur")"
  [ -z "$prev" ] && { printf '%s' "$c"; return; }
  p="$(jq -r ".$f // 0" <<<"$prev")"
  if [ "$pct" = "1" ]; then
    d="$(awk -v a="$c" -v b="$p" 'BEGIN{printf "%+.3fpp", (a-b)*100}')"
    printf '%.4f (%s)' "$c" "$d"
  else
    d="$(awk -v a="$c" -v b="$p" 'BEGIN{printf "%+d", a-b}')"
    printf '%s (%s)' "$c" "$d"
  fi
}

echo "=== wide-recall $LABEL (vs $( [ -n "$prev" ] && jq -r .label <<<"$prev" || echo none )) ==="
echo "  green:                  $(fmt_delta green 0)"
echo "  amber:                  $(fmt_delta amber 0)"
echo "  missed:                 $(fmt_delta missed 0)"
echo "  reconstructed:          $(fmt_delta reconstructed 0)"
echo "  recallAtGreen:          $(fmt_delta recallAtGreen 1)"
echo "  popularityWtdRecall:    $(fmt_delta popularityWeightedRecall 1)"

if [ "$DRY" = "--dry" ]; then
  echo "(dry run — not appended)"
else
  printf '%s\n' "$cur" >> "$LOG"
  echo "appended to docs/judgments/recall-log.jsonl (commit it with the batch)"
fi
