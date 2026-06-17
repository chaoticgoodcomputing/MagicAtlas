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
6. **Commit** — one slice per commit; agents commit, humans push-sign (per `git pushsigned` workflow). Update `libs/magic-ast/schema/destring-worklist.json` for honesty (documentation, not gated).

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
