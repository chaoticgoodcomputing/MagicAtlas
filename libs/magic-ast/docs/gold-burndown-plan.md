<!-- Generated 2026-06-16 by the gold-burndown-spec workflow (10 family specs + synthesis). Claims (corrupt-Input golds, regen-only buckets, entanglements) are LLM analysis — verify each against Scryfall/source when its slice lands. -->

# MagicAST Gold Burndown Plan

Leverage-first, conflict-aware roadmap for retiring gold unparsed nodes and free-text destring sinks across 10 family specs.

## Starting ratchets

| Ratchet | Start |
|---|---|
| **KnownUnparsedGold** (cards with `IUnparsed` nodes) | **14** |
| **Free-text destring sinks** (instances) | **171** |
| └ OtherCharacteristic | 94 |
| └ OtherCondition | 55 |
| └ AbilityText | 18 |
| └ Instructions | 3 |
| └ OtherHistoryPredicate | 1 |

The two ratchets are independent. `OtherCharacteristic` and `OtherCondition` (the deal-where-the-leverage-is) account for 149 of 171 free-text instances. All four `unparsed-*` families plus `self-reference`/`aura`/`comparative-pt`/`structured-characteristic` lower these.

---

## Sequencing principle

Three forces drive the order:

1. **Shared parser code = serialize, never concurrent.** `SpellRuleHelpers`, `StaticRuleHelpers`, `TriggeredRuleHelpers`, `LordPTBuffRule`, `BareKeywordGrantRule`, `AbilityClassifier.cs`, `ClauseSplitter.cs`, `ConditionParser.cs`, and `ObjectFilter.cs` are each touched by multiple families. The second family to land on a file rebases against the first's output and **must re-run regen**.
2. **One shared baseline number per sink.** Every family that lowers `OtherCharacteristic` (or `OtherCondition`) decrements the *same* key in `destring-sink-baseline.json`. Recompute from the live count, never blind-subtract.
3. **Leverage = instances ÷ edit sites.** Land the single-edit / few-file families before the multi-rule ones; land mechanical (low/medium risk) before entangled (high risk).

**Two slices require human co-design before landing** (see "Entangled slices" below) — they are split out and gated.

---

## Slice order

### Phase 0 — Pure regen, zero code (warm-up + ratchet hygiene)

| Slice | Family (subset) | Mechanism | Files |
|---|---|---|---|
| **0** | OtherCondition **Bucket A** (7 golds) + the regen-only members of `unparsed-spell-loselife` (CandyTrail) and `unparsed-legacy` (AspectOfMongoose) | Stale/corrupt golds; current parser already covers them | none — `GoldRegenerationUtility` only |

Bucket A golds (PutridGoblin, StranglerootGeist, UndyingEvil, ButcherGhoul, YoungWolf, GravelgillAxeshark, SafeholdElite) predate `ConditionParser.TriggeringObjectCounter` (commit `d0c31ffc`); regen alone retires 7 `OtherCondition`. CandyTrail/AspectOfMongoose are corrupt-Input re-points (covered in their family slices — do those re-points here to de-risk the later slices). Land first because it requires no code review and proves the regen harness.

> **Ratchet after Slice 0:** unparsed **14**, free-text **171 → 164** (OtherCondition 55 → 48). *(CandyTrail/AspectOfMongoose unparsed clearances are deferred to their family slices so the KnownUnparsedGold list edit lands with the parser change that guarantees them.)*

---

### Phase 1 — Mechanical free-text slices (low/medium risk, surgical)

Ordered smallest-blast-radius first so the heavy `OtherCharacteristic` families rebase against an already-shrinking count.

#### Slice 1 — `self-reference "this card"` (4 instances)
- **Edit:** 3 keyword-def combinators only — `EmbalmKeyword.cs`, `EternalizeKeyword.cs`, `ScavengeKeyword.cs`. Swap hardcoded `OtherCharacteristic{"this card"}` for `ObjectFilter.IsSelf=true`. No parser-rule or AST change (`IsSelf` ships, 195 golds use it).
- **Why early:** zero shared-helper contact, lowest risk, isolated files. `IsSelf` is the dual of `ExcludeSelf` (Slice 6) — landing it first establishes the self/non-self semantics judge guidance.
- **Risk:** low.

> unparsed **14**, free-text **164 → 160** (OtherCharacteristic 94 → 90).

#### Slice 2 — `aura enchanted-subject` (3 instances)
- **Edit:** ONE branch in `TriggeredRuleHelpers.ParseObjectFilter` + new flat `ObjectFilter.IsEnchanted` bool. `Other("enchanted")` → `IsEnchanted=true`.
- **Why before structured-characteristic:** it is the smallest edit to `TriggeredRuleHelpers`/`ParseObjectFilter`, and `structured-characteristic` (Slice 5) also touches that method. Land aura first so Slice 5 rebases against the shrunk count.
- **Do NOT touch** `TSP/AspectOfMongoose` — the worklist mis-attributes it here; its unparsed node is unrelated (handled Slice 9).
- **Risk:** low.

> unparsed **14**, free-text **160 → 157** (OtherCharacteristic 90 → 87).

#### Slice 3 — `comparative P/T filter` (4 instances)
- **Edit:** extend the `Comparison` record (`ObjectFilter.cs`) so RHS can be `RelativeTo: ObjectReference.Self()` (make `Value` nullable, add `RelativeTo` + optional `RelativeCharacteristic`). Two producers: `CantBeBlockedRule.cs` (L155-174) and `MentorKeyword.cs` (L68). Update `ast-schema.json` Comparison def.
- **Why here:** medium risk but self-contained; the `Comparison.Value` nullability change touches a shared record (~12 literal-int consumers) — verify all serialize byte-identically (`RelativeTo` absent). Lands before `structured-characteristic` because GRN Mentor golds and AggressiveMammoth carry sibling `attacking`/`other` nodes those families also edit; pinning the `PowerComparison` first means Slice 5/6 only touch the residual they own.
- **Co-design note:** the prompt lists a sibling `comparative-pt` concern (toughness variant). The `Comparison.RelativeTo` extension is the shared substrate — **if that sibling exists it MUST land in this slice or immediately after**, to avoid two divergent relational-comparison designs. Confirm scope before starting.
- **Risk:** medium.

> unparsed **14**, free-text **157 → 153** (OtherCharacteristic 87 → 83).

#### Slice 4 — `unparsed-spell-loselife` (2 instances)
- **Edit:** `WhirlerRogue` only needs code — `AbilityClassifier.cs` (treat leading `Tap` as a non-mana cost verb when a colon follows) + new `TapPermanentsCostRule.cs` + a `Target creature can't be blocked` activated effect arm. `CandyTrail` already re-pointed in Slice 0.
- **Why here:** `AbilityClassifier.cs` is the most-edited shared file (Slices 4, 7, 8 all touch it). Land this **first** of the classifier-touchers — it is the narrowest classifier change (one cost-verb guard) and gives a clean baseline.
- **Ratchet:** remove `ORI/WhirlerRogue` **and** `WOE/CandyTrail` from `KnownUnparsedGold` in the same change.
- **Risk:** medium.

> unparsed **14 → 12**, free-text **153** (no sink change; both nodes were `IUnparsed`, not destring).

---

### Phase 2 — Heavy free-text slice (medium risk, biggest single retire, schema-additive)

