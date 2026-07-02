#!/usr/bin/env bash
#
# META GATE — fixture immutability (worker-scoped).
#
# Exits nonzero if a worker branch MODIFIES or DELETES any existing gold fixture
# under Fixtures/HandParsedCards/ (additions are always allowed). The orchestrator
# runs this per worker branch before merge (Step 4).
#
# Worker-scoped and unconditional: there is NO allowlist. A worker that edits a gold
# to make its own test pass is the canonical self-confirmation drift vector, and the
# gold Output AST has no external truth to assert against in the core ring (the gold
# IS the spec) — so the only defense is to forbid the worker from touching it.
#
# Back-propagation (a parser change that legitimately re-points other cards' golds)
# is an ORCHESTRATOR action off the worker path, governed by core-green + a mandatory
# re-judge — see docs/scratch/alignment-session/01_deterministic-loop-gates.md.
#
# Usage: bash tools/gate-fixture-immutability.sh <base-sha> <branch>
# Env:   MAST_REPO_DIR        (default ".")
#        MAST_FIXTURES_DIR    (default "tests/magic-ast-tests/Fixtures/HandParsedCards")
# Exit:  0 = clean (additions only); 1 = gate failed (HALT); 2 = usage / precondition error.

set -uo pipefail

REPO_DIR="${MAST_REPO_DIR:-.}"
FIXTURES_DIR="${MAST_FIXTURES_DIR:-tests/magic-ast-tests/Fixtures/HandParsedCards}"

BASE="${1:-}"
BRANCH="${2:-}"
if [ -z "$BASE" ] || [ -z "$BRANCH" ]; then
  echo "usage: $0 <base-sha> <branch>" >&2
  exit 2
fi

git() { command git -C "$REPO_DIR" "$@"; }

if ! git rev-parse --verify "$BASE" >/dev/null 2>&1; then
  echo "FAIL: base sha not found: $BASE" >&2
  exit 2
fi
if ! git rev-parse --verify "$BRANCH" >/dev/null 2>&1; then
  echo "FAIL: branch/ref not found: $BRANCH" >&2
  exit 2
fi

# Modified / Deleted / Renamed existing fixtures between base and branch tip.
touched="$(git diff --diff-filter=MDR --name-only "$BASE..$BRANCH" -- "$FIXTURES_DIR")"
if [ -n "$touched" ]; then
  echo "FAIL: branch '$BRANCH' modifies/deletes existing gold fixture(s):" >&2
  echo "$touched" | sed 's/^/  /' >&2
  echo "Workers may only ADD fixtures. If a gold is wrong, STOP and report —" >&2
  echo "back-prop is an orchestrator action (core-green + mandatory re-judge), not worker work." >&2
  exit 1
fi

added="$(git diff --diff-filter=A --name-only "$BASE..$BRANCH" -- "$FIXTURES_DIR" | grep -c . || true)"
echo "PASS: '$BRANCH' modifies no existing fixtures ($added added)"
exit 0
