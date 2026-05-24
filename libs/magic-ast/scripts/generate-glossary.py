#!/usr/bin/env python3
"""
Generate libs/magic-ast/GLOSSARY.md by scanning the MagicAST source for type
declarations and their XML doc summaries.

Scope:
  - Every .cs file under libs/magic-ast/AST/
  - libs/magic-ast/CardOutputAST.cs and libs/magic-ast/CardInputDTO.cs (root-level AST types)
  - libs/magic-ast/Parsing/Tokens/Keywords/**/*.cs (structural-keyword registry)
  - libs/magic-ast/Parsing/Parsers/**/*.cs (ability parsers)
  - libs/magic-ast/Parsing/AbilityParserRegistry.cs and related infra
  - libs/magic-ast/Serialization/**/*.cs (polymorphic infrastructure)

Output:
  libs/magic-ast/GLOSSARY.md, grouped by directory, sorted by type within each
  group. Each entry shows: type kind (abstract record / sealed record / interface /
  enum / class), summary doc, any registered discriminator (from [Oracle*] /
  [CardAttributeKind] / [PowerToughnessKind] / [StructuralKeyword]), and a
  source file link.

This is plain regex/text parsing — no Roslyn. Re-run after editing AST types;
CI can compare the committed GLOSSARY.md against `python3 scripts/generate-glossary.py --check`.
"""

from __future__ import annotations

import argparse
import os
import re
import sys
from dataclasses import dataclass, field
from datetime import datetime, timezone
from typing import Iterable

LIB_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
OUTPUT_FILE = os.path.join(LIB_ROOT, "GLOSSARY.md")

# Subtrees to scan (relative to LIB_ROOT).
SCAN_TREES = [
    "AST",
    "Diagnostics",
    "Keywords",
    "Parsing/Combinators",
    "Parsing/Parsers",
    "Parsing/Tokens",
    "Serialization",
]

# Single root-level files to include.
SCAN_FILES = [
    "CardInputDTO.cs",
    "CardOutputAST.cs",
    "MagicASTJsonOptions.cs",
    "ParseResult.cs",
    "Parsing/AbilityClassifier.cs",
    "Parsing/AbilityParserRegistry.cs",
    "Parsing/AttributeExtractor.cs",
    "Parsing/CardParser.cs",
    "Parsing/ClauseSplitter.cs",
    "Parsing/IAbilityParser.cs",
    "Parsing/ManaCostParser.cs",
    "Parsing/OracleAbilityParserAttribute.cs",
    "Parsing/OracleParser.cs",
    "Parsing/TypeLineParser.cs",
]

# Map: attribute name -> (label shown in glossary, whether it carries a single-string arg).
DISCRIM_ATTRS = {
    "OracleAbility": "Ability discriminator",
    "OracleEffect": "Effect discriminator",
    "OracleDuration": "Duration discriminator",
    "OracleCost": "Cost discriminator",
    "OracleQuantity": "Quantity discriminator",
    "OracleReplacementEvent": "ReplacementEvent discriminator",
    "CardAttributeKind": "CardAttribute discriminator",
    "PowerToughnessKind": "PowerToughnessValue discriminator",
    "StructuralKeyword": "Structural keyword",
    "PolymorphicBase": "Polymorphic base, discriminator property",
}

# Attributes whose discriminator is a kind enum value, not a string literal.
ENUM_DISCRIM_ATTRS = {
    "OracleAbilityParser": "Ability-parser registration, kind",
}


@dataclass
class TypeEntry:
    name: str
    kind: str  # e.g. "abstract record", "sealed record", "interface", "enum", "static class"
    base_type: str | None
    summary: str
    discriminators: list[tuple[str, str]] = field(default_factory=list)  # (label, value)
    file_path: str = ""  # relative to LIB_ROOT


# ----- Tokenizer-ish line iteration ---------------------------------------------------

def read_text(path: str) -> str:
    with open(path, "r") as f:
        return f.read()


