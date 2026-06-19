namespace MagicAST.Interaction;

/// <summary>
/// The declared "projected" discriminator sets for <see cref="PortWalk"/> — the discriminators that
/// receive a SEMANTIC projection (a specific port label a flow rule can read), as opposed to the
/// coarse totality fallback (<c>emit:&lt;x&gt;</c> / <c>pay:&lt;x&gt;</c> / a coarse trigger role) that
/// guarantees a port exists but which no flow rule consumes — zero recall (alignment initiative 03 #2).
///
/// This is the single source of truth the exhaustiveness ratchet
/// (<c>PortWalkExhaustivenessTests</c>) checks every AST discriminator against: each must be projected
/// here or carry a justified entry in <c>known-coarse-projections.json</c>.
///
/// KEEP IN SYNC with the dispatch in <see cref="PortGraph"/>:
///   - <see cref="EffectTypes"/>      ↔ PortWalk.Effects / PortWalk.EmitPort switch cases
///   - <see cref="CostTypes"/>        ↔ PortWalk.Costs switch cases
///   - <see cref="TriggerEvents"/>    ↔ PortWalk.Trigger event branches
///   - <see cref="GatingRestrictions"/> is consumed directly by PortGraphEngine restriction gating.
///
/// Stage 2 of initiative 03 (typed projection from the AST records via exhaustive switch expressions)
/// will make this compile-time-exhaustive and retire the hand-declared sets; until then the ratchet
/// is the stopgap that forces every new discriminator through a conscious projection decision.
/// </summary>
public static class PortWalkProjection
{
  /// <summary><c>EffectType</c> discriminators with a semantic projection (not the <c>emit:&lt;x&gt;</c> fallback).</summary>
  public static readonly IReadOnlySet<string> EffectTypes = new HashSet<string>(StringComparer.Ordinal)
  {
    "replacement", // PortWalk.Effects — intercept + inner emit (CR 614)
    "copy", // PortWalk.EmitPort — emit:copy (permanent token-copy, copy-inheritance graft) OR emit:copy:spell (CR 707.10 stack spell-copy, Target.Zone:Stack), Subject = copy target filter
    "createToken", // PortWalk.EmitPort — emit:token:<spec>
    "addMana", // PortWalk.EmitPort — emit:mana:<color>
    "putCounters", // PortWalk.EmitPort — emit:counter:<type>:<scope>
    "untap", // PortWalk.EmitPort — emit:untap[:self]
    "modifyPT", // PortWalk.EmitPort — modify:pt (inert, but an explicit stable label)
    "evasion", // PortWalk.EmitPort — evasion:<keyword> (inert, explicit)
    "gainLife", // PortWalk.EmitPort — emit:life:gain:<scope> (life flow arm)
    "loseLife", // PortWalk.EmitPort — emit:life:loss:<scope> (life flow arm)
    "returnToHand", // PortWalk.EmitPort — emit:returntohand:spell (instant/sorcery from graveyard → recast, spell-recast arm) OR coarse emit:returntohand (a bounce, no arm reads)
    "alternativeCast", // PortWalk.EmitPort — emit:returntobattlefield:self + pay:mana recast co-cost (aristocrat recursion, FromZone:Graveyard)
    "returnToHand", // PortWalk.EmitPort — emit:cast (self-bounce → recast a noncreature spell, Displacer Kitten cast-trigger arm) + pay:mana recast co-cost. Only Target:Self bounces project the cast emit; a non-self return stays coarse.
    "optional", // PortWalk.Effects — "you may" wrapper: blink detection (gated emit:blink) + gated recursion into Inner (blink flow arm)
    "composite", // PortWalk.Effects — blink detection (exile+returnToBattlefield(ExiledWith:Self) → emit:blink) + recursion into Effects (blink flow arm)
    "rollDie", // PortWalk.EmitPort — emit:rolldice:<scope> (dice flow arm), Subject = the rolling player (controller)
    "dealDamage", // PortWalk.EmitPort — emit:damage:<combat>:<recipient> (damage flow arm), Subject = the damage source; IsCombat → combat facet
    "conditional", // PortWalk.Effects — recurse Then/Else as GATED inner ports (CR 603 in-ability gate → §8 Amber floor); the gated damage emit that closes Captain Rex's Crash Land self-loop
    "becomesPermanent", // PortWalk.Effects — recurse GainedAbilities (the granted Crash Land triggered ability) as own port units; the grant emit itself is the coarse inert emit:becomespermanent
  };

  /// <summary><c>CostType</c> discriminators with a semantic projection (not the <c>pay:&lt;x&gt;</c> fallback).</summary>
  public static readonly IReadOnlySet<string> CostTypes = new HashSet<string>(StringComparer.Ordinal)
  {
    "sacrifice", // PortWalk.Costs — sac:<fodder>:controlled
    "mana", // PortWalk.Costs — pay:mana:<color> per symbol
    "tap", // PortWalk.Costs — tap:self
  };

  /// <summary>Trigger <c>Event</c> values with a semantic projection (not the coarse-role fallback).
  /// Structured phase events (<c>at:&lt;part&gt;</c>) are handled separately and are not enumerable here.</summary>
  public static readonly IReadOnlySet<string> TriggerEvents = new HashSet<string>(StringComparer.Ordinal)
  {
    "Dies", // PortWalk.Trigger — DeathTrigger label
    "Enters", // PortWalk.Trigger — EntersTrigger label
    "GainsLife", // PortWalk.Trigger — trigger:life:gain:<scope> (life flow arm)
    "LosesLife", // PortWalk.Trigger — trigger:life:loss:<scope> (life flow arm)
    "SpellCast", // PortWalk.Trigger — trigger:cast:<spell>:<scope> (Displacer Kitten cast-trigger arm), Subject = the watched (noncreature) spell filter
    "DiceRolled", // PortWalk.Trigger — trigger:rolldice:<scope> (dice flow arm), Subject = the rolling player (controller)
    // Damage flow arm — trigger:damage:<combat>:<recipient>, Subject = the watched SOURCE (the source-perspective
    // "[source] deals [combat] damage [to recipient]" triggers). Combat facet: combat (CR 510) / noncombat / any (CR 120).
    "DealsDamage", // trigger:damage:any:any — fires on any damage (Captain Rex's Crash Land)
    "DealsCombatDamage", // trigger:damage:combat:any
    "DealsCombatDamageToPlayer", // trigger:damage:combat:player
    "DealsCombatDamageToPlayerOrPlaneswalker", // trigger:damage:combat:playerorpw
    "DealsCombatDamageToCreature", // trigger:damage:combat:creature
    "NoncombatDamageDealt", // trigger:damage:noncombat:any
    "DealsDamageToOpponent", // trigger:damage:any:opponent
    "DealsDamageToOpponents", // trigger:damage:any:opponent
  };

  /// <summary>Restriction values treated as HARD firability gates (ADR-0002 §8). Everything else is a
  /// deliberate non-gate (timing restrictions don't block an intra-turn loop) and must be justified in
  /// the allowlist. Consumed directly by <see cref="PortGraphEngine"/>.</summary>
  public static readonly IReadOnlySet<string> GatingRestrictions = new HashSet<string>(StringComparer.Ordinal)
  {
    "OnlyOnceEachTurn",
    "Conditional",
    "OnlyIfNoUntappedLands",
    // CR 702.177a: Exhaust abilities can be activated "only once" (permanently, not per-turn).
    // Stricter than OnlyOnceEachTurn: an exhaust ability is permanently locked after first use.
    "OnlyOnce",
  };
}
