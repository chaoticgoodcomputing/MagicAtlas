namespace MagicAST.Interaction;

using MagicAST.AST.Costs;
using MagicAST.AST.References;

/// <summary>
/// ADR-0002 §1–3: the deterministic projection of an AST sub-tree onto its canonical colon-label
/// (the <em>leaf</em>). Pure and total — same sub-tree → same label, no heuristics. The query view
/// is this projection's prefix-preimage (ADR-0002 §2): a port matches every prefix of its leaf.
///
/// <para>Canonical facet order is <c>role : subject : [destination] : [scope] : [exclusion]</c>
/// (ADR-0002 §3), with absent facets dropped (so shorter labels stay valid prefixes). This is the
/// S1 POC: it covers the two <em>consume</em>-port roles the Chatterfang × Pitiless gold needs — a
/// "dies" trigger (Pitiless) and a sacrifice cost (Chatterfang). Subsequent vertical slices add the
/// emit/<c>replace</c> roles, the resource-kind axis (§3b), the type-ontology subject-lift
/// (Squirrel ⊂ creature), and quantities (§8).</para>
/// </summary>
public static class PortLabel
{
  /// <summary>
  /// ADR-0002 §2 — the <b>wildcard query operator</b> over a colon-label, generalising the
  /// prefix-preimage to glob wildcards on the <c>:</c>-delimited facets: <c>*</c> matches exactly one
  /// segment, <c>**</c> matches zero or more. A bare prefix query is the special case
  /// <c>&lt;prefix&gt;:**</c>. Deterministic and total. Examples: <c>emit:token:**:controlled</c>
  /// matches every controlled token-emit regardless of subject arity (with or without a subtype);
  /// <c>ltb:**:to-graveyard:**</c> matches any dies-trigger; <c>sac:*:controlled</c> matches only a
  /// single-segment-subject sacrifice (so <c>sac:creature:controlled</c> ✓, <c>sac:artifact:treasure:controlled</c> ✗).
  /// </summary>
  public static bool Matches(string pattern, string label) =>
    Glob(pattern.Split(':'), 0, label.Split(':'), 0);

  /// <summary>Glob match over facet segments: <c>*</c> = one segment, <c>**</c> = zero or more.</summary>
  private static bool Glob(string[] p, int i, string[] l, int j)
  {
    while (i < p.Length)
    {
      if (p[i] == "**")
      {
        for (var k = j; k <= l.Length; k++) // ** absorbs zero..all remaining segments
          if (Glob(p, i + 1, l, k))
            return true;
        return false;
      }
      if (j >= l.Length)
        return false;
      if (p[i] != "*" && !string.Equals(p[i], l[j], StringComparison.OrdinalIgnoreCase))
        return false;
      i++;
      j++;
    }
    return j == l.Length;
  }

  /// <summary>
  /// The subject facet — the object a port acts on, card-type first then subtype (ADR-0002 §3a).
  /// <c>{creature}</c> → <c>"creature"</c>; <c>{creature}+{Squirrel}</c> → <c>"creature:squirrel"</c>.
  /// A subtype-only filter is <b>lifted</b> through the same vendored <see cref="TypeOntology"/> the
  /// operator reads: <c>{Squirrel}</c> → <c>"creature:squirrel"</c> (the kindred card-type is dropped
  /// as a non-permanent — a port acts on a permanent). The lift is a <em>coarse over-approximation</em>
  /// for matching; the operator independently decides the precise <c>Squirrel ⊄ creature</c> straddle
  /// (ADR-0002 §6/§7). <c>null</c> when the filter names no type (and no subtype resolves).
  /// </summary>
  public static string? Subject(ObjectFilter f, TypeOntology ontology)
  {
    var cardTypes = Canon(f.CardTypes) ?? LiftCardTypes(f.Subtypes, ontology);
    var subtypes = Canon(f.Subtypes);
    return (cardTypes, subtypes) switch
    {
      (null, null) => null,
      (not null, null) => cardTypes,
      (null, not null) => subtypes,
      _ => $"{cardTypes}:{subtypes}",
    };
  }