def clean_summary(buffer: list[str]) -> str:
    """Take a list of /// summary lines and produce a single trimmed string,
    normalising XML doc tags into plain Markdown-ish text."""
    text = "\n".join(buffer)
    # Strip the leading `/// ` and the <summary> tags.
    text = re.sub(r'^\s*///\s?', '', text, flags=re.MULTILINE)
    text = re.sub(r'</?summary>', '', text)
    # <see cref="X"/> and <see cref="X.Y"/> -> `X` / `X.Y`
    text = re.sub(r'<see\s+cref="([^"]+)"\s*/>', lambda m: f"`{_simplify_cref(m.group(1))}`", text)
    # <paramref name="x"/> -> `x`
    text = re.sub(r'<paramref\s+name="([^"]+)"\s*/>', r'`\1`', text)
    # <typeparamref name="X"/> -> `X`
    text = re.sub(r'<typeparamref\s+name="([^"]+)"\s*/>', r'`\1`', text)
    # <c>x</c> -> `x`
    text = re.sub(r'<c>(.*?)</c>', r'`\1`', text)
    # <para> tags just become paragraph breaks; strip the tag.
    text = re.sub(r'</?para>', '\n\n', text)
    # <list>, <item>, <description>, <term> — rarely used and rendering inline is fine.
    text = re.sub(r'</?(?:list|item|description|term|remarks)[^>]*>', '', text)
    # Collapse runs of whitespace per paragraph.
    paragraphs = [re.sub(r'\s+', ' ', p).strip() for p in text.split("\n\n")]
    return "\n\n".join(p for p in paragraphs if p).strip()


def _simplify_cref(ref: str) -> str:
    """`MagicAST.AST.Effects.Effect` → `Effect`; preserve generics."""
    # Drop everything before the last "."
    last = ref.split(".")[-1]
    return last


def parse_file(path: str, rel_path: str) -> list[TypeEntry]:
    """Walk a single .cs file and emit TypeEntry records for every top-level type."""
    content = read_text(path)
    lines = content.split("\n")

    entries: list[TypeEntry] = []
    pending_summary: list[str] = []
    in_summary = False
    pending_attrs: list[str] = []

    # Track brace depth so we only emit *top-level* (namespace-level) types.
    # Records can have nested types, but in this codebase they don't.
    brace_depth = 0

    i = 0
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        # Brace tracking — count braces on this line BEFORE we decide what to do.
        # We need this to know whether the next type declaration is top-level.
        # Process declarations FIRST (before adjusting depth for this line), since
        # a class declaration on line N is considered at depth N's *start*.

        # XML doc summary collection
        if stripped.startswith("/// <summary>"):
            pending_summary = [stripped]
            # Handle single-line `/// <summary>foo</summary>` — close immediately.
            in_summary = "</summary>" not in stripped
            i += 1
            continue
        if in_summary:
            pending_summary.append(stripped)
            if "</summary>" in stripped:
                in_summary = False
            i += 1
            continue

        # Attribute collection (only top-level / before declaration)
        if brace_depth == 0 and re.match(r'\s*\[', line) and stripped.endswith("]"):
            pending_attrs.append(stripped)
            i += 1
            continue

        # Type declaration?
        if brace_depth == 0:
            decl = match_type_declaration(line)
            if decl is not None:
                name, kind, base = decl
                summary = clean_summary(pending_summary)
                entry = TypeEntry(
                    name=name,
                    kind=kind,
                    base_type=base,
                    summary=summary,
                    file_path=rel_path,
                )
                entry.discriminators = extract_discriminators(pending_attrs)
                entries.append(entry)
                # consume one full record/class body to advance, but let depth
                # tracking handle nested braces naturally.
                pending_summary = []
                pending_attrs = []
                # fall through to depth tracking below

        # Skip XML doc lines that aren't <summary> (param/remarks/returns)
        if stripped.startswith("///"):
            i += 1
            continue

        # Reset pending summary/attrs if the line is a non-summary, non-attribute,
        # non-declaration thing that isn't whitespace — e.g., a property or method.
        # Use a heuristic: if we have summary or attrs queued and we hit a line that
        # doesn't introduce a type, clear them (they belonged to a member, not a type).
        if pending_summary and not stripped.startswith("[") and not stripped.startswith("///"):
            if brace_depth == 0 and not is_type_decl_line(line):
                # Only matters if we're not inside braces (member-level decls happen inside)
                pass  # leave pending_summary alone for now; will be reset on next type
            else:
                pending_summary = []
                pending_attrs = []

        # Brace tracking
        # Subtract content of string literals and comments to avoid miscounting braces.
        # For our codebase the simple count is safe enough.
        stripped_no_str = strip_string_literals_and_comments(line)
        brace_depth += stripped_no_str.count("{") - stripped_no_str.count("}")
        i += 1

    return entries


