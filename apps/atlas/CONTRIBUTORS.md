# Contributing to MagicAST

> TODO: Decouple this from Flowthru data sources.

MagicAST is a type-safe parsing framework for Magic: The Gathering oracle text, built on compositional parser principles. This guide explains the architecture, parsing philosophy, and how to contribute effectively.

## Architecture Overview

MagicAST follows a three-stage parsing pipeline:

```
Raw Oracle Text → Tokenization → Classification → Specialized Parsing → AST
```

### 1. Tokenization (`OracleTokenizer`)

The tokenizer converts raw oracle text into a stream of typed tokens using the Superpower library. Each token represents a semantic unit:

- **Keywords**: `When`, `Whenever`, `At`, `create`, `draw`, `put`, etc.
- **Literals**: Numbers, mana symbols, P/T notation (`/`)
- **Identifiers**: Card names, creature types, colors
- **Punctuation**: Commas, periods, colons

**Key file**: `src/MagicAST/Parsing/Tokens/OracleTokenizer.cs`

### 2. Classification (`AbilityClassifier`)

The classifier analyzes token streams to identify ability types without fully parsing them:

- Triggered abilities (When/Whenever/At)
- Activated abilities (Cost: Effect)
- Static abilities (continuous effects)
- Modal abilities (Choose one/two/etc.)
- Spell abilities (instant/sorcery effects)

Returns a `ClauseClassification` with ability kind and confidence score.

**Key file**: `src/MagicAST/Parsing/Classification/AbilityClassifier.cs`

### 3. Specialized Parsing

Each ability type has a dedicated parser that transforms classified clauses into AST nodes:

- `TriggeredAbilityParser` → `TriggeredAbility`
- `ActivatedAbilityParser` → `ActivatedAbility`
- `StaticAbilityParser` → `StaticAbility`

**Key directory**: `src/MagicAST/Parsing/Parsers/`

## Compositional Parser Design

### The Problem with Regex-Based Parsing

Early implementations used regex patterns like:

```csharp
@"create (?:a |an )?(\d+)/(\d+) (\w+) (\w+) creature token"
```

**Issues:**
- Brittle: Breaks on slight variations ("two 1/1" vs "a 1/1")
- Overfitted: Works for one card, fails on similar cards
- Unmaintainable: Each new pattern requires a new regex
- No reuse: Token parsing logic duplicated across patterns

### The Compositional Approach

Build **composable parser components** that combine to handle variation:

```csharp
// Instead of one regex for entire pattern:
var tokenSpec = 
    ParseArticle()          // "a", "an", "two", "three", "X"
    .Then(ParsePowerToughness)  // "1/1", "2/2", "X/X"
    .Then(ParseColors)          // "green", "white and black"
    .Then(ParseCreatureTypes)   // "Saproling", "Zombie", "Spirit"
    .Then(ParseAbilities);      // "with flying", "with lifelink"
```

**Benefits:**
- **Reusable**: `ParseColors()` works in any context needing colors
- **Testable**: Each component tested independently
- **Flexible**: Easy to add new color combinations or types
- **Maintainable**: Clear separation of concerns

### Monadic Parser Combinators

Use parser combinators (via Superpower or similar) to build complex parsers from simple ones:

```csharp
// Simple parsers
Parser<string> article = Parse.String("a").Or(Parse.String("an"));
Parser<string> number = Parse.Number;

// Combine them
Parser<TokenSpec> tokenSpec =
    from art in article.Optional()
    from power in number
    from slash in Parse.Char('/')
    from toughness in number
    from colors in ParseColors()
    from types in ParseCreatureTypes()
    select new TokenSpec(art, power, toughness, colors, types);
```

**Key principles:**
- Start with primitives (numbers, keywords, symbols)
- Compose upward to phrases (token specs, trigger events)
- Reuse components across parsers
- Handle variation through alternatives (`.Or()`) not special cases

## The Family-Based Approach

### Think in Card Families, Not Individual Cards

When implementing parser support, analyze **groups of similar cards** to find the generalized pattern:

#### ❌ Wrong Approach: Overfit to One Card

```csharp
// Only handles DeathbloomThallid exactly
if (text == "When this creature dies, create a 1/1 green Saproling creature token.")
{
    return new TriggeredAbility(/* ... */);
}
```

#### ✅ Right Approach: Support Card Families

**Step 1**: Find similar cards using data analysis:

```bash
# Search oracle-cards.json for patterns
grep -o 'dies, create [^."]*creature token' oracle-cards.json | sort | uniq -c
```

**Step 2**: Identify variation dimensions:

From data analysis:
- Articles: "a", "an", "two", "three", "X"
- P/T: "1/1", "2/2", "4/4", "X/X"
- Colors: "green", "black", "white and black", "colorless"
- Types: "Saproling", "Zombie", "Spirit", "Goblin"
- Abilities: (none), "with flying", "with lifelink", "with deathtouch"

**Step 3**: Build compositional parser supporting all variations:

```csharp
public class CreateTokenEffect
{
    public string Article { get; init; }     // "a", "two", etc.
    public int Power { get; init; }
    public int Toughness { get; init; }
    public List<string> Colors { get; init; }
    public List<string> Types { get; init; }
    public List<string> Abilities { get; init; }
}

Parser<CreateTokenEffect> ParseCreateToken =
    from article in ParseArticle()
    from pt in ParsePowerToughness()
    from colors in ParseColorList()
    from types in ParseTypeList()
    from abilities in ParseAbilityList().Optional()
    select new CreateTokenEffect(article, pt.Power, pt.Toughness, colors, types, abilities);
```

