# Contributing to magic-ast

Magic: The Gathering card text parser and AST.

## Tests

The test harness for this library lives at [`tests/magic-ast-tests/`](../../tests/magic-ast-tests/), not in this directory. Run tests from there.

## Glossary

Terms specific to how MAST treats oracle text it has not (yet) fully structured. See [ADR 0001](docs/adr/0001-free-text-is-frontier-only.md) for the decision behind them.

**Free text**:
A fragment of oracle text retained verbatim in the AST instead of being structured into nodes.
_Avoid_: "raw text" / "passthrough" used loosely; reserve "unparsed" for the `Unparsed*` nodes specifically.

**Frontier free text**:
Free text whose declared *type* announces it is unstructured — a discriminated-union variant a consumer can branch on statically (`UnparsedAbility`, `UnparsedEffect`, `OtherHistoryPredicate`). Legitimate and idiomatic: the parser's honest "I stopped here" marker, the role error nodes play in Roslyn or tree-sitter.

**Interior free text**:
A `string` / `List<string>` field on a node that otherwise presents as structured, where the field's type does *not* reveal the value may be unstructured (`ObjectFilter.Characteristics`, `SpellAbility.Instructions`, `HistoryPredicate.Timeframe`). Debt — "stringly typed" — because a consumer cannot branch on it without re-parsing text the parser already held.

**Type-honesty**:
The rule deciding which free text is acceptable: a slot that may hold unstructured content must have a *type* that says so (a union with a residual arm), regardless of where it sits in the tree. Frontier free text is type-honest; interior free text is not.

**Residual arm**:
The typed `Other` / `Unparsed` variant of a discriminated union that carries the literal phrase when no structured variant matches (e.g. `OtherHistoryPredicate`, `OtherCharacteristic`). The honest home for "not yet structured" — counted and reported, so it stays a deferral rather than becoming a destination.

**Characteristic**:
A constraint on an `ObjectFilter` beyond its structured axes — a discriminated union of `KeywordCharacteristic` (a keyword-ability requirement) and the `OtherCharacteristic` residual. The frontier-honest replacement for the former `Characteristics: List<string>` bag (ADR 0001).

**KeywordAbility**:
The canonical identity of a parameterless Magic keyword ability (CR 702), as an enum — the type-honest alternative to bare keyword strings. Currently seeded for `KeywordCharacteristic`; intended to absorb the other keyword-as-string sites (`AbilityAdder`, `KeywordSource`) over subsequent batches.

## Conventions

**Prefer structure; defer honestly.** When oracle text resists structuring:

- Route it to an existing structured field if one fits. Most `ObjectFilter` concepts (card types, colors, zone, comparisons, controller) already have first-class homes — do not re-encode them as free-text characteristics.
- If no structured form exists yet, emit a **residual arm** (`Other*` / `Unparsed*`), never a bare-string interior field.
- A new card family's gold fixture MAY use a residual arm, but MUST NOT add a new bare-string interior field when a structured form is reachable.

**Drive debt down worst-first.** Open, multi-domain string-bags (e.g. `ObjectFilter.Characteristics`) outrank bounded single slots (e.g. `HistoryPredicate.Timeframe`). The residual report (see [ADR 0001](docs/adr/0001-free-text-is-frontier-only.md)) surfaces which cards still carry interior free text.
