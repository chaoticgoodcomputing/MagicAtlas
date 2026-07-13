# MAST judge — batch1 remediation RE-JUDGE

**Date:** 2026-07-10
**Batch:** b1-rejudge (remediation of 2026-07-10 batch1 FAILs F01/F11/F12 + 2 back-propagated golds)
**Commit judged:** `ff5dd3fa` fix(mast): remediate batch1 judge FAILs (+ `b83e0345` baseline advance)
**Result:** PASS (3/3)

## Summary

- PASS: 3
- FAIL: 0

All three original FAILs are correctly fixed and no new sibling-mislabel was introduced.
The remediation touched only parser rules + 3 gold fixtures; no AST enum/node changed
(`ControllerFilter.Any/ThatPlayer/EnchantedPlayer` and `ObjectFilter.ExcludedCardTypes`
are all pre-existing), so no new discriminator / PortWalk projection decision is in scope.

## PASS verdicts

- **F01 dont-untap** — PASS. `PhaseTriggerConditionRule` now maps `"each player"` →
  `Event.Whose = ControllerFilter.Any`. `Any` is the faithful value for "at the beginning
  of each player's <phase>": the clock point recurs on EVERY player's phase, not just the
  controller's. Hokori line-2 (`Upkeep`), Nekusar (`Draw`), and Rites of Flourishing (`Draw`)
  all now carry `"Whose":"Any"`, and Hokori's line-2 effect `Target.Filter.Controller:"ThatPlayer"`
  antecedent ("that player untaps a land they control") correctly resolves to whichever
  player's step fired. Consistency sweep: exactly 3 gold fixtures contain
  "beginning of each player" and all 3 carry `"Whose":"Any"` — none left inconsistent.
  The two back-propagated golds (Nekusar, Rites — previously under-specified with no `Whose`)
  are now correct eventual-truth. Corpus blast radius (uniform, correct): 85 upkeep / 23 end-step /
  13 draw-step / 5 first-main-phase cards.
  Cites CR 502.3 (untap step turn-based action; "effects can keep … permanents from untapping")
  and CR 109.5 (player-pronoun / "that player" reference) — both exist in rules-structure.json,
  neither contradicts the modeling.

- **F11 self-cant-block** — PASS. `CantBlockRule`'s self-by-name arm is now gated by
  `IsProperNounSelfReference`, which rejects any subject containing a type/color/state/relational
  word (`_boardWideSubjectWord`) or a bare single-token plural. Corpus verification of every
  standalone "`<X>` can't block." line:
  - **All 8 board-wide restrictions now fall through (not mislabeled):** Bedlam / Tazeem
    ("Creatures", word+plural), Razorjaw Oni ("Black creatures"), Frenetic Raptor ("Beasts",
    caught by the bare-plural guard since it is not in the word list), Siege Elemental
    ("Untapped creatures"), Magistrate's Veto ("White creatures and blue creatures"),
    Bothersome Quasit ("Goaded creatures your opponents control"),
    Spitting Dilophosaurus ("Creatures your opponents control with -1/-1 counters…").
  - **Genuine self-by-name cases still match:** Norin, Skrelv, Homura, Ozox — plus other
    correct self-names (A-Cauldron Familiar, Arco-Flagellant, Everlasting Lich, Feldon,
    Francisco, Gobland, Managorger Phoenix). No over-rejection.
  - The `\b` word-boundary in `_boardWideSubjectWord` correctly does NOT match "land" inside
    "Gobland", so Gobland's genuine self-name survives.
  No new sibling-mislabel. Cites CR 509.1b (block restrictions checked at declare-blockers)
  and CR 201.5 (a card's own name in its text refers to that object) — both exist.

- **F12 controlled-have-keyword** — PASS. `MayCastFromTopOfLibraryRule` now maps a lowercase
  `"non<type>"` → `CardTypes:["card"] + ExcludedCardTypes:["<type>"]` only when `<type>` is a
  real card type (`KnownCardTypes`), and returns null (declines) for an unknown lowercase word.
  The exact `<a> spells and <b> spells from the top of your library` pattern matches only three
  corpus cards:
  - **Madame Web, Clairvoyant** ("Spider spells and noncreature spells") → `noncreature`
    now correctly yields `CardTypes:["card"] + ExcludedCardTypes:["creature"]` (Spider →
    `Subtypes:["Spider"]`). Fixed.
  - **Mystic Forge** ("artifact spells and colorless spells") → UNAFFECTED (fixture confirms
    `CardTypes:["artifact"]` + `IsColorless:true`; the colorless branch precedes the non-branch).
  - **Sigarda, Font of Blessings** ("Angel spells and Human spells") → UNAFFECTED (fixture
    confirms `Subtypes:["Angel"]` / `["Human"]`).
  The new decline-unknown-lowercase branch fires on no current corpus card, so there is no
  regression. Cites CR 205.2a (card-types enumeration) and CR 105.1 (colorless is absence of
  color) — both exist and match.

## Glossary gaps

None.

## Process notes

- Minor citation nuance (not a FAIL): CR 109.5's literal text scopes "you"/"your"; the code
  comment invokes it for the "that player" antecedent. A more precise rule may exist for
  "that player" resolution, but CR 109.5 concerns player-pronoun reference resolution and is
  non-contradictory, so per doctrine it passes.
- F11's `_boardWideSubjectWord` does not list every creature-type plural (e.g. "Beasts");
  those are caught by the separate bare-single-token-plural guard. Multi-word board-wide
  subjects always contain a listed word (creatures/color/state/relational), so coverage holds.
- Pre-existing (out-of-scope) coverage gap surfaced during the sweep: "This token can't block."
  (Rat token) matches neither `_cantBlockPattern` (which lists creature/land/permanent, not
  token) nor the self-by-name arm — it simply falls through. Not a regression from this
  remediation; noted for future triage only.

**ALL PASS**