  /// <summary>
  /// Lift a subtype-only fodder filter to its <b>permanent</b> card-type(s) via the ontology
  /// (ADR-0002 §3a): <c>Squirrel</c> → <c>creature</c> (its <c>kindred</c> membership is filtered
  /// out — kindred is not a permanent type, and a sac/death port acts on a permanent). <c>null</c>
  /// if no subtype resolves to a permanent type.
  /// </summary>
  private static string? LiftCardTypes(IReadOnlyList<string>? subtypes, TypeOntology ontology)
  {
    var lifted = LiftPermanentCardTypeList(subtypes, ontology);
    return lifted is null ? null : string.Join("+", lifted);
  }

  /// <summary>
  /// The list form of <see cref="LiftCardTypes"/> — a subtype's permanent (non-kindred) owner card
  /// type(s), for callers that need the <see cref="ObjectFilter.CardTypes"/> list itself rather than
  /// the joined label facet (e.g. <c>PortGraph</c>'s sacrifice-cost Subject: CR 701.21a guarantees a
  /// sac cost's fodder is a permanent already, so a bare <c>Subtypes:["Squirrel"]</c> filter with no
  /// explicit <c>CardTypes</c> can safely carry its lifted permanent type — the operator's general
  /// <c>Subsumes</c> stays conservative for a context-free filter (the Squirrel⊄creature straddle is a
  /// real ambiguity there), but a sac-cost object is never that ambiguous). <c>null</c> if no subtype
  /// resolves to a permanent type.
  /// </summary>
  internal static IReadOnlyList<string>? LiftPermanentCardTypeList(
    IReadOnlyList<string>? subtypes,
    TypeOntology ontology
  )
  {
    if (subtypes is null || subtypes.Count == 0)
      return null;
    var permanents = ontology.PermanentTypes.ToHashSet(StringComparer.OrdinalIgnoreCase);
    var lifted = subtypes
      .SelectMany(s =>
        ontology.SubtypeToCardTypes.TryGetValue(s, out var cardTypes)
          ? cardTypes
          : Enumerable.Empty<string>()
      )
      .Where(permanents.Contains)
      .Select(c => c.ToLowerInvariant())
      .Distinct()
      .OrderBy(c => c, StringComparer.Ordinal)
      .ToList();
    return lifted.Count == 0 ? null : lifted;
  }

  /// <summary>
  /// The scope facet — the controller axis (ADR-0002 §3): <c>You</c> → <c>"controlled"</c>,
  /// <c>Opponent</c> → <c>"opponent"</c>, <c>Any</c>/unmarked → <c>null</c> (the broadest prefix).
  /// Ownership is the orthogonal axis — <c>Owner = You</c> → <c>"owned"</c> (never conflated with
  /// control, CR 108.3 vs 108.4).
  /// </summary>
  public static string? Scope(ObjectFilter f) =>
    f.IsSelf == true ? "self" // "this creature" — the source itself (ADR-0002 §3/§6); narrowest scope
    : ScopeToken(f.Controller) ?? (f.Owner == ControllerFilter.You ? "owned" : null);

  /// <summary>The control-axis token (shared by object filters and replacement events).</summary>
  private static string? ScopeToken(ControllerFilter? controller) =>
    controller switch
    {
      ControllerFilter.You => "controlled",
      ControllerFilter.Opponent => "opponent",
      _ => null,
    };

  /// <summary>The exclude-self qualifier — the CR "another" (ADR-0002 §3); the self-scope counterpart
  /// (<c>this creature</c> → <c>self</c>) is blocked on the parser self-binding (ADR-0002 §6).</summary>
  public static string? Exclusion(ObjectFilter f) => f.ExcludeSelf == true ? "another" : null;

  /// <summary>
  /// A "dies" trigger — leaves-the-battlefield to graveyard (CR 700.4), the destination carried as a
  /// qualifier of <c>ltb</c> so <c>ltb:…:to-graveyard ⊆ ltb:…</c> (ADR-0002 §3).
  /// </summary>
  public static string DeathTrigger(ObjectFilter dying, TypeOntology ontology) =>
    Join("ltb", Subject(dying, ontology), "to-graveyard", Scope(dying), Exclusion(dying));