#### Slice 5 — `structured-characteristic` (44 instances) ⚠ biggest lever
- **Edit:** the shared qualifier→axis mapping is duplicated across `SpellRuleHelpers`, `StaticRuleHelpers`, `TriggeredRuleHelpers`, `ActivatedRuleHelpers`. **Extract ONE shared helper first**, then route call sites. New axes on `ObjectFilter.cs`: `ExcludedColors`; new `Characteristic` variants `TappedStateCharacteristic` and `CounterCharacteristic`; reuse existing `ExcludedCardTypes`/`ExcludedSupertypes`/`CardTypes`/`IsToken`/`Colors`/`CombatStateCharacteristic`.
- **Why after Slices 1-4:** it edits every helper the earlier surgical slices touched; landing those first means it rebases once against a known state. It is the single highest-instance family — doing the helper extraction here makes 44 instances collapse in one fix instead of ~40 scattered touches.
- **Schema entanglement (review gate):** adds discriminator kinds `tapped`, `counter` to `PolymorphicReflectionConverter`; every exhaustive `switch` over `CharacteristicKind` in `libs/mast-interaction` must learn them or silently drop. These are filter predicates (CR 110.5/122) — **not** engine actions, so no firability change, but confirm consumers don't try to evaluate the new typed node. Regenerate any `SchemaExportTests` snapshot.
- **Cross-slice dedup:** AdeptWatershaper, SarythTheVipersFang, GoblinWardriver, Niambi, LivingTotem, MerryEsquire carry BOTH a `tapped`/`attacking` instance (this slice) AND an `other`/`another` instance (Slice 6). **Land Slice 5 before Slice 6**, then Slice 6 rebases and re-regenerates those shared golds. GRN Mentor + AggressiveMammoth carry `attacking` (this slice) + `with power less than` (Slice 3, already landed) — keep the `PowerComparison` untouched.
- **Risk:** medium.

> unparsed **12**, free-text **153 → 109** (OtherCharacteristic 83 → 39).

---

### Phase 3 — ⚠ ENTANGLED: needs human co-design BEFORE landing

These two slices couple the parser change to an **engine/interaction-firability** decision. Landing the parser fix alone is a *silent regression* the green test suite won't catch. **Do not hand these to a mechanical sub-agent.**

