# Gold oracle-text fidelity cleanup — TDD-loop worklist

_Generated 2026-06-05. Status snapshot; the loop updates it as families close._


## What this is

An audit (the `GoldOracleTextFidelityTests` smoke test) found **105 / 950** `HandParsedCards` golds whose
`Input.OracleText` does not match the corpus the parser consumes (`card-inputs.json`, a faithful
projection of Scryfall — ground-truthed against live Scryfall). The corpus is trustworthy; the **golds**
drift. ~30 were an entirely different card; the rest are templating drift, reminder-text stripping, dropped
"another", or partial (subset) text. Critically, a chunk were authored from text the parser *can* handle
while the real card has constructs it *can't* — **false parser coverage** sourced from the golds (cf.
`project_mast_triage_false_coverage`).

All 105 are on `Fixtures/oracle-text-quarantine.json` (the smoke test's shrinking ratchet) so the invariant
is green now and no *new* gold can drift. This worklist burns the quarantine down.

## Methodology (per card)

1. **Bootstrap**: `GoldRegenerationUtility` (the `[Explicit]` maintenance test) lifts the real `Input` from
   the corpus and regenerates a *draft* `Output` from the current parser. Self-consistent by construction.
2. **Judge** the draft AST against the Comprehensive Rules (`mast-judge`). The judge sees what parser tests
   can't: semantic inversion, dropped abilities, lossy `Characteristic.Other` free-text.
3. **PASS** → keep, de-quarantine, commit. (Snapshot ≡ hand-authored when the parser is correct.)
   **FAIL** → the card is a red gold driving a **parser fix**; close the parser gap, re-bootstrap, re-judge,
   then commit. This is the TDD loop — the snapshot just bootstraps the red.

A 12-card judge sample already ran: 9 PASS (3 with the deferred `another→ExcludeSelf` gap noted), 3 FAIL
(GoblinRabblemaster `mustAttack`→Self **inversion**; ArmadilloCloak `Characteristic.Other("enchanted")`
trigger-subject; KariZev dropped legendary supertype + entry-state + delayed self-exile on its token).

## Known parser-fix families (drive these as TDD slices)

- **another-ExcludeSelf** — trigger parser drops "another" (no `ExcludeSelf`). Entangled with interaction
  fix #5 (the operator floors `Subsumes` on `sup.ExcludeSelf`, so the engine needs a cross-card firability
  carve-out or the canonical combos demote). Do the parser + engine together.
- **mustAttack-restriction** — "Other X you control attack" mis-parses to `Target:Self` (inversion). Audit
  the whole `mustAttack`/`mustBlock` restriction class.
- **aura-enchanted** — "enchanted creature" trigger-subject falls back to `Characteristic.Other`; reuse the
  `EnchantedOrEquipped` reference (cf. `project_mast_keyword_trigger_bypass`, `project_mast_migration_debt`).
- **complex-token** — created-token sub-structure (supertype, entry-state, delayed triggers) dropped.
- **`[GAP]` families** — `ParsedAbilities < TotalAbilities`: the parser produces no node for ≥1 ability
  (replacement effects, some keyword mechanics/actions, each-player sacrifice). Genuine parser extensions.

## Families (105 cards)


### another-ExcludeSelf — 7 card(s) · 1 parse-GAP

- `NPH/SuturePriest` — Suture Priest (1/2) **[GAP]**
- `ALA/Deathgreeter` — Deathgreeter (1/1)
- `AVR/Wingcrafter` — Wingcrafter (2/2) · OtherCondition
- `BFZ/ZulaportCutthroat` — Zulaport Cutthroat (1/1)
- `LCI/WarrenSoultrader` — Warren Soultrader (1/1) · TokenDefinition.AbilityText
- `PLC/EssenceWarden` — Essence Warden (1/1)
- `RIX/PitilessPlunderer` — Pitiless Plunderer (1/1) · TokenDefinition.AbilityText

### mustAttack-restriction — 1 card(s)

- `M15/GoblinRabblemaster` — Goblin Rabblemaster (3/3)

### aura-enchanted — 5 card(s) · 2 parse-GAP

