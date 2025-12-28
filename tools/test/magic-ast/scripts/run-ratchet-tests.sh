#!/usr/bin/env bash
set +e  # Don't exit on error - we need to parse the output
set -uo pipefail

# Ratchet Test Runner for MagicAST.Tests
# 
# This script runs dotnet test and parses the output to determine the exit code:
# - Exit 0: No regressions detected (stable failures are OK)
# - Exit 1: Regressions detected (tests that were passing now fail)
#
# This allows CI to pass when tests fail IF those failures are already tracked
# in the baseline, while still blocking PRs that introduce new regressions.

# Capture output and exit code
output_file=$(mktemp)
trap 'rm -f "$output_file"' EXIT

# Run tests with detailed output, capturing both stdout and stderr
dotnet test --logger:"console;verbosity=detailed" 2>&1 | tee "$output_file"
test_exit_code=${PIPESTATUS[0]}

# Check for regression markers in the output
if grep -q "RESULT: FAILED" "$output_file"; then
    echo ""
    echo "❌ RATCHET TEST FAILURE: Regressions detected"
    echo "Tests that were passing in the baseline are now failing."
    echo "Run 'nx run test/magic-ast:test:update-baseline' to accept these changes."
    exit 1
fi

# Check for the success marker
if grep -q "RESULT: PASSED - No regressions detected" "$output_file"; then
    echo ""
    echo "✅ RATCHET TEST SUCCESS: No regressions detected"
    
    # Count stable failures if present (using portable grep)
    stable_failures=$(grep -o "STABLE FAILURES ([0-9]*)" "$output_file" | grep -o "[0-9]*" || echo "0")
    if [ "$stable_failures" -gt 0 ]; then
        echo "ℹ️  Note: $stable_failures stable failure(s) tracked in baseline"
    fi
    
    exit 0
fi

# If we get here, something went wrong with the test run itself
# (e.g., compilation error, test infrastructure failure)
echo ""
echo "⚠️  Test run failed with exit code $test_exit_code"
echo "This appears to be a test infrastructure failure, not a regression."
exit "$test_exit_code"
