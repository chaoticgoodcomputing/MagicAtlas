# Keyword decomposition audit — comprehensive ADR-0003 enforcement

**Status:** planned (2026-05-31). Tracking doc, not an ADR — the doctrine is
[ADR 0003](../../libs/magic-ast/docs/adr/0003-keywords-decompose-into-shared-primitives.md)
(keywords expand into their true rules decomposition over shared primitives); this
round *enforces* it across all 160 keyword definitions.

## Why

Firebending shipped as `StaticAbility{FirebendingEffect{Value:2}}` — wrong ability
category + opaque marker — and was only caught by accident. An audit shows it is not
a one-off: **~90 of 160 keywords still expand to opaque markers** that hide their
mechanical content from clustering. Two failure shapes:

1. **Bespoke `*Effect{Value/Cost}` markers** (~60) under `AST/Effects/Keyword/` —
   `BushidoEffect{Value}`, `AfterlifeEffect{Value}`, `FlashbackEffect{Cost}`,
   `SuspendEffect{N,Cost}`, etc. Same shape as the deleted `FirebendingEffect`.
2. **`KeywordAbilityEffect`-collapsed complex keywords** (~30) — Cascade, Storm,
   Convoke, Persist, Undying, Evolve, Exploit, Extort, Rebound… were folded into the
   evergreen marker by ADR 0006, but they are triggered/cost abilities that ADR 0003
   says must decompose.

## Test approach — keyword-expansion golds, "sans examples"

New harness (batch 0): `tests/magic-ast-tests/Data/KeywordExpansions/{Keyword}.json`,
each `{ "Keyword", "Parameter"?, "Expected": <decomposed Ability subtree> }`. The test
parses the canonical printed form (e.g. `"Bushido 2"`, `"Flashback {3}{R}"`) through
the keyword's `Combinator` (and, where present, `Definition.CreateExpansion`) and
asserts the produced `Ability` equals `Expected`. **No example card** — the keyword's
glossary/CR/reminder definition is the spec. One gold per keyword; all 160 get one, so
the suite proves *every* keyword expands correctly and the next silent
under-decomposition fails a test. Authored from the **glossary + CR + reminder text**.

Doctrine per keyword: ADR 0003 cluster-value test — decompose a clause iff it creates
a cluster axis a consumer would query. Engine execution stays implicit (zone
bookkeeping, mana-emptying, legality, layering). A keyword that genuinely needs a new
shared `Effect` trait or primitive is a **HITL stop**, not a silent invention.

CR numbers below are pointers from the glossary; workers pull verbatim CR text per the
Step-2 briefing discipline (numbers have proven stale before — e.g. Reconfigure).

---

## Bucket A — Atomic; LOCK as-is (gold = current shape). ~55

These are genuinely irreducible. Their current expansion is the correct subtree.

- **Evasion → `EvasionEffect`:** Flying, Fear, Intimidate, Menace, Shadow, Skulk,
  Horsemanship, Forestwalk, Islandwalk, Mountainwalk, Plainswalk, Swampwalk.
- **Combat-damage timing → `CombatDamageTimingEffect`:** First Strike, Double Strike.
- **Semantic single-effect:** Lifelink (`LifelinkEffect`), Protection (`ProtectionEffect`),
  Daybound/Nightbound (`DayNightEffect`), Flash (`TimingModificationEffect`), Toxic
  (`ToxicEffect{N}` — combat-damage poison rider; marginal, keep).
- **`KeywordAbilityEffect` markers (truly atomic):** Deathtouch, Trample, Vigilance,
  Haste, Reach, Hexproof, Shroud, Indestructible, Defender, Changeling, Devoid, Infect,
  Wither, Banding, Phasing, Split Second, Prowess?, Myriad?, Ascend?, Battle? — verify
  the marginal ones during batch 0 (some may move to Bucket C).
- **Deck-construction / pre-game (no in-game subtree):** Partner, Partner With, Choose a
  Background, Doctor's Companion, Friends Forever-style, Companion-ish. Lock as marker.

