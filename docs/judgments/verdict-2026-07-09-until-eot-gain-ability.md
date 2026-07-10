# MAST judge — batch verdict

**Date:** 2026-07-09
**Scope:** 2 files + 1 projection decision (1 fixture, 1 spell rule, 1 initiative-03 item)
**Branch:** mast/until-eot-target-creature-gains-ability
**Result:** FAIL

## Summary

- PASS: 1
- FAIL: 2

## FAIL verdicts

### tests/magic-ast-tests/Fixtures/HandParsedCards/DKA/AbnormalEndurance.json
**Verdict:** FAIL
**Issue:** Input.OracleText is not byte-identical to the real card — curly typographic quotes/apostrophe instead of the source's ASCII straight ones.
**Rule citation:** N/A (byte-identity check, not a rules check)
**What the fixture says:** `... and gains “When this creature dies, return it to the battlefield tapped under its owner’s control.”` (U+201C / U+201D / U+2019)
**Real card (oracle-cards.json, the pipeline source of truth):** `... and gains "When this creature dies, return it to the battlefield tapped under its owner's control."` (0x22 / 0x27)
**Why this fails:** The fixture's Input.OracleText must be a byte-exact copy of the card text the parser consumes. It differs (sha256 mismatch). This is the ONLY HandParsed fixture using curly quotes; every sibling granted-ability fixture uses straight quotes matching Scryfall. The rule's regex was written to accept both curly and straight quotes (`[“""]`, `['’]`) — a tell that the gold was hand-typed rather than copied byte-exact.
**Suggested fix:** Replace the curly quotes/apostrophe in Input.OracleText with the ASCII straight `"` (0x22) and `'` (0x27) copied verbatim from oracle-cards.json. The gold AST body is otherwise correct and needs no change.

Note: the gold AST decomposition itself is faithful — two sibling continuous effects (`modifyPT` +2/+0 on target creature; `gainAbility` granting a `triggered{When, Dies, IsSelf}` -> `returnToBattlefield{Self, Tapped, UnderControl:Owner}`), both bounded by `untilTime Turn/End`. No unparsed, no unstructured effect, no lossy drop/merge.

### libs/magic-ast/Parsing/Parsers/Spell/Rules/ModifyPTAndGainDiesReturnAbilitySpellRule.cs
**Verdict:** FAIL
**Issue:** Two doc-comment CR citations are mischaracterized (wrong rule, contradictory text).
**Rule citation:** cited CR 603.6d (actual: CR 603.10a); cited CR 113.6 (actual: CR 113.10 / 113.12)
**Rule text:**
> CR 603.6d: "Some permanents have text that reads '[This permanent] enters with . . . ,' ... Such text is a static ability-not a triggered ability-whose effect occurs as part of the event that puts the permanent onto the battlefield."
> CR 603.10a: "Some zone-change triggers look back in time. These are leaves-the-battlefield abilities, abilities that trigger when a player sacrifices a permanent, ..."
> CR 113.6: "Abilities of an instant or sorcery spell usually function only while that object is on the stack. Abilities of all other objects usually function only while that object is on the battlefield. ..."
> CR 113.10: "Effects can add or remove abilities of objects. An effect that adds an ability will state that the object 'gains' or 'has' that ability, ..."
**What the doc-comment says:** "CR 113.6 — an ability granted by an effect is still a full-fledged ability of the gaining permanent ... CR 603.6d — a dies trigger looks back in time to the permanent's last existence on the battlefield."
**Why this misrepresents the rules:** CR 603.6d is about "[this permanent] enters with…" STATIC abilities (explicitly "not a triggered ability") — it does not describe a dies trigger looking back in time; that is CR 603.10/603.10a. CR 113.6 is about the zone in which abilities function, not about granted abilities being full-fledged; that is CR 113.10/113.12. The cited rules exist but their text contradicts / does not match the modeled dies-triggered granted ability.
**Suggested fix:** Change the "looks back in time" citation from CR 603.6d to CR 603.10a, and the "granted ability is a full-fledged ability" citation from CR 113.6 to CR 113.10 (or 113.12). CR 611 (Continuous Effects) and CR 603 (Handling Triggered Abilities, parent) are correct and can stay.

## PASS verdicts

- `mast/until-eot-target-creature-gains-ability#projection` — PASS. No new discriminator (newAstNode=false, shared=[]); reuses existing `modifyPT`/`gainAbility`/`Dies`/`returnToBattlefield` nodes. The non-blink self-return already carries a sensible coarse `PortWalkProjection` entry (Persist/Undying-style self-return -> `emit:returntobattlefield`), which correctly covers this Persist-like grant. No new projection decision required.

## Glossary gaps

- None. (Terms used — dies, tapped, owner's control, until end of turn, continuous effect — are all covered by CR/glossary.)

## Process notes

- Shared edits: none. The diff touches exactly two files (the new rule + the new fixture), so check (4) "shared edits are sound generalizations" passes vacuously.
- The rule's parser logic (regex + emitted effect list) is sound and matches the gold; parser correctness is NUnit's job and is out of judge scope. Both FAILs are cleanly fixable in place (fixup the fixture's quote bytes; correct two citation numbers) without any structural change.

HALT: mast/until-eot-target-creature-gains-ability
