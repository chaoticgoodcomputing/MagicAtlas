#!/usr/bin/env bash
#
# Self-tests for the discriminator governance lint (libs/magic-ast/scripts/lint-discriminators.py).
#
# Hermetic: builds throwaway synthetic .cs source + baseline/justification files under a temp
# dir and asserts the lint's exit codes. No dependency on the live AST tree. CI-safe.
#
# Usage: bash tools/test/schema/run.sh
# Exit:  0 = every self-test passed; 1 = at least one failed.

set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
REPO="$(cd "$HERE/../../.." && pwd)"
LINT="$REPO/libs/magic-ast/scripts/lint-discriminators.py"

PASS=0
FAIL=0
ok()  { PASS=$((PASS + 1)); echo "  ok   — $1"; }
bad() { FAIL=$((FAIL + 1)); echo "  FAIL — $1" >&2; }

assert_zero()    { local d="$1"; shift; if "$@" >/dev/null 2>&1; then ok "$d"; else bad "$d (expected 0, got $?)"; fi; }
assert_nonzero() { local d="$1"; shift; if "$@" >/dev/null 2>&1; then bad "$d (expected nonzero, got 0)"; else ok "$d"; fi; }

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
EMPTY_BASELINE="$WORK/empty-baseline.json"   # intentionally absent → empty baseline

lint() { python3 "$LINT" --source-root "$1" --baseline "$2" --justifications "$3"; }

echo "lint-discriminators.py"

# 1. Clean: one unique discriminator, empty baseline → pass.
S1="$WORK/clean"; mkdir -p "$S1"
printf '[OracleEffect("uniqueThing")]\npublic sealed record A : Effect {}\n' > "$S1/A.cs"
assert_zero "clean single discriminator passes" lint "$S1" "$EMPTY_BASELINE" "$WORK/none.json"

# 2. HARD FAIL: same discriminator twice within one family.
S2="$WORK/dup"; mkdir -p "$S2"
printf '[OracleEffect("dupz")]\npublic sealed record A : Effect {}\n' > "$S2/A.cs"
printf '[OracleEffect("dupz")]\npublic sealed record B : Effect {}\n' > "$S2/B.cs"
assert_nonzero "duplicate within a family hard-fails" lint "$S2" "$EMPTY_BASELINE" "$WORK/none.json"

# 3. NOT a collision: same string in DIFFERENT families (per-family scoping).
S3="$WORK/crossfamily"; mkdir -p "$S3"
printf '[OracleEffect("xyzzy")]\npublic sealed record A : Effect {}\n' > "$S3/A.cs"
printf '[OracleCost("xyzzy")]\npublic sealed record B : Cost {}\n' > "$S3/B.cs"
assert_zero "same string across families passes (per-family, not global)" lint "$S3" "$EMPTY_BASELINE" "$WORK/none.json"

# 4. SOFT FAIL: a NEW discriminator near an existing baseline one, no justification.
S4="$WORK/near"; mkdir -p "$S4"
printf '[OracleEffect("frobnicates")]\npublic sealed record A : Effect {}\n' > "$S4/A.cs"
printf '%s\n' '{"discriminators":["OracleEffect:frobnicate"]}' > "$WORK/base-frob.json"
assert_nonzero "new near-duplicate soft-fails without justification" lint "$S4" "$WORK/base-frob.json" "$WORK/none.json"

# 5. ...and passes once a justification entry is added.
printf '%s\n' '[{"name":"frobnicates","near":"frobnicate","reason":"distinct test concept"}]' > "$WORK/just-frob.json"
assert_zero "near-duplicate passes with a justification entry" lint "$S4" "$WORK/base-frob.json" "$WORK/just-frob.json"

# 6. SOFT FAIL: prefix-stem near-dup (dealDamage / dealDamageToEach), no justification.
S6="$WORK/stem"; mkdir -p "$S6"
printf '[OracleEffect("dealDamageToEach")]\npublic sealed record A : Effect {}\n' > "$S6/A.cs"
printf '%s\n' '{"discriminators":["OracleEffect:dealDamage"]}' > "$WORK/base-dd.json"
assert_nonzero "prefix-stem near-duplicate soft-fails" lint "$S6" "$WORK/base-dd.json" "$WORK/none.json"

# 7. New discriminator far from everything → passes (not over-eager).
S7="$WORK/farnew"; mkdir -p "$S7"
printf '[OracleEffect("teleportation")]\npublic sealed record A : Effect {}\n' > "$S7/A.cs"
printf '%s\n' '{"discriminators":["OracleEffect:dealDamage"]}' > "$WORK/base-dd2.json"
assert_zero "distinct new discriminator passes" lint "$S7" "$WORK/base-dd2.json" "$WORK/none.json"

echo
echo "lint self-tests: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ] || exit 1