  /// <summary>An "enters the battlefield" trigger (CR 603.6a) — consumes an entering object.</summary>
  public static string EntersTrigger(ObjectFilter entering, TypeOntology ontology) =>
    Join("etb", Subject(entering, ontology), Scope(entering), Exclusion(entering));

  /// <summary>
  /// A cast-from-graveyard permission (CR 601.3e — Gravecrawler's <c>alternativeCast</c>/
  /// <c>FromZone:Graveyard</c>) projected as the existing <c>emit:returntobattlefield</c> label so the
  /// §8-B one-shot-self-removal carve-out (which keys on that label for Persist/Undying) retains the
  /// self-death recursion cycle. The scope is <c>self</c> — the card itself leaves the graveyard and
  /// re-enters the battlefield (a new object, CR 400.7, but the same card identity that refuels the sac).
  /// The gating condition rides as the port Subject so the operator tiers the recast (aristocrat-recursion-scope §2a).
  /// </summary>
  public static string ReturnToBattlefieldEmit() => "emit:returntobattlefield:self";

  /// <summary>
  /// A <b>blink</b> (flicker) effect — "exile [target] permanent, then return that card to the
  /// battlefield" (the linked exile-then-return, <c>ExiledWith:Self</c>). The returned object is a NEW
  /// object (CR 603.6e / 400.7): it RE-ENTERS (its ETB retriggers) and re-enters UNTAPPED (discharging a
  /// tap gate). Both flow consequences ride one label — the engine's blink arm connects it to an
  /// <c>etb</c> consume (refueling an ETB-driven loop) and to a <c>tap:self</c> renewal (the dual of an
  /// untap, copy-inheritance Decision 4). The blinked permanent's filter rides as the port Subject
  /// (NON-NULL, never a scalar null-default GREEN — adding-a-flow-arm anti-pattern 3); the operator tiers
  /// the re-entry/renewal on it ("the label names, the operator decides", ADR-0002 §7). The scope facet
  /// (<c>self</c> when the card blinks ITSELF, else absent — a target permanent) distinguishes a
  /// self-blink (Cloudshift on itself, n/a here) from blinking another permanent.
  /// </summary>
  public static string BlinkEmit(ObjectFilter blinked, TypeOntology ontology) =>
    Join("emit", "blink", Subject(blinked, ontology), blinked.IsSelf == true ? "self" : null);

  /// <summary>
  /// A <b>spell-recursion</b> emit — "return target instant or sorcery card from your graveyard to your
  /// hand" (Archaeomancer / Izzet Chronarch / Mnemonic Wall's ETB; Snapback, Call to the Netherworld for
  /// other-zone variants). The returned instant/sorcery goes to HAND, where it can be <b>recast</b> (CR
  /// 601.2 — "to cast a spell is to take it from where it is, usually the hand"). That recast re-fires the
  /// spell's effects, so this emit refuels a <c>cast:spell</c> consume (the spell-recast flow arm): the
  /// spell-recursion → recast → spell-effect loop the Ghostly Flicker × Archaeomancer / Izzet Chronarch
  /// combos turn on. Distinct from a battlefield <b>bounce</b> (Boomerang's "return target permanent to
  /// hand" — a creature/permanent to its owner's hand): a bounce projects the coarse <see cref="ReturnToHandEmit"/>
  /// no arm reads, because returning a permanent to hand re-CASTS a creature/permanent (a re-entry, not a
  /// spell-effect re-fire) — only an instant/sorcery returned to a castable zone is a spell-recast enabler.
  /// The returned-card filter (the instant/sorcery the operator tiers the recast on) rides as the port
  /// Subject (NON-NULL — never the scalar null-default GREEN, adding-a-flow-arm anti-pattern 3).
  /// </summary>
  public static string SpellRecursionEmit(ObjectFilter returned, TypeOntology ontology) =>
    Join("emit", "returntohand", "spell", Subject(returned, ontology));

