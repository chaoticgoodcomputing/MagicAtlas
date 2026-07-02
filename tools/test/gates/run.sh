#!/usr/bin/env bash
#
# Self-tests for the MAST loop meta-gates (tools/gate-*.sh).
#
# Hermetic: builds throwaway git repos under a temp dir, exercises each gate's good
# AND bad paths, asserts the exit codes. No network, no dependency on the live corpus.
# This is the one "diagonal" — it is bash (it tests the meta machinery) but it runs in
# CI like a core test, because it is fully self-contained.
#
# Usage: bash tools/test/gates/run.sh
# Exit:  0 = every self-test passed; 1 = at least one failed.

set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
TOOLS="$(cd "$HERE/../.." && pwd)"   # repo/tools

PASS=0
FAIL=0
ok()  { PASS=$((PASS + 1)); echo "  ok   — $1"; }
bad() { FAIL=$((FAIL + 1)); echo "  FAIL — $1" >&2; }

# assert_zero <desc> <cmd...>    — command must exit 0
assert_zero() {
  local desc="$1"; shift
  if "$@" >/dev/null 2>&1; then ok "$desc"; else bad "$desc (expected exit 0, got $?)"; fi
}
# assert_nonzero <desc> <cmd...> — command must exit nonzero
assert_nonzero() {
  local desc="$1"; shift
  if "$@" >/dev/null 2>&1; then bad "$desc (expected nonzero, got 0)"; else ok "$desc"; fi
}

mk_repo() {
  local dir="$1"
  mkdir -p "$dir"
  command git init -q "$dir"
  command git -C "$dir" config user.email t@t.t
  command git -C "$dir" config user.name t
  command git -C "$dir" config commit.gpgsign false
}

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# ---------------------------------------------------------------------------
echo "gate-judge-verdict.sh"
printf '%s\n' '{"items":[{"target":"X","verdict":"PASS","citations":["CR 702.1"],"reason":"ok"}]}' > "$WORK/good.json"
printf '%s\n' '{"items":[{"target":"X","verdict":"PASS"},{"target":"Y","verdict":"FAIL","reason":"bad"}]}' > "$WORK/fail.json"
printf '%s\n' '{not valid json' > "$WORK/malformed.json"
printf '%s\n' '{"items":[]}' > "$WORK/empty.json"

assert_zero    "all-PASS verdict passes"   bash "$TOOLS/gate-judge-verdict.sh" "$WORK/good.json"
assert_nonzero "FAIL verdict fails"        bash "$TOOLS/gate-judge-verdict.sh" "$WORK/fail.json"
assert_nonzero "malformed JSON fails"      bash "$TOOLS/gate-judge-verdict.sh" "$WORK/malformed.json"
assert_nonzero "empty items fails"         bash "$TOOLS/gate-judge-verdict.sh" "$WORK/empty.json"
assert_nonzero "missing file fails"        bash "$TOOLS/gate-judge-verdict.sh" "$WORK/nope.json"

# ---------------------------------------------------------------------------
echo "gate-fixture-immutability.sh"
R="$WORK/fixrepo"
mk_repo "$R"
mkdir -p "$R/fixtures"
printf '{"a":1}\n' > "$R/fixtures/Existing.json"
command git -C "$R" add -A
command git -C "$R" commit -qm base
FBASE="$(command git -C "$R" rev-parse HEAD)"

command git -C "$R" checkout -qb edits-gold
printf '{"a":2}\n' > "$R/fixtures/Existing.json"
command git -C "$R" commit -qam edit

command git -C "$R" checkout -q "$FBASE"
command git -C "$R" checkout -qb adds-only
printf '{"b":1}\n' > "$R/fixtures/New.json"
command git -C "$R" add -A
command git -C "$R" commit -qm add

assert_nonzero "branch editing a gold fails" \
  env MAST_REPO_DIR="$R" MAST_FIXTURES_DIR=fixtures bash "$TOOLS/gate-fixture-immutability.sh" "$FBASE" edits-gold
assert_zero "branch adding only passes" \
  env MAST_REPO_DIR="$R" MAST_FIXTURES_DIR=fixtures bash "$TOOLS/gate-fixture-immutability.sh" "$FBASE" adds-only
assert_nonzero "missing base sha errors" \
  env MAST_REPO_DIR="$R" MAST_FIXTURES_DIR=fixtures bash "$TOOLS/gate-fixture-immutability.sh" deadbeef adds-only

# ---------------------------------------------------------------------------
echo "gate-preflight.sh"
P="$WORK/prerepo"
mk_repo "$P"
printf 'x\n' > "$P/f"
command git -C "$P" add -A
command git -C "$P" commit -qm base

assert_zero "clean repo under thresholds passes" \
  env MAST_REPO_DIR="$P" bash "$TOOLS/gate-preflight.sh"

printf 'y\n' > "$P/f"
assert_nonzero "dirty tree fails" \
  env MAST_REPO_DIR="$P" bash "$TOOLS/gate-preflight.sh"
command git -C "$P" checkout -- f

command git -C "$P" branch mast-tdd/2026-01-01-a
command git -C "$P" branch mast-tdd/2026-01-01-b
command git -C "$P" branch mast-tdd/2026-01-01-c
assert_nonzero "over branch threshold fails" \
  env MAST_REPO_DIR="$P" MAST_MAX_TDD_BRANCHES=2 bash "$TOOLS/gate-preflight.sh"
assert_zero "under raised branch threshold passes" \
  env MAST_REPO_DIR="$P" MAST_MAX_TDD_BRANCHES=10 bash "$TOOLS/gate-preflight.sh"

# ---------------------------------------------------------------------------
echo "gate-isolation.sh"
WT="$WORK/proj/.claude/worktrees/wt1"
mk_repo "$WT"
printf 'x\n' > "$WT/f"
command git -C "$WT" add -A
command git -C "$WT" commit -qm base
WBASE="$(command git -C "$WT" rev-parse HEAD)"

assert_zero "isolated worktree on base passes" \
  env MAST_REPO_DIR="$WT" bash "$TOOLS/gate-isolation.sh" "$WBASE"
assert_nonzero "wrong base fails" \
  env MAST_REPO_DIR="$WT" bash "$TOOLS/gate-isolation.sh" 0000000000000000000000000000000000000000

NOTWT="$WORK/plainrepo"
mk_repo "$NOTWT"
printf 'x\n' > "$NOTWT/f"
command git -C "$NOTWT" add -A
command git -C "$NOTWT" commit -qm base
NBASE="$(command git -C "$NOTWT" rev-parse HEAD)"
assert_nonzero "non-worktree checkout fails isolation" \
  env MAST_REPO_DIR="$NOTWT" bash "$TOOLS/gate-isolation.sh" "$NBASE"

# ---------------------------------------------------------------------------
echo
echo "gate self-tests: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ] || exit 1
