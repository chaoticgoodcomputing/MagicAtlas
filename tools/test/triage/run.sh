#!/usr/bin/env bash
#
# Self-tests for the triage cluster homogeneity gate (tools/gate-triage-cluster.sh).
#
# Hermetic: builds throwaway triage-report JSON fixtures and asserts the gate's exit codes.
# CI-safe; no dependency on a live triage run.
#
# Usage: bash tools/test/triage/run.sh
# Exit:  0 = every self-test passed; 1 = at least one failed.

set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../.." && pwd)"
GATE="$REPO/tools/gate-triage-cluster.sh"

PASS=0
FAIL=0
ok()  { PASS=$((PASS + 1)); echo "  ok   — $1"; }
bad() { FAIL=$((FAIL + 1)); echo "  FAIL — $1" >&2; }
assert_zero()    { local d="$1"; shift; if "$@" >/dev/null 2>&1; then ok "$d"; else bad "$d (expected 0, got $?)"; fi; }
assert_nonzero() { local d="$1"; shift; if "$@" >/dev/null 2>&1; then bad "$d (expected nonzero, got 0)"; else ok "$d"; fi; }

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# Report: rank 1 homogeneous (1.0), rank 2 heterogeneous (0.5), rank 3 just-below (0.84).
cat > "$WORK/report.json" <<'EOF'
{
  "TopYieldClusters": [
    {"Rank": 1, "Template": "<SELF> deals <N> damage", "DominantShare": 1.0},
    {"Rank": 2, "Template": "<SUBTYPE> <TYPE>",        "DominantShare": 0.5},
    {"Rank": 3, "Template": "<KW> <SUBTYPE>",          "DominantShare": 0.84}
  ]
}
EOF

# Report missing DominantShare (pre-initiative-02 shape) → precondition error.
cat > "$WORK/old.json" <<'EOF'
{"TopYieldClusters":[{"Rank":1,"Template":"x"}]}
EOF

echo "gate-triage-cluster.sh"

# Audit mode is advisory → exit 0 regardless of heterogeneous clusters.
assert_zero "audit mode exits 0 (advisory)" bash "$GATE" "$WORK/report.json"

# Dispatch-guard: a homogeneous rank passes.
assert_zero "dispatch-guard passes a homogeneous rank" bash "$GATE" "$WORK/report.json" 1

# Dispatch-guard: a heterogeneous rank fails.
assert_nonzero "dispatch-guard fails a heterogeneous rank" bash "$GATE" "$WORK/report.json" 2

# Dispatch-guard: just-below-threshold rank fails (0.84 < 0.85).
assert_nonzero "dispatch-guard fails a just-below-threshold rank" bash "$GATE" "$WORK/report.json" 3

# Dispatch-guard: any bad rank in a set fails the whole pick.
assert_nonzero "dispatch-guard fails when any rank in the set is bad" bash "$GATE" "$WORK/report.json" 1 2

# Threshold override: lowering it below 0.5 lets rank 2 pass.
assert_zero "lowered threshold lets a low cluster pass" \
  env MAST_MIN_HOMOGENEITY=0.4 bash "$GATE" "$WORK/report.json" 2

# Missing DominantShare → precondition error (exit 2, nonzero).
assert_nonzero "report without DominantShare errors" bash "$GATE" "$WORK/old.json" 1

# Missing file → error.
assert_nonzero "missing report errors" bash "$GATE" "$WORK/nope.json" 1

echo
echo "triage gate self-tests: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ] || exit 1
