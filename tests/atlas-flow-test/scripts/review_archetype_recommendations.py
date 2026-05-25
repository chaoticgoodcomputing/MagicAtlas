"""Pretty-print ArchetypeRecommendations as a human-curatable markdown report.

Sorts clusters by verdict (NEW first, then REFINE, then MERGE, then COVERED), shows top-N
of each, and formats each cluster's sample lines as ready-to-copy seed prototypes for
canonical-archetypes.json.

Workflow:
    1. `dotnet run -- --flow Discovery --no-cache`           ← produce recommendations
    2. `python scripts/review_archetype_recommendations.py`  ← read this report
    3. Edit canonical-archetypes.json based on report
    4. `dotnet run -- --only AttributeLines --no-cache` etc. ← validate
    5. Re-run Discovery to find what changed

Run:
    uv run --project ../../libs/atlas-flows python scripts/review_archetype_recommendations.py
    uv run --project ../../libs/atlas-flows python scripts/review_archetype_recommendations.py --verdict NEW --top 20
    uv run --project ../../libs/atlas-flows python scripts/review_archetype_recommendations.py --markdown archetype-recommendations.md
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RECS = ROOT / "Data" / "_07_ModelOutput" / "Datasets" / "archetype-recommendations.json"


def _format_split_group(parent_slug: str, siblings: list[dict]) -> str:
    """Render all SPLIT siblings for one parent archetype as a single unit, so the curator
    sees the proposed sub-archetypes side-by-side."""
    siblings = sorted(siblings, key=lambda r: -r["n_members"])
    total_members = sum(r["n_members"] for r in siblings)
    parts: list[str] = []
    parts.append(f"### `{parent_slug}` — claimed by {len(siblings)} distinct clusters "
                 f"({total_members:,} total members)")
    parts.append("")
    parts.append(
        f"The existing `{parent_slug}` archetype is being identified as the closest match "
        f"for these mutually-distinct sub-clusters. Each is tighter internally than its match "
        f"to `{parent_slug}` — strong evidence the archetype bundles several semantic regions. "
        f"Consider splitting into {len(siblings)} archetypes."
    )
    parts.append("")
    for i, rec in enumerate(siblings):
        parts.append(f"#### Sub-cluster #{i + 1} — cluster {rec['cluster_id']}  "
                     f"({rec['n_members']:,} members)")
        parts.append("")
        parts.append(
            f"- **Cohesion:** {rec['cohesion']:.3f}  ·  "
            f"**Match to `{parent_slug}`:** {rec['closest_archetype_cosine']:.3f}  ·  "
            f"**Gap:** +{rec['cohesion'] - rec['closest_archetype_cosine']:.3f}"
        )
        parts.append(f"- **c-TF-IDF tokens:** {rec['ctfidf_tokens']}")
        parts.append(f"- **Suggested slug:** `{rec['suggested_slug']}`")
        parts.append("")
        parts.append(f"**Medoid:**")
        parts.append(f"> {rec['medoid_line_text']}")
        parts.append("")
        parts.append("**Sample lines (ready as seed prototypes):**")
        parts.append("```json")
        parts.append("\"prototypes\": [")
        for sample in str(rec["sample_lines_joined"]).split(" | "):
            clean = sample.replace("\n", " ⏎ ").replace('"', '\\"')
            parts.append(f'  "{clean}",')
        parts.append("]")
        parts.append("```")
        parts.append("")
    return "\n".join(parts)


def _format_cluster(rec: dict, idx: int) -> str:
    parts = []
    verdict = rec["verdict"]
    parts.append(f"### #{idx + 1} · cluster {rec['cluster_id']} · {verdict}")
    parts.append("")
    parts.append(
        f"- **Members:** {rec['n_members']:,}  ·  "
        f"**Cohesion:** {rec['cohesion']:.3f}"
    )
    parts.append(
        f"- **Closest existing archetype:** `{rec['closest_archetype_slug']}` "
        f"(cosine {rec['closest_archetype_cosine']:.3f})"
    )
    parts.append(f"- **c-TF-IDF tokens (core identity):** {rec['ctfidf_tokens']}")
    # Ring diagnostics — what each outer ring is picking up that the core (p0-25) doesn't.
    # Empty rings (e.g. on small clusters) are omitted to keep reports tight.
    ring_lines = [
        ("p25-50 (inner)",   rec.get("ring_p25_50_tokens", "")),
        ("p50-75 (mid)",     rec.get("ring_p50_75_tokens", "")),
        ("p75-99 (periphery)", rec.get("ring_p75_99_tokens", "")),
    ]
    visible_rings = [(label, toks) for label, toks in ring_lines if toks]
    if visible_rings:
        parts.append("- **Ring drift (tokens overrepresented vs core):**")
        for label, toks in visible_rings:
            parts.append(f"  - **{label}:** {toks}")
    parts.append(f"- **Suggested slug:** `{rec['suggested_slug']}`")
    parts.append("")
    parts.append(f"**Medoid line:**")
    parts.append(f"> {rec['medoid_line_text']}")
    parts.append("")
    parts.append("**Sample lines (ready as seed prototypes):**")
    parts.append("```json")
    parts.append("\"prototypes\": [")
    for sample in str(rec["sample_lines_joined"]).split(" | "):
        clean = sample.replace("\n", " ⏎ ").replace('"', '\\"')
        parts.append(f'  "{clean}",')
    parts.append("]")
    parts.append("```")
    parts.append("")
    return "\n".join(parts)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--verdict", default=None,
                    help="Filter to one verdict: NEW, REFINE, MERGE, SPLIT, COVERED. Default: all in priority order.")
    ap.add_argument("--top", type=int, default=None,
                    help="Limit per-verdict to top N by member count.")
    ap.add_argument("--markdown", type=str, default=None,
                    help="Write the report to this file as markdown.")
    args = ap.parse_args()

    if not RECS.exists():
        print(f"[!] {RECS} not found. Run: dotnet run -- --flow Discovery --no-cache", file=sys.stderr)
        sys.exit(1)

    recs = json.loads(RECS.read_text())
    if not recs:
        print("[!] ArchetypeRecommendations is empty.")
        sys.exit(0)

    by_verdict: dict[str, list[dict]] = {}
    for r in recs:
        by_verdict.setdefault(r["verdict"], []).append(r)
    # Sort each verdict's clusters by n_members desc
    for v in by_verdict:
        by_verdict[v].sort(key=lambda r: -r["n_members"])

    verdict_order = ["SPLIT", "NEW", "REFINE", "MERGE", "COVERED"]
    if args.verdict:
        verdict_order = [args.verdict.upper()]

    counts = {v: len(by_verdict.get(v, [])) for v in verdict_order}

    lines: list[str] = []
    lines.append(f"# Archetype Recommendations")
    lines.append("")
    lines.append(f"Total clusters: {len(recs)}")
    lines.append(f"Verdict breakdown: {counts}")
    lines.append("")
    lines.append(
        f"Verdict legend: SPLIT (existing archetype bundles ≥2 distinct sub-clusters; should be "
        f"split into N), NEW (no current archetype matches), REFINE (existing archetype is too "
        f"broad — single sub-region), MERGE (cluster spans two archetypes ~equally), COVERED "
        f"(well-matched)."
    )
    lines.append("")

    for v in verdict_order:
        clusters = by_verdict.get(v, [])
        if not clusters:
            continue
        lines.append(f"---")
        lines.append("")

        if v == "SPLIT":
            # Group SPLIT verdicts by their shared parent archetype, render each group as
            # a unit so the curator sees all sibling sub-clusters together.
            by_parent: dict[str, list[dict]] = {}
            for r in clusters:
                by_parent.setdefault(r["closest_archetype_slug"], []).append(r)
            n_parents = len(by_parent)
            lines.append(f"## SPLIT ({len(clusters)} clusters across {n_parents} parent archetypes)")
            lines.append("")
            sorted_parents = sorted(by_parent.items(), key=lambda kv: -sum(c["n_members"] for c in kv[1]))
            if args.top:
                sorted_parents = sorted_parents[: args.top]
            for parent, siblings in sorted_parents:
                lines.append(_format_split_group(parent, siblings))
            continue

        if args.top:
            clusters = clusters[: args.top]
        lines.append(f"## {v} ({len(clusters)} clusters)")
        lines.append("")
        for i, rec in enumerate(clusters):
            lines.append(_format_cluster(rec, i))

    body = "\n".join(lines)
    print(body)

    if args.markdown:
        path = Path(args.markdown)
        path.write_text(body)
        print(f"\nWrote markdown to {path}", file=sys.stderr)


if __name__ == "__main__":
    main()
