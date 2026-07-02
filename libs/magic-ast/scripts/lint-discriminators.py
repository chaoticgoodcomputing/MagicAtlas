#!/usr/bin/env python3
"""
Discriminator governance lint for MagicAST (alignment initiative 02).

The AST vocabulary is a set of polymorphic discriminator strings declared via
attributes ([OracleEffect("dealDamage")], [OracleCost("tap")], [ConditionKind("count")], …).
Over many TDD batches this sprawls: a worker adds `dealDamageToEach` beside `dealDamage`,
or two concurrent workers land colliding strings. This lint converts the convention
into an exit code.

Scope is PER-FAMILY (per polymorphic base), NOT global. Cross-base reuse is legitimate
and common in this codebase: `untap` is an Effect, a Cost, AND a ReplacementEvent;
`sacrifice`/`tap`/`exile` are both Cost and Effect. A duplicate is only a collision
when two types in the SAME family claim the same string.

Checks:
  - HARD FAIL: the same discriminator declared twice within one family (a real
    serialization collision — the polymorphic converter can't disambiguate).
  - SOFT FAIL: a NEW discriminator (vs the committed baseline) that is a near-duplicate
    of an existing one in the same family — within Levenshtein <= 2 (case-insensitive)
    or where one is a prefix-stem of the other — unless a justification entry exists
    in discriminator-justifications.json.

Files (under libs/magic-ast/schema/, overridable):
  - discriminator-baseline.json       {"discriminators": ["OracleEffect:dealDamage", ...]}
                                       (sorted, family-qualified; defines "new")
  - discriminator-justifications.json  [{"name": "...", "near": "...", "reason": "..."}]
                                       (append-only, judge-reviewed)

Modes:
  (default)            lint current source against the baseline; exit 1 on any hard/soft fail.
  --update-baseline    rewrite the baseline from current source (run per merge group AFTER
                       a clean lint). Also prints a full intra-family near-duplicate audit.
  --audit              print every intra-family near-duplicate pair (for seeding
                       justifications); exit 0.
  --list               dump all discriminators (family:value); exit 0.

Overrides for self-tests: --source-root, --baseline, --justifications.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from dataclasses import dataclass

LIB_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
DEFAULT_SOURCE_ROOT = os.path.join(LIB_ROOT, "AST")
SCHEMA_DIR = os.path.join(LIB_ROOT, "schema")
DEFAULT_BASELINE = os.path.join(SCHEMA_DIR, "discriminator-baseline.json")
DEFAULT_JUSTIFICATIONS = os.path.join(SCHEMA_DIR, "discriminator-justifications.json")

# Attribute families whose single string argument is a polymorphic discriminator.
FAMILIES = (
    "OracleAbility",
    "OracleEffect",
    "OracleDuration",
    "OracleCost",
    "OracleQuantity",
    "OracleReplacementEvent",
    "CardAttributeKind",
    "PowerToughnessKind",
    "CharacteristicKind",
    "ConditionKind",
    "CopyModificationKind",
    "HistoryPredicateKind",
    "AbilityReferenceKind",
)

# [Family("value"  -> capture family + value. Tolerates extra args after the string.
_ATTR_RE = re.compile(
    r'\[(' + "|".join(FAMILIES) + r')\(\s*"([^"]+)"'
)

# Near-duplicate thresholds.
LEVENSHTEIN_MAX = 2
STEM_MIN_LEN = 4


@dataclass(frozen=True)
class Discriminator:
    family: str
    value: str
    file: str  # relative to source root
    line: int

    @property
    def qualified(self) -> str:
        return f"{self.family}:{self.value}"


# ----- extraction ---------------------------------------------------------------------

def extract(source_root: str) -> list[Discriminator]:
    found: list[Discriminator] = []
    for dirpath, _, filenames in os.walk(source_root):
        for fn in filenames:
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, source_root)
            with open(path, "r") as f:
                for lineno, line in enumerate(f, start=1):
                    for m in _ATTR_RE.finditer(line):
                        found.append(Discriminator(m.group(1), m.group(2), rel, lineno))
    return found


# ----- near-duplicate detection -------------------------------------------------------

def levenshtein(a: str, b: str) -> int:
    a, b = a.casefold(), b.casefold()
    if a == b:
        return 0
    if not a:
        return len(b)
    if not b:
        return len(a)
    prev = list(range(len(b) + 1))
    for i, ca in enumerate(a, start=1):
        cur = [i]
        for j, cb in enumerate(b, start=1):
            cost = 0 if ca == cb else 1
            cur.append(min(prev[j] + 1, cur[j - 1] + 1, prev[j - 1] + cost))
        prev = cur
    return prev[-1]


def shares_stem(a: str, b: str) -> bool:
    """One value is a prefix-stem of the other (e.g. dealDamage / dealDamageToEach)."""
    x, y = a.casefold(), b.casefold()
    if x == y:
        return False
    short, long = (x, y) if len(x) <= len(y) else (y, x)
    return len(short) >= STEM_MIN_LEN and long.startswith(short)


def is_near(a: str, b: str) -> bool:
    if a == b:
        return False
    return levenshtein(a, b) <= LEVENSHTEIN_MAX or shares_stem(a, b)


# ----- baseline / justifications ------------------------------------------------------

def load_baseline(path: str) -> set[str]:
    if not os.path.isfile(path):
        return set()
    with open(path) as f:
        data = json.load(f)
    return set(data.get("discriminators", []))


def load_justifications(path: str) -> set[tuple[str, str]]:
    """Return a set of (name, near) pairs that have been explained."""
    if not os.path.isfile(path):
        return set()
    with open(path) as f:
        data = json.load(f)
    pairs: set[tuple[str, str]] = set()
    for entry in data:
        name = entry.get("name")
        near = entry.get("near")
        if name and near:
            # Justification is symmetric — a pair is explained either direction.
            pairs.add((name, near))
            pairs.add((near, name))
    return pairs


def write_baseline(path: str, discs: list[Discriminator]) -> None:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    qualified = sorted({d.qualified for d in discs})
    with open(path, "w") as f:
        json.dump({"discriminators": qualified}, f, indent=2)
        f.write("\n")


# ----- checks -------------------------------------------------------------------------

def find_hard_collisions(discs: list[Discriminator]) -> list[tuple[str, str, list[Discriminator]]]:
    """Same (family, value) declared more than once."""
    by_key: dict[tuple[str, str], list[Discriminator]] = {}
    for d in discs:
        by_key.setdefault((d.family, d.value), []).append(d)
    return [(fam, val, ds) for (fam, val), ds in sorted(by_key.items()) if len(ds) > 1]


def find_near_pairs(discs: list[Discriminator]) -> list[tuple[str, Discriminator, Discriminator]]:
    """All intra-family near-duplicate pairs (each pair once)."""
    by_family: dict[str, list[Discriminator]] = {}
    for d in discs:
        by_family.setdefault(d.family, []).append(d)
    pairs: list[tuple[str, Discriminator, Discriminator]] = []
    for fam, members in by_family.items():
        uniq = sorted({m.value: m for m in members}.values(), key=lambda d: d.value)
        for i in range(len(uniq)):
            for j in range(i + 1, len(uniq)):
                if is_near(uniq[i].value, uniq[j].value):
                    pairs.append((fam, uniq[i], uniq[j]))
    return pairs


# ----- modes --------------------------------------------------------------------------

def run_lint(discs, baseline, justifications) -> int:
    failures = 0

    collisions = find_hard_collisions(discs)
    if collisions:
        failures += len(collisions)
        sys.stderr.write("HARD FAIL — duplicate discriminator(s) within a family:\n")
        for fam, val, ds in collisions:
            locs = ", ".join(f"{d.file}:{d.line}" for d in ds)
            sys.stderr.write(f"  [{fam}] \"{val}\" declared {len(ds)}x: {locs}\n")

    current_qualified = {d.qualified for d in discs}
    new_qualified = current_qualified - baseline
    new_discs = [d for d in discs if d.qualified in new_qualified]

    # Compare each NEW discriminator against every OTHER known one in the same family.
    by_family: dict[str, set[str]] = {}
    for d in discs:
        by_family.setdefault(d.family, set()).add(d.value)
    for q in baseline:
        if ":" in q:
            fam, val = q.split(":", 1)
            by_family.setdefault(fam, set()).add(val)

    soft = []
    for nd in sorted(set((d.family, d.value) for d in new_discs)):
        fam, val = nd
        for other in sorted(by_family.get(fam, set())):
            if is_near(val, other) and (val, other) not in justifications:
                soft.append((fam, val, other))
    if soft:
        failures += len(soft)
        sys.stderr.write("SOFT FAIL — new near-duplicate discriminator(s) (add a justification or rename):\n")
        for fam, val, other in soft:
            sys.stderr.write(f"  [{fam}] new \"{val}\" ~ existing \"{other}\"\n")

    if failures:
        sys.stderr.write(
            f"\ndiscriminator lint FAILED ({len(collisions)} collision(s), {len(soft)} unexplained near-dup(s)).\n"
            "Resolve collisions by renaming; resolve near-dups by renaming or adding an entry to\n"
            f"{os.path.relpath(DEFAULT_JUSTIFICATIONS, LIB_ROOT)} ({{name, near, reason}}).\n"
        )
        return 1

    print(f"discriminator lint OK ({len(discs)} discriminators, {len(new_qualified)} new vs baseline).")
    return 0


def run_audit(discs) -> int:
    pairs = find_near_pairs(discs)
    if not pairs:
        print("No intra-family near-duplicate pairs.")
        return 0
    print(f"{len(pairs)} intra-family near-duplicate pair(s) — seed justifications or a consolidation TODO:")
    for fam, a, b in pairs:
        print(f'  [{fam}] "{a.value}" ({a.file}:{a.line})  ~  "{b.value}" ({b.file}:{b.line})')
    print("\nJustification stub:")
    stub = [{"name": a.value, "near": b.value, "reason": "TODO"} for _, a, b in pairs]
    print(json.dumps(stub, indent=2))
    return 0


def main() -> int:
    p = argparse.ArgumentParser(description="MagicAST discriminator governance lint.")
    p.add_argument("--update-baseline", action="store_true", help="Rewrite the baseline from current source.")
    p.add_argument("--audit", action="store_true", help="List all intra-family near-duplicate pairs.")
    p.add_argument("--list", action="store_true", help="Dump all discriminators.")
    p.add_argument("--source-root", default=os.environ.get("MAST_AST_ROOT", DEFAULT_SOURCE_ROOT))
    p.add_argument("--baseline", default=os.environ.get("MAST_DISC_BASELINE", DEFAULT_BASELINE))
    p.add_argument("--justifications", default=os.environ.get("MAST_DISC_JUSTIFICATIONS", DEFAULT_JUSTIFICATIONS))
    args = p.parse_args()

    discs = extract(args.source_root)

    if args.list:
        for d in sorted(discs, key=lambda d: (d.family, d.value)):
            print(f"{d.qualified}\t{d.file}:{d.line}")
        return 0

    if args.audit:
        return run_audit(discs)

    if args.update_baseline:
        # Surface near-dups even on baseline refresh (informational), then write.
        run_audit(discs)
        write_baseline(args.baseline, discs)
        print(f"Wrote baseline {args.baseline} ({len({d.qualified for d in discs})} discriminators).")
        return 0

    baseline = load_baseline(args.baseline)
    justifications = load_justifications(args.justifications)
    return run_lint(discs, baseline, justifications)


if __name__ == "__main__":
    sys.exit(main())
