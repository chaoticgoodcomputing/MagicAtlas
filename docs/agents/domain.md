# Domain Docs

How the engineering skills should consume this repo's domain documentation when exploring the codebase. This repo uses `CONTRIBUTING.md` (not `CONTEXT.md`) so the same files serve both humans and agents.

## Before exploring, read these

- **Root `CONTRIBUTING.md`** — repo-wide conventions and the map of contexts. Always read first.
- **The relevant context's `CONTRIBUTING.md`** — each app/lib has its own. Read whichever ones touch the area you're about to work in.
- **`docs/adr/`** at the root for system-wide architectural decisions, plus `<context>/docs/adr/` for context-scoped ones.

If any of these files don't exist, **proceed silently**. Don't flag their absence; don't suggest creating them upfront. The producer skill (`/grill-with-docs`) creates them lazily when terms or decisions actually get resolved.

## Contexts in this monorepo

| Context       | Path                | Test harness             |
| ------------- | ------------------- | ------------------------ |
| `atlas-api`   | `apps/atlas-api/`   | —                        |
| `atlas-site`  | `apps/atlas-site/`  | —                        |
| `atlas-web`   | `apps/atlas-web/`   | —                        |
| `atlas-flows` | `libs/atlas-flows/` | `tests/atlas-flow-test/` |
| `magic-ast`   | `libs/magic-ast/`   | `tests/magic-ast-tests/` |

For `atlas-flows` and `magic-ast`, the test harness lives outside the library directory — when adding or running tests for those libs, work in the corresponding `tests/` directory.

## File structure

```
/
├── CONTRIBUTING.md                       ← root: conventions + context map
├── docs/adr/                             ← system-wide decisions
├── apps/
│   ├── atlas-api/CONTRIBUTING.md
│   ├── atlas-site/CONTRIBUTING.md
│   └── atlas-web/CONTRIBUTING.md
├── libs/
│   ├── atlas-flows/CONTRIBUTING.md
│   └── magic-ast/CONTRIBUTING.md
└── tests/
    ├── atlas-flow-test/                  ← atlas-flows test harness
    └── magic-ast-tests/                  ← magic-ast test harness
```

## Use the glossary's vocabulary

When your output names a domain concept (in an issue title, a refactor proposal, a hypothesis, a test name), use the term as defined in the relevant `CONTRIBUTING.md`. Don't drift to synonyms the glossary explicitly avoids.

If the concept you need isn't in the glossary yet, that's a signal — either you're inventing language the project doesn't use (reconsider) or there's a real gap (note it for `/grill-with-docs`).

## Flag ADR conflicts

If your output contradicts an existing ADR, surface it explicitly rather than silently overriding:

> _Contradicts ADR-0007 (event-sourced orders) — but worth reopening because…_
