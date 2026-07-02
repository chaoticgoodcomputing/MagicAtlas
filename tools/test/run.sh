#!/usr/bin/env bash
#
# Aggregator for all MAST loop self-tests (the bash/meta tier).
#
# Runs every tools/test/<area>/run.sh — the deterministic gate self-tests (initiative 01),
# the discriminator-lint self-tests (initiative 02), the triage homogeneity gate self-tests
# (initiative 02), and any future sibling. Each sub-runner is hermetic and CI-safe.
#
# Usage: bash tools/test/run.sh
# Exit:  0 = all sub-suites passed; 1 = any sub-suite failed.

set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
fail=0

for runner in "$HERE"/*/run.sh; do
  [ -f "$runner" ] || continue
  echo "=== $(basename "$(dirname "$runner")") ==="
  if ! bash "$runner"; then
    fail=1
  fi
  echo
done

if [ "$fail" -eq 0 ]; then
  echo "ALL meta self-test suites passed."
  exit 0
fi
echo "Some meta self-test suite(s) FAILED." >&2
exit 1