  /// <summary>The coarse <c>returnToHand</c> emit for a non-spell-recursion bounce (a permanent to hand) —
  /// an explicit, stable label no flow arm reads (a bounce is not a spell-recast). Carries the bounced
  /// filter as the Subject when present (faithful; never a fudged label, anti-pattern 3).</summary>
  public static string ReturnToHandEmit(ObjectFilter? bounced, TypeOntology ontology) =>
    bounced is null ? "emit:returntohand" : Join("emit", "returntohand", Subject(bounced, ontology));

  /// <summary>
  /// A <b>cast</b> consume — an instant/sorcery spell being cast (CR 601.2). A <c>Kind:spell</c> ability is
  /// the spell's on-cast effect; casting it re-fires those effects. The cast consume is what a spell-recast
  /// refuels: a <see cref="SpellRecursionEmit"/> returns the instant/sorcery to hand, and casting it from
  /// hand drives the spell's effects again (the card-defined consume→emit edges). The spell's own
  /// self-type — an instant or sorcery on the stack — rides as the port Subject (NON-NULL; the operator
  /// tiers the recast on it, against the returned-card filter). The card type isn't threaded into the walk,
  /// so the Subject is the broadest faithful spell type, <c>{instant, sorcery}</c> self — never a fudged
  /// label or a scalar null-default GREEN (adding-a-flow-arm anti-pattern 3).
  /// </summary>
  public static string CastConsume() => "cast:spell:self";

  /// <summary>
  /// A <b>spell-copy</b> effect — "copy target [instant or sorcery] spell" (Dualcaster Mage's ETB,
  /// Reiterate, Narset's Reversal). CR 707.10: a copy of a spell is put on the stack and <em>isn't
  /// cast</em>; it reproduces the copied spell's characteristics, modes, targets, and X. This is a
  /// DIFFERENT resource from a token-copy of a permanent (<see cref="CreateTokenEmit"/> / the
  /// <c>emit:copy</c> permanent path the copy-inheritance graft reads): the copied object lives on the
  /// STACK, not the battlefield, so it carries no ETB/untap to graft onto the copier's permanent loop.
  /// The label carries a distinct <c>:spell</c> resource facet so the copy-inheritance permanent graft
  /// (which keys on the bare <c>emit:copy</c>) cleanly ignores it — a spell-copy must never be grafted
  /// as a permanent. The copied-spell filter rides as the port Subject (NON-NULL, the
  /// instant/sorcery-on-stack discriminator the operator would tier a future spell-copy arm on; never a
  /// scalar null-default GREEN — adding-a-flow-arm anti-pattern 3).
  ///
  /// <para><b>Consumed by the <c>("copy","cast")</c> arm</b> (LANDED 2026-06-18, interaction-judge
  /// PROCEED — <c>PortGraphEngine.SpellCopyReFiresEffects</c>): a spell-copy <em>reproduces the copied
  /// spell's effects</em> (CR 707.10), so it feeds a type-compatible spell's <c>cast:spell:self</c>
  /// effect-driver — NOT a <c>trigger:cast</c> (a copy isn't cast, so it can't feed "whenever you cast"),
  /// which the arm's Role≠trigger keying makes structurally impossible. Reconstructs Dualcaster × Ghostly
  /// Flicker / × Cackling Counterpart, Narset's Reversal × Reiterate (all Missed→Amber) and the
  /// Storm-Kiln × Reverberate × Pair o' Dice Lost dice combo. Honest AMBER via the IsSelf Subsumes=No
  /// floor (the copied "instant or sorcery" only Intersects the recast spell's self-type).</para>
  /// </summary>
  public static string SpellCopyEmit(ObjectFilter spell, TypeOntology ontology) =>
    Join("emit", "copy", "spell", Subject(spell, ontology));