- `FDN/FindThePath` — Find the Path (2/3) **[GAP]**
- `ROE/BearUmbra` — Bear Umbra (2/3) **[GAP]**
- `INV/ArmadilloCloak` — Armadillo Cloak (3/3) · OtherCharacteristic
- `TSP/AspectOfMongoose` — Aspect of Mongoose (3/3)
- `ZEN/SpreadingSeas` — Spreading Seas (3/3)

### complex-token — 1 card(s)

- `AER/KariZevSkyshipRaider` — Kari Zev, Skyship Raider (3/3)

### replacement-effect — 1 card(s) · 1 parse-GAP

- `CLB/LaezelVlaakithsChampion` — Lae'zel, Vlaakith's Champion (1/2) **[GAP]**

### each-player — 4 card(s) · 3 parse-GAP

- `MRD/BarterInBlood` — Barter in Blood (0/1) **[GAP]**
- `ODY/InnocentBlood` — Innocent Blood (0/1) **[GAP]**
- `RitesOfFlourishing` — Rites of Flourishing (0/2) **[GAP]**
- `CLB/ExquisiteHuntmaster` — Exquisite Huntmaster (2/2)

### keyword-mechanic — 17 card(s) · 2 parse-GAP

- `DFT/PointTheWay` — Point the Way (1/2) **[GAP]**
- `FDN/TheForetoldSoldier` — The Foretold Soldier (3/4) **[GAP]**
- `DMU/ArgivianPhalanx` — Argivian Phalanx (2/2)
- `FRF/SavingGrasp` — Saving Grasp (2/2) · OtherCharacteristic
- `FUT/KeldonHalberdier` — Keldon Halberdier (2/2)
- `IKO/SeaDasherOctopus` — Sea-Dasher Octopus (3/3)
- `ISD/CacklingCounterpart` — Cackling Counterpart (2/2)
- `MRD/ArcboundWorker` — Arcbound Worker (2/2)
- `ONS/BarrenMoor` — Barren Moor (3/3)
- `ONS/ForgottenCave` — Forgotten Cave (3/3)
- `ONS/TranquilThicket` — Tranquil Thicket (3/3)
- `RNA/SpikewheelAcrobat` — Spikewheel Acrobat (1/1) · OtherCondition
- `RTR/RakdosDrake` — Rakdos Drake (3/3) · OtherCondition
- `TOR/DirtyWererat` — Dirty Wererat (2/2)
- `TSP/DurkwoodBaloth` — Durkwood Baloth (1/1)
- `UDS/AvalancheRiders` — Avalanche Riders (3/3) · OtherCondition
- `UDS/Rebuild` — Rebuild (2/2)

### keyword-action — 8 card(s) · 2 parse-GAP

- `BLB/WildcallSpree` — Wildcall (0/1) **[GAP]**
- `KTK/AbzanSkycaptain` — Abzan Skycaptain (1/2) **[GAP]**
- `GRN/DimirInformant` — Dimir Informant (1/1)
- `GRN/WatcherInTheMist` — Watcher in the Mist (2/2)
- `GRN/WhisperAgent` — Whisper Agent (2/2)
- `PTK/GluttonousCyclops` — Gluttonous Cyclops (1/1)
- `THS/GodsWilling` — Gods Willing (2/2)
- `WOE/CandyTrail` — Candy Trail (2/2)

### land-mana — 6 card(s) · 1 parse-GAP

- `LRW/WindbriskHeights` — Windbrisk Heights (3/4) **[GAP]**
- `JMP/ThrivingIsle` — Thriving Isle (2/2)
- `MID/HauntedRidge` — Haunted Ridge (2/2)
- `RAV/AzoriusChancery` — Azorius Chancery (3/3)
- `RAV/BorosGarrison` — Boros Garrison (3/3)
- `RAV/SelesnayaSanctuary` — Selesnya Sanctuary (3/3)

### predefined-token-reminder — 2 card(s)

- `NEO/CrackOpen` — Crack Open (1/1) · TokenDefinition.AbilityText
- `XLN/RuthlessKnave` — Ruthless Knave (2/2) · TokenDefinition.AbilityText

### misc-clean — 53 card(s) · 17 parse-GAP

