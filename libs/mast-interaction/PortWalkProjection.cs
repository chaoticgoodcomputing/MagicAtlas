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
    "createToken", // PortWalk.EmitPort — emit:token:<spec>
    "addMana", // PortWalk.EmitPort — emit:mana:<color>
    "putCounters", // PortWalk.EmitPort — emit:counter:<type>:<scope>
    "untap", // PortWalk.EmitPort — emit:untap[:self]
    "modifyPT", // PortWalk.EmitPort — modify:pt (inert, but an explicit stable label)
    "evasion", // PortWalk.EmitPort — evasion:<keyword> (inert, explicit)
    "gainLife", // PortWalk.EmitPort — emit:life:gain:<scope> (life flow arm)
    "loseLife", // PortWalk.EmitPort — emit:life:loss:<scope> (life flow arm)
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
  };

  /// <summary>Restriction values treated as HARD firability gates (ADR-0002 §8). Everything else is a
  /// deliberate non-gate (timing restrictions don't block an intra-turn loop) and must be justified in
  /// the allowlist. Consumed directly by <see cref="PortGraphEngine"/>.</summary>
  public static readonly IReadOnlySet<string> GatingRestrictions = new HashSet<string>(StringComparer.Ordinal)
  {
    "OnlyOnceEachTurn",
    "Conditional",
    "OnlyIfNoUntappedLands",
  };
}
