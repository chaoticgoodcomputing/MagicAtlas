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
  - HARD FAIL (the GATE): the same discriminator declared twice within one family — a real
    serialization collision, the polymorphic converter cannot disambiguate. Exits nonzero.
    Needs no whitelist: a genuine duplicate is always a bug.
  - REPORT ONLY: every intra-family near-duplicate pair — Levenshtein <= 2 (case-insensitive)
    or one a prefix-stem of the other — split into EXPLAINED (some declaration site names the
    other in [Family("x", NearDuplicateOf = new[] { "y" }, Reason = "...")]) and UNEXPLAINED.
    Printed, never fatal.

STATELESS, and no whitelist FILE (2026-07-20, ADR-0004 issue #38). The lint has no baseline: the
committed `discriminator-baseline.json` snapshot was a debt baseline that drifted (330 committed
entries against 364 in source), and regenerating it would have made the check vacuous.

The justification whitelist has now gone the same way, for the same reason one step removed. It was
`discriminator-justifications.json`, a JSON file naming pairs; a file like that can outlive its
subject — delete a type and its justification survives, describing a discriminator that no longer
exists. The rulings now live as named arguments on the discriminator attribute at the DECLARATION
SITE, so liveness is structural: deleting the type deletes its justification in the same edit.

With no whitelist to enforce, the near-duplicate half stops being a gate and becomes a report — the
Flowthru `DiscriminatorGovernance` flow, plus this lint's own printout. What a report is FOR is
distinguishing an explained pair from a new one, which is why the Reason survived even though the
gate did not. The hard-collision half is still a gate and still exits nonzero.

Source of truth (no data files):
  - libs/magic-ast/AST/**/*.cs   [Family("value", NearDuplicateOf = new[] { "other" }, Reason = "…")]

Modes:
  (default)            hard-collision GATE + near-duplicate REPORT; exit 1 only on a collision.
  --audit              print every intra-family near-duplicate pair with its explanation
                       status (and a stub attribute for the unexplained ones); exit 0.
  --list               dump all discriminators (family:value); exit 0.

Override for self-tests: --source-root.
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

# [Family("value" ... )] -> capture family, value and the named-argument tail. Attributes carrying a
# NearDuplicateOf/Reason ruling span several lines, so this matches across newlines and stops at the
# first attribute terminator.
_ATTR_RE = re.compile(
    r'\[(' + "|".join(FAMILIES) + r')\(\s*"([^"]+)"(.*?)\)\]',
    re.DOTALL,
)

# The declaration-site justification: NearDuplicateOf = new[] { "a", "b" } , Reason = "…"
_NEAR_RE = re.compile(r'NearDuplicateOf\s*=\s*(?:new(?:\s*\[\s*\])?\s*)?\{([^}]*)\}')
_NEAR_SINGLE_RE = re.compile(r'NearDuplicateOf\s*=\s*"([^"]+)"')
_REASON_RE = re.compile(r'Reason\s*=\s*"([^"]*)"', re.DOTALL)
_STRING_RE = re.compile(r'"([^"]*)"')

# Near-duplicate thresholds.
LEVENSHTEIN_MAX = 2
STEM_MIN_LEN = 4


@dataclass(frozen=True)
class Discriminator:
    family: str
    value: str
    file: str  # relative to source root
    line: int
    near: tuple[str, ...] = ()   # declared NearDuplicateOf counterparts
    reason: str | None = None    # the ruling behind them

    @property
    def qualified(self) -> str:
        return f"{self.family}:{self.value}"


# ----- extraction ---------------------------------------------------------------------

def extract(source_root: str) -> list[Discriminator]:
    found: list[Discriminator] = []
    for dirpath, _, filenames in os.walk(source_root):
        for fn in sorted(filenames):
            if not fn.endswith(".cs"):
                continue
            path = os.path.join(dirpath, fn)
            rel = os.path.relpath(path, source_root)
            with open(path, "r") as f:
                text = f.read()
            for m in _ATTR_RE.finditer(text):
                tail = m.group(3)
                near: tuple[str, ...] = ()
                nm = _NEAR_RE.search(tail)
                if nm:
                    near = tuple(_STRING_RE.findall(nm.group(1)))
                else:
                    single = _NEAR_SINGLE_RE.search(tail)
                    if single:
                        near = (single.group(1),)
                rm = _REASON_RE.search(tail)
                found.append(
                    Discriminator(
                        m.group(1),
                        m.group(2),
                        rel,
                        text.count("\n", 0, m.start()) + 1,
                        near,
                        rm.group(1) if rm else None,
                    )
                )
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


# ----- justifications (read from the DECLARATION SITES, no data file) ----------------------

def load_justifications(discs: list[Discriminator]) -> dict[tuple[str, str], tuple[str, str]]:
    """(a, b) -> (declaring discriminator, reason), for every declared near-duplicate ruling.

    Symmetric: a pair is explained if EITHER side names the other. Convention is that the ruling sits
    on the more specific member, but the lint does not care which.
    """
    pairs: dict[tuple[str, str], tuple[str, str]] = {}
    for d in discs:
        for other in d.near:
            reason = d.reason or ""
            pairs[(d.value, other)] = (d.value, reason)
            pairs.setdefault((other, d.value), (d.value, reason))
    return pairs


def find_dead_justifications(discs: list[Discriminator]) -> list[tuple[Discriminator, str]]:
    """A declared NearDuplicateOf counterpart that no longer exists in the same family, or that is no
    longer actually near. Reported (not fatal) — the structural liveness the attribute buys is that the
    ruling dies with its OWN type; this catches the other side going away."""
    by_family: dict[str, set[str]] = {}
    for d in discs:
        by_family.setdefault(d.family, set()).add(d.value)
    dead: list[tuple[Discriminator, str]] = []
    for d in discs:
        for other in d.near:
            if other not in by_family.get(d.family, set()):
                dead.append((d, f'"{other}" is not declared in [{d.family}]'))
            elif not is_near(d.value, other):
                dead.append((d, f'"{other}" is no longer a near-duplicate of "{d.value}"'))
    return dead


def find_missing_reasons(discs: list[Discriminator]) -> list[Discriminator]:
    """NearDuplicateOf without a Reason — an unexplained explanation."""
    return [d for d in discs if d.near and not (d.reason or "").strip()]


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
    """The GATE is the hard per-family collision only. The near-duplicate half is a REPORT.

    See the module docstring: with the justification whitelist relocated to the declaration sites there
    is no data file for a soft gate to enforce, and a near-duplicate is a design question (is this
    sprawl?) rather than a defect. A collision is always a defect, so it stays fatal.
    """
    collisions = find_hard_collisions(discs)
    if collisions:
        sys.stderr.write("HARD FAIL — duplicate discriminator(s) within a family:\n")
        for fam, val, ds in collisions:
            locs = ", ".join(f"{d.file}:{d.line}" for d in ds)
            sys.stderr.write(f"  [{fam}] \"{val}\" declared {len(ds)}x: {locs}\n")

    pairs = find_near_pairs(discs)
    unexplained = [(fam, a, b) for fam, a, b in pairs if (a.value, b.value) not in justifications]
    dead = find_dead_justifications(discs)
    missing_reasons = find_missing_reasons(discs)

    print(
        f"discriminator report: {len(discs)} discriminators, {len(pairs)} intra-family near-duplicate "
        f"pair(s), {len(pairs) - len(unexplained)} explained at their declaration site, "
        f"{len(unexplained)} unexplained."
    )
    for fam, a, b in unexplained:
        print(
            f'  UNEXPLAINED [{fam}] "{a.value}" ({a.file}:{a.line})  ~  "{b.value}" ({b.file}:{b.line})'
        )
    for d, why in dead:
        print(f'  DEAD RULING  [{d.family}] "{d.value}" ({d.file}:{d.line}): {why}')
    for d in missing_reasons:
        print(f'  NO REASON    [{d.family}] "{d.value}" ({d.file}:{d.line}) declares NearDuplicateOf')
    if unexplained:
        print(
            "\nUnexplained pairs are a REPORT, not a failure. Either rename/consolidate, or record the "
            "ruling on the more specific type:\n"
            '  [OracleEffect("gift", NearDuplicateOf = new[] { "graft" }, Reason = "…CR citation…")]'
        )

    if collisions:
        sys.stderr.write(
            f"\ndiscriminator lint FAILED ({len(collisions)} collision(s)). Resolve by renaming — a "
            "duplicate discriminator inside one family is a serialization bug, never a judgement call.\n"
        )
        return 1

    print("discriminator lint OK (no intra-family collisions).")
    return 0


def run_audit(discs) -> int:
    justifications = load_justifications(discs)
    pairs = find_near_pairs(discs)
    if not pairs:
        print("No intra-family near-duplicate pairs.")
        return 0
    print(f"{len(pairs)} intra-family near-duplicate pair(s):")
    for fam, a, b in pairs:
        held = justifications.get((a.value, b.value))
        mark = f"explained on \"{held[0]}\"" if held else "UNEXPLAINED"
        print(f'  [{fam}] "{a.value}" ({a.file}:{a.line})  ~  "{b.value}" ({b.file}:{b.line})  — {mark}')
    unexplained = [(fam, a, b) for fam, a, b in pairs if (a.value, b.value) not in justifications]
    if unexplained:
        print("\nDeclaration-site stub for the unexplained pair(s) — put it on the more specific type:")
        for fam, a, b in unexplained:
            print(f'  [{fam}("{b.value}", NearDuplicateOf = new[] {{ "{a.value}" }}, Reason = "TODO")]')
    return 0


def main() -> int:
    p = argparse.ArgumentParser(description="MagicAST discriminator governance lint.")
    p.add_argument("--audit", action="store_true", help="List all intra-family near-duplicate pairs + their explanation status.")
    p.add_argument("--list", action="store_true", help="Dump all discriminators.")
    p.add_argument("--source-root", default=os.environ.get("MAST_AST_ROOT", DEFAULT_SOURCE_ROOT))
    args = p.parse_args()

    discs = extract(args.source_root)

    if args.list:
        for d in sorted(discs, key=lambda d: (d.family, d.value)):
            print(f"{d.qualified}\t{d.file}:{d.line}")
        return 0

    if args.audit:
        return run_audit(discs)

    return run_lint(discs, load_justifications(discs))


if __name__ == "__main__":
    sys.exit(main())
