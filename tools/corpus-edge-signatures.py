#!/usr/bin/env python3
"""Per-card edge signatures from card-edges.json — the input to the corpus-edge-diff overfit gate.

The mast-tdd-loop's #1 recurring FAIL is the OVERFIT: a new/edited rule silently mislabels a sibling
corpus card (drops a filter, flips a derived kind LifeLost<->Mill), invisible to the worker suite (the
sibling has no gold fixture) and to the per-combo bench (only 33 pinned combos). The signal that moves
is the card's PORT PROJECTION — its labels in card-edges.json (the materialized union interaction graph
over ~2,900 cards). A parse-records diff is blind to it (the semantic content changes, not the counts).

This emits a compact, deterministic per-card signature: for each card, the sha256 of its sorted, unique
edge tuples (the card as either endpoint), so any change to a card's interaction footprint changes its
signature. The gate (tools/gate-corpus-edge-diff.sh) diffs two signature snapshots and HALTs on any
NON-TARGET card whose signature changed — mechanizing the sibling-sweep the judge did by hand.

Usage:  tools/corpus-edge-signatures.py <card-edges.json>  > sigs.json
"""
import hashlib
import json
import sys
from collections import defaultdict


def main(argv):
    if len(argv) != 1:
        print(__doc__, file=sys.stderr)
        return 2
    edges = json.loads(open(argv[0]).read())
    # Attribute each edge to BOTH endpoints, so a change to either card's labels moves its signature.
    by_card = defaultdict(set)
    for e in edges:
        tup = "{}|{}->{}|{}|{}|{}".format(
            e.get("fromCard", ""), e.get("fromLabel", ""),
            e.get("toCard", ""), e.get("toLabel", ""),
            e.get("resource", ""), e.get("tier", ""),
        )
        by_card[e.get("fromCard", "")].add(tup)
        by_card[e.get("toCard", "")].add(tup)
    sigs = {
        card: hashlib.sha256("\n".join(sorted(tups)).encode()).hexdigest()
        for card, tups in by_card.items()
        if card
    }
    json.dump(sigs, sys.stdout, indent=0, sort_keys=True)
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