  // --- Casting a spell as a flowing event (CR 601 / 603.2). -----------------------------------
  // A "whenever you cast a [noncreature] spell" TRIGGER is a consume; a RE-CAST of a bounced-to-hand
  // spell (a noncreature permanent that returns ITSELF to hand and is recast — Displacer Kitten ×
  // Mourning/Conviction) is the matching emit. The flow arm (PortFlowMatcher) connects an
  // emit:cast of a spell to a trigger:cast whose watched-spell filter is type-compatible. The
  // discriminating spell filter — the card-type axis ("a spell", "a NONcreature spell" via the trigger's
  // ExcludedCardTypes) — rides as the port Subject so the OPERATOR tiers the connection (ADR-0002 §7: the
  // label names the broad role, the operator decides certainty). The label carries only the coarse
  // subject/scope; the negation (`!creature`) lives in the Subject, never the label.

  /// <summary>A "whenever you cast a [noncreature] spell" trigger (CR 603.2) — consumes a spell-cast
  /// event. The watched-spell filter rides as the NON-NULL port Subject (the `!creature` exclusion the
  /// operator tiers on); never a scalar null-default GREEN (adding-a-flow-arm anti-pattern 3).</summary>
  public static string CastTrigger(ObjectFilter spell, TypeOntology ontology) =>
    Join("trigger", "cast", Subject(spell, ontology) ?? "spell", Scope(spell), Exclusion(spell));

  /// <summary>
  /// A RE-CAST of a spell (CR 601) — the matching emit a <see cref="CastTrigger"/> consumes. A
  /// noncreature permanent that returns ITSELF to hand (a self-bounce, <c>returnToHand</c> of
  /// <c>Self</c>) becomes a card in hand that can be cast again as a spell — and that cast genuinely fires
  /// a "whenever you cast a spell" trigger (unlike a spell-COPY, which CR 707.10 makes uncast; see
  /// <see cref="SpellCopyEmit"/>). The recast carries the card's OWN mana cost as a <c>pay:mana</c>
  /// co-cost (attached at the projection, mirroring the aristocrat recast — <see cref="ReturnToBattlefieldEmit"/>),
  /// so the §8 mana balance floors a loop whose recast mana the loop can't itself refill (Displacer Kitten ×
  /// Mourning: the {1}{B} recast is paid by lands Peregrine Drake untaps — an enabler outside the
  /// reconstructed loop — so the loop is mana-unbalanced and tiers AMBER, never a fudged GREEN). The
  /// recast-spell filter (the card cast again — broadest "a spell" when the card type is not threaded into
  /// the walk) rides as the NON-NULL port Subject; the operator tiers the cast↔trigger type-compatibility.
  /// </summary>
  public static string CastEmit(ObjectFilter spell, TypeOntology ontology) =>
    Join("emit", "cast", Subject(spell, ontology) ?? "spell");

  // --- Life as a flowing resource (CR 119). ---------------------------------------------------
  // A life-gain/loss EFFECT is an emit; a "whenever [a player] gains/loses life" TRIGGER is a consume.
  // The flow arm (PortFlowMatcher) connects same-direction pairs; the PLAYER axis — who
  // gains/loses vs whom the trigger watches — rides as the port Subject so the operator tiers it
  // (You↔You is GREEN; "a player" ⊋ "an opponent" is a sound AMBER, ADR-0002 §3/§7). The label carries
  // only the scope name; the operator, not the label, decides certainty. The <c>who</c> filter is the
  // affected/watched player (You → controlled, Opponent → opponent, an unqualified target player → no
  // scope — the broadest, which is what floors the loss hop to AMBER until the parse layer sharpens it).
  public static string LifeGainEmit(ObjectFilter? who) => Join("emit", "life", "gain", who is null ? null : Scope(who));

  public static string LifeLossEmit(ObjectFilter? who) => Join("emit", "life", "loss", who is null ? null : Scope(who));

  public static string LifeGainTrigger(ObjectFilter? who) => Join("trigger", "life", "gain", who is null ? null : Scope(who));

  public static string LifeLossTrigger(ObjectFilter? who) => Join("trigger", "life", "loss", who is null ? null : Scope(who));

