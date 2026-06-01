# The query line: MAST owns match meaning, the engine owns match mechanism

## Status

Accepted (2026-05-31) — implementation staged

## Context

Every downstream consumer reaches MAST the same way: by **querying for subtree shapes**. The explorable atlas labels archetypes ("burn" is a `DealDamageEffect` at a chosen target); the planned interaction project links cards by matched producer/consumer subtrees. There is no consumer that does *not* query. That makes *how MAST is queried* a boundary question on the level of the AST schema itself, not a per-consumer detail.

Two failure modes pulled in opposite directions:

1. **Every consumer reimplements matching.** Each reinvents traversal, type dispatch, and — worst — the *meaning* of an `ObjectFilter`: does "creature you control" overlap "creature an opponent controls"? The semantics drift from MAST's actual types and from one another.
2. **The engine is absorbed into MAST.** The pattern language, matcher, and result model migrate into the parser, re-importing the consumer concerns [ADR 0004](0004-ast-engine-line.md) just expelled, and coupling the engine to C# and to MAST's in-memory records — which the Python / notebook consumers cannot use.

MAST is C#; its consumers span C# (the API, Flowthru steps) and Python (data-science). The only artifact all of them share is the serialized AST.

## Decision

Extend the [ADR 0004](0004-ast-engine-line.md) engine-line to the query layer as a **meaning / mechanism** split.

**MAST owns match meaning** — the three things that state what the types *are*:

- a machine-readable **schema export** (node taxonomy, discriminator keys, axis vocabulary), generated from the same XML-doc source as `GLOSSARY.md` and content-hashed so it is a pinnable version;
- a **canonical serialization** (deterministic key order) so a subtree has one identity across processes and languages — record `GetHashCode` is per-process-randomized and must never serve as a cross-engine identity;
- the **`ObjectFilter` overlap / subsumption operator** — "can these two filters denote intersecting sets" is answerable only by the owner of what the axes mean.

**MAST does not own match mechanism** — the pattern DSL, the matcher, the three-valued result model, the fixture format. Those live in a sibling library, `mast-query` (its [ADR 0001](../../../mast-query/docs/adr/0001-queries-are-runnable-fixtures.md)), bound to the schema export.

The reusable test, mirroring 0004: **if a datum states what a node *means*, or how two nodes relate *by their type semantics*, it is MAST; if it states how someone wants to *search or score* nodes, it is the engine.**

## Considered options

- **Each consumer matches directly over the records.** Rejected: N reimplementations of traversal and filter semantics, drifting from MAST and from one another; Python consumers are shut out entirely.
- **Absorb the query engine into MAST.** Rejected: re-imports the consumer concerns 0004 expelled; couples the engine to C# and the in-memory representation; the interaction project's induced resource ontology would have no honest home.
- **Adopt an existing query platform's language as the contract** (XQuery / Cypher / SQL-JSONpath). Rejected: none is three-valued over partial data and none knows `intersects`; they are candidate *engines* behind the contract, not the contract.

## Consequences

- MAST gains a generated, content-hashed `schema/` export and a canonical-serialization form; both are published artifacts the engine and the conformance suite pin to.
- The `ObjectFilter` overlap operator lands in MAST as a pure function over the existing axes (types, colors, controller, zone, comparisons, and the relational axes `ExiledWith` / `Counters` / `AttachedTo` grown by [ADR 0003](0003-keywords-decompose-into-shared-primitives.md) / [ADR 0004](0004-ast-engine-line.md)).
- `mast-query` is created as a sibling library that depends on the schema export — never the reverse. The interaction project's resource ontology references MAST shapes from further downstream still.
- The line is reviewable: a proposed MAST addition is rejected if it encodes how to *search* (a pattern operator, a similarity score, an archetype label) rather than what a node *means*.
