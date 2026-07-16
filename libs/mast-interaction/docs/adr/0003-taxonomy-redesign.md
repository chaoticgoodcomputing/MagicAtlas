# Taxonomy redesign — a resource/event ontology, subsumption over bridges

## Status

**Draft — accumulating observations (opened 2026-07-16). NOT a decision.** This document is an
observations log toward an eventual taxonomy-redesign ADR that will refine (and in places supersede)
[ADR 0002](0002-port-labels-are-deterministic-ast-projections.md) §3 (the colon-ontology) and §6
(curated bridges). It exists to collect grounded notes as they surface — mostly driven, so far, by the
**atlas-web card explorer** exposing taxonomy questions the union graph alone never forced. Append
observations here; promote to a Decision section only once a thread is settled and rules-checked.

Do not implement from this doc. The projection, engine, and gold fixtures still reflect ADR 0002.

## Context — why reopen the ontology

ADR 0002 fixed labels as deterministic AST projections over a soft (hierarchical) colon-vocabulary, with
the query as the projection read backwards, and made two axes explicit (action/object + resource-kind).
It has held well for the union-graph reconstruction. Two pressures reopened it:

1. **The frontend is a taxonomy oracle.** The card explorer's "what feeds X / what X feeds" columns are a
   *direct* rendering of the emit↔consume relation. Where the union graph tolerated a coarse family edge
   (a combo either reconstructs or it doesn't), the explorer surfaces every family adjacency as a
   user-visible claim — so a coarse `token→cast` combo-ring edge shows as "Chatterfang feeds Aang," which
   is plainly wrong. The frontend makes taxonomy imprecision *legible* in a way the batch pipeline did not.
2. **A single worked case (Chatterfang's sacrifice) cracked open the event model.** Deciding how "Sacrifice
   X Squirrels" should render forced the question of whether a sacrifice is a *consume* (current: `sac:` cost
   + curated `sac→ltb` bridge) or an *emit of an event* sitting in a subsumption hierarchy. An
   interaction-judge pass (2026-07-16, below) returned that hierarchy CR-correct, which generalizes well
   past sacrifice.

The through-line: **ADR 0002 names ports by mechanism/role; the redesign should lean harder on naming by
the resource/event that flows, so that subsumption (not curation) carries most cross-resource matching.**

---

## Observations log

### O1 — Event subsumption: `sacrifice ⊂ dies ⊂ leaves-the-battlefield` (judge-PASSED)
An interaction-judge review (all 6 claims PASS, PROCEED) grounded the hierarchy in CR:
- **sacrifice ⊂ dies** — CR 701.21a (sacrifice moves a permanent *from the battlefield directly to its
  owner's graveyard*) + 700.4 (*dies* = put into a graveyard from the battlefield). A "whenever a creature
  dies" trigger fires on a sacrifice.
- **dies ⊂ LTB** — CR 700.4 + 603.6 + 603.10a (a death is a battlefield→graveyard zone change; LTB watchers
  fire on it).
- **`sacrifice` also feeds "whenever you sacrifice"** natively — CR 603.10a lists sacrifice-triggers,
  dies-triggers, and LTB-triggers as look-back observers of the *same* event.

Today only **dies ⊂ LTB** is encoded (`DeathTrigger` = `ltb:…:to-graveyard`, the destination as an `ltb`
qualifier). `sacrifice` is a *separate `sac:` role* wired to dies-triggers only by a curated rules-bridge
(`sac → ltb:…:to-graveyard`). **Implication:** fold sacrifice into the `ltb` hierarchy as its narrowest
rung and retire the bridge — subsumption then reaches *all three* consumer rungs, including "when
sacrificed," which the dies-only bridge misses.

### O2 — Costs/effects are frequently DUAL (emit an event AND consume a resource)
The sacrifice cost both **emits** the LTB event (subject = fodder type — what triggers *see*) and
**consumes** the fodder permanent (what is *removed*). The judge's load-bearing condition: the emit's
subject is a **type descriptor, not a pool decrement** — dropping the `sac:` consume loses the §8
balance/multi-cost-conjunction floor and re-admits the Ruthless-Knave false-GREENs. **Implication:** the
redesign must let one clause project **both** a resource-consume and an event-emit, and keep them on
separate axes (flow/balance vs existence/certainty). This likely generalizes: "destroy," "exile," "mill,"
"draw-as-a-cost," etc. are candidates for the same consume+emit duality.

### O3 — Name by event/resource, not by action-mechanism
`sac`, `ltb`, `etb`, `cast`, `attacks` are **mechanism/action** roles. The subsumption in O1 only falls out
cleanly once sacrifice is expressed as *an LTB event with a manner*, i.e. named by the **event** it
produces. **Implication (thesis):** prefer event/resource-centric leaves so hierarchy is structural. A
"cost" is then a *consume of a resource* + optionally an *emit of the event that paying it produces*, not a
first-class role. Open: how far to push this without exploding the vocabulary (see O8).

### O4 — The facet grammar needs a `manner` slot
To keep dies a proper generalization of sacrifice while letting "when sacrificed" match *only* sacrifices,
the leaf needs a cause/manner facet **after** destination:
`role : subject : [destination] : [manner] : [scope] : [exclusion]`
(e.g. `ltb:creature:squirrel:to-graveyard:sacrificed:controlled`). The judge flagged that the rungs are
**not** naive subject-prefixes (subtype sits between subject and destination) — they are realized by the
**glob** operator (`ltb:**:to-graveyard:**`) plus the **operator** deciding subject-subsumption. So this is
a real grammar extension, authored so glob patterns generalize correctly. Manner tokens to enumerate:
`sacrificed`, `destroyed`, `combat` (combat-damage death), `state-based`, ….

### O5 — Two axes are being conflated: engine-ROLE vs frontend-FAMILY
`ResourceFamilies.Of(label)` derives the coarse family from the **role prefix** (`sac:` → `sacrifice`).
That family drives the resource graph + the explorer columns; the **role** drives the §8 engine. These are
different consumers with different needs: the engine wants the `sac:` role (conjunction/balance); the
explorer wants "Chatterfang consumes a *creature/token*, and emits a *death*." Deriving the family from the
role forces one to serve both and mislabels the card page. **Implication:** make family a first-class,
possibly event/subject-derived axis, decoupled from the engine role. (This is the concrete fork blocking
the Chatterfang card-page fix: fodder-consume family = `creature` [faithful, non-canonical] vs `token`
[drives columns, wrong for non-token fodder].)

### O6 — Subsumption vs curated bridges — draw the line deliberately
The engine has two cross-resource mechanisms: **facet-prefix subsumption** (dies⊂LTB, structural) and
**curated rules-bridges** in `PortGraphEngine` (`sac→ltb`, untap-lands→mana, blink→etb, spell-recursion→cast,
…). O1 shows at least one bridge (`sac→ltb`) is really a *missing subsumption rung*. **Implication:** audit
every curated bridge — which are genuine game-rule shortcuts (a *different* resource enabling another) vs
subsumption the vocabulary should express directly. Hypothesis: bridges should be reserved for
cross-*resource* enablement (untap→mana), and same-event narrowings (sacrifice→death) should be subsumption.

### O7 — Over-approximation is principled, and must stay principled
`sacrifice ⊂ dies` is over-approximate: a CR-614 graveyard replacement (Rest in Peace) sends the sacrificed
permanent to exile, so it *leaves the battlefield* (absolute) but does *not* die (the `to-graveyard`
destination is over-asserted). This is the **same** prune-able status the current `sac→ltb` bridge already
carries ("the label names; the operator decides," ADR 0002 §6/§7). The redesign must preserve: the
*projection over-proposes the destination, the operator/board prunes*. Related straddle: the subtype→creature
lift (`Squirrel` → `creature:squirrel`) is a matching over-approximation — CR 205.3m/308.1 leave
`Squirrel ⊄ creature` Unknown for a bare-subtype filter, so the operator floors it to AMBER (never a false
GREEN). Both belong in the redesign's "over-approximate axes" register.

### O8 — Single source of truth + generated docs
The taxonomy is spread across ~5 places with no generated reference:
`PortWalkProjection.cs` (projected role/effect/cost/trigger sets), `PortLabel.cs` (the facet builders),
`ResourceFamilies.cs` (families + canonical set), ADR 0002 (the spec), and a **partial duplicate in the
frontend** (`mock.ts` `GROUPS` / the live-canonical set derived from `resourceFamilyRows`). There is an
exhaustiveness ratchet (`PortWalkExhaustivenessTests`) forcing each AST discriminator through a projection
decision, but **no centralized definition of the families and their subsumption hierarchies**, and no
generated facet-grammar reference. **Implication:** the redesign should land a single machine-readable
taxonomy (families, rungs, manner tokens, subsumption edges) that both the engine and the frontend consume,
plus a doc generator. The user explicitly wants standardization + doc generation here.

### O9 — Canonical family set is due for review under the event model
Current 17: `mana token sacrifice death etb recur dice damage life blink copy cast combat untap tap counter
phase`. Under O1/O3, `sacrifice` is not a peer resource — it's a *manner* of `death`, which is a rung of
LTB; `etb` is the enter dual of LTB; `recur` (return-to-battlefield/hand) is zone-change too. **Implication:**
a zone-change umbrella (`ltb` / `etb` / `recur` as directions of one axis) may collapse several "families"
into one hierarchy, which would also fix the explorer's family membership questions at the root.

### O10 — "Fodder" is a strategic gloss, not a resource: separate OBJECTS from EVENTS
"Fodder" (what a sacrifice consumes) is Magic slang for expendable creatures — **not AST-derivable** (a
creature's fodder-ness is a game-plan judgment, absent from the card text) and not a 1/1-specific notion.
What a sacrifice actually consumes is a **creature** (the permanent named by the `sac:` subject filter —
Ashnod's `creature`, Chatterfang `creature:squirrel`); what the doubler emits is **creature tokens**
(`emit:token:creature:squirrel`), never `emit:fodder`. The label names what an object *is*, not the role a
player assigns it. This exposes a cleaner cut: distinguish **object-resources** (the things that flow around a
loop — creatures/permanents, mana, tokens, counters) from **events** (what happens to them — enters,
leaves-battlefield / dies / sacrificed, cast). A sacrifice = `consume(object: creature)` + `emit(event: LTB)`;
the creature flows, the sacrifice/death is the event feeding payoffs. **Refines** O2 (the duality is
object-consume + event-emit), O3 (name by object *and* by event, on separate axes), O5 (the fodder-consume
family is `creature`, the object — reframing the fork), and O9 (`creature`/`permanent` is an object-resource
the canonical family set omits; `sacrifice`/`death`/`etb` are events, not peer object-families). Corollary
(the enabling principle, ADR 0002 §2): a single oracle clause legitimately projects **multiple** ports —
Chatterfang's one sacrifice clause is a consume AND an emit — because a port is a *query* against the AST, and
one sub-tree satisfies many queries. The many-to-one (text → labels) is the design, not a smell.

### O11 — Slang categories are first-class when AST-derivable: `fodder` as a P/T-derived subset
**Corrects O10's over-dismissal.** The bar for a port term is **AST-derivability, not CR-canonicity** — MAST
is a *categorization* system, so a slang category earns its place if it's a definable **query**, not a
game-plan judgment. "Fodder" fails only as vague "creatures you'd sacrifice"; pinned to structure it is
legitimate and useful: `fodder` := a **1/1 creature** (or, for the Skullclamp reading, a **toughness-1**
creature — one a −1-toughness effect kills), read off the `createToken`/creature P/T the AST already carries.
It is a **subset leaf** on the object axis (`emit:token:creature:squirrel:…:fodder ⊆ emit:token`), consistent
with the soft hierarchy (a port satisfies its prefixes), so it adds precision without breaking coarse
`emit:token` matching. **Motivating case — Skullclamp** ("+1/−1; whenever equipped creature dies, draw two
cards; Equip {1}") is a `consume:fodder` engine: it turns toughness-1 creatures into cards. **Chatterfang's
1/1 Squirrels are `emit:…:fodder`**, so the taxonomy surfaces **Chatterfang × Skullclamp** structurally —
which bare `emit:token`/`consume:creature` labels miss, being too coarse to know these are the *small*
creatures Skullclamp wants. **Caveat — polysemy:** a sac outlet wants *any* creature, Skullclamp wants
*toughness-1*, an aristocrat wants *anything that dies*; so each derived category must be **pinned to exactly
one query** (or carry a small family: `fodder` = 1/1, `fodder:t1` = toughness-1), never a catch-all.
**Implication:** the object axis (O10) carries **derivable sub-categories** (P/T, subtype) as subset leaves —
this is a primary source of the taxonomy's synergy-surfacing value, and the redesign should treat such
categories as named, singly-defined query aliases layered under the structural labels.

### O12 — Initial top-level supergroups (slang-anchored)
First structural scaffold for the taxonomy root. Slang supplies the equivalence classes players reason in
(mtg.fandom.com/wiki/List_of_Magic_slang); "removal" is the flagship — a broad class no single CR term names.
The supergroups sit on the event axis + the resource axis (O10) plus a clock axis, and the **object axis
(subject + derived categories, O11) crosses through them** — a full leaf is roughly
`<side> : <supergroup> : <object-subject> : <qualifiers(destination/manner/scope)> : <quantity>`.

**A · Object zone-transitions (events on objects):**
1. **Removal** — an object in play (battlefield OR stack) will no longer be in play. Subsumes by
   **destination** (`→graveyard` = *dies* / `→exile` / `→hand` = *bounce* / `→library` / *off-stack* =
   *countered*) and **manner** (*sacrificed*, *destroyed*, *combat*, *state-based*). **This tree IS O1**:
   `removal:creature:…:to-graveyard:sacrificed` = Chatterfang's sac; bare `removal:creature` (LTB) /
   `…:to-graveyard` (dies) match it by glob. Sacrifice⊂dies⊂LTB is a slice of Removal.
2. **Deployment** — an object enters play (→battlefield/stack). Subsumes cast, ETB, token-creation,
   reanimation/recursion, blink-return, copy. Removal's dual (flicker = Removal→Deployment; recursion =
   removal-then-redeployment).
3. **Modification** — an object altered in place (stays in play): P/T (modify/set/switch), keyword/ability
   grants, type/color/control change. (Slang: pump, anthems.)

**B · Resources (fungible pools):**
4. **Mana** (ramp/fixing/rituals) · 5. **Cards** (draw/dig/tutor/mill/discard — the hidden hand/library/
   graveyard resource) · 6. **Life** (gain/loss/drain; **damage** resolves here as life/toughness loss) ·
   7. **Counters** (+1/+1, proliferate, energy).

**C · Structure (the clock):**
8. **Structure** — phases, steps, extra turns, extra combats, untap, priority (tempo/untappers/time-walk).

**Known straddles (edges, to resolve — not blockers):** *Damage* is a Life verb whose *lethality* is a
derived edge into Removal, not its own supergroup. *Counters* straddle Modification (a +1/+1 counter changes
P/T) and Resources (proliferate/energy pool) — likely own supergroup + a modification edge. *Sacrifice-as-cost*
= a Removal emit that also decrements the object pool (the O2 duality restated). **Open naming:** Deployment
vs Arrival/Development; Structure vs Sequencing/Tempo.

### O13 — Removal needs a source-zone facet; zone-change (from→to) is the primitive
Removal's first cut ("an object in play will no longer be in play," O12) is battlefield/stack-anchored, but
graveyard hate removes an object from a NON-play zone — **Soul-Guide Lantern** ("When this artifact enters,
exile target card from a graveyard") is `removal:card:from-graveyard:to-exile`. Generalize: a Removal leaf
carries an explicit **from-zone** facet — `removal:<object>:from-<zone>:to-<zone>:<manner>:<scope>` — covering
battlefield→graveyard (dies), battlefield→exile, battlefield→hand (bounce), graveyard→exile (gy hate),
hand→graveyard (discard), library→graveyard (mill), stack→graveyard (countered). Deeper: **zone-change
(from → to) is the primitive event**; Removal and Deployment (O12) are slang supergroups anchored on the
battlefield/stack endpoint — Removal = battlefield/stack is the FROM, Deployment = it's the TO — and
non-play-endpoint zone-changes (gy hate, discard, mill) are Removal in the broad "answer/disrupt" sense.
**Open:** do Removal/Deployment stay separate supergroups sharing a from/to facet pair, or collapse into one
Zone-change supergroup with the slang as derived views?

### O14 — `:` is is-a taxonomy; attributes are an unordered qualification set (not colon-nested)
ADR 0002 §3 flattens everything into one ordered colon-chain (`role:subject:[destination]:[scope]:[exclusion]`),
conflating genuine hierarchy with orthogonal filters and forcing a canonical order + glob to match subsets.
Split them:
- **Taxonomy stem (`:`, ordered, genuine is-a):** `<side>:<supergroup>:<card-type>` — e.g. `emit:removal:creature`,
  `emit:deployment:creature`. Card TYPE (permanent ⊃ creature/artifact/…) is the is-a spine; a bare
  `removal:permanent` really is a prefix of `removal:creature`. Keyword grants may nest here where is-a holds
  (`modification:evasion:forestwalk`; forestwalk ⊂ landwalk ⊂ evasion).
- **Attributes (an unordered qualification SET on the leaf):** `subtype`, `control`, `from-zone`, `to-zone`,
  `manner`, `p/t`, `color`, `token`, `qty`, … ORTHOGONAL axes, not deeper taxonomy. A Squirrel is-a creature in
  the *general* tree, but in a *port* "creature" is the type and "squirrel" filters *which* creature, alongside
  "controlled." Notate as a set — `emit:removal:creature[subtype=squirrel, control=you, from=battlefield,
  to=graveyard, manner=sacrificed]` — never `removal:creature:squirrel:…:controlled`.
- **Matching = attribute-subset, order-free:** an emit satisfies a consume iff the consume's constraints ⊆ the
  emit's attributes (Blood Artist `consume:removal:creature[]` matches `emit:removal:creature[subtype=squirrel,
  manner=sacrificed]`). Cleaner and more robust than ADR 0002's positional glob.
- **Derived categories (O11) are named attribute-constraints, NOT stem nodes:** `fodder` := `[p/t=1/1]` (or
  `[toughness=1]`). This also **corrects the O13 table**: the sac consumes ANY controlled Squirrel, so its
  filter is `[subtype=squirrel, control=you]` with *no* fodder constraint; `fodder` is an attribute of the
  *emitted* tokens (and of what Skullclamp *wants*), not a requirement of the sac.

### O15 — Prior art: the design is a typed-feature-structure system; stay in the tractable profile
Research pass (2026-07-16) against three literatures, to catch down-the-road issues:

1. **Typed feature structures / unification grammars (HPSG).** O14's design — a type hierarchy (the is-a
   stem) + attribute-value pairs, matched by subsumption — IS the TFS formalism: types ordered by
   information content, attributes inherited down the hierarchy, matching = unification/subsumption. Two
   TFS lessons to adopt: **(a) appropriateness conditions** — each stem type licenses a declared set of
   attribute keys (`removal:*` licenses `from/to/manner`; `mana` licenses `color/qty`; nonsense like
   `mana[toughness=1]` is ill-typed) — this is the machine-readable schema O8's doc-generator should emit;
   **(b) absent-attribute semantics** — an unspecified attribute means *unknown/any*, and matching against
   it is an OPEN-world judgment, not a failed lookup. (The operator's Subsumes/Intersects/Overlaps trilean
   already implements exactly this — e.g. the Squirrel⊄creature straddle floors to AMBER.)
2. **Description logics.** Subsumption in EL-family logics (conjunction + existential only) is
   **polynomial**; adding general boolean operators (ALC) makes it **EXPTIME**. Our matching — conjunctive
   attribute-subset + per-value lattices, no disjunction/negation in queries — sits in the tractable EL
   profile. **Guardrail:** keep it there. The one existing negation (`:another` / exclude-self) is a bounded
   exception to confine; resist general boolean attribute constraints in the query language.
3. **Faceted classification.** Ranganathan's **Colon Classification** (1933!) is the ancestor of the
   colon-label idea, and its core finding is O14's split: rigid enumerative hierarchies pigeonhole subjects;
   orthogonal facets compose. ADR-0002's single ordered chain was drifting enumerative; O14's stem+facets is
   the analytico-synthetic correction.

**Answer to the prefix-capture question:** yes — by construction, once capture is defined per-facet, not as
string prefix. `query(Q) captures emit(E)` iff (stem of Q is an ancestor-or-equal of stem of E in the is-a
lattice) ∧ (each attribute constraint in Q subsumes E's value in that attribute's own value lattice —
`control: self ⊆ controlled ⊆ any`, zones, the TypeOntology for subtypes; unconstrained = captures all).
So `removal:creature` implicitly captures every deeper stem and every attribute combination — `removal:
creature:*` for free — and ADR-0002's positional `**` glob (needed only because attributes sat inside the
ordered chain) is retired. Unknown-vs-constrained mismatches resolve by trilean, not boolean.

### O16 — The AST already carries the feature structure; the label string is the lossy step
Mesh check against current code: the O14/O15 model is *closer* to the existing machinery than ADR-0002's
strings are. `ObjectFilter` (Subtypes, Controller, IsToken, …) **is** the attribute set;
`ObjectFilterRelations.Subsumes/Intersects` over the `TypeOntology` **is** the per-facet lattice matching with
trilean open-world semantics; `PortNode` already carries the filter as its `Subject`. The lossy step is
`PortLabel` **flattening** the filter into an ordered colon-string (`LiftCardTypes`, facet joins) and then
*re-matching on the string* with globs — projecting structure down to text and parsing it back. Structural
changes the redesign implies:
1. **Match on the structured port, not the string.** PortNode grows explicit stem + attribute fields (subject
   filter, from/to zone, manner, qty binding); `FlowFeasible`/the engine's arms become instances of ONE generic
   `captures(Q,E)` (O15) plus the §8 accounting — the per-arm switch shrinks to the genuinely cross-resource
   bridges (O6).
2. **The colon-label becomes a serialization/display format** (and the dump/query surface), generated FROM the
   structure — never the matching substrate. Collision-lint and determinism guarantees transfer unchanged.
3. **Appropriateness schema as data** (O15): a single machine-readable taxonomy file — stems, licensed
   attribute keys per stem, per-attribute value lattices, derived-category aliases (O11), slang supergroup
   views (O12) — consumed by the projection, the engine, the doc generator (O8), AND the frontend (which
   already derives canonicality live from the API; this replaces the last hardcoded frontend duplicate).
4. **Dumps/API**: CardPortRow gains structured attributes (jsonb, like spans) so atlas-web matches by
   attribute-subset instead of family-string equality — fixing the column-precision problems (Aang) at the
   root.
5. **Migration posture**: projection + engine first behind the existing gold-fixture gates (labels regenerate
   deterministically from structure), resource-graph/dumps regen after, frontend last. The AST itself needs
   little: filters/zones/quantities are already typed fields; the main parser asks are the O4 manner facet and
   per-cost spans (already logged).

---

## The triggering case (worked, for reference)

Chatterfang, Squirrel General — full-text labeling under O10–O14 (stem `side:supergroup:card-type` +
attribute set `[…]`; labels illustrative pending final facet names):

| Oracle text | Side | Stem | Attributes |
|---|---|---|---|
| `Forestwalk (…reminder…)` | emit | `modification:evasion:forestwalk` | `[subject=self]` — inert; reminder text = no port |
| `If one or more tokens would be created under your control,` | intercept | `deployment` | `[token, control=you]` — the replaced creation event |
| `those tokens plus that many 1/1 green Squirrel creature tokens are created instead.` | emit | `deployment:creature` | `[token, subtype=squirrel, color=green, p/t=1/1, control=you, qty=that-many]` (p/t=1/1 ⇒ **fodder**) |
| `{B},` | consume | `mana` | `[color=black, qty=1]` |
| `Sacrifice X Squirrels` (dual, O2) | consume | `creature` | `[subtype=squirrel, control=you, qty=X]` — fodder pool decrement (**§8**; **no** fodder attr — any Squirrel) |
| `Sacrifice X Squirrels` | emit | `removal:creature` | `[subtype=squirrel, control=you, from=battlefield, to=graveyard, manner=sacrificed, qty=X]` |
| `Target creature gets +X/-X until end of turn.` | emit | `modification:creature` | `[stat=p/t, delta=+X/-X, target=any, duration=eot, qty=X]` — −X toughness ⇒ **lethal edge** → derived Removal (cf. Skullclamp) |

Match check: Blood Artist / Pitiless (`consume:removal:creature[]`, any creature death) satisfy the sac emit
because their `[]` ⊆ its attributes; the spurious `emit:cast` rows (Aang) never arise. `X` binds across the sac
cost, the removal qty, and the +X/−X magnitude (§8); `that-many` binds the doubler emit to the intercept.

## Open questions (to resolve before the Decision)
- **O5 fork:** fodder-consume family = `creature` or `token`? (Needs the family/role decoupling first.)
- **O3 scope:** how event-centric to go — reframe all costs as consume+event-emit, or only the LTB family?
- **O6 audit:** which curated bridges become subsumption rungs; which are genuinely cross-resource.
- **O9:** collapse `sacrifice`/`death`/`etb`/`recur` into a zone-change axis, or keep flat families?
- **Migration:** every sacrifice/death gold fixture + the resource graph regenerate; staging plan TBD.

## Provenance
- interaction-judge taxonomy verdict, 2026-07-16 (6/6 PASS, PROCEED with: keep fodder consume; re-root
  emit under `ltb` with a `manner` facet; `to-graveyard` stays over-approximate). CR: 701.21a, 700.4,
  603.6, 603.10a, 614.6, 111.1/205.3m.
- Driven by the atlas-web Card Explorer flow-adjacency work (see the atlas-diag / Chatterfang session).
