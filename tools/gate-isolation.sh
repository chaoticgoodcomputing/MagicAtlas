#!/usr/bin/env bash
#
# META GATE — worker isolation.
#
# Exits nonzero if the caller is NOT running inside an isolated agent worktree on the
# orchestrator-named base SHA. Each worker runs this FIRST (Step 0), before creating
# its branch or making any edit. Replaces the prose self-check in mast-worker.md.
#
# Two independent failure modes, both of which have corrupted past runs:
#   - toplevel is the main checkout (isolation: worktree was forgotten) -> ISOLATION FAILED
#   - HEAD is not the base the orchestrator named (stale ancestor)      -> WRONG BASE
#
# Usage: bash tools/gate-isolation.sh <expected-base-sha>
# Env:   MAST_REPO_DIR          (default "." — run from inside the worktree)
#        MAST_WORKTREE_MARKER   (default "/.claude/worktrees/")
# Exit:  0 = isolated on base (proceed); 1 = isolation/base check failed (STOP); 2 = usage error.

set -uo pipefail

REPO_DIR="${MAST_REPO_DIR:-.}"
WORKTREE_MARKER="${MAST_WORKTREE_MARKER:-/.claude/worktrees/}"

BASE="${1:-}"
if [ -z "$BASE" ]; then
  echo "usage: $0 <expected-base-sha>" >&2
  exit 2
fi

git() { command git -C "$REPO_DIR" "$@"; }

toplevel="$(git rev-parse --show-toplevel 2>/dev/null)" || {
  echo "FAIL: not a git repository: $REPO_DIR" >&2
  exit 2
}

case "$toplevel" in
  *"$WORKTREE_MARKER"*) ;;
  *)
    echo "ISOLATION FAILED: toplevel '$toplevel' is not under '$WORKTREE_MARKER'." >&2
    echo "                 Running in the main checkout — make NO changes. STOP." >&2
    exit 1
    ;;
esac

head="$(git rev-parse HEAD)"
base_full="$(git rev-parse "$BASE" 2>/dev/null)" || base_full="$BASE"
if [ "$head" != "$base_full" ]; then
  echo "WRONG BASE: HEAD '$head' != expected base '$base_full'. STOP." >&2
  exit 1
fi

echo "PASS: isolated worktree '$toplevel' on base '$head'"
exit 0
