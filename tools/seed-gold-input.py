#!/usr/bin/env python3
"""Seed authoritative gold Inputs from the corpus — the mast-tdd-loop pre-dispatch oracle-fidelity gate.

The loop's hard-won lesson: a worker must NEVER develop against a hand-composed / mis-transcribed
oracle text. The fidelity test (GoldOracleTextFidelityTests) compares a gold's Input.OracleText to the
corpus (card-inputs.json) — but that test SKIPS in worker worktrees (the gitignored corpus is absent),
so Input drift is invisible to the worker + judge and only surfaces at the orchestrator's post-merge
CORE gate, AFTER the worker built a parser against the wrong text.

This tool moves the check BEFORE delegation: the orchestrator runs it on main (where the corpus lives)
to emit each wave card's canonical gold Input STRAIGHT FROM THE CORPUS — authoritative by construction,
never composed. The orchestrator embeds the emitted Input verbatim in the worker brief; the worker is
forbidden from editing/paraphrasing Input and authors only the Output AST + parser. Cards that are
missing or ambiguous are flagged so they are NOT dispatched blind.

Usage:
  tools/seed-gold-input.py "Dramatic Reversal" "Isochron Scepter" ...
  tools/seed-gold-input.py --file cards.txt          # one card name per line
  tools/seed-gold-input.py --json "Card A" "Card B"   # machine-readable {name: Input} map on stdout

Exit code is nonzero if ANY requested card is missing-from-corpus (so it can gate a dispatch script).
"""
import json
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parents[1]
CORPUS = REPO / "tests/magic-ast-tests/Data/_02_Intermediate/Datasets/card-inputs.json"
BULK = REPO / "tests/magic-ast-tests/Data/_01_Raw/Datasets/External/oracle-cards.json"
GOLD_INPUT_FIELDS = ["Name", "ManaCost", "TypeLine", "OracleText", "Power", "Toughness", "Colors", "ColorIdentity"]


def load_corpus():
    """name -> canonical gold Input (8 fields). Corpus wins; Scryfall bulk fills only corpus gaps."""
    by_name = {}
    if CORPUS.exists():
        for rec in json.loads(CORPUS.read_text()):
            inp = rec.get("Input") if isinstance(rec, dict) else None
            if inp and inp.get("Name") and inp["Name"] not in by_name:
                by_name[inp["Name"]] = ("corpus", inp)
    bulk_index = {}
    if BULK.exists():
        for c in json.loads(BULK.read_text()):
            n = c.get("name")
            if n and n not in bulk_index:
                bulk_index[n] = c
    return by_name, bulk_index


def canonical_input(name, source, raw):
    if source == "corpus":
        return {k: raw[k] for k in GOLD_INPUT_FIELDS if k in raw}
    # bulk fallback (card filtered OUT of the commander-legal corpus): project the Scryfall fields.
    return {
        "Name": raw.get("name"),
        "ManaCost": raw.get("mana_cost", ""),
        "TypeLine": raw.get("type_line", ""),
        "OracleText": raw.get("oracle_text", ""),
        "Power": raw.get("power"),
        "Toughness": raw.get("toughness"),
        "Colors": raw.get("colors", []),
        "ColorIdentity": raw.get("color_identity", []),
    }


def main(argv):
    as_json = "--json" in argv
    argv = [a for a in argv if a != "--json"]
    if argv and argv[0] == "--file":
        names = [l.strip() for l in Path(argv[1]).read_text().splitlines() if l.strip()]
    else:
        names = argv
    if not names:
        print(__doc__)
        return 2

    by_name, bulk_index = load_corpus()
    if not by_name and not bulk_index:
        print(f"FATAL: no corpus at {CORPUS} and no bulk at {BULK} — run the InteractionTriage/MagicAstTriage flow.", file=sys.stderr)
        return 3

    out = {}
    missing = []
    for name in names:
        if name in by_name:
            src, raw = by_name[name]
            out[name] = {"source": "corpus", "input": canonical_input(name, "corpus", raw)}
        elif name in bulk_index:
            out[name] = {"source": "bulk-fallback", "input": canonical_input(name, "bulk", bulk_index[name])}
        else:
            missing.append(name)
            out[name] = {"source": "MISSING", "input": None}

    if as_json:
        print(json.dumps(out, indent=2))
    else:
        for name in names:
            e = out[name]
            print(f"\n=== {name}  [{e['source']}] ===")
            if e["input"] is None:
                print("  !! NOT FOUND in corpus or bulk — do NOT dispatch; verify the card name / refresh triage.")
            else:
                i = e["input"]
                print(f"  ManaCost: {i.get('ManaCost')!r}  TypeLine: {i.get('TypeLine')!r}  P/T: {i.get('Power')}/{i.get('Toughness')}")
                print(f"  Colors: {i.get('Colors')}  ColorIdentity: {i.get('ColorIdentity')}")
                print(f"  OracleText (AUTHORITATIVE — use verbatim in the gold Input, do not paraphrase):")
                print("    " + (i.get("OracleText") or "").replace("\n", "\n    "))

    if missing:
        print(f"\nFAIL: {len(missing)} card(s) missing from corpus+bulk: {missing}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
