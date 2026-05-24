#!/usr/bin/env zsh
# Script to investigate a specific failing test with full JSON diff output
# Usage: ./investigate-test.sh "TestName"

if [ -z "$1" ]; then
  echo "Usage: $0 <test-name-filter>"
  echo "Example: $0 BaneslayerAngel"
  exit 1
fi

TEST_FILTER="$1"

echo "🔍 Running detailed investigation for tests matching: $TEST_FILTER"
echo ""

# Run with full verbosity and filter for the specific test
dotnet test \
  --filter "FullyQualifiedName~$TEST_FILTER" \
  --verbosity normal \
  --logger "console;verbosity=detailed" \
  --settings test.runsettings

echo ""
echo "✅ Investigation complete"
echo "💡 Tip: Check TestResults/ directory for detailed logs"
