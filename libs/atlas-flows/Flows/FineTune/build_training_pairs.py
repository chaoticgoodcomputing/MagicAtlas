"""Build the training corpus for fine-tuning the embedding model.

Three tiers of positive pairs / hard-negative triplets, all merged into one output table:

  Tier 1 (glossary + CR grounding):
      (keyword, glossary_definition)
      (keyword, CR_section_text)   for each cross-referenced CR section

  Tier 2 (reminder-text paraphrase):
      (reminder_text, keyword)              -- model learns reminder phrasing ↔ keyword
      (reminder_text, glossary_definition)  -- model learns reminder phrasing ↔ formal rule

  Tier 3 (hard-negative triplets):
      A handful of seed templates baked in here as `_SEED_TRIPLETS` to surface word-level
      mechanical distinctions (`target` / `another target`, `a` / `another`, `target` / `all`).
      Production triplet mining over oracle-text patterns is left as a future step — for now
      the curated triplets (CuratedTriplets) carry the project-specific signal.

  Curated overrides:
      CuratedDefinitions: adds/overrides glossary entries.
      CuratedTriplets:    emitted as `source="curated_triplet"` rows.
      GlossaryExclusions: filters those names out of tier 1+2 before any pair generation.

Inputs:
    glossary_text: str   — full glossary.txt content (blank-line-delimited blocks).
    rules_text:    str   — full rules.txt content (numbered CR sections).
    card_oracle:   DataFrame[card_id, name, oracle_text]  — oracle text WITH parentheticals.
    curated_defs:  DataFrame[name, definition].
    curated_trips: DataFrame[anchor, positive, negative, rationale?].
    exclusions:    DataFrame[name, reason?].

Output: DataFrame[anchor, positive, negative, weight, source].
"""
from __future__ import annotations

import logging
import re

import pandas as pd
from flowthru import step

logger = logging.getLogger(__name__)


# ─── Parsers ────────────────────────────────────────────────────────────────

# Section IDs in rules.txt look like `100.1` or `702.9a` or `702.9.` — capture the id and the
# trailing body on the same line.
_RULE_HEAD = re.compile(r"^(\d+\.\d+[a-z]?)[\.\s]\s*(.*)$")


def _parse_glossary(text: str) -> list[dict]:
    """Glossary blocks are blank-line-separated; first line is the name, rest are the
    definition. Cross-references look like "See rule 702.9, "Flying."" — we extract them as
    a list of section ids for tier-1 CR joining."""
    entries: list[dict] = []
    for block in text.split("\n\n"):
        block = block.strip()
        if not block:
            continue
        lines = [ln.strip() for ln in block.split("\n") if ln.strip()]
        if len(lines) < 2:
            continue
        name = lines[0]
        if name.lower() == "glossary":
            continue
        definition = " ".join(lines[1:])
        refs = re.findall(r"see\s+rule\s+(\d+\.\d+[a-z]?)", definition, re.IGNORECASE)
        entries.append({"name": name, "definition": definition, "rule_refs": refs})
    return entries


def _parse_rules(text: str) -> dict[str, str]:
    """Returns {section_id: section_body}. Section bodies span multiple lines until the next
    section id."""
    rules: dict[str, str] = {}
    current_id: str | None = None
    current_body: list[str] = []
    for line in text.split("\n"):
        m = _RULE_HEAD.match(line)
        if m:
            if current_id:
                rules[current_id] = " ".join(b for b in current_body if b).strip()
            current_id = m.group(1)
            current_body = [m.group(2).strip()]
        elif current_id:
            current_body.append(line.strip())
    if current_id:
        rules[current_id] = " ".join(b for b in current_body if b).strip()
    return rules


# ─── Tier 2: reminder-text extraction ───────────────────────────────────────

# A reminder text is the inner content of `(...)` in oracle text. The keyword that the
# parenthetical describes is typically the word(s) immediately preceding it. This pattern
# captures both: `KEYWORD (REMINDER)`.
_REMINDER_RE = re.compile(r"\b([A-Z][A-Za-z' \-]*?)\s*\(([^)]+)\)")


def _extract_reminder_pairs(
    oracle_texts: list[str],
    glossary_by_name: dict[str, str],
) -> list[tuple[str, str, str]]:
    """For each oracle text, find (keyword, reminder) pairs and emit (reminder, keyword) and
    (reminder, glossary_def) pairs. Returns a list of (kind, anchor, positive) tuples."""
    pairs: list[tuple[str, str, str]] = []
    seen: set[tuple[str, str]] = set()  # de-dup (reminder, target) pairs across cards
    for text in oracle_texts:
        if not text:
            continue
        for match in _REMINDER_RE.finditer(text):
            keyword_phrase = match.group(1).strip()
            reminder = match.group(2).strip()
            if not keyword_phrase or not reminder:
                continue
            # Use the LAST word(s) of the captured phrase as the keyword — pre-paren text may
            # include the full ability stem like "Whenever you cast a spell, Storm" → keyword
            # is "Storm". Take the trailing 1-3 words.
            tokens = keyword_phrase.split()
            for span in (1, 2, 3):
                if len(tokens) >= span:
                    candidate = " ".join(tokens[-span:]).lower().strip(",.; ")
                    if candidate in glossary_by_name:
                        keyword = candidate
                        break
            else:
                # Fallback: full phrase if no glossary hit
                keyword = keyword_phrase.lower()

            key = (reminder, keyword)
            if key in seen:
                continue
            seen.add(key)
            # (reminder, keyword) pair — anchors reminder phrasing to the bare keyword.
            pairs.append(("reminder_text", reminder, keyword))
            # (reminder, glossary def) pair if we matched a real glossary entry.
            if keyword in glossary_by_name:
                pairs.append(("reminder_text", reminder, glossary_by_name[keyword]))
    return pairs


