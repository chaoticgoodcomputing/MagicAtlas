#!/usr/bin/env bash
#
# META GATE — pre-dispatch hygiene.
#
# Exits nonzero if the loop environment is in a state that has historically led to
# incidents: too many leftover agent branches, too many agent worktrees (a pool once
# reached 318), or a dirty working tree. The orchestrator runs this at Step 0 before
# dispatching any worker; it folds the "remember to clean up" reminder into a hard gate.
#
# Usage: bash tools/gate-preflight.sh
# Env:   MAST_REPO_DIR           (default ".")
#        MAST_MAX_TDD_BRANCHES   (default 30)
#        MAST_MAX_WORKTREES      (default 20)
# Exit:  0 = clear (dispatch ok); 1 = gate failed (HALT and clean up).

set -uo pipefail

REPO_DIR="${MAST_REPO_DIR:-.}"
MAX_BRANCHES="${MAST_MAX_TDD_BRANCHES:-30}"
MAX_WORKTREES="${MAST_MAX_WORKTREES:-20}"

git() { command git -C "$REPO_DIR" "$@"; }

fail=0

branches="$(git for-each-ref --format='%(refname:short)' \
  'refs/heads/mast-tdd/*' 'refs/heads/worktree-agent-*' | grep -c . || true)"
if [ "$branches" -gt "$MAX_BRANCHES" ]; then
  echo "FAIL: $branches agent branches (mast-tdd/* + worktree-agent-*) > $MAX_BRANCHES." >&2
  echo "      Run: nx run mast:worktree-clean" >&2
  fail=1
else
  echo "ok: $branches agent branches (<= $MAX_BRANCHES)"
fi

worktrees="$(git worktree list --porcelain 2>/dev/null \
  | awk '/^worktree /{print $2}' | grep -c '/\.claude/worktrees/' || true)"
if [ "$worktrees" -gt "$MAX_WORKTREES" ]; then
  echo "FAIL: $worktrees agent worktrees under .claude/worktrees/ > $MAX_WORKTREES." >&2
  echo "      Run: nx run mast:worktree-clean" >&2
  fail=1
else
  echo "ok: $worktrees agent worktrees (<= $MAX_WORKTREES)"
fi

if [ -n "$(git status --porcelain)" ]; then
  echo "FAIL: working tree is dirty. Commit, stash, or clean before dispatch." >&2
  fail=1
else
  echo "ok: working tree clean"
fi

if [ "$fail" -eq 0 ]; then
  echo "PASS: preflight clear"
  exit 0
fi
exit 1