## Bucket B — Already decomposed; LOCK (gold = current shape). 9

Equip, Cycling, Echo, Firebending, Bestow, Kicker, Reconfigure, Flash, Affinity.

---

## Bucket C — Decompose targets (~90), grouped into batches by shape-family

One keyword per worker (the ADR-0003 dispatch model). Each batch shares a target shape
so the parser surface is consolidated.

### Batch 1 — Alternative-cast cost (cast from a zone/condition for a stated cost → `AlternativeCost` + zone/condition rider)
| Keyword | CR | Target subtree |
|---|---|---|
| Flashback | 702.34 | `AlternativeCost{Cost, FromZone:Graveyard}`; exile-after is engine |
| Madness | 702.35 | `AlternativeCost{Cost, FromZone:Exile, Condition:discarded}` |
| Mayhem | 702.x | `AlternativeCost{Graveyard, this-turn}` (verify CR) |
| Escape | 702.138 | `AlternativeCost{Graveyard}` + `AdditionalCost{exile N from gy}` |
| Aftermath | 702.127 | `AlternativeCost{Graveyard, this half only}` |
| Disturb | 702.146 | `AlternativeCost{Graveyard, transformed}` |
| Jump-start | 702.x | `AlternativeCost{Graveyard}` + `AdditionalCost{discard a card}` |
| Retrace | 702.x | `AlternativeCost{Graveyard}` + `AdditionalCost{discard a land}` |

### Batch 2 — Conditional alternative cost (alt cost gated on a game condition / with a rider)
| Keyword | CR | Target subtree |
|---|---|---|
| Surge | 702.x | `AlternativeCost{Condition: you/teammate cast a spell}` |
| Spectacle | 702.x | `AlternativeCost{Condition: opponent lost life}` |
| Freerunning | 702.x | `AlternativeCost{Condition: combat dmg by Assassin/commander}` |
| Evoke | 702.x | `AlternativeCost` + `TriggeredAbility{enters → SacrificeEffect(Self)}` |
| Dash | 702.x | `AlternativeCost` + haste + `TriggeredAbility{EOT → return to hand}` |
| Blitz | 702.x | `AlternativeCost` + haste + dies-draw + EOT-sacrifice triggers |
| Miracle | 702.x | `AlternativeCost{first card drawn}` + reveal trigger |
| Warp | 702.x | `AlternativeCost` + exile rider (verify CR — new mechanic) |

### Batch 3 — Additional-cast cost (extra cost on cast → `AdditionalCost`)
| Keyword | CR | Target subtree |
|---|---|---|
| Multikicker | 702.33 | `AdditionalCost{IsOptional, repeatable}` |
| Replicate | 702.x | `AdditionalCost{repeatable}` + copy-on-resolve trigger |
| Buyback | 702.x | `AdditionalCost{IsOptional}` + return-instead replacement |
| Entwine | 702.x | `AdditionalCost{choose all modes}` |
| Escalate | 702.x | `AdditionalCost{per extra mode}` |
| Splice | 702.x | `AdditionalCost{reveal + pay to add text}` |
| Conspire | 702.78 | `AdditionalCost{tap 2 sharing-color creatures}` + copy |
| Squad | 702.x | `AdditionalCost{repeatable}` + ETB token copies |

### Batch 4 — Alternative payment (tap/exile-to-pay → likely a shared `AlternativePayment` primitive; HITL if new trait needed)
| Keyword | CR | Target subtree |
|---|---|---|
| Convoke | 702.51 | tap creatures to pay (alt-pay) |
| Improvise | 702.126 | tap artifacts to pay |
| Delve | 702.66 | exile cards from graveyard to pay {1} each |
| Assist | 702.x | another player may pay |
| Emerge | 702.x | `AlternativeCost` reduced by sacrificed creature's MV |