- `9ED/SpiketailDrake` — Spiketail Drake (1/2) **[GAP]**
- `BLB/EmberheartChallenger` — Emberheart Challenger (2/3) **[GAP]**
- `BOK/WheelAndDeal` — Wheel and Deal (1/2) **[GAP]**
- `DSK/ScreamingSwarm` — Screaming Swarm (1/3) **[GAP]**
- `DST/HoverguardSweepers` — Hoverguard Sweepers (1/2) **[GAP]**
- `Divert` — Divert (0/1) **[GAP]**
- `FIN/BusterSword` — Buster Sword (2/3) **[GAP]**
- `GRN/SilhanaWayfinder` — Silhana Wayfinder (0/1) **[GAP]**
- `KHM/TailSwipe` — Tail Swipe (0/1) **[GAP]**
- `M10/GoblinChieftain` — Goblin Chieftain (1/2) **[GAP]**
- `M13/LeylineOfTheVoid` — Leyline of the Void (1/2) **[GAP]**
- `NEO/VilespawnSpider` — Vilespawn Spider (2/3) **[GAP]**
- `OGW/StoneforgeMasterwork` — Stoneforge Masterwork (1/2) **[GAP]**
- `SOM/PrecursorGolem` — Precursor Golem (1/2) **[GAP]**
- `VIS/Necrosavant` — Necrosavant (0/1) **[GAP]**
- `WTH/Aboroth` — Aboroth (0/1) **[GAP]**
- `ZEN/GuardGomazoa` — Guard Gomazoa (2/3) **[GAP]**
- `AER/Disallow` — Disallow (1/1)
- `AKH/AnointedProcession` — Anointed Procession (1/1)
- `AVR/BloodflowConnoisseur` — Bloodflow Connoisseur (1/1)
- `AggressiveMammoth` — Aggressive Mammoth (2/2) · OtherCharacteristic
- `BFZ/ClutchOfCurrents` — Clutch of Currents (2/2)
- `BFZ/CoastalDiscovery` — Coastal Discovery (2/2)
- `CHK/KeiganTheTideStar` — Keiga, the Tide Star (2/2)
- `DGM/UnflinchingCourage` — Unflinching Courage (2/2)
- `DGM/WearTear` — Wear // Tear (4/4)
- `DKA/NiblisOfTheUrn` — Niblis of the Urn (2/2)
- `DTK/OjutaisBreath` — Ojutai's Breath (2/2)
- `ELD/CorridorMonitor` — Corridor Monitor (1/1)
- `GPT/NivMizzet` — Niv-Mizzet, the Firemind (3/3)
- `JUD/AnuridMurkdiver` — Anurid Murkdiver (1/1)
- `JUD/PhantomNantuko` — Phantom Nantuko (4/4)
- `JUD/PhantomTiger` — Phantom Tiger (2/2)
- `LRW/ElvishHarbinger` — Elvish Harbinger (2/2)
- `M10/ActOfTreason` — Act of Treason (1/1)
- `M11/AetherAdept` — Aether Adept (1/1)
- `M14/ToweringIndrik` — Towering Indrik (1/1)
- `M21/CrystalSeer` — Crystal Seer (2/2)
- `MBS/GoblinWardriver` — Goblin Wardriver (1/1) · OtherCharacteristic
- `MIR/BurningShieldAskari` — Burning Shield Askari (2/2) · OtherCharacteristic
- `MOM/BackupAgent` — Backup Agent (1/1)
- `MRD/LeoninDenGuard` — Leonin Den-Guard (1/1) · OtherCondition
- `MindRake` — Mind Rake (2/2)
- `NEO/SpiritedCompanion` — Spirited Companion (1/1)
- `ONE/JawboneDuelist` — Jawbone Duelist (2/2)
- `ONE/SerumCoreChimera` — Serum-Core Chimera (3/3) · OtherCharacteristic
- `ONS/Stifle` — Stifle (1/1)
- `Pacifism` — Pacifism (2/2)
- `Peek` — Peek (2/2)
- `SNC/FleetfootDancer` — Fleetfoot Dancer (3/3)
- `SOI/NiblisOfTheMist` — Niblis of the Mist (2/2)
- `TMP/DiabolicEdict` — Diabolic Edict (1/1)
- `WAR/SpellgorgerWeird` — Spellgorger Weird (1/1) · OtherCharacteristic

_Total: 105 cards._