  // --- Die rolls as a flowing resource (CR 706). -----------------------------------------------
  // A "roll [N] dice" EFFECT is an emit; a "whenever you roll one or more dice" TRIGGER is a consume.
  // The flow arm (PortFlowMatcher) connects emit→trigger so a self-feeding roll engine
  // closes (roll → trigger → effect → … → roll). The ROLLING PLAYER (the controller — "you roll" watches
  // YOUR rolls) rides as the port Subject so the operator tiers it (You↔You is GREEN). Player-scoped,
  // never a null-default-GREEN scalar.
  public static string RollDiceEmit(ObjectFilter? who) => Join("emit", "rolldice", who is null ? null : Scope(who));

  public static string RollDiceTrigger(ObjectFilter? who) => Join("trigger", "rolldice", who is null ? null : Scope(who));

  // --- Additional combat phases as a loop driver (CR 500.8). -----------------------------------
  // An "additional combat phase" EFFECT (Aggravated Assault, Breath of Fury, Combat Celebrant) is an emit
  // that lets a creature attack AGAIN. The extra-combat arm connects it to an attacksorblocks consume (a
  // creature's re-attack opportunity), re-driving combat damage to close an infinite-combat loop — with an
  // attack-roll creature's roll as the offshoot. The combat-damage emit stays Gated, so the loop floors to
  // AMBER (never a false GREEN; the turn's free first combat is the seed outside the cycle).
  public static string AdditionalCombatEmit() => "emit:additionalcombat";

  // "This creature attacks/blocks" as a consume on a creature's combat presence. Role "attacksorblocks"
  // matches the coarse attack-trigger role the AttacksOrBlocks trigger event projects, so ONE extra-combat
  // arm satisfies both this (re-driving combat damage) and a card's own "whenever this attacks" roll trigger.
  public static string AttacksConsume() => "attacksorblocks:self";

  // --- Damage as a flowing resource (CR 119/120 general, CR 510 combat). -----------------------
  // A "deals N damage" EFFECT is an emit; a "whenever [a source] deals [combat] damage [to X]" TRIGGER is
  // a consume. The flow arm (PortFlowMatcher) connects an emit to a trigger whose COMBAT facet
  // is compatible (a non-combat emit feeds a general "deals damage" trigger and a non-combat trigger, but
  // NEVER a combat-specific trigger — the combat-vs-noncombat soundness, CR 510 vs 120) and whose RECIPIENT
  // class is compatible (a player-recipient emit can't feed a "deals combat damage to a CREATURE" trigger).
  // The discriminating SOURCE (who deals the damage — "this Vehicle", "a creature you control") rides as the
  // port Subject so the operator tiers it (ADR-0002 §7); a self-watching trigger ("whenever THIS deals
  // damage") is matched same-card-only by the arm (the operator can't see object identity). Both facets are
  // ALWAYS emitted (never dropped) so the index-based facet read in the engine is stable. NON-NULL Subject.

  /// <summary>The combat facet of a damage label: <c>combat</c> (CR 510), <c>noncombat</c> (CR 120),
  /// or <c>any</c> (a bare "deals damage" trigger — fires on either). A constant vocabulary so the
  /// engine's facet read is total.</summary>
  public const string DamageCombat = "combat";
  public const string DamageNoncombat = "noncombat";
  public const string DamageAnyKind = "any";

  /// <summary>A "deals N damage to [recipient]" EFFECT emit (CR 119/120). The <paramref name="combatFacet"/>
  /// is <see cref="DamageCombat"/> when the effect's damage is combat damage, else <see cref="DamageNoncombat"/>
  /// (the default — explicit damage effects are non-combat). <paramref name="recipient"/> is the recipient
  /// class facet. The SOURCE rides as the NON-NULL port Subject (never the scalar null-default GREEN —
  /// adding-a-flow-arm anti-pattern 3).</summary>
  public static string DealDamageEmit(string combatFacet, string recipient) =>
    Join("emit", "damage", combatFacet, recipient);