### Batch 5 — Exile-based delayed cast (ADR-0004 exile primitives + later cast)
| Keyword | CR | Target subtree |
|---|---|---|
| Suspend | 702.62 | exile w/ N time counters + upkeep remove-counter trigger + cast-free-when-last-removed + haste |
| Foretell | 702.143 | `{2}` exile face down + later `ActivatedAbility{cast for foretell cost}` |
| Plot | 702.170 | pay+exile + later cast free (sorcery, not the turn plotted) |
| Rebound | 702.88 | cast → exile-instead-of-gy + next-upkeep delayed cast-free |

### Batch 6 — Activated from graveyard / token-copy (exile-from-gy → effect)
| Keyword | CR | Target subtree |
|---|---|---|
| Unearth | 702.x | `ActivatedAbility{from gy}: return + haste + exile EOT/on-leave` |
| Embalm | 702.128 | `ActivatedAbility{cost, exile from gy}: create white token copy (no mana cost)` |
| Eternalize | 702.129 | `ActivatedAbility{cost, exile from gy}: create 4/4 black token copy` |
| Encore | 702.141 | `ActivatedAbility{cost, exile from gy}: token copies attacking each opponent` |
| Scavenge | 702.x | `ActivatedAbility{cost, exile from gy, sorcery}: distribute N +1/+1 counters` |
| Recover | 702.x | `TriggeredAbility{another creature dies → return this from gy / else exile}` |

### Batch 7 — Activated-ability keywords (→ `ActivatedAbility`)
| Keyword | CR | Target subtree |
|---|---|---|
| Crew | 702.122 | `ActivatedAbility{tap creatures total power ≥ N: becomes artifact creature}` |
| Saddle | 702.x | `ActivatedAbility{tap creatures total power ≥ N: saddled until EOT}` |
| Outlast | 702.107 | `ActivatedAbility{cost,{T}, sorcery: +1/+1 counter}` |
| Monstrosity | 701.37 | `ActivatedAbility{cost: N +1/+1 counters, become monstrous}` |
| Adapt | 701.46 | `ActivatedAbility{cost: if no +1/+1 counters, put N}` |
| Transmute | 702.49 | `ActivatedAbility{cost, discard this, sorcery: search same-MV card}` |

### Batch 8 — Dies-triggered (→ `TriggeredAbility{dies}` + effect)
| Keyword | CR | Target subtree |
|---|---|---|
| Afterlife | 702.135 | dies → `createToken` N×(1/1 W/B Spirit, flying) |
| Persist | 702.79 | dies w/o -1/-1 counter → return with a -1/-1 counter |
| Undying | 702.93 | dies w/o +1/+1 counter → return with a +1/+1 counter |
| Soulshift | 702.46 | dies → may return target Spirit MV ≤ N from gy to hand |
| Modular | 702.43 | ETB with N +1/+1 counters; dies → move counters to target artifact creature |
| Vanishing | 702.x | ETB w/ N time counters; upkeep remove; last → sacrifice (timer) |
| Fading | 702.x | ETB w/ N fade counters; upkeep remove; none → sacrifice |
| Cumulative Upkeep | 702.24 | upkeep → age counter + `PreventableEffect{sacrifice unless pay cost×counters}` |

