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
  - SOFT FAIL: ANY two discriminators in the same family that are near-duplicates —
    within Levenshtein <= 2 (case-insensitive) or where one is a prefix-stem of the
    other — unless a justification entry exists in discriminator-justifications.json.

STATELESS (2026-07-20). This lint has no baseline and no notion of a "new" discriminator.
It previously compared against a committed `discriminator-baseline.json` snapshot, which is
a debt baseline: it grandfathers everything that already existed and only asks about the
delta. It drifted the way debt baselines do — 330 committed entries against 364 in source,
so 34 discriminators had been permanently "new" (and re-reported on every run) because the
baseline refresh step was a manual chore nobody had run. The obvious repair — regenerate the
baseline — is worse than the drift: it makes every pair not-new and the check vacuous.

The stateless form has no such failure mode. Every near-duplicate pair in the source must
carry a named justification, asked and answered on every run, with the whitelist as the only
state. This is the project's standing "stateless invariants + explicit named whitelists,
never shrink-only debt baselines" rule, applied to the one place still violating it.

Files (under libs/magic-ast/schema/, overridable):
  - discriminator-justifications.json  [{"name": "...", "near": "...", "reason": "..."}]
                                       (judge-reviewed; matched symmetrically)

Modes:
  (default)            lint the current source; exit 1 on any hard/soft fail.
  --audit              print every intra-family near-duplicate pair (for seeding
                       justifications); exit 0.
  --list               dump all discriminators (family:value); exit 0.

Overrides for self-tests: --source-root, --justifications.
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


# ----- justifications ------------------------------------------------------

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

def run_lint(discs, justifications) -> int:
    """STATELESS. Every near-duplicate pair in the CURRENT source must carry a justification.

    There is deliberately no notion of "new" — see the module docstring for why the retired
    `discriminator-baseline.json` had to go rather than be refreshed.
    """
    failures = 0

    collisions = find_hard_collisions(discs)
    if collisions:
        failures += len(collisions)
        sys.stderr.write("HARD FAIL — duplicate discriminator(s) within a family:\n")
        for fam, val, ds in collisions:
            locs = ", ".join(f"{d.file}:{d.line}" for d in ds)
            sys.stderr.write(f"  [{fam}] \"{val}\" declared {len(ds)}x: {locs}\n")

    soft = [
        (fam, a, b)
        for fam, a, b in find_near_pairs(discs)
        if (a.value, b.value) not in justifications
    ]
    if soft:
        failures += len(soft)
        sys.stderr.write("SOFT FAIL — unjustified near-duplicate discriminator(s) (justify or rename):\n")
        for fam, a, b in soft:
            sys.stderr.write(
                f"  [{fam}] \"{a.value}\" ({a.file}:{a.line})  ~  \"{b.value}\" ({b.file}:{b.line})\n"
            )

    if failures:
        sys.stderr.write(
            f"\ndiscriminator lint FAILED ({len(collisions)} collision(s), {len(soft)} unexplained near-dup(s)).\n"
            "Resolve collisions by renaming; resolve near-dups by renaming or adding an entry to\n"
            f"{os.path.relpath(DEFAULT_JUSTIFICATIONS, LIB_ROOT)} ({{name, near, reason}}).\n"
        )
        return 1

    print(
        f"discriminator lint OK ({len(discs)} discriminators, "
        f"{len(find_near_pairs(discs))} near-dup pair(s), all justified)."
    )
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
    p.add_argument("--audit", action="store_true", help="List all intra-family near-duplicate pairs.")
    p.add_argument("--list", action="store_true", help="Dump all discriminators.")
    p.add_argument("--source-root", default=os.environ.get("MAST_AST_ROOT", DEFAULT_SOURCE_ROOT))
    p.add_argument("--justifications", default=os.environ.get("MAST_DISC_JUSTIFICATIONS", DEFAULT_JUSTIFICATIONS))
    args = p.parse_args()

    discs = extract(args.source_root)

    if args.list:
        for d in sorted(discs, key=lambda d: (d.family, d.value)):
            print(f"{d.qualified}\t{d.file}:{d.line}")
        return 0

    if args.audit:
        return run_audit(discs)

    justifications = load_justifications(args.justifications)
    return run_lint(discs, justifications)


if __name__ == "__main__":
    sys.exit(main())
