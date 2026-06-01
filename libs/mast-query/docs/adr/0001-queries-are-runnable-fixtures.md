# Queries are runnable fixtures over a three-valued match contract

## Status

Accepted (2026-05-31) — initial scaffold; engine operators staged

## Context

Every MAST consumer queries ([magic-ast ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)). For a query to be the shared interface, it must behave like the gold fixtures MAST already trusts: declarative data, deterministic, portable across runners, versioned in git, self-verifying.

Two hazards are specific to this corpus:

- **Partial coverage.** ~34% of cards fully parse; `IUnparsed` nodes are everywhere. A two-valued match silently lies — reporting "no match" where an unparsed region *might* match is a false negative that poisons clustering recall and interaction precision alike.
- **A moving target.** MAST's coverage grows continuously, so a query's match-set shifts as the parser improves. That drift must be **loud** — a failing fixture — not a silent change a consumer discovers in production.

## Decision

The query contract has four pinned parts.

1. **Query artifact.** A declarative, typed match-tree — discriminator constraints, wildcards, scalar predicates, captures, the `intersects` filter operator — plus metadata (`name`, `intent`). Authored as JSON: a label is one query; an interaction edge is two queries joined over their captures; an archetype is a `union` of labels. Patterns are *data*, not code.

2. **Three-valued result, by certain-answers semantics.** A pattern over a subtree yields `Match`, `NoMatch`, or `Unknown`. `Unknown` arises when an `IUnparsed` region falls within the pattern's scope — a draw the parser has not yet structured *might* be direct damage. This is the certain / possible-answers distinction (Libkin, *SQL's Three-Valued Logic and Certain Answers*) made operational: `Match` is certain, `Unknown` is possible-not-certain. A `$unparsed-regex` predicate may resolve a specific `Unknown` to `Match` / `NoMatch` at a lower **provenance** tier, and logs the residual span as a parse gap — pressure to parse, not a crutch.

3. **Determinism.** Stable match ordering (card id, then node path), stable capture bindings, identity via MAST's canonical serialization ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)), never record `GetHashCode`. Same query + same corpus snapshot ⇒ identical result, in any conforming engine.

4. **Conformance suite.** Query + frozen reference corpus + expected result, diffed — the model is CodeQL's `.expected` test framework. Any engine must reproduce it; this is what makes a query *portable* and parser-drift *loud*. Domain fixtures (labels, edges) run against the live corpus and pin **invariants** (`must_match` / `must_not`); only the frozen conformance corpus pins exact result sets.

The **reference engine is C#**, project-referencing MAST to reuse its node types, `ResidualWalker` traversal, and the `intersects` operator. The contract is language-neutral; a Python engine for notebook work may follow and is held honest by the same conformance suite. C# is first because the substrate is C# and the first consumer — atlas labeling as a Flowthru C# step — runs there.

## Considered options

- **Two-valued match (drop `Unknown`).** Rejected: silently miscounts every card with an unparsed region; dishonest about coverage in both directions.
- **Queries as code (a fluent C# / LINQ API).** Rejected: not data, so not versionable / diffable as a fixture, not authorable by non-engineers, not shareable with a second-language engine.
- **Exact expected-sets against the live corpus.** Rejected: every parser improvement would break every fixture; the live corpus pins invariants, only the frozen corpus pins exact sets.
- **Adopt CodeQL / XQuery / Cypher wholesale.** Rejected ([ADR 0008](../../../magic-ast/docs/adr/0008-the-query-line.md)): none is three-valued or knows `intersects`. Borrow CodeQL's `.expected` *structure* and certain-answers *semantics*; reject their languages.

## Consequences

- `libs/mast-query/` is created: the pattern model, the three-valued result model, the engine interface, and a C# reference engine; depends on `MagicAST` and its schema export.
- `tests/mast-query-test/` is created: a frozen reference corpus and conformance fixtures (query + expected), including an `Unknown` case so the tri-state is exercised from day one.
- Consumers materialize results offline and serve them (atlas: `card→label` rows in Postgres via GraphQL); the app runs no engine for v1.
- The **join layer** (cross-query captures, `emits` / `listens` event matching, `intersects` overlap) and the induced **resource ontology** belong to the interaction project, deferred — they reference MAST shapes, never extend them.
- **The `Unknown` tier is a coverage signal, not engine-resolved** (decided 2026-05-31 against the live corpus: `burn.spot` returned 512 certain matches, 9,764 certain non-matches, and 19,338 `Unknown` at ~34% parser coverage). An any-depth query cannot rule out an unparsed clause, so `Unknown` honestly tracks MAST coverage and shrinks as it grows. Consumers (labels, edges) take the certain `matched` set; the `Unknown` count is a MAST-coverage KPI, surfaced as a "decidable %" by the runner. The `$unparsed-regex` escape hatch remains specified but is **not** the default path — it is opt-in recovery, not a standing resolver.
- Canonical worked fixtures: `burn` (with the `burn.spot` / `burn.mass` union) and the Cultivator Colossus × Abundance edge pair.