def strip_string_literals_and_comments(line: str) -> str:
    # Strip "..." literals
    line = re.sub(r'"(?:\\.|[^"\\])*"', '""', line)
    # Strip @"..." raw-ish literals (best-effort)
    line = re.sub(r'@"[^"]*"', '""', line)
    # Strip // comments
    line = re.sub(r'//.*$', '', line)
    return line


def is_type_decl_line(line: str) -> bool:
    return match_type_declaration(line) is not None


def match_type_declaration(line: str) -> tuple[str, str, str | None] | None:
    """
    Returns (type_name, kind_descriptor, base_type) or None if line is not a top-level
    type declaration.
    Examples of kind_descriptor: "abstract record", "sealed record", "interface", "enum", "sealed class".
    """
    # Skip lines that look like primary-constructor argument lists by requiring the
    # `record|class|interface|enum` keyword to appear after the access modifier(s).
    m = re.match(
        r'\s*'
        r'(?:public|internal|private|protected)?\s+'
        r'(?:(abstract|sealed|partial|static)\s+)?'
        r'(?:(abstract|sealed|partial|static)\s+)?'
        r'(record|class|interface|enum)\s+'
        r'(\w+)\b'
        r'(?:\s*\([^)]*\))?'           # optional primary constructor
        r'(?:\s*:\s*([\w<>?,\s\.]+?))?'  # optional base / interfaces
        r'\s*(?:\{|;|$)',
        line,
    )
    if m is None:
        return None
    mods = [g for g in (m.group(1), m.group(2)) if g]
    kw = m.group(3)
    name = m.group(4)
    base = m.group(5).strip() if m.group(5) else None
    kind = (" ".join(mods + [kw])).strip() if mods else kw
    return name, kind, base


def extract_discriminators(attrs: Iterable[str]) -> list[tuple[str, str]]:
    """Pull (label, value) tuples for any recognised discriminator/polymorphism attributes."""
    out: list[tuple[str, str]] = []
    for raw in attrs:
        # Strip outer [ ]
        inner = raw.strip()
        if inner.startswith("[") and inner.endswith("]"):
            inner = inner[1:-1].strip()

        # Match the attribute name and first string arg
        m = re.match(r'(\w+)\s*\(\s*"([^"]+)"\s*[,)]', inner)
        if m:
            attr_name = m.group(1)
            value = m.group(2)
            if attr_name in DISCRIM_ATTRS:
                out.append((DISCRIM_ATTRS[attr_name], value))
            continue

        # Match the enum-arg attributes like [OracleAbilityParser(AbilityKind.Triggered)]
        m = re.match(r'(\w+)\s*\(\s*([\w\.]+)\s*\)', inner)
        if m:
            attr_name = m.group(1)
            value = m.group(2)
            if attr_name in ENUM_DISCRIM_ATTRS:
                out.append((ENUM_DISCRIM_ATTRS[attr_name], value))
            continue
    return out


# ----- Walking the source tree --------------------------------------------------------

