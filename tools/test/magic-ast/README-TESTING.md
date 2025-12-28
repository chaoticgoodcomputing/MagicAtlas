# MagicAST Test Helper Commands

Add these to your shell profile (`.zshrc` or `.bashrc`):

```bash
# Quick test summary (just the numbers)
alias magictest='dotnet test tests/MagicAST.Tests/ --nologo 2>&1 | tail -1'

# Full test run with hotspot summary
alias magictest-full='dotnet test tests/MagicAST.Tests/ --nologo -v n 2>&1 | sed -n "/FAILURE HOTSPOTS/,/RESULT:/p"'

# Investigate a specific test with full JSON diffs
function magictest-debug() {
  cd tests/MagicAST.Tests && ./investigate-test.sh "$1"
}
```

## Usage

### Quick check (just pass/fail counts):
```bash
magictest
# Output: Failed!  - Failed:    24, Passed:    96, Skipped:     0, Total:   120
```

### Full analysis (see failure hotspots):
```bash
magictest-full
# Shows categorized failures by pattern type
```

### Deep dive into specific test:
```bash
magictest-debug BaneslayerAngel
# Shows full JSON diff for that specific card
```

## Test Output Interpretation

### Failure Patterns

- **UnparsedTriggered**: Triggered ability parser not implemented for this pattern
- **UnparsedStatic**: Static ability parser missing support  
- **AmbiguousStructure**: Ability type classification unclear
- **ConditionalEffect**: "If you do" / intervening-if clauses
- **ComplexTargeting**: Multiple or compound target specs
- **FieldMismatch**: Parsing works but output format differs (often `isOptional` fields)
- **MalformedCard**: Expected to fail (test validation)
- **VariableEffect**: Variable cost or X effects
- **UnparsedUnparsed**: Double unparsed (ability + pattern fallthrough)

### Hotspot Analysis

The failure summary shows:
1. **Pattern categories** sorted by frequency (most common first)
2. **Example tests** for each pattern (up to 3)
3. **Total count** at the bottom

This helps identify which parsers need the most work.

### Ratchet Testing

The summary also shows:
- ✓ **Progressions**: Tests that now pass (regressions prevented)
- ⚠ **Stable Failures**: Baseline failures we're working to fix
- ✗ **Regressions**: New failures (these break the build)