This now handles:
- "create a 1/1 green Saproling creature token" ✓
- "create two 2/2 black Zombie creature tokens" ✓
- "create a 1/1 white and black Spirit creature token with flying" ✓
- "create X X/X colorless Eldrazi creature tokens" ✓

### Data-Driven Grammar Design

Use the oracle card database to guide parser development:

1. **Query for patterns**: Use `grep`, `jq`, or `awk` to extract patterns
2. **Count frequencies**: Identify common vs. rare constructions
3. **Group by structure**: Find natural clusters (e.g., all "dies" triggers)
4. **Implement high-value families**: Start with most common patterns
5. **Iterate**: Add support for new families as needed

**Example workflow:**

```bash
# Find all "When X enters" triggers
grep -Eo 'When [^,]+ enters, [^.]+\.' oracle-cards.json | sort | uniq -c | sort -rn

# Find all token creation patterns
grep -o 'create [^."]*creature token' oracle-cards.json | sort | uniq -c | sort -rn

# Find trigger event frequencies
grep -Eo '(When|Whenever|At) [^,]+' oracle-cards.json | sort | uniq -c | sort -rn
```

## How to Contribute

### Adding Support for New Card Families

1. **Identify the family**: Find cards with similar oracle text patterns
2. **Analyze variation**: Use data queries to understand the pattern space
3. **Design AST nodes**: Create types representing the semantic structure
4. **Build compositional parsers**: Use parser combinators for flexibility
5. **Write tests**: Cover the family, not just one card
6. **Document patterns**: Add examples to test cases

### Example: Supporting "Enters" Triggers

**Step 1**: Query the database:

```bash
cd examples/MagicAtlas/Data/_01_Raw/Datasets
grep -Eo 'When this creature enters, [^.]+\.' oracle-cards.json | \
  sed 's/When this creature enters, //' | \
  sort | uniq -c | sort -rn | head -20
```

**Step 2**: Identify effect categories:

- Draw effects: "draw a card", "draw two cards"
- Token creation: "create a Treasure token"
- Life gain: "you gain 3 life"
- Counter placement: "put a +1/+1 counter on target creature"

**Step 3**: Create AST types:

```csharp
public abstract record EntersEffect;
public record DrawCardsEffect(int Count) : EntersEffect;
public record CreateTokenEffect(TokenSpec Token) : EntersEffect;
public record GainLifeEffect(int Amount) : EntersEffect;
public record PutCounterEffect(CounterType Type, Target Target) : EntersEffect;
```

**Step 4**: Build effect parsers:

```csharp
Parser<EntersEffect> ParseEntersEffect =
    ParseDrawEffect
    .Or(ParseCreateTokenEffect)
    .Or(ParseGainLifeEffect)
    .Or(ParsePutCounterEffect);
```

**Step 5**: Write family-based tests:

```csharp
[TestCase("When this creature enters, draw a card.")]
[TestCase("When this creature enters, draw two cards.")]
[TestCase("When this creature enters, you may draw a card.")]
public void ParseEntersDrawEffects(string oracleText)
{
    var result = parser.Parse(oracleText);
    Assert.That(result, Is.InstanceOf<TriggeredAbility>());
}
```

### Testing Guidelines

- **Test families, not individuals**: One test method per pattern family
- **Use `[TestCase]` attributes**: Cover variations within the family
- **Use ratchet testing**: Track stable failures to prevent regressions
- **Prioritize coverage**: High-frequency patterns first

### Code Style

- **Prefer immutable records** for AST nodes
- **Use required properties** for non-nullable fields
- **Name parsers descriptively**: `ParseCreateTokenEffect`, not `ParseEffect`
- **Document pattern coverage**: Add comments showing supported variations

## Understanding the Test Suite

MagicAST uses **ratchet testing** to track progress on real-world cards:

```csharp
[TestCaseSource(nameof(HandParsedCards))]
public void ParseHandParsedCards(string cardName, string oracleText)
{
    // Test passes if parsing succeeds
    // Failures are tracked in baseline file
    // Prevents regressions when adding new features
}
```

**How it works:**
- Baseline file tracks "expected failures" (cards we know don't parse yet)
- Tests pass if: (1) parsing succeeds, OR (2) failure is in baseline
- Adding new parser features should reduce baseline failures
- New failures (regressions) cause test failures

Run tests and check progress:

```bash
nx run test/magic-ast:test
# Look for: "Test Run Failed. Total tests: 68, Passed: 43"
# Goal: Increase "Passed" count over time
```

## Resources

### Key Files
- `src/MagicAST/Parsing/OracleParser.cs` - Main orchestrator
- `src/MagicAST/Parsing/Tokens/OracleTokenizer.cs` - Tokenization
- `src/MagicAST/Parsing/Classification/AbilityClassifier.cs` - Classification
- `src/MagicAST/AST/` - AST node definitions
- `tests/MagicAST.Tests/` - Test suite

### Data Sources
- `examples/MagicAtlas/Data/_01_Raw/Datasets/oracle-cards.json` - Full card database (NDJSON format)
- Each line is a complete card object with `oracle_text` field

### Parser Combinator Resources
- [Superpower Documentation](https://github.com/datalust/superpower)
- [Parser Combinators in C#](https://fsharpforfunandprofit.com/posts/understanding-parser-combinators/)

## Philosophy

**Fail at compile-time, not runtime** - Use strong typing to catch errors early.

**Compose, don't concatenate** - Build complex parsers from simple, reusable pieces.

**Data guides design** - Let the card database show you the patterns, don't guess.

**Families over individuals** - Support groups of similar cards, not one-off regex patterns.

**Iterate incrementally** - Perfect is the enemy of good; start with high-frequency patterns and expand.

---

Welcome to MagicAST! If you have questions, check existing parsers for examples or ask in issues.