def iter_source_files() -> list[str]:
    """Return all .cs file paths (absolute) to scan, deduplicated."""
    seen: set[str] = set()
    files: list[str] = []

    for tree in SCAN_TREES:
        abs_root = os.path.join(LIB_ROOT, tree)
        if not os.path.isdir(abs_root):
            continue
        for dirpath, _, filenames in os.walk(abs_root):
            for fn in filenames:
                if not fn.endswith(".cs"):
                    continue
                p = os.path.join(dirpath, fn)
                if p not in seen:
                    seen.add(p)
                    files.append(p)

    for relf in SCAN_FILES:
        p = os.path.join(LIB_ROOT, relf)
        if os.path.isfile(p) and p not in seen:
            seen.add(p)
            files.append(p)

    return files


# ----- Markdown rendering -------------------------------------------------------------

def group_by_directory(entries: list[TypeEntry]) -> dict[str, list[TypeEntry]]:
    groups: dict[str, list[TypeEntry]] = {}
    for e in entries:
        d = os.path.dirname(e.file_path) or "."
        groups.setdefault(d, []).append(e)
    return groups


def render(entries: list[TypeEntry]) -> str:
    out: list[str] = []
    out.append("# MagicAST Node Glossary")
    out.append("")
    out.append(
        "_Auto-generated by `scripts/generate-glossary.py`. "
        "Do not edit by hand. Re-run the script after adding or modifying any AST type._"
    )
    out.append("")
    out.append(f"_Last generated: {datetime.now(timezone.utc).strftime('%Y-%m-%d %H:%M UTC')}_")
    out.append("")

    groups = group_by_directory(entries)

    # Display order: sort groups by depth then alphabetically, but pin `.` (root) at top.
    def group_sort_key(d: str) -> tuple[int, str]:
        return (0 if d == "." else 1, d)

    for directory in sorted(groups.keys(), key=group_sort_key):
        header = "/" if directory == "." else f"/{directory}/"
        out.append(f"## `{header}`")
        out.append("")

        # Sort entries within group: bases first (abstract / interface), then sealed/static, then enums.
        def entry_sort_key(e: TypeEntry) -> tuple[int, str]:
            order = 0
            if "abstract" in e.kind:
                order = 0
            elif e.kind == "interface":
                order = 1
            elif "sealed" in e.kind or "static" in e.kind or e.kind == "class":
                order = 2
            elif e.kind == "enum":
                order = 3
            else:
                order = 4
            return (order, e.name)

        for entry in sorted(groups[directory], key=entry_sort_key):
            render_entry(entry, out)

    return "\n".join(out).rstrip() + "\n"


def render_entry(e: TypeEntry, out: list[str]) -> None:
    base = f" : {e.base_type}" if e.base_type else ""
    out.append(f"### `{e.name}` — *{e.kind}*{base}")
    out.append("")
    if e.summary:
        out.append(e.summary)
        out.append("")
    if e.discriminators:
        for label, value in e.discriminators:
            out.append(f"- **{label}:** `{value}`")
        out.append("")
    out.append(f"[Source]({e.file_path})")
    out.append("")


# ----- CLI ----------------------------------------------------------------------------

def main() -> int:
    parser = argparse.ArgumentParser(description="Generate MagicAST node glossary.")
    parser.add_argument(
        "--check",
        action="store_true",
        help="Don't write — exit 1 if GLOSSARY.md would change.",
    )
    args = parser.parse_args()

    paths = iter_source_files()
    all_entries: list[TypeEntry] = []
    for p in paths:
        rel = os.path.relpath(p, LIB_ROOT)
        all_entries.extend(parse_file(p, rel))

    markdown = render(all_entries)

    if args.check:
        existing = ""
        if os.path.isfile(OUTPUT_FILE):
            existing = read_text(OUTPUT_FILE)
        # Compare ignoring the "Last generated" line so timestamp churn doesn't fail CI.
        def strip_timestamp(s: str) -> str:
            return re.sub(r'_Last generated:.*?_\n', '', s, count=1)
        if strip_timestamp(existing) != strip_timestamp(markdown):
            sys.stderr.write(
                "GLOSSARY.md is stale. Re-run scripts/generate-glossary.py.\n"
            )
            return 1
        return 0

    with open(OUTPUT_FILE, "w") as f:
        f.write(markdown)
    print(f"Wrote {OUTPUT_FILE} ({len(all_entries)} type entries)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