  /// <summary>A "whenever [source] deals [combat] damage [to recipient]" TRIGGER consume (CR 120 general /
  /// CR 510 combat). <paramref name="combatFacet"/> is <see cref="DamageCombat"/> (a <c>DealsCombatDamage*</c>
  /// trigger), <see cref="DamageNoncombat"/> (a <c>NoncombatDamageDealt</c> trigger), or
  /// <see cref="DamageAnyKind"/> (a bare <c>DealsDamage</c> trigger — fires on either). The watched SOURCE
  /// rides as the NON-NULL port Subject.</summary>
  public static string DamageTrigger(string combatFacet, string recipient) =>
    Join("trigger", "damage", combatFacet, recipient);

  /// <summary>
  /// A sacrifice cost. CR 701.21a: a player only sacrifices a permanent they control, so an unscoped
  /// fodder filter floors to <c>controlled</c> (the rules-invariant lives here, not in the parse).
  /// </summary>
  public static string SacrificeCost(ObjectFilter fodder, TypeOntology ontology) =>
    Join("sac", Subject(fodder, ontology), Scope(fodder) ?? "controlled", Exclusion(fodder));

  /// <summary>
  /// ADR-0003 §5 — the death EVENT a sacrifice cost raises (CR 701.21a: sacrificing moves the fodder from
  /// the battlefield directly to its owner's graveyard). The dual EMIT of the sac cost's fodder consume
  /// (O2/O10 — one clause, two ports): its subject is the fodder, its narrowest rung is
  /// <c>removal:creature[to=graveyard, manner=sacrificed]</c>, which a dies (<c>to=graveyard</c>), bare LTB,
  /// or "when sacrificed" (<c>manner=sacrificed</c>) consume all capture by subsumption — retiring the
  /// curated <c>sac→dies</c> bridge. The fodder carries the same <c>controlled</c> guarantee the sac cost
  /// already floors it to, so <see cref="Scope"/> round-trips with <c>SacrificeCost</c>.
  /// </summary>
  public static string SacrificeDeathEmit(ObjectFilter fodder, TypeOntology ontology) =>
    Join("emit", "removal", Subject(fodder, ontology), "to-graveyard", "sacrificed", Scope(fodder), Exclusion(fodder));

  /// <summary>
  /// A mana cost (CR 118) → per-requirement consume resources with their quantities. Each colored
  /// symbol is a <c>pay:mana:&lt;color&gt;</c> requirement (grouped + counted), generic symbols sum
  /// into a color-less <c>pay:mana</c>, and <c>{C}</c> is <c>pay:mana:colorless</c>. <b><c>{0}</c>
  /// projects <c>pay:mana</c> quantity 0</b> — the zero-magnitude activation cost a cost-modifier
  /// attaches to (ADR-0002 §4/§6, the totality principle), never dropped. Quantity rides beside the
  /// label (§8), not in it. Exotic symbols (hybrid, variable <c>{X}</c>, snow) are a later slice.
  /// </summary>
  public static IReadOnlyList<(string Label, int Quantity)> PayMana(IReadOnlyList<ManaSymbol> symbols)
  {
    var requirements = new List<(string Label, int Quantity)>();

    // Generic: one `pay:mana` requirement summing every generic symbol — emitted even at 0 ({0}).
    var generic = symbols.Where(s => s.Kind == ManaSymbolKind.Generic).ToList();
    if (generic.Count > 0)
      requirements.Add(("pay:mana", generic.Sum(s => s.GenericAmount ?? 0)));

    // Colorless {C}: its own requirement.
    var colorless = symbols.Count(s => s.Kind == ManaSymbolKind.Colorless);
    if (colorless > 0)
      requirements.Add(("pay:mana:colorless", colorless));

    // Colored: one requirement per color, counting the symbols of that color (deterministic order).
    foreach (
      var group in symbols
        .Where(s => s.Kind == ManaSymbolKind.Colored && s.Colors is { Count: > 0 })
        .SelectMany(s => s.Colors!)
        .GroupBy(c => c)
        .OrderBy(g => g.Key)
    )
      requirements.Add(($"pay:mana:{group.Key.ToString().ToLowerInvariant()}", group.Count()));

    return requirements;
  }

