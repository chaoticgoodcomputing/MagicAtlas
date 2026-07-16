#!/usr/bin/env python3
"""Interaction-rollup generator (ADR-0003 §8, Stage 0b).

Reads the hand-authored interaction golds, validates them, and generates the four rollup artifacts
(port-topology + port-interactions, each with a .cited verbose twin). The lean files are a
strip(provenance) projection of the verbose ones — one pass, no drift. Conflicts FAIL the pass.

Usage:  python3 tools/interaction-rollup/generate.py [--check]
  --check : validate + report only; do not write the rollup files (CI/gate mode).

Exit 0 on success; nonzero on any validation error or rule conflict.
"""
import json, sys, os, glob

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
GOLDS = os.path.join(ROOT, "tests", "magic-ast-tests", "Fixtures", "Interactions", "golds")
OUT   = os.path.join(ROOT, "tests", "magic-ast-tests", "Fixtures", "Interactions", "rollup")

STRUCTURAL = {"subsumption", "card-defined", "modifier"}         # need no residual rule
SECTIONS   = ["polarity", "match_policy", "guards", "bridges"]
CORE = {  # fields that DEFINE a rule (a mismatch here across golds = conflict); everything else is provenance
    "polarity":     ["attr", "context", "value"],
    "match_policy": ["consume_kind", "subject"],
    "guards":       ["impl"],
    "bridges":      ["from_stem", "to_stem", "ceiling"],
}
errors, conflicts = [], []


def load():
    golds = []
    for p in sorted(glob.glob(os.path.join(GOLDS, "*.json"))):
        try:
            golds.append((os.path.basename(p), json.load(open(p))))
        except Exception as e:
            errors.append(f"{os.path.basename(p)}: unparseable JSON — {e}")
    return golds


def validate(fname, g):
    gid = g.get("id", fname)
    for k in ("id", "unit", "cards", "ports", "edges", "declares"):
        if k not in g:
            errors.append(f"{gid}: missing required key '{k}'")
    if g.get("unit") not in ("single-card", "pairwise", "combo"):
        errors.append(f"{gid}: unit must be single-card|pairwise|combo, got {g.get('unit')!r}")
    # port ids unique per card; build the resolvable "Card.Id" set
    ports = set()
    for card, plist in g.get("ports", {}).items():
        seen = set()
        for p in plist:
            pid = p.get("id")
            if pid in seen:
                errors.append(f"{gid}: duplicate port id {card}.{pid}")
            seen.add(pid)
            ports.add(f"{card}.{pid}")
            for req in ("side", "kind", "stem", "attrs"):
                if req not in p:
                    errors.append(f"{gid}: port {card}.{pid} missing '{req}'")
    # local declared rule ids
    local_rules = {r["id"] for sec in SECTIONS for r in g.get("declares", {}).get(sec, [])}
    # edges resolve; non-structural mechanisms cite a rule
    eids = set()
    for e in g.get("edges", []):
        eid = e.get("id")
        if eid in eids:
            errors.append(f"{gid}: duplicate edge id {eid}")
        eids.add(eid)
        for end in ("from", "to"):
            ref = e.get(end)
            if ref not in ports:
                errors.append(f"{gid}: edge {eid} {end}={ref!r} does not resolve to a declared port")
        mech = e.get("mechanism")
        if mech not in STRUCTURAL:
            rule = e.get("rule")
            if not rule:
                errors.append(f"{gid}: edge {eid} mechanism={mech!r} must cite a 'rule'")
            elif rule not in local_rules:
                e["_rule_external"] = rule  # resolved against the global union later
    return ports


def rule_status(witnesses, golds_by_id):
    """observed(1) → corroborated(≥2) → confirmed(any witness judge-PASSed)."""
    if any(golds_by_id[w].get("judge", {}).get("verdict") == "PASS" for w in witnesses):
        return "confirmed"
    return "corroborated" if len(witnesses) >= 2 else "observed"


