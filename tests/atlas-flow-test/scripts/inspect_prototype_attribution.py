"""Per-archetype prototype-attribution inspector.

For each of the top-N archetypes (by attribution count), surface:
    - Count + breakdown by source (prototype vs pattern)
    - Cosine-confidence distribution stats + tiny histogram
    - K example lines at five percentile points along the confidence distribution
      (0.01, 0.25, 0.50, 0.75, 0.99) — exactly the strategy that revealed the inference
      pipeline's bleed problem in the previous codebase
    - Per-example: text, card name, ALL other archetype attributions for that line
      (so we can see if the line is also strongly attributed to neighboring archetypes)

Use this to:
    - Spot-check whether an archetype's prototype list captures coherent semantics across
      the confidence range
    - Find archetypes that need tightening (q01 examples should still be plausible matches)
    - Identify over-broad prototypes (line attributed to many unrelated archetypes simultaneously)
    - Distinguish type-shaped archetypes (equipment, aura) from effect-shaped ones

Run:
    uv run --project ../../libs/atlas-flows python scripts/inspect_prototype_attribution.py
    uv run --project ../../libs/atlas-flows python scripts/inspect_prototype_attribution.py --top-n 5 --k 5
    uv run --project ../../libs/atlas-flows python scripts/inspect_prototype_attribution.py --archetypes evasion,removal,burn --markdown
"""
from __future__ import annotations

import argparse
import json
import sys
import uuid
from collections import defaultdict
from pathlib import Path

import numpy as np
import pandas as pd

ROOT = Path(__file__).resolve().parents[1]
PRIMARY = ROOT / "Data" / "_03_Primary" / "Datasets"
CONFIG = ROOT / "Data" / "_00_Config" / "Datasets"

_PERCENTILES = [0.01, 0.25, 0.50, 0.75, 0.99]
_PERCENTILE_WINDOW = 0.01


def _normalize_guid(v) -> str | None:
    if v is None:
        return None
    if isinstance(v, float) and pd.isna(v):
        return None
    if isinstance(v, (bytes, bytearray)):
        try:
            return str(uuid.UUID(bytes=bytes(v)))
        except ValueError:
            return None
    s = str(v)
    return s if s else None


def _load():
    assignments = pd.read_parquet(PRIMARY / "oracle-line-canonical-assignments.parquet")
    assignments["line_id"] = assignments["line_id"].map(_normalize_guid)

    lines = pd.read_json(PRIMARY / "oracle-lines.json")
    lines["line_id"] = lines["line_id"].map(_normalize_guid)

    cards = pd.read_json(PRIMARY / "filtered-cards-core.json").rename(
        columns={"Id": "card_id", "Name": "name"}
    )

    with open(CONFIG / "canonical-archetypes.json") as f:
        archetypes = {a["slug"]: a for a in json.load(f)}

    return assignments, lines, cards, archetypes


def _build_card_context(lines: pd.DataFrame, cards: pd.DataFrame, assignments: pd.DataFrame) -> dict:
    """card_id → {name, attributions[(slug, conf, source)]}."""
    card_name = dict(zip(cards["card_id"], cards["name"]))
    line_to_card = dict(zip(lines["line_id"], lines["card_id"]))
    card_attrs: dict[str, list[tuple[str, float, str]]] = defaultdict(list)
    for lid, slug, conf, src in zip(
        assignments["line_id"], assignments["canonical_slug"],
        assignments["confidence"], assignments["source"],
    ):
        cid = line_to_card.get(lid)
        if cid:
            card_attrs[cid].append((str(slug), float(conf), str(src)))
    return {"name": card_name, "attrs": card_attrs}


def _line_other_attrs(assignments_by_line: dict, line_id: str, exclude_slug: str) -> list[str]:
    """Return formatted list of other archetypes this line is attributed to."""
    attrs = sorted(
        [(s, c, src) for (s, c, src) in assignments_by_line.get(line_id, []) if s != exclude_slug],
        key=lambda x: -x[1],
    )
    return [f"{s}({c:.2f}{('|' + src) if src != 'prototype' else ''})" for s, c, src in attrs]


def _histogram(values: np.ndarray, bins: int = 10) -> str:
    if len(values) == 0:
        return "(empty)"
    counts, _ = np.histogram(values, bins=bins, range=(0.0, 1.0))
    max_c = max(counts) if counts.max() > 0 else 1
    blocks = " ▁▂▃▄▅▆▇█"
    return "".join(blocks[min(len(blocks) - 1, int(round(c / max_c * (len(blocks) - 1))))] for c in counts)


def _window_sample(members: pd.DataFrame, conf_target: float, k: int, window: float, rng: np.random.Generator):
    in_window = members[members["confidence"].between(conf_target - window, conf_target + window)]
    if len(in_window) >= k:
        return in_window.sample(n=k, random_state=int(rng.integers(0, 2**31 - 1)))
    return members.iloc[(members["confidence"] - conf_target).abs().argsort().values[:k]]