  /// <summary>
  /// An emit port for a <c>createToken</c> effect (ADR-0002 §3b): the resource-kind axis carries
  /// <c>token</c>, the token's object-type is the subject (via the same lift), and the creator's
  /// control is the scope (CR 111.2 — a token's creator controls it). A 1/1 Squirrel you create →
  /// <c>emit:token:creature:squirrel:controlled</c>.
  /// </summary>
  public static string CreateTokenEmit(ObjectFilter token, TypeOntology ontology) =>
    Join("emit", Kind(ResourceKind.Token), Subject(token, ontology), Scope(token));

  /// <summary>
  /// An emit port for a mana-producing effect (ADR-0002 §3b): the resource-kind axis carries a
  /// <em>scalar</em> resource with no object subject, the color as its qualifier —
  /// <c>emit:mana:black</c>, or <c>emit:mana:any</c> for producer-chosen any-color mana. This is the
  /// axis the card-type <see cref="Subject"/> facet cannot express; it rides beside it, not within it.
  /// </summary>
  public static string ManaEmit(string color) => Join("emit", Kind(ResourceKind.Mana), color.ToLowerInvariant());

  /// <summary>
  /// A replacement effect (CR 614) — the <c>replace</c> role over the event it intercepts (ADR-0002
  /// §3). Anointed Procession's "if one or more tokens would be created under your control … instead" →
  /// <c>replace:token-creation:controlled</c>; Bruvac the Grandiloquent's "if an OPPONENT would mill …"
  /// → <c>replace:mill:opponent</c>.
  ///
  /// <para>The intercept scope is carried whenever the parsed event provides a controller, and
  /// <c>PortGraph</c> now reads it (ADR-0004 §6, the widened-attribute fix). CR 614.1 replaces a
  /// SPECIFIC event, so a scoped event must project a scoped intercept: dropping the facet modelled
  /// these cards as replacing anyone's event, which for Bruvac meant doubling the controller's OWN mill
  /// — flatly unsupported by the printed text, and a false GREEN in any self-mill loop.</para>
  ///
  /// <para><b>Remaining gap (parse-side, not here):</b> Chatterfang, Squirrel General prints the same
  /// "under your control" as its four siblings, but <c>TokenAugmentationReplacementRule</c> alone omits
  /// <c>Controller</c> from the <c>tokenCreation</c> event it builds, so Chatterfang still projects
  /// <b>unscoped</b>. That is a parser-fidelity gap (ADR-0002 §10), not a projection one — the moment
  /// the rule states the facet, the port is born correct with no change here.</para>
  /// </summary>
  public static string Replacement(string replacedEventType, ControllerFilter? eventController = null) =>
    Join("replace", ReplacedEvent(replacedEventType), ScopeToken(eventController));

  /// <summary>The replaced-event subject token (ADR-0002 §3).</summary>
  private static string ReplacedEvent(string eventType) =>
    eventType switch
    {
      "tokenCreation" => "token-creation",
      _ => eventType.ToLowerInvariant(),
    };

  /// <summary>The resource-kind facet (ADR-0002 §3b) — the flowing resource, lifted from <see cref="ResourceKind"/>.</summary>
  private static string Kind(ResourceKind kind) =>
    kind switch
    {
      ResourceKind.Token => "token",
      ResourceKind.Mana => "mana",
      ResourceKind.Counter => "counter",
      ResourceKind.Death => "death",
      ResourceKind.EntersBattlefield => "etb",
      ResourceKind.LeavesBattlefield => "ltb",
      ResourceKind.Sacrifice => "sacrifice",
      ResourceKind.Cast => "cast",
      _ => kind.ToString().ToLowerInvariant(),
    };

  /// <summary>Join the facets in canonical order, dropping the absent ones.</summary>
  private static string Join(params string?[] facets) =>
    string.Join(":", facets.Where(f => !string.IsNullOrEmpty(f)));

  /// <summary>Canonicalise a type list to a deterministic, lower-cased, sorted <c>+</c>-join.</summary>
  private static string? Canon(IReadOnlyList<string>? xs) =>
    xs is null || xs.Count == 0
      ? null
      : string.Join("+", xs.Select(x => x.ToLowerInvariant()).OrderBy(x => x, StringComparer.Ordinal));
}
