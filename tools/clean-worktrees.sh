#!/usr/bin/env bash
#
# Reap MAST TDD-loop agent worktrees and their branches.
#
# The loop spawns one isolated git worktree per family agent (under
# .claude/worktrees/, gitignored). Subagent worktrees are only auto-removed by
# Claude Code when they finish *clean*; ours finish with commits, so they
# persist, and the branch refs accumulate. Left unchecked this reaches hundreds
# of worktrees (a prior run hit 318), which is what caused the worktree pool to
# fall back to in-place checkouts. Run this after every batch.
#
# Usage:
#   bash tools/clean-worktrees.sh            # remove agent worktrees; delete MERGED agent branches only
#   bash tools/clean-worktrees.sh --force    # also force-delete UNMERGED agent branches (use after DISCARDING a batch)
#
# Only touches worktrees under .claude/worktrees/ and branches matching
# mast-tdd/* or worktree-agent-*. The primary checkout and integration branch
# are never touched.

set -uo pipefail

ROOT="$(git rev-parse --show-toplevel)" || { echo "not in a git repo" >&2; exit 1; }
cd "$ROOT"

FORCE=""
[ "${1:-}" = "--force" ] && FORCE=1

before="$(git worktree list | wc -l | tr -d ' ')"

# 1. Remove every worktree registered under .claude/worktrees/ (the agent pool).
#    --force --force removes even locked / dirty worktrees.
git worktree list --porcelain 2>/dev/null \
  | awk '/^worktree /{print $2}' \
  | grep '/\.claude/worktrees/' \
  | while IFS= read -r wt; do
      git worktree remove --force --force "$wt" 2>/dev/null || true
    done
git worktree prune 2>/dev/null || true

after="$(git worktree list | wc -l | tr -d ' ')"
echo "worktrees: ${before} -> ${after}"

# 2. Delete agent branches. Merged-only (-d) by default; -D with --force.
del_flag="-d"
[ -n "$FORCE" ] && del_flag="-D"
deleted=0
while IFS= read -r b; do
  [ -z "$b" ] && continue
  if git branch "$del_flag" "$b" >/dev/null 2>&1; then
    deleted=$((deleted + 1))
  fi
done < <(git for-each-ref --format='%(refname:short)' 'refs/heads/mast-tdd/*' 'refs/heads/worktree-agent-*')

remaining="$(git for-each-ref --format='%(refname:short)' 'refs/heads/mast-tdd/*' 'refs/heads/worktree-agent-*' | wc -l | tr -d ' ')"
echo "agent branches deleted: ${deleted}; remaining: ${remaining}"
if [ "$remaining" != "0" ] && [ -z "$FORCE" ]; then
  echo "  (remaining branches are unmerged; re-run with --force if you intend to discard them)"
fi