def _fmt_example(row: pd.Series, ctx: dict, assignments_by_line: dict, indent: str = "  ") -> list[str]:
    card_id = row["card_id"]
    name = ctx["name"].get(card_id, "<unknown>")
    text = str(row["text"]).replace("\n", " ⏎ ")
    if len(text) > 110:
        text = text[:107] + "…"
    src_tag = "" if row["source"] == "prototype" else f"|{row['source']}"
    out = [f"{indent}[{row['confidence']:.3f}{src_tag}] \"{text}\"  ({name})"]
    other = _line_other_attrs(assignments_by_line, row["line_id"], row["canonical_slug"])
    if other:
        s = ", ".join(other[:8])
        if len(other) > 8:
            s += f", +{len(other)-8} more"
        out.append(f"{indent}    also: {s}")
    return out


def _print_archetype(slug: str, members: pd.DataFrame, ctx: dict, assignments_by_line: dict,
                     archetypes: dict, k: int, out: list[str], rng: np.random.Generator):
    arch = archetypes.get(slug, {})
    name = arch.get("name", slug)
    n_proto = len(arch.get("prototypes", []) or [])
    counts = members["source"].value_counts().to_dict()
    out.append(f"\n████ {slug}  ({name}, {n_proto} prototypes) ████")
    out.append(f"  attributions: {len(members)}  ({counts})")

    inferred = members[members["source"] == "prototype"]
    if len(inferred) >= 5:
        v = inferred["confidence"].to_numpy()
        hist = _histogram(v)
        out.append(
            f"  prototype: n={len(inferred)}  min={v.min():.3f}  q25={np.quantile(v, 0.25):.3f}  "
            f"med={np.median(v):.3f}  q75={np.quantile(v, 0.75):.3f}  max={v.max():.3f}  |{hist}|"
        )
        out.append(f"  examples at percentile points (cosine to prototype centroid):")
        for p in _PERCENTILES:
            qv = float(np.quantile(inferred["confidence"], p))
            out.append(f"  q{int(p*100):02d} (conf ≈ {qv:.3f}):")
            sample = _window_sample(inferred, qv, k, _PERCENTILE_WINDOW, rng)
            for _, row in sample.iterrows():
                out.extend(_fmt_example(row, ctx, assignments_by_line, indent="    "))

    pattern_rows = members[members["source"] == "pattern"]
    if len(pattern_rows) > 0:
        out.append(f"\n  pattern attributions: n={len(pattern_rows)}  (showing random {min(k, len(pattern_rows))})")
        sample = pattern_rows.sample(n=min(k, len(pattern_rows)),
                                     random_state=int(rng.integers(0, 2**31 - 1)))
        for _, row in sample.iterrows():
            out.extend(_fmt_example(row, ctx, assignments_by_line, indent="    "))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--top-n", type=int, default=10,
                    help="Top N archetypes by attribution count (default 10)")
    ap.add_argument("--k", type=int, default=4,
                    help="Examples per percentile point (default 4)")
    ap.add_argument("--archetypes", type=str, default=None,
                    help="Comma-separated slugs to inspect, overrides --top-n")
    ap.add_argument("--markdown", type=str, default=None,
                    help="Also write to this path as plain markdown")
    ap.add_argument("--seed", type=int, default=42)
    args = ap.parse_args()

    rng = np.random.default_rng(args.seed)
    print("Loading…", file=sys.stderr)
    assignments, lines, cards, archetypes = _load()
    print(
        f"Loaded {len(assignments)} attributions, {len(lines)} lines, "
        f"{len(cards)} cards, {len(archetypes)} archetypes",
        file=sys.stderr,
    )

    ctx = _build_card_context(lines, cards, assignments)
    assignments_by_line = ctx["attrs"]

    joined = assignments.merge(lines[["line_id", "card_id", "text"]], on="line_id", how="inner")

    if args.archetypes:
        targets = [s.strip() for s in args.archetypes.split(",") if s.strip()]
    else:
        targets = joined["canonical_slug"].value_counts().head(args.top_n).index.tolist()

    out_lines: list[str] = []
    for tgt in targets:
        members = joined[joined["canonical_slug"] == tgt]
        if len(members) == 0:
            out_lines.append(f"\n[!] No attributions for archetype '{tgt}'")
            continue
        _print_archetype(tgt, members, ctx, assignments_by_line, archetypes, args.k, out_lines, rng)

    body = "\n".join(out_lines)
    print(body)

    if args.markdown:
        path = Path(args.markdown)
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(f"# Prototype attribution inspector\n\n```\n{body}\n```\n")
        print(f"\nWrote markdown to {path}", file=sys.stderr)


if __name__ == "__main__":
    main()