def build(write=True):
    golds = load()
    if not golds:
        errors.append("no golds found")
    for fname, g in golds:
        validate(fname, g)
    golds_by_id = {g["id"]: g for _, g in golds if "id" in g}

    # ---- union rules (with conflict detection + witnesses) ----
    rules = {sec: {} for sec in SECTIONS}   # sec -> id -> {rule, core, witnesses:set}
    for _, g in golds:
        for sec in SECTIONS:
            for r in g.get("declares", {}).get(sec, []):
                rid = r["id"]
                core = {k: r.get(k) for k in CORE[sec]}
                slot = rules[sec].get(rid)
                if slot is None:
                    rules[sec][rid] = {"rule": r, "core": core, "witnesses": {g["id"]}}
                elif slot["core"] != core:
                    conflicts.append(f"{sec}:{rid} — {g['id']} declares {core} but a prior gold declared {slot['core']}")
                else:
                    slot["witnesses"].add(g["id"])

    all_rule_ids = {rid for sec in SECTIONS for rid in rules[sec]}
    # every external rule ref must exist somewhere in the union
    for _, g in golds:
        for e in g.get("edges", []):
            ext = e.get("_rule_external")
            if ext and ext not in all_rule_ids:
                errors.append(f"{g['id']}: edge {e['id']} cites rule {ext!r} declared by no gold")

    # ---- ladder coherence: a GREEN edge/loop must rest on confirmed rules ----
    def status_of(rid):
        for sec in SECTIONS:
            if rid in rules[sec]:
                return rule_status(rules[sec][rid]["witnesses"], golds_by_id)
        return None
    for _, g in golds:
        if g.get("loop_tier") == "GREEN" or any(e.get("tier") == "GREEN" for e in g.get("edges", [])):
            for e in g.get("edges", []):
                if e.get("tier") == "GREEN" and e.get("rule"):
                    st = status_of(e["rule"])
                    if st != "confirmed":
                        errors.append(f"{g['id']}: GREEN edge {e['id']} rests on rule {e['rule']} "
                                      f"(status={st}) — only 'confirmed' rules may certify GREEN")

    # ---- build artifacts ----
    stems, axes = {}, {}
    kinds = {"EVENT": set(), "STATE": set(), "BEHAVIOR": set()}
    for _, g in golds:
        for card, plist in g.get("ports", {}).items():
            for p in plist:
                stem, kind = p["stem"], p["kind"]
                kinds.setdefault(kind, set()).add(stem)
                s = stems.setdefault(stem, {"kind": kind, "parent": (stem.rsplit(":", 1)[0] if ":" in stem else None),
                                            "status": "witnessed", "attrs_seen": set(), "witnesses": set()})
                s["witnesses"].add(g["id"])
                for ak, av in p["attrs"].items():
                    s["attrs_seen"].add(ak)
                    ax = axes.setdefault(ak, {"stems": set(), "values_seen": set(), "provenance_or_polarity": False})
                    ax["stems"].add(stem)
                    if isinstance(av, dict):
                        ax["provenance_or_polarity"] = True
                        av = av.get("value")
                    ax["values_seen"].add(str(av))

    def jset(x):  # sorted list for stable output
        return sorted(x) if isinstance(x, (set,)) else x

    topology = {"$generated": "tools/interaction-rollup", "$golds": sorted(golds_by_id),
                "kinds": {k: jset(v) for k, v in kinds.items() if v},
                "stems": {s: {"kind": d["kind"], "parent": d["parent"], "status": d["status"],
                              "attrs": jset(d["attrs_seen"])} for s, d in sorted(stems.items())},
                "attribute_axes": {a: {"stems": jset(d["stems"]), "values_seen": jset(d["values_seen"]),
                                       "carries_provenance_or_polarity": d["provenance_or_polarity"]}
                                   for a, d in sorted(axes.items())}}
    topology_cited = {**topology,
                      "stems": {s: {**topology["stems"][s], "witnesses": jset(stems[s]["witnesses"])} for s in topology["stems"]}}

    def lean_rule(sec, rid, slot):
        r = {"id": rid, **{k: slot["rule"].get(k) for k in CORE[sec]},
             "status": rule_status(slot["witnesses"], golds_by_id)}
        if sec == "bridges":
            r["from_attrs"] = slot["rule"].get("from_attrs")
        return r
    def cited_rule(sec, rid, slot):
        return {**lean_rule(sec, rid, slot), "witnesses": jset(slot["witnesses"]),
                "desc": slot["rule"].get("desc"), "cr": slot["rule"].get("cr"),
                "corroborates": slot["rule"].get("corroborates")}

    interactions = {"$generated": "tools/interaction-rollup", "$golds": sorted(golds_by_id), "conflicts": conflicts,
                    **{sec: [lean_rule(sec, rid, rules[sec][rid]) for rid in sorted(rules[sec])] for sec in SECTIONS}}
    interactions_cited = {"$generated": "tools/interaction-rollup", "$golds": sorted(golds_by_id), "conflicts": conflicts,
                          **{sec: [cited_rule(sec, rid, rules[sec][rid]) for rid in sorted(rules[sec])] for sec in SECTIONS}}

    ok = not errors and not conflicts
    if write and ok:
        os.makedirs(OUT, exist_ok=True)
        for name, obj in [("port-topology.json", topology), ("port-topology.cited.json", topology_cited),
                          ("port-interactions.json", interactions), ("port-interactions.cited.json", interactions_cited)]:
            json.dump(obj, open(os.path.join(OUT, name), "w"), indent=2, ensure_ascii=False)
            open(os.path.join(OUT, name), "a").write("\n")

    # ---- report ----
    nrules = sum(len(rules[s]) for s in SECTIONS)
    prom = {}
    for sec in SECTIONS:
        for rid, slot in rules[sec].items():
            prom[rule_status(slot["witnesses"], golds_by_id)] = prom.get(rule_status(slot["witnesses"], golds_by_id), 0) + 1
    print(f"golds: {len(golds)}  ({', '.join(g['unit'] for _, g in golds if 'unit' in g)})")
    print(f"stems: {len(stems)}   attribute-axes: {len(axes)}   rules: {nrules}  {prom}")
    print(f"edges: {sum(len(g.get('edges',[])) for _,g in golds)}   conflicts: {len(conflicts)}")
    for e in errors:    print("  ERROR   ", e)
    for c in conflicts: print("  CONFLICT", c)
    if ok:
        print("OK — rollup " + ("written to " + os.path.relpath(OUT, ROOT) if write else "validated (--check)"))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(build(write="--check" not in sys.argv))
