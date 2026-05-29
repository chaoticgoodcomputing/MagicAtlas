# Free text belongs at the parsing frontier, not a node's interior

## Status

Accepted (2026-05-28)

## Context

MAST is a descriptive AST: it records what oracle text *says* and never throws on a parse it cannot complete. Retaining raw oracle text is therefore unavoidable; the real question is *where* it is legitimate. The codebase had accumulated raw-string slots at several levels — type-honest error nodes (`UnparsedAbility`, `UnparsedEffect`), a typed union catch-all (`OtherHistoryPredicate`), and bare `string` / `List<string>` fields on otherwise-structured nodes (`ObjectFilter.Characteristics` across 144 fixtures, `SpellAbility.Instructions`, `HistoryPredicate.Timeframe`).

## Decision

Raw text is governed by **type-honesty**: a slot that may hold unstructured content must have a *type* that announces it — a discriminated-union variant a consumer can branch on statically (a "residual arm"), regardless of where it sits in the tree.

- **Frontier free text** — type announces unstructured (`Unparsed*`, `Other*`) — is legitimate and idiomatic. It is the parser equivalent of error nodes in Roslyn (`SkippedTokensTrivia`) or tree-sitter (`ERROR`/`MISSING`): an honest "I stopped here" marker.
- **Interior free text** — a `string` / `List<string>` field on a node that presents as structured — is debt. It is "stringly typed": it hides an enumerable-or-richer domain the type does not reveal, forcing every consumer to re-parse text the parser already held.

Debt is remediated by replacing the bare-string slot with a discriminated union carrying a typed residual arm (mirroring `HistoryPredicate` / `OtherHistoryPredicate`), routing entries that already have structured homes back to those fields (most `ObjectFilter` concepts — types, colors, zone, comparisons — already have first-class homes), and introducing a shared keyword-identity type (realized as the `KeywordAbility` enum) for the sites that encode keywords as strings (`ObjectFilter.Characteristics`, `CopyModification.AbilityAdder.AbilityText`, the `Affinity` fallback, and ultimately `Ability.KeywordSource`). Remediation is **severity-graded** — open multi-domain string-bags before bounded single slots — and policed by a residual-count metric + report wired into the TDD loop, plus an authoring rule: a new family's gold fixture may use a residual arm, but must not add a new bare-string interior field when a structured form is reachable.

The first migration batch landed `ObjectFilter.Characteristics: List<string>` → `List<Characteristic>` (union of `KeywordCharacteristic` carrying a `KeywordAbility` and the `OtherCharacteristic` residual). `KeywordAbility` is seeded with only the keyword abilities `Characteristics` exercises (Flying, Reach, Shadow) and is designed to grow; adopting it across `AbilityAdder.AbilityText` and subsuming the 398-fixture `Ability.KeywordSource` are staged follow-up batches, deliberately split so `KeywordAbility`'s parameterized-keyword shape is proven on a small surface first.

## Considered options

- **Fidelity-first** (raw text fine anywhere as long as it round-trips): rejected — it green-lights stringly-typed interior fields and pushes the parser's own job onto every downstream consumer.
- **Demand-driven** (structure only once a consumer needs it): rejected — the AST is a published, serialized contract, so structuring later is a breaking schema change; we pay churn instead of avoiding it. Today nothing reads these fields, so the cost is *latent* — which is the trap, not the all-clear.
- **Zero-tolerance** (no interior string, ever): rejected as the day-to-day rule — it would false-block a new card family that legitimately needs a residual arm before its structure exists, creating pressure to game the count.

## Consequences

- `ObjectFilter.Characteristics: List<string>` → `List<Characteristic>` (discriminated union) is a breaking change to the serialized AST; gold fixtures migrate with it.
- A residual arm is a deferral, not a destination: it is counted and must trend down, or it quietly becomes the new junk drawer — the failure mode this decision exists to prevent.
