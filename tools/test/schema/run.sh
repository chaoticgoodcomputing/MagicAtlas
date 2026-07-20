#!/usr/bin/env bash
#
# Self-tests for the discriminator governance lint (libs/magic-ast/scripts/lint-discriminators.py).
#
# Hermetic: builds throwaway synthetic .cs source under a temp dir and asserts the lint's exit codes.
# No dependency on the live AST tree. CI-safe.
#
# The lint has no data files any more (ADR-0004 issue #38): the baseline is gone (bce69ad7) and the
# justification whitelist moved onto the discriminator attributes at their declaration sites. So the
# only thing these tests can set up is SOURCE, and the only fatal condition is a hard per-family
# collision — the near-duplicate half is a report and never changes the exit code.
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
assert_stdout()  { local d="$1" pat="$2"; shift 2; if "$@" 2>/dev/null | grep -q "$pat"; then ok "$d"; else bad "$d (stdout missing /$pat/)"; fi; }

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

lint() { python3 "$LINT" --source-root "$1"; }

echo "lint-discriminators.py"

# 1. Clean: one unique discriminator → pass.
S1="$WORK/clean"; mkdir -p "$S1"
printf '[OracleEffect("uniqueThing")]\npublic sealed record A : Effect {}\n' > "$S1/A.cs"
assert_zero "clean single discriminator passes" lint "$S1"

# 2. HARD FAIL (the gate): same discriminator twice within one family.
S2="$WORK/dup"; mkdir -p "$S2"
printf '[OracleEffect("dupz")]\npublic sealed record A : Effect {}\n' > "$S2/A.cs"
printf '[OracleEffect("dupz")]\npublic sealed record B : Effect {}\n' > "$S2/B.cs"
assert_nonzero "duplicate within a family hard-fails" lint "$S2"

# 3. NOT a collision: same string in DIFFERENT families (per-family scoping).
S3="$WORK/crossfamily"; mkdir -p "$S3"
printf '[OracleEffect("xyzzy")]\npublic sealed record A : Effect {}\n' > "$S3/A.cs"
printf '[OracleCost("xyzzy")]\npublic sealed record B : Cost {}\n' > "$S3/B.cs"
assert_zero "same string across families passes (per-family, not global)" lint "$S3"

# 4. An UNEXPLAINED near-duplicate is REPORTED but not fatal (it is a design question, not a defect).
S4="$WORK/near"; mkdir -p "$S4"
printf '[OracleEffect("frobnicate")]\npublic sealed record A : Effect {}\n' > "$S4/A.cs"
printf '[OracleEffect("frobnicates")]\npublic sealed record B : Effect {}\n' > "$S4/B.cs"
assert_zero   "unexplained near-duplicate does not fail the lint" lint "$S4"
assert_stdout "unexplained near-duplicate is reported" "UNEXPLAINED" lint "$S4"

# 5. ...and is no longer reported once the declaration site carries the ruling.
printf '[OracleEffect(\n  "frobnicates",\n  NearDuplicateOf = new[] { "frobnicate" },\n  Reason = "distinct test concept"\n)]\npublic sealed record B : Effect {}\n' > "$S4/B.cs"
assert_zero   "explained near-duplicate passes" lint "$S4"
assert_stdout "explained near-duplicate counts as explained" "1 explained at their declaration site" lint "$S4"

# 6. Prefix-stem near-dup (dealDamage / dealDamageToEach) is detected as near.
S6="$WORK/stem"; mkdir -p "$S6"
printf '[OracleEffect("dealDamage")]\npublic sealed record A : Effect {}\n' > "$S6/A.cs"
printf '[OracleEffect("dealDamageToEach")]\npublic sealed record B : Effect {}\n' > "$S6/B.cs"
assert_stdout "prefix-stem near-duplicate is detected" "UNEXPLAINED" lint "$S6"

# 7. Distinct discriminators are not over-eagerly flagged.
S7="$WORK/far"; mkdir -p "$S7"
printf '[OracleEffect("dealDamage")]\npublic sealed record A : Effect {}\n' > "$S7/A.cs"
printf '[OracleEffect("teleportation")]\npublic sealed record B : Effect {}\n' > "$S7/B.cs"
assert_stdout "distinct discriminators report zero pairs" "0 intra-family near-duplicate pair" lint "$S7"

# 8. A DEAD ruling (counterpart no longer declared) is reported, not fatal.
S8="$WORK/dead"; mkdir -p "$S8"
printf '[OracleEffect(\n  "frobnicates",\n  NearDuplicateOf = new[] { "frobnicate" },\n  Reason = "distinct test concept"\n)]\npublic sealed record B : Effect {}\n' > "$S8/B.cs"
assert_zero   "dead ruling does not fail the lint" lint "$S8"
assert_stdout "dead ruling is reported" "DEAD RULING" lint "$S8"

echo
echo "lint self-tests: $PASS passed, $FAIL failed"
[ "$FAIL" -eq 0 ] || exit 1
