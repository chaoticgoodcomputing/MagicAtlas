# AGENTS.md

Project-level instructions for AI coding agents working in this repo.

## Agent skills

### Issue tracker

Issues and PRDs live in GitHub Issues (`chaoticgoodcomputing/MagicAtlas`), accessed via the `gh` CLI. See [docs/agents/issue-tracker.md](docs/agents/issue-tracker.md).

### Triage labels

Five canonical roles, default strings: `needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`. See [docs/agents/triage-labels.md](docs/agents/triage-labels.md).

### Domain docs

Multi-context monorepo. Root `CONTRIBUTING.md` is the map; each app/lib has its own `CONTRIBUTING.md`. ADRs live in `docs/adr/` (root) and optionally `<context>/docs/adr/`. See [docs/agents/domain.md](docs/agents/domain.md).