# ─── Tier 3: seed hard-negative triplets ────────────────────────────────────

_SEED_TRIPLETS: list[dict] = [
    {
        "anchor": "destroy target creature",
        "positive": "exile target creature",
        "negative": "destroy another target creature",
        "rationale": "'another' adds a non-self constraint — combo-relevant",
    },
    {
        "anchor": "sacrifice a creature",
        "positive": "exile a creature",
        "negative": "sacrifice another creature",
        "rationale": "'a' / 'another' constraint flip",
    },
    {
        "anchor": "target creature gets +1/+1",
        "positive": "target creature gets +2/+2",
        "negative": "creatures you control get +1/+1",
        "rationale": "target / all: single-target buff vs board-wide buff",
    },
    {
        "anchor": "deal 3 damage to any target",
        "positive": "deal 2 damage to any target",
        "negative": "deal 3 damage to target creature",
        "rationale": "any target / target creature: 'any target' covers planeswalkers + players",
    },
]


# ─── Main step ──────────────────────────────────────────────────────────────


@step(
    inputs=[
        "GlossaryText",
        "RulesText",
        "CardOracleTexts",
        "CuratedDefinitions",
        "CuratedTriplets",
        "GlossaryExclusions",
    ],
    outputs="TrainingPairs",
)
def build_training_pairs(
    glossary_text: str,
    rules_text: str,
    card_oracle: pd.DataFrame,
    curated_defs: pd.DataFrame,
    curated_triplets: pd.DataFrame,
    exclusions: pd.DataFrame,
) -> pd.DataFrame:
    logger.info(
        "Inputs: glossary=%d bytes, rules=%d bytes, %d cards, "
        "%d curated defs, %d curated triplets, %d exclusions",
        len(glossary_text), len(rules_text),
        len(card_oracle), len(curated_defs),
        len(curated_triplets), len(exclusions),
    )

    glossary_entries = _parse_glossary(glossary_text)
    rules_by_id = _parse_rules(rules_text)
    logger.info(
        "Parsed %d glossary entries and %d CR sections",
        len(glossary_entries), len(rules_by_id),
    )

    excl_names = {row.lower() for row in exclusions["name"].fillna("")}
    glossary_entries = [e for e in glossary_entries if e["name"].lower() not in excl_names]

    # Merge curated defs into the glossary view (curated wins on name collision).
    glossary_by_name: dict[str, str] = {e["name"].lower(): e["definition"] for e in glossary_entries}
    for _, row in curated_defs.iterrows():
        glossary_by_name[str(row["name"]).lower()] = str(row["definition"])

    rows: list[dict] = []

    # ── Tier 1: keyword ↔ glossary, keyword ↔ CR ──
    # For a CR reference like "702.9", the rule body is split across "702.9", "702.9a", "702.9b"
    # — the top-level body is often just the rule title. Concatenate the whole subtree.
    def _expand_rule(ref: str) -> str | None:
        parts = [rules_by_id[k] for k in rules_by_id if k == ref or k.startswith(ref) and k[len(ref):len(ref) + 1] in ("", "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k", "l", "m", "n", "o", "p", "q", "r", "s", "t", "u", "v", "w", "x", "y", "z")]
        body = " ".join(p for p in parts if p).strip()
        return body or None

    for entry in glossary_entries:
        name = entry["name"]
        rows.append({
            "anchor": name,
            "positive": entry["definition"],
            "negative": None,
            "weight": 1.0,
            "source": "glossary",
        })
        for ref in entry["rule_refs"]:
            body = _expand_rule(ref)
            if body and body.lower() != name.lower():
                rows.append({
                    "anchor": name,
                    "positive": body,
                    "negative": None,
                    "weight": 1.0,
                    "source": "glossary_cr",
                })

    # Curated definitions also seed tier-1 rows (in addition to overriding the merge above).
    for _, row in curated_defs.iterrows():
        rows.append({
            "anchor": str(row["name"]),
            "positive": str(row["definition"]),
            "negative": None,
            "weight": 1.0,
            "source": "curated_definition",
        })

    n_tier1 = len(rows)
    logger.info("Tier 1 (glossary + CR + curated_def): %d pairs", n_tier1)

    # ── Tier 2: reminder text ↔ keyword/glossary ──
    oracle_texts = card_oracle["oracle_text"].fillna("").astype(str).tolist()
    reminder_pairs = _extract_reminder_pairs(oracle_texts, glossary_by_name)
    for source, anchor, positive in reminder_pairs:
        rows.append({
            "anchor": anchor,
            "positive": positive,
            "negative": None,
            "weight": 1.0,
            "source": source,
        })
    logger.info("Tier 2 (reminder_text): %d pairs", len(rows) - n_tier1)

    # ── Tier 3: seed triplets + curated triplets ──
    n_before_tier3 = len(rows)
    for trip in _SEED_TRIPLETS:
        rows.append({
            "anchor": trip["anchor"],
            "positive": trip["positive"],
            "negative": trip["negative"],
            "weight": 1.5,  # boost over plain positives
            "source": "template:seed",
        })
    for _, row in curated_triplets.iterrows():
        rows.append({
            "anchor": str(row["anchor"]),
            "positive": str(row["positive"]),
            "negative": str(row["negative"]),
            "weight": 1.5,
            "source": "curated_triplet",
        })
    logger.info("Tier 3 (triplets): %d rows", len(rows) - n_before_tier3)

    df = pd.DataFrame(rows)
    logger.info(
        "Emitted %d total training rows (positives: %d, triplets: %d)",
        len(df),
        int(df["negative"].isna().sum()),
        int(df["negative"].notna().sum()),
    )
    return df