### Batch 9 — ETB-triggered (→ `TriggeredAbility{enters}`)
| Keyword | CR | Target subtree |
|---|---|---|
| Fabricate | 702.x | ETB → modal(`N +1/+1 counters` OR `N 1/1 Servo tokens`) |
| Devour | 702.x | ETB → may sacrifice creatures; enters with counters per |
| Bloodthirst | 702.x | ETB with N +1/+1 counters if an opponent lost life this turn |
| Graft | 702.x | ETB with N +1/+1 counters; another ETB → may move a counter |
| Amplify | 702.x | ETB → reveal, +1/+1 counter per revealed sharing type |
| Tribute | 702.x | ETB → opponent chooses (N +1/+1 counters) or (trigger) |
| Champion | 702.x | ETB → exile a creature you control; leaves → return it |
| Sunburst | 702.x | ETB with a counter per color of mana spent |
| Living Weapon | 702.x | ETB → create 0/0 Germ token + attach (Equip subtree) |
| Riot / Unleash | 702.x | ETB → choose haste-or-counter / may enter with +1/+1 (can't block) |

### Batch 10 — Combat-triggered (→ `TriggeredAbility{attacks/blocks/deals combat damage}`)
| Keyword | CR | Target subtree |
|---|---|---|
| Bushido | 702.45 | blocks or becomes blocked → `modifyPT +N/+N` until EOT |
| Battle Cry | 702.x | attacks → other attackers get +1/+0 until EOT |
| Mentor | 702.134 | attacks → +1/+1 counter on a lesser-power attacker |
| Exalted | 702.83 | a creature attacks alone → that creature +1/+1 until EOT |
| Flanking | 702.25 | becomes blocked by non-flanker → blocker -1/-1 until EOT |
| Melee | 702.121 | attacks → +1/+1 until EOT per opponent attacked |
| Afflict | 702.x | becomes blocked → defending player loses N life |
| Provoke | 702.39 | attacks → may untap + force a creature to block |
| Ingest | 702.115 | combat dmg to player → exile top card of their library |
| Dethrone | 702.x | attacks player with most life → +1/+1 counter |
| Renown | 702.x | combat dmg to player → becomes renowned (N +1/+1 counters) |
| Training | 702.x | attacks with a greater-power ally → +1/+1 counter |
| Myriad | 702.116 | attacks → token copies attacking each other opponent |

### Batch 11 — Cast-triggered (→ `TriggeredAbility{cast}`)
| Keyword | CR | Target subtree |
|---|---|---|
| Cascade | 702.85 | cast → exile from top until lesser-MV nonland, `CastWithoutPaying` it |
| Storm | 702.40 | cast → copy this spell per spell cast before it this turn |
| Extort | 702.101 | cast a spell → may pay {W/B}, each opp loses 1 / you gain |
| Cipher | 702.99 | resolves → exile encoded on a creature; its combat dmg → cast copy |
| Replicate dup? | — | (in Batch 3) |

### Batch 12 — Face-down cast (morph family → `AlternativeCost`(face-down 2/2) + `ActivatedAbility`(turn face up))
| Keyword | CR | Target subtree |
|---|---|---|
| Morph | 702.37 | cast face down as 2/2 for {3}; `ActivatedAbility{morph cost: turn face up}` |
| Megamorph | 702.37 | morph + enters w/ +1/+1 counter when turned up for megamorph cost |
| Disguise | 702.168 | morph + ward {2} while face down |

### Misc / HITL review (special structures; may keep bespoke node or need a new primitive)
Haunt (702.55, exile-link), Mutate (merge), Soulbond (pairing), Hideaway, Prototype
(alt cost + alt characteristics), Awaken (kicker + animate land), Backup (ETB grant +
counters), Splice (cross-references), Harmonize, Mobilize, WebSlinging, Prepared,
Job Select, Start Your Engines, Spree, Bargain, For Mirrodin (Living-Weapon variant),
Totem/Umbra Armor (totem armor = `PreventableEffect`-ish replacement). Triage each
against the cluster-value test before assigning a batch; escalate genuine
trait-boundary cases.

---

## Batch sequence
0. Expansion-gold harness + author all 160 golds (lock Buckets A & B; Bucket C golds
   are the failing specs).
1–12. Decompose Bucket C by family (above), one keyword per worker, judge novel-shape
   branches, NUnit-gate per merge group. Each keyword owns its own `*Keyword.cs` (+ any
   new rule file) — disjoint, so they parallelize; the deleted `*Effect` records are
   each owned by one worker.

## Progress
- [ ] Batch 0 — harness + 160 golds
- [ ] Batches 1–12 — see tables
- [ ] Delete dead `*Effect` markers; regenerate GLOSSARY each batch
- [ ] Final: every keyword has a passing expansion gold; no opaque keyword markers remain