#### Slice 6 — `another-ExcludeSelf` (23 instances) ⚠ CO-DESIGN: parser + interaction carve-out
- **Parser edit:** central — `StaticRuleHelpers.ClassifyTypeNounPhrase` (L580: `Other("other")` → `ExcludeSelf=true`). Anthem/gainAbility path — stop threading `"other"` into the `characteristics` string list in `LordPTBuffRule.cs` and `BareKeywordGrantRule.cs`; carry `isOther` separately and append `ExcludeSelf=true` at each returned `ObjectFilter`. Dedicated rules — `TribalAnthemModifyPTRule.cs:51`, `WithKeywordAnthemModifyPTRule.cs:61` (keep the kw Characteristic). Trigger/target path (Barrin, Merry, Niambi, LivingTotem) — factor ONE shared `TryPeelSelfExclusion` detector, hoisted next to `SpellAbilityParser.cs:265`.
- **⚠ THE ENTANGLEMENT (must land in the SAME batch):** once these filters emit structured `ExcludeSelf=true`, `ObjectFilterRelations.SupUndecidedAxis` (`ObjectFilterRelations.cs:833`) floors `Subsumes()` to `Unknown` whenever `sup.ExcludeSelf==true`. `PortGraphEngine.AddRulesEdge` (`PortGraphEngine.cs:696`) stamps that on every interaction edge → canonical combos routing through an anthem/bounce/counter filter **demote GREEN→AMBER**. The fix is a **cross-card firability carve-out**: when `from.Card != to.Card` (structural twin of `BlinkSatisfiesEnter`'s same-card guard at `PortGraphEngine.cs:563`), promote the `ExcludeSelf` `Subsumes` Unknown → Yes. **Human decision required:** confirm the carve-out boundary (cross-card only; `Intersects`/`UndecidedAxis` already ignores `ExcludeSelf` and must stay untouched). Spec parser + carve-out together; run `mast-interaction` tier tests (canonical-combo asserts) before AND after.
- **Ordering:** AFTER Slice 5 (shares the exact `Characteristics`-emission sites; land ExcludeSelf narrowly so structured-characteristic already converted the co-occurring residuals) and AFTER Slice 1 (IsSelf/ExcludeSelf semantics paired). All 22 golds are FIX (no deletes).
- **Risk:** high.

> unparsed **12**, free-text **109 → 86** (OtherCharacteristic 39 → 16).

#### Slice 7 — `OtherCondition tail` structured buckets (22-23 instances) ⚠ CO-DESIGN: reference-not-resolution semantics
- **Parser edit:** SINGLE central change point — `ConditionParser.cs`. Add arms before the `OtherCondition` fallback: `KeywordStateCondition{Keyword}` (Saddle/Ascend/Echo/Soulbond — 11 golds, Buckets C/D/E/I), `DuringPhaseCondition{Phase}` (4 golds, Bucket G), optional `SelfCounterCondition` / widen `TriggeringObjectCounter` to accept present-tense `has` (1 gold, Bucket B). Bucket A (7) already done in Slice 0.
- **⚠ THE ENTANGLEMENT (engine co-design):** `KeywordStateCondition` is **reference-not-resolution** (ADR 0004) — the engine must evaluate "is this permanent saddled / do you have city's blessing / did this come under control since last upkeep / is this paired" against live game state via the keyword's linked-ability semantics, NOT a pre-baked bool (mirrors `KeywordCostPaidCondition`). `DuringPhaseCondition` entangles with CR 500 turn-structure evaluation. New `ConditionKind` discriminators (`keywordState`, `duringPhase`) must round-trip `PolymorphicReflectionConverter`. **Human decision:** sign off the reference-not-resolution contract and the `GamePhase` enum surface before landing.
- **Ordering:** AFTER `unparsed-triggered` (Slice 8) — both touch `TriggeredAbilityParser.cs` intervening-if extraction (Buckets A/E); if Slice 8 restructures L130-188 it stomps this regen baseline. `ConditionParser.cs` itself is leaf/additive (no collision), but the *golds* must regen after both land. Coordinate with `comparative-pt` (Slice 3, already landed) on `AsLongAsStaticGrantRule.cs`.
- **Leave as residual this batch:** Buckets H (tap-state, 7), L (reveal, 2), M (opp-lost-life, 2), O (spell-cast-count, 3), the 5 EOE LTB/warp, and 8 irreducible singletons — ~22 instances stay `OtherCondition`.
- **Risk:** medium (parser) / high (engine contract).

> unparsed **12**, free-text **86 → 64** (OtherCondition 48 → 26, assuming Bucket B's 1 optional arm lands; if deferred, OtherCondition 48 → 27 and free-text → 65).

---

### Phase 4 — ⚠ ENTANGLED: unparsed grammar gaps (high risk, multi-gap)

#### Slice 8 — `unparsed-triggered` (6 nodes, 5 cards) ⚠ split into A-E sub-changes
Land as **3-5 small changes, NOT one** — the "shared grammar gap" is loose:
- **(A) Hylderblade** — widen `AttachTriggeredRule.cs` to accept `attach this <X> to target...`. Lowest risk; land first.
- **(B) ElegyAcolyte** — relax `DealsCombatDamageConditionRule` to plural `deal`, add `one or more creatures you control` branch to `TriggeredRuleHelpers.ParseObjectFilter`, extend `TryParseYouDrawAndYouLoseLife` for optional `you`.
- **(C) Chorale arm 2** — new `SacrificeSelfUnlessConditionTriggeredRule.cs`. *Routes the Void string to `OtherCondition`* — coordinate with Slice 7 so the Void grammar lands once.
- **(D) Chorale arm 1** — new reanimation rule + new `Attacking` bool on `ReturnToBattlefieldEffect` (CR 508.4). Medium.
- **(E) Precursor + Bill** — ⚠ **hardest, dedicated TDD batch**: new `targets only self` `BecomesTarget` condition + reflexive `CopyEffect{Target:It, Count:per-each, MayChooseNewTargets}`. **Interaction-firability sensitive** — `CopyEffect.Target=It` + self/other-Golem distinction feed the self/any axis the operator gates; an over-broad filter (dropping `only`/`other`) is rules-wrong AND a downstream recall loss. Co-design with Slice 6's `ExcludeSelf`/`IsSelf` work (shares `ParseObjectFilter` self-logic).
- **⚠ Entanglement:** ElegyAcolyte's pre-existing structured Void arm must stay byte-identical; Chorale's TWO unparsed nodes must BOTH clear or it stays on the list. The Void condition strings (C/D) MOVE free-text into `OtherCondition` — net debt move, not elimination; bump baseline deliberately, don't let it silently ratchet up.
- **Ordering:** `TriggeredRuleHelpers.ParseObjectFilter` shared with Slices 2, 5, 6, 7 — land AFTER those. Within the slice: A → B → C+D → E.
- **Risk:** high.

> unparsed **12 → 7** (Hylderblade, ElegyAcolyte, Chorale, Precursor, Bill cleared). Free-text: `OtherCondition` may grow +2-3 from the Void strings — bump baseline deliberately and net it against Slice 7. Track honestly.

#### Slice 9 — `unparsed-static-ability` (5 cards) ⚠ + `unparsed-legacy` (3 cards)
Both are multi-gap, AbilityClassifier/ClauseSplitter-heavy, with corrupt-gold re-points already de-risked in Slice 0.

**`unparsed-static-ability` — 3 independent edits:**
- **(A) Void spell cards** (PlasmaBolt, TragicTrajectory) — `AbilityClassifier.cs` Void-aware arm routing the `...instead if <cond>` body to `AbilityKind.Spell`; `SpellAbilityParser` structures the conditional alternate with `AbilityWord=Void` + `OtherCondition` gate (InterceptorMechan precedent). Debt-move into `OtherCondition` — bump baseline.
- **(B) Spree** (WildcallSpree) — `ClauseSplitter.cs` Spree-cluster recognizer (parallels modal machinery). ⚠ **Re-point `Input.Name` off `Wildcall` first** (collides with FRF Manifest Wildcall) — do NOT regen as-is.
- **(C) Split `//`** (WearTear) — `ClauseSplitter` emits zero clauses for a standalone `//`. ⚠ **Hand-correct the fabricated Input first** (real Tear = "Destroy target enchantment", not "Deal 2 damage"). WearTear's 2nd node (bogus Deal-2 line) belongs to `unparsed-spell-loselife` and is gold-data-error — fix Input, don't parse it.

**`unparsed-legacy` — 2 new rules + regen:**
- WindbriskHeights — new `MayPlayExiledWithoutPayingIfConditionEffectRule` reusing `MayPlayFromExileEffect` + new optional `WithoutPayingManaCost` bool + `Condition` (OtherCondition for the attack-count gate). Re-point Input (stale, missing "enters tapped" line).
- IxidorRealitySculptor — new `TurnFaceUpEffect` AST node + rule. ⚠ couples to Slice 5's face-down characteristic — **land Slice 5 first** so face-down is structured, not a new residual.
- AspectOfMongoose — regen-only, already re-pointed in Slice 0.

- **Ordering:** AFTER Slice 4 and Slice 8 (`AbilityClassifier.cs` shared); Ixidor AFTER Slice 5. `ClauseSplitter` is unique to this slice (no other family touches it).
- **Risk:** high.

> unparsed **7 → 0** (all remaining KnownUnparsedGold cleared; remove every entry from the HashSet). Free-text: `OtherCondition` +2-3 (Void/Windbrisk gates) — bump baseline deliberately.

---

## Final ratchet state (target)

| Ratchet | Start | End |
|---|---|---|
| **KnownUnparsedGold** | 14 | **0** |
| **Free-text sinks** | 171 | **~64** (OtherCharacteristic 94→16, OtherCondition 55→26-29 net of Void debt-moves, AbilityText/Instructions/OtherHistoryPredicate **18/3/1 untouched**) |

The residual ~64 is: ~16 `OtherCharacteristic` not in any of these 10 families' scope, ~22-29 `OtherCondition` (Buckets H/L/M/O + EOE LTB-warp + singletons), and the **deferred adjunct sinks** — `AbilityText` (18: HelmOfTheHost/KikiJiki keyword-enum migration), `Instructions` (3: may-pay-cost effort), `OtherHistoryPredicate` (1: RowdyResearch). Those three are explicitly **out of scope** for these slices.

---

## Deletable golds

**None.** Every gold across all 10 families is a real, in-corpus card with disposition `fix`. Four golds carry **corrupt/stale Input** that must be **re-pointed (not deleted)** via `GoldRegenerationUtility`:
- `WOE/CandyTrail` — fabricated Bargain/Food body; re-point to authoritative "Artifact — Food Clue" (Slice 0).
- `TSP/AspectOfMongoose` — fabricated can't-be-blocked/bounce body; re-point to real shroud/LTB-return text (Slice 0).
- `BLB/WildcallSpree` — `Input.Name="Wildcall"` collides with FRF Manifest Wildcall; **rename Input.Name** before any regen (Slice 9).
- `DGM/WearTear` — fabricated combined split-card text; **hand-correct Input** (Tear = destroy enchantment), then regen (Slice 9).

---

## Per-slice TDD loop

Every slice follows the same gated cycle; **no ratchet tolerance — all gates green to commit.**

1. **Parser fix** — make the code change (extract shared helper first where the spec calls for it). `dotnet build` clean.
2. **Regen** — write affected gold rel-paths to `/tmp/golds-to-regen.txt`; run `MAST_REGEN_LIST=/tmp/golds-to-regen.txt dotnet test --filter Regenerate_listed_golds_from_corpus` (the `[Explicit]` `GoldRegenerationUtility`). For corrupt-Input golds, hand-correct/re-point Input **before** regen.
3. **mast-judge** — per-gold rules verdict (CR cross-check per the family's `regenerateJudgeChecklist`): correct axis/condition encoding, co-occurring siblings preserved (not dropped), no over-application, no new `IUnparsed`/`IResidual` leaked beyond the owning family's scope. Any FAIL halts the loop → drive a parser fix → re-regen → re-judge.
4. **Ratchet baseline shrink** — edit `tests/magic-ast-tests/Fixtures/destring-sink-baseline.json` to the **new live count** (recompute, never blind-subtract — concurrent families share the key). For unparsed slices, remove cleared cards from `KnownUnparsedGold` in `GoldFixtureUnparsedTests.cs`. For debt-move slices (Void/LTB conditions), bump `OtherCondition` **up** deliberately and net against the structuring batch.
5. **Full suite green** — `DestringSinkRatchetTests` (sink shrank, baseline matches), `GoldFixtureUnparsedTests` (no listed gold parses clean while still listed; no cleared gold still listed), the full `magic-ast-tests` parse suite, and — **mandatory for Slices 6 and 8E** — the `mast-interaction` tier tests (canonical-combo asserts) to catch GREEN→AMBER demotion before AND after the firability carve-out.
6. **Commit** — one slice per commit; agents commit, humans push-sign (per `git pushsigned` workflow). Re-run `nx run mast:free-text-census` to see the new per-sink numbers (`Data/_08_Reporting/free-text-residual-census.json`). The old committed `libs/magic-ast/schema/destring-worklist.json` is deleted — it was a frozen measurement with no regenerator (ADR-0004 issue #38); the census recomputes it.

**Entangled slices (6, 7, 8E) additionally require human design sign-off in step 1** — the engine carve-out / reference-not-resolution contract is specced and reviewed *with* the parser change, never after.

---

## Run 1 results (2026-06-16, autonomous workflow `wf_a4a109bd-ace`)

**Committed (green + judge-PASS), HEAD now at the Slice-4 commit:**
- **Slice 0** — `TSP/AspectOfMongoose` re-pointed (drift + unparsed entries cleared). `1f256700`
- **Slice 1** — self-reference `IsSelf`: AdornedPouncer, HonoredHydra, DrudgeBeetle, TerrusWurm. `56e62caf`
- **Slice 4** — Whirler Rogue unparsed cleared. `07f34f7f`

**Deferred (judge-FAIL → reverted clean → continued):** Slices 2, 3, 5.

### The key structural learning: the whole-gold judge + whole-slice revert fights multi-residual golds
The `mast-judge` gate FAILS a regenerated gold if **any** rules-meaningful free-text residual remains — even when the slice's own change was correct. Many golds carry residuals spanning **multiple** slices, so a single axis-slice cannot produce a fully-clean gold for them:

- **Slice 3 ⇄ Slice 5 are coupled through Mentor cards** (HammerDropper, BargingSergeant, BladeInstructor): each carries BOTH "lesser power" (Slice 3's `PowerComparison`) AND "attacking" (Slice 5's combat-state). The judge confirmed **both slices' actual changes were correct/improvements**, but each leaves the other's residual → FAIL. They must land **together** (regenerate the shared golds only after both axes are structured), or the judge must be scoped to slice-delta correctness rather than whole-gold purity.
- **Slice 2 surfaced a pre-existing badly-wrong gold** — `ROE/BearUmbra`: gold has +3/+3 (real +2/+2), drops the gainAbility grant of "untap all lands", inverts a self-reference, uses an obsolete keyword. Needs a real re-derive, not just `IsEnchanted` (which dragged the otherwise-fine slice to a revert on one gold).
- **Slice 5** also failed `DisplacerKitten` (an `AbilityWord` encoding issue) alongside the Mentor coupling.

### Deferred parser work (folds into the Slice 6+ batch, needs engagement)
- `WOE/CandyTrail` — re-point drops the "gain 3 life" conjunct of "gain 3 life and draw a card" → effect-conjunction parsing.
- **Bucket A** (7 Persist/Undying counter-gates) — keyword-path → `TriggeringObjectCounterCondition` wiring.
- `ROE/BearUmbra` — multi-defect re-derive (magnitude + gainAbility-grant + self-ref).
- **Mentor set** (Slice 3+5 coupled) — land combat-state + `PowerComparison` together, regen shared golds once.
- `DisplacerKitten` — `AbilityWord` encoding.

### Process refinement for the next run
Group slices by **gold-set**, not by axis (so a multi-residual gold is fully cleaned in one atomic slice), **or** loosen the judge to "this slice structured its axis correctly and introduced no new residual" (delta-judge) rather than "whole gold residual-free." Slices 1/4/0 committed precisely because their golds were single-concern.

---

## Parser batch (revised, gold-set-grouped)

Revision of the deferred Run-1 work (Slices 2/3/5 + folded-in CandyTrail/Bucket-A/BearUmbra/DisplacerKitten), re-cut for the **delta-judge** harness. Operating contract assumed for every slice below: *a slice need only correctly structure ITS axis on its golds and introduce no NEW residual; it need not clean residuals owned by OTHER axes — but coupled axes landing on the SAME gold are grouped into ONE atomic slice.* All facts below were re-verified against live Scryfall (`/cards/named?exact=`) and the actual gold/parser files on 2026-06-16 — not from memory or the earlier sections.

**Excludes Slice 6 (`another`→`ExcludeSelf`)**, co-designed by a human. Every gold in the sets below that ALSO carries an `other`/`another` exclusion residual is flagged inline as **[S6-SHARED]** and consolidated in the "Slice-6-shared golds" roster at the end, so the two batches can be sequenced (land these slices first; Slice 6 then rebases and regens the shared golds) or co-grouped.

### Verified ground-truth corrections to the earlier sections
- **Mentor set is 3 golds, not the names the Run-1 note implies.** The real Mentor-keyword golds are `GRN/HammerDropper`, `GRN/BargingSergeant`, `GRN/BladeInstructor` — each carries BOTH `attacking` (structured-characteristic) AND `with power less than this creature's power` (comparative-power). `CSP/ResplendentMentor` is **NOT** a Mentor card (it only has "Mentor" in its *name*; it is a white-creature anthem) — exclude it. `AggressiveMammoth` carries `other` + `with power less than this creature's power` but **no `attacking`**; its power-comparison is real but it is Slice-6-coupled, not Mentor-coupled.
- **BearUmbra gold is wrong on four axes** (Scryfall: `Enchantment — Aura`, `+2/+2`, grants `"Whenever this creature attacks, untap all lands you control."`, keyword `Umbra armor`). The gold has `+3/+3`; drops the grant entirely (encodes the untap as a *self* triggered ability on the Aura instead of a granted ability on the enchanted creature); inverts the self-reference ("Whenever **the enchanted creature** attacks" should be the granted ability's own "this creature"); and uses the obsolete keyword `Totem armor` (renamed `Umbra armor`). Needs a genuine re-derive, not just `IsEnchanted`.
- **Bucket A golds carry ONLY the InterveningIf residual** (`OtherCondition{"it had no ±1/±1 counters on it"}`) — verified: zero `OtherCharacteristic`, zero other `OtherCondition`. They are single-concern → each fully cleans in this one slice. The `ConditionParser.TriggeringObjectCounter` regex **already matches** both `"it had no -1/-1 counters on it"` and `"it had no +1/+1 counters on it"` (confirmed by running it); the gates are hardcoded `OtherCondition` at three producer sites, never routed through the parser.
- **CandyTrail** real text (Scryfall): `Artifact — Food Clue`; `"When this artifact enters, scry 2."` + `"{2}, {T}, Sacrifice this artifact: You gain 3 life and draw a card."` The gold's Input is a *fabricated* Bargain/Food-sorcery body (corrupt) and the body is `unparsed`. The re-point + parse must produce **two** structured effects from "You gain 3 life and draw a card" (effect-conjunction), or the re-point silently drops the gain-3-life conjunct.
- **DisplacerKitten**: real text is `"Avoidance — Whenever you cast a noncreature spell, …"`. The gold already encodes `AbilityWord: "Avoidance"` but leaves `noncreature` as an `OtherCharacteristic` residual. "Avoidance" is **not** a real CR ability word (CR 207.2c list) — it is the card's own italic flavor label; the encoding issue is whether to keep `AbilityWord` at all and whether `noncreature` is structured. See Slice PB-6.

---

### Slice ordering / shared-code serialization (within this batch)

Serialize on shared files; the second writer to a file rebases + re-regens:
- `ObjectFilter.cs` (`Comparison` record) — **PB-2** only writer in this batch.
- `Characteristic.cs` (new variants) — **PB-3** only writer.
- `TriggeredRuleHelpers.ParseObjectFilter` / `StaticRuleHelpers` / `SpellRuleHelpers` / `ActivatedRuleHelpers` (the qualifier→axis mapping) — **PB-3** only writer in this batch (Slice 6 also writes here later → land PB-3 first).
- `ConditionParser.cs` — **PB-4** (Bucket A) is leaf/additive (no new arm needed — regex already matches), and **PB-5** (CandyTrail) doesn't touch it. No collision inside the batch.

Recommended land order: **PB-4 → PB-1 → PB-5 → PB-6 → PB-2 → PB-3** (single-concern/leaf first; the coupled comparative+characteristic megaslice PB-3 last because it is the broadest `*RuleHelpers` edit and Slice 6 must rebase on it).

---

### Slice PB-4 — Persist/Undying counter-gate → structured `TriggeringObjectCounterCondition` (Bucket A)
- **Gold set (7):** `CSP/PutridGoblin`, `DKA/StranglerootGeist`, `DKA/UndyingEvil`, `INR/ButcherGhoul`, `INR/YoungWolf`, `MOR/GravelgillAxeshark`, `SHM/SafeholdElite`.
- **Parser change:** route the three hardcoded gates through the *existing* `ConditionParser.Parse` (or directly construct `TriggeringObjectCounterCondition`):
  1. `libs/magic-ast/Keywords/Definitions/PersistKeyword.cs` — `InterveningIf = new OtherCondition { Text = InterveningIfText }` → `InterveningIf = ConditionParser.Parse(InterveningIfText)` (the const is `"it had no -1/-1 counters on it"`).
  2. `libs/magic-ast/Keywords/Definitions/UndyingKeyword.cs` — same swap, text `"it had no +1/+1 counters on it"` (covers ButcherGhoul, YoungWolf, StranglerootGeist's Undying).
  3. `libs/magic-ast/Parsing/Parsers/Spell/Rules/TargetCreatureGainsKeywordRule.cs` (L194, the `"undying"` arm of `MapKeywordToStaticAbility`) — same swap (covers UndyingEvil, the granted-ability spell).
- **Structured AST target:** `TriggeringObjectCounterCondition { CounterType: "-1/-1"|"+1/+1", Present: false }`. (Persist → `-1/-1`; Undying → `+1/+1`.)
- **Dependencies/ordering:** none inside this batch. Land first (lowest risk, leaf change, no new AST/schema). `ConditionParser.cs` itself needs **no** new arm — only the producers change.
- **Per-gold judge checklist:**
  - `InterveningIf` is `triggeringObjectCounter` with `Present=false` and the correct polarity (Persist=`-1/-1`, Undying=`+1/+1`) — CR 702.79a / 702.93a.
  - The `returnToBattlefield` effect, `WithCounters`, `UnderControl=Owner`, `Trigger{When,Dies}` and (for the creatures) `IsSelf=true` are **byte-identical** to before (only the `InterveningIf` node changed).
  - UndyingEvil: the gate sits on the **GainedAbility** (the granted Undying), not on the spell ability; the spell's `gainAbility` target/duration are unchanged.
  - No new `OtherCondition`/`OtherCharacteristic` introduced; no gold leaves an `IUnparsed`.
  - Remove all 7 from `whitelist-freetext.json` (sink `OtherCondition`).
- **[S6-SHARED]:** none.

### Slice PB-1 — aura `IsEnchanted` (+ BearUmbra full re-derive)
- **Gold set (3):** `ROE/LuminousWake`, `M14/UnhallowedPact`, `ROE/BearUmbra`. (These are the only golds carrying `OtherCharacteristic{"enchanted"}`.)
- **Parser change:**
  - New flat `bool? IsEnchanted` on `ObjectFilter.cs` (mirrors `IsSelf`/`IsToken`; doc it as "the object enchanted by this Aura, CR 303.4 / 702.5"). Route the one qualifier branch in `TriggeredRuleHelpers.ParseObjectFilter` (and any static path that emits `Other("enchanted")`) to set `IsEnchanted=true` instead of appending the residual.
  - **BearUmbra re-derive (Input is wrong — re-point Input THEN code):** correct `Input.OracleText` to the Scryfall text: `Enchant creature\nEnchanted creature gets +2/+2 and has "Whenever this creature attacks, untap all lands you control."\nUmbra armor (…)`. The corrected parse must produce: (a) `modifyPT +2/+2` on `EnchantedOrEquipped`; (b) a **`gainAbility`** effect on `Target:{EnchantedOrEquipped}` whose `GainedAbility` is the *triggered* ability `Whenever {this creature, IsSelf} attacks → untap Each {land, Controller:You}` (the self-reference is the granted ability's own source per CR 109/702, NOT a separate "the enchanted creature" filter); (c) keyword `Umbra armor` (not the obsolete `Totem armor`). Reuse the GorgonsHead/GuardDuty `gainAbility`-on-Aura precedent (those grant a *static* keyword; BearUmbra grants a *triggered* ability — same effect node, triggered `GainedAbility`).
- **Structured AST target:** `ObjectFilter.IsEnchanted=true` (LuminousWake, UnhallowedPact, and BearUmbra's modifyPT/gainAbility targets); BearUmbra additionally: correct magnitude, restored `gainAbility` grant, corrected keyword enum.
- **Dependencies/ordering:** writes `TriggeredRuleHelpers.ParseObjectFilter` — must land **before PB-3** (which rewrites the qualifier→axis mapping in the same method); PB-3 rebases. Independent of PB-4.
- **Per-gold judge checklist:**
  - LuminousWake / UnhallowedPact: the former `Other("enchanted")` is now `IsEnchanted=true`; all sibling axes (controller, types, the aura's other effects) byte-identical; no new residual.
  - BearUmbra: PT is **+2/+2** (CR check vs Scryfall); the `gainAbility` grant of the untap-lands triggered ability is **present** and correctly attached to the enchanted creature; the granted trigger's self-reference resolves to the *granted ability's* source (IsSelf), not to the Aura; keyword is `Umbra armor`; Input was re-pointed (not deleted). No `OtherCharacteristic{"enchanted"}` remains.
  - Remove all 3 from `whitelist-freetext.json` (sink `OtherCharacteristic`).
- **[S6-SHARED]:** none.

### Slice PB-5 — CandyTrail re-point + effect-conjunction ("gain N life and draw a card")
- **Gold set (1):** `WOE/CandyTrail`.
- **Parser change:**
  - **Re-point Input first** (corrupt): set `Input.TypeLine = "Artifact — Food Clue"` and `Input.OracleText = "When this artifact enters, scry 2.\n{2}, {T}, Sacrifice this artifact: You gain 3 life and draw a card."` (Scryfall-exact). Then regen.
  - Parser: ensure the activated-ability effect body `"You gain 3 life and draw a card"` parses as **two** structured effects (a `gainLife{3}` + a `drawCards{1}`) joined by an effect-conjunction, not as one residual. This is the load-bearing fix called out in Run-1: the naive re-point drops the gain-3-life conjunct. Locate the conjunction split in the activated/spell effect pipeline (the `... and ...` effect splitter); add the `gain N life and draw a card` arm if the existing splitter doesn't already cover the verb pair. The ETB `scry 2` and the activated cost (`{2},{T},Sacrifice this`) are already-covered shapes.
- **Structured AST target:** an activated ability with cost `{2},{T},Sacrifice(this)` and a composite/two-element effect list `[gainLife 3, drawCards 1]`; plus the ETB `scry 2` triggered ability. **Zero `IUnparsed`.**
- **Dependencies/ordering:** independent; touches the effect-conjunction splitter (not shared with PB-2/PB-3/PB-4 in this batch). Land after PB-4 for risk-ordering only.
- **Per-gold judge checklist:**
  - Input re-pointed to the real Food/Clue card (type line + both lines), not deleted.
  - The sac-ability produces BOTH `gainLife 3` AND `drawCards 1` — the gain-life conjunct is **not** dropped (the explicit Run-1 failure mode).
  - Cost is `{2}` + `{T}` + `Sacrifice this artifact`; ETB `scry 2` present.
  - Remove `WOE/CandyTrail` from `whitelist-unparsed.json` (the unparsed node is gone) — and confirm it introduced no `OtherCondition`/`OtherCharacteristic`.
- **[S6-SHARED]:** none.

### Slice PB-6 — DisplacerKitten AbilityWord encoding
- **Gold set (1):** `DisplacerKitten`.
- **Diagnosis (verified):** real text `"Avoidance — Whenever you cast a noncreature spell, …"`. "Avoidance" is **not** a CR 207.2c ability word (it is the card's printed italic label, mechanically inert). The gold encodes it as `AbilityWord: "Avoidance"` and leaves `noncreature` as `OtherCharacteristic{"noncreature"}`. Two coupled defects on the one gold, so grouped:
  1. **AbilityWord:** decide the encoding. Recommended: drop the `AbilityWord` field for non-CR labels (ability words are reminder-grouping only and carry no rules meaning, CR 207.2c) **or** structure it as a flavor/printed label distinct from the typed `AbilityWord` enum if the schema reserves `AbilityWord` for real ability words. The delta-judge must confirm the choice does not assert a false CR ability word.
  2. **`noncreature`:** structure as `CardTypes:["spell"]` + `ExcludedCardTypes:["creature"]` (the existing non-`<type>` negation axis), removing the `OtherCharacteristic` residual.
- **Parser change:** the spell-cast trigger filter producer for `"noncreature spell"` → emit `ExcludedCardTypes:["creature"]` (reuse the existing non-type negation path); adjust the AbilityWord handling per the decision above. Confirm against `WAR/SpellgorgerWeird`, `OTJ/SlickshotShowOff`, `ZEN/SpellPierce` which carry the same `noncreature` residual (they are NOT in this slice's gold set — but verify the shared producer change doesn't regress them; if it cleans them too, that is a bonus, remove them from the whitelist as well).
- **Structured AST target:** trigger filter `{CardTypes:["spell"], ExcludedCardTypes:["creature"], Controller:You}`; AbilityWord per decision.
- **Dependencies/ordering:** touches a spell-cast-trigger filter producer — light overlap risk with PB-3's mapping extraction; land before PB-3 OR fold the `noncreature` producer change into PB-3 if they touch the same method (verify at implementation time). Independent of PB-1/PB-2/PB-4/PB-5.
- **Per-gold judge checklist:**
  - `noncreature` is structured as `CardTypes:["spell"]`+`ExcludedCardTypes:["creature"]`; no `OtherCharacteristic` remains.
  - The AbilityWord encoding decision does not assert a non-existent CR ability word; the trigger event/effects (exile-then-return composite, `ExiledWith:Self`, `UnderControl:Owner`) are unchanged.
  - Remove `DisplacerKitten` from `whitelist-freetext.json` (sink `OtherCharacteristic`).
- **[S6-SHARED]:** none.

### Slice PB-2 — comparative `PowerComparison` ("with power less than this creature's power") — **see PB-3 (merged)**
This was the standalone "Slice 3" axis. Because **every** gold that carries `with power less than this creature's power` ALSO carries a structured-characteristic residual on the **same** gold (the 3 Mentor golds carry `attacking`; AggressiveMammoth carries `other`), there is no gold where the comparative axis lands alone *and* leaves the gold clean. Per the grouping contract, **PB-2 is merged into PB-3** for the Mentor golds. The `Comparison`-record extension itself (the shared substrate) is specced here and lands as part of PB-3:
- **Parser change (substrate):** extend the `Comparison` record in `ObjectFilter.cs` so the RHS can be relative-to-an-object, not just a literal int. Make `Value` nullable (`int?`) and add `ObjectReference? RelativeTo` (+ optional `RelativeCharacteristic` for the "power" axis), so "power less than this creature's power" → `PowerComparison { Operator: LessThan, RelativeTo: ObjectReference.Self(), RelativeCharacteristic: Power }`. **Audit all ~12 literal-int `Comparison` consumers** (grep: `SoulshiftKeyword`, `SearchLibraryToBattlefieldEffectRule`, `CounterSpellRule`, `DestroyTargetWithFilterRule`, `ExileTargetQualifiedRule`, `CantBeCastRestrictionRule`, `SpellRuleHelpers`, `ObjectFilterRelations`, plus `Count` in `CountCondition`) — they must serialize **byte-identically** (`RelativeTo`/nullable `Value` absent via `WhenWritingNull`).
- **Producers:** `MentorKeyword.cs` (replace `Characteristic.Other("with power less than this creature's power")` with a `PowerComparison` on the target filter); and the comparative branch in `CantBeBlockedRule.cs` (the existing relative-power convention the Mentor doc cites). Update `ast-schema.json` `Comparison` def + any `SchemaExportTests` snapshot.

### Slice PB-3 — structured-characteristic megaslice (attacking / tapped / untapped / type / color / +1+1-counter) **+ merged comparative-power** (the gold-set-grouped atomic slice)
This is the consolidated slice the Run-1 retro demanded: it owns BOTH the structured-characteristic axis AND the comparative-power axis, so the Mentor golds — which carry both — are fully cleaned in ONE atomic regen.

- **Gold set.** Partitioned by which structurable axis they carry (all currently `OtherCharacteristic` debt). Golds whose ONLY residual is the structured-characteristic axis are fully cleaned here; golds that ALSO carry an `other`/`another` exclusion are **[S6-SHARED]** (this slice cleans their characteristic axis; Slice 6 cleans the exclusion — regen the shared gold only after BOTH land, or co-group).

  - **Mentor (attacking + comparative-power, both owned here):** `GRN/HammerDropper`, `GRN/BargingSergeant`, `GRN/BladeInstructor`.
  - **`tapped`:** `10E/Vengeance`, `Galestrike`, `AdeptWatershaper` **[S6-SHARED]** (`other tapped`), `SarythTheVipersFang` **[S6-SHARED]** (`other tapped` + `other untapped` — needs a `tapped:false`/`untapped` representation).
  - **`attacking` (combat-state, already-structurable via `CombatStateCharacteristic`):** `MBS/GoblinWardriver` (carries `attacking`×2; its `ExcludeSelf` is ALREADY structured → NOT S6-shared anymore).
  - **`attacking alone`:** `ALA/AkrasanSquire`, `M13/KnightOfGlory`, `QasaliPridemage`, `SHM/AvenSquire` (note: `CombatState.AttackingAlone` already exists — these may already be structurable via `Characteristic.FromLabel`; verify they aren't already clean before including).
  - **type axes** (`artifact`/`token`/`nonland`/`nonbasic`/`noncreature`/`instant`/`sorcery` etc., via `CardTypes`/`ExcludedCardTypes`/`ExcludedSupertypes`/`IsToken`): `10E/Fear` **[S6? no]**, `10E/SeveredLegion`, `9ED/RazortoothRats`, `UDS/SquirmingMass` (each `['artifact','black']` → `CardTypes`+`Colors`), `AVR/Vanishment`/`Disperse`/`M14/PlanarCleansing`/`M15/VoidSnare`/`MB6/KickedBounce` (`nonland`), `CMD/Ruination` (`nonbasic`→`ExcludedSupertypes:["Basic"]`), `DTK/VirulentPlague`/`GTC/IllnessInTheRanks` (`token`→`IsToken`), `NEO/VilespawnSpider` (`artifact`), `M14/YoungPyromancer` (`instant`/`sorcery`), `OTJ/SlickshotShowOff`/`WAR/SpellgorgerWeird`/`ZEN/SpellPierce` (`noncreature` — shared producer with PB-6; sequence/fold accordingly).
  - **color axes** (`black`/`nonblack`/`nonblue`/`nonwhite`/`shares a color` → `Colors`/`ExcludedColors`/`SharesColorWith`): `DoomBlade` (`nonblack`), `GPT/Frazzle`/`Inundate` (`nonblue`), `Saltblast` (`nonwhite`), `M13/KrenkosEnforcer` (`artifact`+`shares a color`).
  - **`+1/+1` counter axis** (`with a +1/+1 counter` → new `CounterCharacteristic`): `CrownedCeratok`, `SapphireDrake`.

  *(Golds with residuals OUTSIDE these five axes — e.g. `BFZ/FathomFeeder` `top`/`that player's`, `LRW/GaddockTeeg` `{X} in mana cost`, `THS/TritonFortuneHunter` `targeting this creature`, `RoadOfReturn` `your commander`, `M10/Falter`/`MIR/*` flanking/flying-without — are OUT of this slice's axes; leave their residual and keep them whitelisted. Per the delta-judge contract this slice need not clean them.)*

- **Parser change:**
  - **Extract ONE shared qualifier→axis helper first** (the mapping is duplicated across `SpellRuleHelpers`, `StaticRuleHelpers`, `TriggeredRuleHelpers`, `ActivatedRuleHelpers`), then route all call sites through it. This is the single edit that collapses ~40 scattered touches into one fix.
  - New `Characteristic` variants in `Characteristic.cs`: `TappedStateCharacteristic { bool Tapped }` (covers `tapped` AND `untapped`; CR 110.5) and `CounterCharacteristic { string CounterType, Comparison? }` (covers `with a +1/+1 counter`). Extend `Characteristic.FromLabel` to map `tapped`/`untapped`/`with a +1/+1 counter`.
  - New `ObjectFilter` axis `ExcludedColors` (for `nonblack`/`nonblue`/`nonwhite`); reuse existing `Colors`, `CardTypes`, `ExcludedCardTypes`, `ExcludedSupertypes`, `IsToken`, `SharesColorWith`, and the existing `CombatStateCharacteristic`.
  - **Merge PB-2:** land the `Comparison.RelativeTo` substrate + `MentorKeyword`/`CantBeBlockedRule` producers here (so the Mentor golds get attacking→`CombatStateCharacteristic` AND lesser-power→`PowerComparison` in the same regen).
  - **Schema:** add discriminator kinds `tapped`, `counter` to the `Characteristic` `PolymorphicReflectionConverter`; every exhaustive `switch` over `CharacteristicKind` in `libs/mast-interaction` must learn them or silently drop (CR 110.5/122 filter predicates — not engine actions, so no firability change, but confirm consumers don't try to *evaluate* the new typed node). Regenerate `SchemaExportTests` snapshot.
- **Structured AST target:** per axis — `CombatStateCharacteristic{Attacking}`; `TappedStateCharacteristic{Tapped:true/false}`; `CardTypes`/`ExcludedCardTypes`/`ExcludedSupertypes`/`IsToken`; `Colors`/`ExcludedColors`/`SharesColorWith`; `CounterCharacteristic{"+1/+1"}`; and (Mentor) `PowerComparison{LessThan, RelativeTo:Self, RelativeCharacteristic:Power}`.
- **Dependencies/ordering:** land **after PB-1** (both touch `TriggeredRuleHelpers.ParseObjectFilter`; PB-1 first, PB-3 rebases) and **after PB-6** (or fold the `noncreature` producer into PB-3). Land **before Slice 6** — Slice 6 writes the same `Characteristics`-emission sites and must rebase; regen the **[S6-SHARED]** golds only after Slice 6 also lands.
- **Per-gold judge checklist (apply per gold, delta-scoped):**
  - **Mentor ×3:** `attacking` → `CombatStateCharacteristic{Attacking}` AND `with power less than this creature's power` → `PowerComparison{LessThan, RelativeTo:Self, RelativeCharacteristic:Power}` — BOTH structured, the gold is fully residual-free (no `OtherCharacteristic` left). CR 702.134.
  - **tapped/untapped golds:** `tapped`→`TappedStateCharacteristic{Tapped:true}`, `untapped`→`{Tapped:false}` (Saryth must distinguish its two anthem clauses). For `AdeptWatershaper`/`SarythTheVipersFang` the `other`/`another` residual MAY remain (Slice 6 owns it) — the delta-judge passes the *characteristic* delta and confirms only that NO NEW residual was added.
  - **type/color golds:** correct axis (`ExcludedCardTypes`/`ExcludedSupertypes`/`IsToken`/`Colors`/`ExcludedColors`), no over-application (e.g. `nonbasic`→`ExcludedSupertypes:["Basic"]`, NOT `ExcludedCardTypes`), siblings preserved (`['artifact','black']` → BOTH `CardTypes:["artifact"]` and `Colors:["B"]`).
  - **counter golds:** `with a +1/+1 counter` → `CounterCharacteristic{"+1/+1"}`.
  - No gold gains a new `IUnparsed`/`OtherCondition`; schema snapshot regenerated; `mast-interaction` `CharacteristicKind` switches updated (no silent drop). Remove fully-cleaned golds from `whitelist-freetext.json`; leave S6-shared golds whitelisted until Slice 6 lands.
- **[S6-SHARED] within this slice:** `AdeptWatershaper`, `SarythTheVipersFang`. (Mentor golds and `MBS/GoblinWardriver` are NOT shared — Mentor carries no exclusion residual; GoblinWardriver's exclusion is already structured.)

---

### Slice-6-shared golds (roster for sequencing with the human-designed `ExcludeSelf` batch)

Golds in THIS batch's sets that ALSO carry an `other`/`another` exclusion residual owned by Slice 6 (regen the shared gold only after both Slice 6 and the owning slice here land):

- `AdeptWatershaper` (PB-3: `tapped`; S6: `other`)
- `SarythTheVipersFang` (PB-3: `tapped`/`untapped`; S6: `other`×2 on the anthem clauses — note its activated arm already has structured `ExcludeSelf`)

For completeness, the **full Slice-6 gold population** (22 golds carrying an `other`/`another` exclusion residual, for the human's batch — only the two above intersect this batch's sets): `AdeptWatershaper`, `AggressiveMammoth`, `BenalishMarshal`, `C16/RavosSoultender`, `CHK/SachiDaughterOfSeshiro`, `CN2/GrenzoSRuffians`, `DTK/StormwingDragon`, `ExpeditionRaptor`, `FelhidePetrifier`, `HeraldOfDromoka`, `LCI/RegalImperiosaur`, `LTR/MerryEsquireOfRohan`, `M10/GoblinChieftain`, `M21/BarrinTolarianArchmage`, `M21/NiambiEsteemedSpeaker`, `MH1/KingOfThePride`, `MOM/LivingTotem`, `RIX/LegionLieutenant`, `RatColony`, `SarythTheVipersFang`, `UltramarinesHonourGuard`, `WindstormDrake`. (`AggressiveMammoth` additionally carries the comparative-power residual — if Slice 6 does not structure that, the comparative substrate from PB-3 should be applied to it in whichever batch regens it, to avoid two divergent relational-comparison designs.)

---

## Slice 6 — another → ExcludeSelf (RATIFIED design, 2026-06-16)

The entangled slice — done HANDS-ON (not in the autonomous batch), AFTER the parser batch lands (it must rebase on PB-3, which structures the tapped/untapped axis on the shared golds).

**Parser half** (~22 golds, all real, all fix): route "other"/"another" to structured `ObjectFilter.ExcludeSelf=true` instead of `OtherCharacteristic("other")` free-text:
- `StaticRuleHelpers.ClassifyTypeNounPhrase` — the central peel.
- `LordPTBuffRule` + `BareKeywordGrantRule` — carry `isOther` separately, append `ExcludeSelf=true` (stop seeding the characteristics string list).
- `TribalAnthemModifyPTRule` + `WithKeywordAnthemModifyPTRule` — replace `Other("other")`, keep the kw characteristic.
- Trigger/target path (Barrin, Merry, Niambi, Living Totem) — a shared `TryPeelSelfExclusion` detector.

**The entanglement (verified in code):** structured `ExcludeSelf=true` makes `ObjectFilterRelations.Subsumes` return `Unknown("ExcludeSelf")` via `SupUndecidedAxis` (`ObjectFilterRelations.cs:564,827`) — but ONLY when every other axis already subsumed. `PortGraphEngine.AddRulesEdge` (`PortGraphEngine.cs:696,710`) stamps that as the edge tier → canonical anthem/bounce/counter combos demote GREEN→AMBER. `Intersects` already ignores `ExcludeSelf` (`ObjectFilterRelations.cs:499–505`) and stays untouched.

**The fix — RATIFIED: cross-card carve-out** in `AddRulesEdge`, right after the `Subsumes` call (line ~696):
```csharp
if (reliability.Value == Trilean.Unknown
    && reliability.Reason == "ExcludeSelf"
    && from.Card != to.Card)
    reliability = new SubsumeMatch(Trilean.Yes);
```
`ExcludeSelf` only excludes the sup's own source object; cross-card the `from` object is a different card, so the exclusion imposes no constraint → subsumption holds → GREEN restored. Same-card stays `Unknown` (correct). Surgical: keys on `Reason == "ExcludeSelf"`, so it fires only when self-exclusion is the sole doubt. The operator-tier twin of the engine's existing same-card guards. (Rejected: card-aware inside `Subsumes` — that filter operator stays pure/card-unaware.)

**Non-negotiable:** parser + carve-out land in the SAME commit, with the `mast-interaction` tier tests (canonical-combo asserts) run BEFORE and AFTER to catch any silent GREEN↔AMBER flip.

**Slice-6-shared golds with the parser batch:** `AdeptWatershaper`, `SarythTheVipersFang` — both land in PB-3 (tapped/untapped axis) AND carry the `other`/`another` residual owned here. Regenerate them only after BOTH PB-3 and Slice 6 land (or co-group), so neither half leaves the other's residual.

---

## PB batch results (2026-06-16, delta-judge harness `wf_2fedae4b-9b4`)

**All 5 slices committed, zero deferred** — the delta-judge + partial-commit harness resolved Run-1's coupling failure (the Mentor golds passed because PB-3 merged comparative+characteristic into one atomic regen). 50 golds regenerated.

| Slice | Commit | Golds | Notes |
|---|---|---|---|
| PB-4 — Bucket A counter-gate | `c8d34e3b` | 7 | Persist/Undying intervening-if → `TriggeringObjectCounterCondition` |
| PB-1 — aura IsEnchanted + BearUmbra | `f928203e` | 4 | `IsEnchanted` axis; BearUmbra re-derived (+2/+2, restored grant, Umbra armor) |
| PB-5 — CandyTrail conjunction | `f37084cb` | 1 | re-point + "gain 3 life and draw a card" → two structured effects |
| PB-6 — DisplacerKitten | `0ea242a6` | 3 | `noncreature` → ExcludedCardTypes; AbilityWord resolved |
| PB-3 — structured-characteristic megaslice | `9eb241fd` | 35 | tapped/counter/type/color axes + merged comparative-power |

**Whitelist delta (session):** freetext 149→100, unparsed 14→11, oracle-drift 87→81. Recall unchanged **5G/18A/10M**; full suite 4525 + bench 41 green (two-layer equivalence + tier tests pass).

**Partial-commit carry-overs (PB-3 kept 3 entries):** `AdeptWatershaper`, `SarythTheVipersFang`, `AggressiveMammoth` — their characteristic/comparative axis is structured, but their `other`/`another` exclusion residual is left whitelisted for **Slice 6** to clean. This is the bridge: Slice 6 now lands on the 22-gold exclusion population (incl. these 3) on top of a structured-characteristic base.

---

## Slice 6 — LANDED (2026-06-16, commit dd4fd542, hands-on)

`another→ExcludeSelf` parser sweep (8 emission sites + StaticRuleHelpers central peel) + the
cross-card firability carve-out in `PortGraphEngine.AddRulesEdge`. **22 golds** migrated to structured
`ExcludeSelf`, all `mast-judge` PASS; carve-out `interaction-judge` PASS.

- **Carve-out is PRECAUTIONARY, not load-bearing (corrected premise).** 2-phase test: recall is
  5G/18A/10M *with or without* it — no eligible-33 combo routes through a cross-card `ExcludeSelf` edge
  (the 22 ExcludeSelf golds are anthem/utility creatures, not combo pieces). It prevents a *future* silent
  GREEN→AMBER demotion, and is rules-proven sound (CR 109.2 / 400.7 / 111 / 707.2 — no false GREEN across
  token/copy-graft/multi-instance identity). Guarded by `PortGraphEngineTest` (cross-card→Yes, same-card→Unknown).
- **GoblinChieftain conjunction RESOLVED, not deferred.** Its authoritative "Other Goblin creatures … get
  +1/+1 and have haste" exposed a modifyPT+gainAbility conjunction gap; `SupertypeAnthemAndKeywordGrantRule`
  generalized from supertype-only to also handle "Other [Subtype] creatures … and have <kw>" (ExcludeSelf).
- **2 golds out-of-scope** (kept whitelisted, re-tagged): `GrenzoSRuffians` ("each other opponent" player
  filter), `ExpeditionRaptor` ("support N … other target creatures").

Session whitelist totals after Slice 6: **freetext 149→80, unparsed 14→11, quarantine 87→80.**

Remaining for full gold stability: the **long-tail disposition** — residuals no slice owns
(FathomFeeder `top`, GaddockTeeg `{X} in cost`, flanking, …), the remaining unparsed grammar gaps
(Chorale/Elegy/Hylderblade/Precursor/Bill), and the adjunct sinks (`AbilityText` 18 / `Instructions` 3 /
`OtherHistoryPredicate` 1) → each gets a slice or a justified `irreducible` tag.

---

## Long-tail disposition (2026-06-16)

Every residual whitelist entry is now tagged `debt` (→ a named slice) or `irreducible` (justified). Result:
**68 debt across 3 slices + 23 irreducible floor.**

### Debt (68 → 3 slices)
- **PB-7 — structured conditions (45 `OtherCondition`).** ENTANGLED: the reference-not-resolution engine
  contract (the engine evaluates the condition against live state via the keyword's linked ability, not a
  pre-baked bool). Families: LTB-event ("a nonland permanent left the battlefield"), control-duration
  ("came under your control since…"), tapped-state ("this artifact/land remains/is tapped", "it's
  untapped"), turn-phase ("during your turn"), keyword-state (saddled, city's blessing), this-turn-event
  ("an opponent lost life", "you discarded this card", "you attacked"), spell-count, enchanted-color,
  targeting. Needs human co-design of the condition-evaluation contract (like Slice 6's carve-out).
- **PB-8 — unparsed grammar gaps (11).** Triggered: ChoraleoftheVoid, ElegyAcolyte, Hylderblade,
  PrecursorGolem, BillPotts (Precursor/Bill are the entangled reflexive-`CopyEffect{Target:It}` shape —
  ties to the copy-recursion fixpoint). Static: PlasmaBolt, TragicTrajectory (Void conditional-alternate),
  WildcallSpree (Spree cluster; corrupt-Input rename), WearTear (split `//`; corrupt-Input). Legacy:
  WindbriskHeights (may-play-exiled-without-paying), IxidorRealitySculptor (turn-face-up effect).
- **PB-9 — misc structured-characteristic (12 `OtherCharacteristic`).** Mechanical: keyword-absence
  ("withoutFlanking"), disjunctive-type ("creature or Vehicle card"), ownership ("you own"), self-ref
  ("this permanent" → IsSelf).

### Irreducible floor (23, tagged + justified — the stable end state after PB-7/8/9)
- **`AbilityText` (15)** — recursive parsing of token / granted-ability bodies; a separate de-string
  initiative, not a quick slice.
- **`Instructions` (3)** — may-pay-cost effort structuring (deferred adjunct).
- **`OtherHistoryPredicate` (1)** — bespoke history predicate (deferred adjunct).
- **4 `OtherCharacteristic`** — out-of-scope/positional with no clean structured node: GrenzoSRuffians
  ("each other opponent" player filter), ExpeditionRaptor ("support N other target"), and positional
  "top" / "that player's" / "single target" predicates.

### Out of scope of this disposition
The **`oracle-text-quarantine` (80 drift golds)** is a SEPARATE track — gold-input text fidelity
(the gold-fidelity-cleanup worklist), not a parser-output cleanliness invariant. Re-pointing each may
expose parser gaps (as GoblinChieftain did), so it is its own initiative.

**Target after PB-7/8/9 land:** the free-text + unparsed whitelists shrink from 91 → the **23 irreducible
floor**, all named + justified — a stable, minimal end state for the larger TDD loops.
