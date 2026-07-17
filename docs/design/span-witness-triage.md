# Span-witness outlier triage (2026-07-16)

Motivation (user): make the port `SourceSpan` a **witness** — the exact oracle-text characters a
port claims — so authorship reiterates the wording *and* we can detect false positives by comparing a
port against its own evidence. Prototype: for every parsed (Green/Amber) port with a span, slice the
oracle text and check it contains the anchor word its label asserts (`sac`→"sacrific", `etb`→"enters",
`trigger:damage`→"damage", `emit:token`→"create", …).

## Sweep result over the live corpus (dumps/card-ports.json)

- **262** derived token-affordance ports (span borrowed from the `emit:token` clause) — correctly
  excluded; these want an explicit `derived` provenance flag (separate workstream).
- **289** *misalignment* outliers — **every one a double-faced card**. The stored `OracleText` is EMPTY
  (len 0) for DFC/MDFC cards while the parser composes text from `CardFaces` and computes spans against
  *that*; the spans run past the (empty) served text → **this is the real "spans aren't highlighted"
  on DFCs**. Separate workstream (fix: store/serve the composed face text the spans are relative to).
- **36** *semantic* outliers — the span has text but it lacks the anchor. Triaged below.

## The 36 semantic outliers — ZERO false-positive PORTS

Every one is a genuine MTG ability. The outliers are entirely **span-provenance** issues in four shapes.

### 1. Anchor-vocab gap (6) — port AND span fine; the check's vocabulary was incomplete
Verified against oracle-cards.json:
- `firebending N` → `emit:mana:red` — "Whenever this creature attacks, add {R}." (Iroh, Mai and Zuko, Ozai)
- `modular N` → `ltb:…:to-graveyard` — "when it dies, put its +1/+1 counters on …" (Power Depot, Zabaz)
- `embalm` → `emit:token` — "Exile from graveyard: create a token copy …" (Vizier of Many Faces)

Fix: extend the anchor vocabulary with keyword→effect aliases (a table the future span-witness
diagnostic owns). These are NOT bugs.

### 2. Keyword-cost span (10) — `tap:self` span points at the keyword label, not `{T}`
Abilities written `Keyword — {T}: …` project a real `tap:self`, but its cost span is the keyword
prefix ("Vivi", "Meta", "Impr", "Doma", "Parl", "Exhaust —"), not the `{T}` token. Cards: Bloom Tender
(Vivid), Mox Opal / Vedalken Certarch (Metalcraft), Myr Welder / Dino DNA / Panoptic Mirror (Imprint),
Jodah's Codex / Prismatic Geoscope (Domain), Selvala (Parley), Loot (Exhaust).
Fix: the per-cost span (ActivatedAbilityParser.ParseCosts) must offset past the keyword label prefix.

### 3. Granted-ability span (10) — span points at the GRANT clause, truncated before the inner trigger
An equipment/aura/soulbond grants a creature a triggered ability; the granted port inherits the OUTER
grant clause's span ("Enchant creature / Equipped creature has \"…") which is truncated before the
inner "…deals combat damage…" / "…enters…" / "…dies…". Cards: Biorganic Carapace, Mark of Sakiko, The
Reaver Cleaver, Snake Umbra, Tandem Lookout, Doom Weaver, Thornbite Staff, Lavabelly Sliver,
Necrosynthesis, Nurturing Presence.
Fix: the granted (nested `gainAbility` / "has \"…\"") ability needs its own inner-text SourceSpan from
the parser; the projection's grant-inheritance (PortGraph.cs grantPortsStart) should stamp THAT, not
the outer clause. (Same shape as the Deadeye grant fix, but the inner span isn't parsed today.)

### 4. Modal-ability span (10) — span lands on the "choose a mode" preamble or the WRONG mode
"As this enchantment enters, choose X or Y. • X — … • Y — …" — a mode's port gets the preamble span or
the other mode's span. Genuinely wrong mode: Outpost Siege (`emit:damage` from Dragons → points at
Khans), Palace Siege (`emit:life` from Dragons → Khans), Windcrag Siege (`emit:token` from Jeskai →
Mardu). Also Battle of Hoover Dam, Frostcliff Siege, Mirrodin Besieged (×2), Phenomenon Investigators (×2).
Fix: the parser must attribute each mode's abilities to that mode's clause span; the projection must
carry per-mode provenance through the modal recursion.

## Convergence: one parser capability

Classes 2–4 here **plus** the Class/Talent per-level-span gap (found by the derived-port guard) are all
the same missing capability: **per-inner-clause span provenance**. The parser emits a span for the
top-level ability but not for nested/granted/modal/leveled sub-clauses, so their ports fall back to the
container's (or line 0's) span. This is a parser slice (mast-tdd-loop Parse track), scoped precisely by
the cases above. Until it lands, these are span-imprecision (a chip highlights a coarse/adjacent clause)
— never a wrong PORT, so the interaction graph is unaffected.

## Follow-ups
- The span-witness anchor vocabulary + this triage seed a Flowthru `_08_Reporting` **span-witness
  diagnostic** (census of outliers by class) — the repeatable false-positive detector.
- `derived` provenance flag (excludes the 262 affordance ports cleanly + distinct frontend render).
- DFC composed-text fix (the 289 misalignments / "no highlights on DFCs").
- The **per-inner-clause span parser slice** (classes 2–4 + Class/Talent).
