namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Counter;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Tokens;
using Superpower.Model;

/// <summary>
/// Parses activated abilities of the form "[Cost]: [Effect.]"
/// Handles:
/// - Mana abilities: {T}: Add {G}.
/// - Complex activated abilities: {3}{B}{B}: Creatures you control gain lifelink until end of turn.
/// - Loyalty abilities: +2: Discard up to two cards, then draw that many cards.
/// </summary>
[OracleAbilityParser(AbilityKind.Activated)]
public sealed partial class ActivatedAbilityParser : IAbilityParser
{
  private readonly FallbackParser _fallback = new();

  // Registry dispatch (Phase 5): reflection-discovered cost- and effect-component
  // rules, each in its own file under Parsers/Activated/Rules/. Static so the
  // instance ParseCosts/ParseEffects dispatchers can reach them; discovered once
  // at type-init. Priorities are migrated order-preserving from the legacy
  // ParseCosts/ParseEffects chains (Priority = 1000 - chain index).
  private static readonly IReadOnlyList<DiscoveredRule<Activated.IActivatedEffectRule>> _effectRules =
    RuleRegistry.Discover<Activated.IActivatedEffectRule, Activated.ActivatedEffectRuleAttribute>(
      "ActivatedAbilityParser"
    );

  private static readonly IReadOnlyList<DiscoveredRule<Activated.IActivatedCostRule>> _costRules =
    RuleRegistry.Discover<Activated.IActivatedCostRule, Activated.ActivatedCostRuleAttribute>(
      "ActivatedAbilityParser"
    );

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var parsed = TryParse(clause, classification);
    if (parsed is not null)
    {
      return [parsed];
    }
    return
    [
      _fallback.Parse(
        clause,
        classification,
        "Activated ability parser not yet implemented",
        lastAttemptedRule: "ActivatedAbilityParser.Parse",
        failurePosition: clause.SourceSpan.Start
      ),
    ];
  }

  /// <summary>
  /// Attempts to parse an activated ability from a clause.
  /// Returns null if parsing fails.
  /// </summary>
  public ActivatedAbility? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var text = clause.RawText;

    // Strip surrounding parens from parenthetical-wrapped abilities like
    // "({T}: Add {B} or {R}.)" so cost/effect parsing proceeds on the inner text.
    if (text.StartsWith('(') && text.EndsWith(')'))
    {
      text = text[1..^1].Trim();
    }

    // Peel the em-dash prefix ("Metalcraft — ", …) if the classifier detected an
    // ability word or printed label. The label is mechanically inert (CR 207.2c) but
    // its "Word — " prefix must be stripped before cost/effect splitting, otherwise
    // the text before the colon includes the label and the cost parse fails.
    var dashPrefix = classification.DashPrefix;
    if (dashPrefix is not null)
    {
      var emDashIndex = text.IndexOf('—');
      if (emDashIndex >= 0)
      {
        text = text[(emDashIndex + 1)..].TrimStart();
      }
    }

    // Find the colon that separates cost from effect
    var colonIndex = text.IndexOf(':');
    if (colonIndex < 0)
    {
      return null;
    }

    // Split into cost and effect parts
    var costPart = text[..colonIndex].Trim();
    var effectPart = text[(colonIndex + 1)..].Trim();

    // Clause-accurate effect-half span (upstream-atlas-data-plan §4): the region after
    // the cost colon, absolute into the original oracle text. Half-granularity — every
    // effect the ability produces shares it. Mirrors the precedent already used for the
    // L1 UnstructuredEffect fallback below; hoisted here so the success path stamps it
    // onto each parsed effect too. (An em-dash ability-word prefix, if present, is not
    // reflected in colonIndex — a pre-existing limitation shared with the fallback.)
    var effectSpan = new MagicAST.AST.TextSpan(
      clause.SourceSpan.Start + colonIndex + 1,
      Math.Max(0, clause.RawText.Length - (colonIndex + 1))
    );

    // Parse costs — thread the absolute start of the (trimmed) cost half so ParseCosts can stamp each
    // comma-split component's SourceSpan (same clause.SourceSpan.Start basis as effectSpan; shares the
    // em-dash/paren caveat noted above).
    var rawCostPart = text[..colonIndex];
    var costPartStart = clause.SourceSpan.Start + (rawCostPart.Length - rawCostPart.TrimStart().Length);
    var costs = ParseCosts(costPart, classification, costPartStart);
    if (costs == null)
    {
      return null;
    }

    // Strip trailing parenthetical reminder text (Rule 207.2) before effect
    // parsing. Reminder text follows the effect sentence as "(explanation...)" —
    // e.g. "Create a Treasure token. (It's an artifact with ...)".
    // MUST run before ExtractActivationRestrictions: when the reminder is the
    // final sentence (e.g. the Phyrexian "({B/P} can be paid with either {B} or
    // 2 life.)" — CR 107.4f), restriction extraction inspects the parenthetical,
    // fails to match "Activate only as a sorcery", and bails — leaving the
    // restriction sentence glued to the effect.
    StripTrailingReminder(ref effectPart);

    // Extract a trailing "Any player may activate this ability." / "Only your
    // opponents may activate this ability[, and only as a sorcery]." permission
    // sentence (CR 602.2's "unless the object specifically says otherwise" branch)
    // before restriction/effect parsing. MUST run before ExtractActivationRestrictions
    // for the same reason as StripTrailingReminder: if left glued to the effect text,
    // TryParseMultiEffectSentences will try (and fail) to parse it as a second effect,
    // degrading the whole ability to UnparsedEffect.
    var whoMayActivate = ExtractActivationPermission(ref effectPart, out var impliedRestriction);

    // Extract trailing "Activate only as ..." restriction sentences from
    // effectPart before effect parsing. These are not effects — they constrain
    // when the ability can be activated (Rule 602.5). Stripping them prevents
    // TryParseMultiEffectSentences from failing when it encounters them.
    // Also extracts "Activate only if [condition]" into a structured Condition
    // (ADR 0007 — conditions are one union; CR 602.5c).
    var restrictions = ExtractActivationRestrictions(ref effectPart, out var activationCondition);

    // Fold in a restriction implied by the compound permission sentence (e.g.
    // "Only your opponents may activate this ability and only as a sorcery." —
    // the "and only as a sorcery" clause shares the permission sentence rather
    // than forming its own trailing ". "-delimited sentence, so
    // ExtractActivationRestrictions never sees it independently).
    if (impliedRestriction is not null)
    {
      restrictions = restrictions is null
        ? [impliedRestriction.Value]
        : [.. restrictions, impliedRestriction.Value];
    }

    // Parse effects
    var effects = ParseEffects(effectPart);
    if (effects == null || effects.Count == 0)
    {
      // Cost parsed but the effect interior didn't. Surface as the real Activated
      // ability shell carrying the interior as an UnstructuredEffect residual
      // (fidelity L1) so the cost-half lands in the AST with the effect held
      // verbatim and ACCOUNTED — not a whole-ability hole (L0), and not a silent
      // drop. Upgraded from UnparsedEffect (L0) to UnstructuredEffect (L1) as part
      // of the fidelity ladder; the parsed cost gives the interaction graph its
      // consume-side ports.
      var unparsedEffect = new MagicAST.AST.Effects.Core.UnstructuredEffect
      {
        SourceSpan = effectSpan,
        Text = effectPart,
      };
      // CR 702.177a: inject OnlyOnce for Exhaust abilities even when the effect is unparsed.
      if (classification.AbilityWord?.Equals("Exhaust", StringComparison.OrdinalIgnoreCase) == true)
      {
        var existing = restrictions ?? [];
        restrictions = [..existing, ActivationRestriction.OnlyOnce];
      }
      return new ActivatedAbility
      {
        Costs = costs,
        Effects = [unparsedEffect],
        Restrictions = restrictions,
        ActivationCondition = activationCondition,
        WhoMayActivate = whoMayActivate,
        IsManaAbility = false,
        LoyaltyCost = classification.LoyaltyCost,
        VariableLoyaltyCost = classification.VariableLoyaltyCost,
        AbilityWord = classification.AbilityWord,
      };
    }

    // Determine if this is a mana ability. CR 605.1a: a mana ability "is not a
    // loyalty ability", so a loyalty ability that adds mana (Chandra, Hope's
    // Beacon's "+2: Add two mana …") is NEVER a mana ability regardless of its
    // effect.
    var isLoyaltyAbility =
      classification.LoyaltyCost.HasValue || classification.VariableLoyaltyCost is not null;
    var isManaAbility = !isLoyaltyAbility && IsManaAbility(costs, effects);

    // CR 702.177a: "Exhaust — [Cost]: [Effect]" means "[Cost]: [Effect]. Activate only once."
    // The "Activate only once" restriction is implied by the keyword and not printed as a
    // separate "Activate only …" sentence in the oracle text, so ExtractActivationRestrictions
    // cannot see it. Inject it here when the AbilityWord identifies this as an Exhaust ability.
    if (classification.AbilityWord?.Equals("Exhaust", StringComparison.OrdinalIgnoreCase) == true)
    {
      var existing = restrictions ?? [];
      restrictions = [..existing, ActivationRestriction.OnlyOnce];
    }

    // Stamp the clause-accurate effect-half span on every parsed effect (half-granularity:
    // all emits share the post-colon region). Only SourceSpan changes; parsed effect
    // kinds/values are untouched, so the port graph's emit ports trace back to the
    // post-colon effect substring while the cost-derived consumes fall back to the whole
    // ability span (cost-span threading is out of scope — see PortGraph consume fallback).
    effects = effects.Select(e => e with { SourceSpan = effectSpan }).ToList();

    // Build the activated ability
    return new ActivatedAbility
    {
      Costs = costs,
      Effects = effects,
      Restrictions = restrictions,
      ActivationCondition = activationCondition,
      WhoMayActivate = whoMayActivate,
      IsManaAbility = isManaAbility,
      LoyaltyCost = classification.LoyaltyCost,
      VariableLoyaltyCost = classification.VariableLoyaltyCost,
      AbilityWord = classification.AbilityWord,
    };
  }

  // Anchored regex for "Activate only if [condition]." — must start at the
  // beginning of the candidate sentence (after trimming) so it cannot match
  // a substring of a longer clause.
  private static readonly Regex _activateOnlyIfPattern = new(
    @"^[Aa]ctivate\s+(?:this\s+ability\s+)?only\s+if\s+(?<cond>.+?)\.?\s*$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Strips trailing "Activate only as a sorcery." / "Activate only once each turn." /
  /// "Activate only if [condition]." sentences from <paramref name="effectPart"/> and
  /// returns the parsed <see cref="ActivationRestriction"/> list (null if none found).
  /// Also extracts "Activate only if [condition]" into a structured
  /// <see cref="MagicAST.AST.Abilities.Condition"/> via <paramref name="activationCondition"/>
  /// (ADR 0007; CR 602.5c). Modifies <paramref name="effectPart"/> in-place to remove
  /// the extracted sentences.
  /// </summary>
  private static IReadOnlyList<ActivationRestriction>? ExtractActivationRestrictions(
    ref string effectPart,
    out Condition? activationCondition
  )
  {
    var restrictions = new List<ActivationRestriction>();
    activationCondition = null;

    // Greedily strip "Activate only as ..." / "Activate only if ..." sentences
    // from the end of effectPart. These always appear after the real effect sentence(s).
    string? remaining = effectPart;
    while (remaining is not null)
    {
      // Match the last sentence (after the final ". ")
      var lastDotSpace = remaining.LastIndexOf(". ", StringComparison.Ordinal);
      string candidate;
      string? prefix;
      if (lastDotSpace >= 0)
      {
        candidate = remaining[(lastDotSpace + 2)..].Trim();
        prefix = remaining[..lastDotSpace].Trim();
      }
      else
      {
        candidate = remaining.Trim();
        prefix = null;
      }

      // Try "Activate only if [condition]" first — it produces a structured Condition,
      // not an ActivationRestriction enum value.
      var ifMatch = _activateOnlyIfPattern.Match(candidate);
      if (ifMatch.Success)
      {
        var conditionPhrase = ifMatch.Groups["cond"].Value.Trim();
        activationCondition = ConditionParser.Parse(conditionPhrase);
        remaining = prefix;
        continue;
      }

      var restriction = TryParseActivationRestriction(candidate);
      if (restriction is null)
      {
        break;
      }
      restrictions.Add(restriction.Value);
      remaining = prefix;
    }

    if (restrictions.Count == 0 && activationCondition is null)
    {
      return null;
    }

    restrictions.Reverse(); // restore original order (we iterated from the end)
    effectPart = remaining ?? string.Empty;
    return restrictions.Count > 0 ? restrictions : null;
  }

  // Anchored regex for "Any player may activate this ability." — must match the
  // ENTIRE candidate sentence (after trimming) so it cannot match a substring of a
  // longer clause. CR 602.2: "unless the object specifically says otherwise."
  private static readonly Regex _anyPlayerMayActivatePattern = new(
    @"^[Aa]ny\s+player\s+may\s+activate(?:\s+this\s+ability)?\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // Anchored regex for "Only your opponents may activate this ability[, ]and
  // only as a sorcery." (Detention Vortex) — must match the ENTIRE candidate
  // sentence (after trimming), like _anyPlayerMayActivatePattern, so it cannot
  // match a substring of a longer clause. CR 602.2: "unless the object
  // specifically says otherwise." The trailing "and only as a sorcery" clause is
  // optional: it shares this sentence rather than forming its own trailing
  // ". "-delimited restriction sentence, so it's captured here (via the
  // "sorcery" group) rather than by ExtractActivationRestrictions.
  private static readonly Regex _onlyOpponentsMayActivatePattern = new(
    @"^[Oo]nly\s+your\s+opponents?\s+may\s+activate(?:\s+this\s+ability)?(?:,?\s+and\s+only\s+as\s+an?\s+(?<sorcery>sorcery|instant))?\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  /// <summary>
  /// Strips a trailing "Any player may activate this ability." / "Only your
  /// opponents may activate this ability[, and only as a sorcery]." sentence
  /// from <paramref name="effectPart"/> (CR 602.1's "Activation instructions"
  /// slot; CR 602.2's "unless the object specifically says otherwise" branch),
  /// mutating it in place. Returns the parsed <see cref="ActivationPermission"/>
  /// when a sentence is found and stripped, else null (leaving the default
  /// controller-only permission implicit). <paramref name="impliedRestriction"/>
  /// carries the "and only as a sorcery/instant" clause folded into the opponent
  /// sentence, when present — a restriction that never forms its own trailing
  /// ". "-delimited sentence for <see cref="ExtractActivationRestrictions"/> to see.
  /// </summary>
  private static ActivationPermission? ExtractActivationPermission(
    ref string effectPart,
    out ActivationRestriction? impliedRestriction
  )
  {
    impliedRestriction = null;

    var lastDotSpace = effectPart.LastIndexOf(". ", StringComparison.Ordinal);
    string candidate;
    string? prefix;
    if (lastDotSpace >= 0)
    {
      candidate = effectPart[(lastDotSpace + 2)..].Trim();
      prefix = effectPart[..lastDotSpace].Trim();
    }
    else
    {
      candidate = effectPart.Trim();
      prefix = null;
    }

    if (_anyPlayerMayActivatePattern.IsMatch(candidate))
    {
      effectPart = prefix ?? string.Empty;
      return ActivationPermission.AnyPlayer;
    }

    var opponentMatch = _onlyOpponentsMayActivatePattern.Match(candidate);
    if (opponentMatch.Success)
    {
      if (opponentMatch.Groups["sorcery"].Success)
      {
        impliedRestriction = opponentMatch.Groups["sorcery"].Value.Equals("instant", StringComparison.OrdinalIgnoreCase)
          ? ActivationRestriction.OnlyAsInstant
          : ActivationRestriction.OnlyAsSorcery;
      }
      effectPart = prefix ?? string.Empty;
      return ActivationPermission.Opponent;
    }

    return null;
  }

  /// <summary>
  /// Strips a trailing parenthetical "(reminder text)" from <paramref name="effectPart"/>,
  /// mutating it in place (via ref). Reminder text in oracle cards (Rule 207.2) follows
  /// the effect sentence as "(explanation...)" and has no rules meaning.
  /// Only strips the LAST parenthetical so mid-text parens are left intact.
  /// </summary>
  private static void StripTrailingReminder(ref string effectPart)
  {
    var m = Regex.Match(effectPart, @"\s*\(([^)]+)\)\s*\.?\s*$");
    if (m.Success)
    {
      effectPart = effectPart[..m.Index].Trim();
    }
  }

  private static ActivationRestriction? TryParseActivationRestriction(string sentence)
  {
    var trimmed = sentence.Trim().TrimEnd('.');
    var lower = trimmed.ToLowerInvariant();
    if (lower == "activate only as a sorcery" || lower == "activate this ability only as a sorcery")
    {
      return ActivationRestriction.OnlyAsSorcery;
    }
    // CR 602.5a: by default activated abilities can be activated any time you could cast an instant;
    // "Activate only as an instant" is an explicit instant-speed restriction (Lion's Eye Diamond).
    if (lower == "activate only as an instant" || lower == "activate this ability only as an instant")
    {
      return ActivationRestriction.OnlyAsInstant;
    }
    // "Activate only during your upkeep." — a step-scoped timing restriction (CR 602.5),
    // strictly narrower than "…during your turn". ANCHORED to whole-string equality (like
    // the OnlyAsSorcery branch above) so it can NOT match compound restrictions such as
    // "Activate only during your upkeep and only once each turn." / "…and only if …" —
    // matching those as a substring would silently drop the trailing sibling restriction.
    if (lower == "activate only during your upkeep" || lower == "activate this ability only during your upkeep")
    {
      return ActivationRestriction.OnlyDuringYourUpkeep;
    }
    if (lower.Contains("activate only during your turn") || lower.Contains("activate this ability only during your turn"))
    {
      return ActivationRestriction.OnlyDuringYourTurn;
    }
    // "Activate only during combat." — phase-scoped (CR 506 / CR 602.5). ANCHORED to whole-string
    // equality (like the OnlyDuringYourUpkeep branch above) so it cannot swallow a trailing sibling
    // restriction in a compound "Activate only during combat and only once each turn."-style sentence.
    if (lower == "activate only during combat" || lower == "activate this ability only during combat")
    {
      return ActivationRestriction.OnlyDuringCombat;
    }
    if (lower.Contains("activate only once each turn") || lower.Contains("activate this ability only once each turn"))
    {
      return ActivationRestriction.OnlyOnceEachTurn;
    }
    return null;
  }

  /// <summary>
  /// Parses the cost portion of an activated ability.
  /// Returns null if parsing fails.
  /// </summary>
  private List<Cost>? ParseCosts(string costPart, ClauseClassification classification, int costPartStart)
  {
    var costs = new List<Cost>();

    // Handle loyalty abilities (costs are empty, loyalty is tracked separately on
    // LoyaltyCost / VariableLoyaltyCost). A variable "−X" cost never reaches the
    // cost-component parser as a symbol, so short-circuit here too.
    if (classification.LoyaltyCost.HasValue || classification.VariableLoyaltyCost is not null)
    {
      return costs;
    }

    // Split by comma to get individual cost components, tracking each component's absolute char offset
    // in the original oracle text (costPartStart + its position within costPart) so we can stamp a
    // per-component SourceSpan (upstream-atlas-data-plan §4; ADR-0003 Stage 1).
    var hasParsedAnyCost = false;
    var cursor = 0;

    foreach (var rawComponent in costPart.Split(','))
    {
      var leading = rawComponent.Length - rawComponent.TrimStart().Length;
      var component = rawComponent.Trim();
      var componentStart = costPartStart + cursor + leading;
      cursor += rawComponent.Length + 1; // advance past this segment + the comma delimiter

      if (component.Length == 0)
      {
        continue;
      }

      // Registry-first dispatch (Phase 5): try reflection-discovered cost rules in
      // priority order; first non-null wins. Shapes not yet extracted fall through
      // to the legacy chain below.
      Cost? registryCost = null;
      foreach (var entry in _costRules)
      {
        registryCost = entry.Rule.TryMatch(component);
        if (registryCost is not null)
        {
          break;
        }
      }
      if (registryCost is not null)
      {
        costs.Add(registryCost with { SourceSpan = new MagicAST.AST.TextSpan(componentStart, component.Length) });
        hasParsedAnyCost = true;
        continue;
      }

      // If we can't parse this component, the whole cost parse fails
      // (we should be able to understand all cost components).
    }

    // If we couldn't parse any costs, return null to signal failure
    return hasParsedAnyCost ? costs : null;
  }

  /// <summary>
  /// Parses the effect portion of an activated ability.
  /// Returns null if parsing fails.
  /// </summary>
  private List<Effect>? ParseEffects(string effectPart)
  {
    // First, try multi-sentence dispatch: "X. Y." where each sentence is a
    // distinct effect. Recombine the parsed list when both sentences parse
    // successfully; fall through to the single-effect path otherwise.
    var multi = TryParseMultiEffectSentences(effectPart);
    if (multi is not null)
    {
      return multi;
    }

    // Next, try the multi-effect rule path (mirrors IMultiSpellRule): a single
    // ", then"-joined sentence that expands to a flat sibling list — e.g. Sensei's
    // Divining Top's "Draw a card, then put this artifact on top of its owner's
    // library." This runs BEFORE the single-effect loop so a greedy single-effect
    // rule (e.g. DrawCardsEffectRule, which matches on any "draw") can't claim the
    // sentence and silently drop the second clause.
    var multiRuleEffects = TryParseMultiRuleEffects(effectPart);
    if (multiRuleEffects is not null)
    {
      return multiRuleEffects;
    }

    // Registry-first dispatch (Phase 5): try reflection-discovered effect rules in
    // priority order; first non-null wins. Shapes not yet extracted fall through to
    // the legacy chain below.
    foreach (var entry in _effectRules)
    {
      var effect = entry.Rule.TryMatch(effectPart);
      if (effect is not null)
      {
        return new List<Effect> { effect };
      }
    }

    // No rule recognised the effect; signal fall-back to unparsed.
    return null;
  }

  /// <summary>
  /// Tries each discovered effect rule that also implements
  /// <see cref="Activated.IMultiActivatedEffectRule"/> in priority order, returning
  /// the first non-null flat effect list. Mirrors the spell parser's
  /// <c>TryParseMultiSpellRuleEffects</c>: a single ", then"-joined sentence whose
  /// two clauses are sibling effects is recognized whole by one rule rather than
  /// split by the dispatcher (because not every ", then" is a join). Returns null
  /// when no multi-rule fires.
  /// </summary>
  private List<Effect>? TryParseMultiRuleEffects(string effectPart)
  {
    var trimmed = effectPart.Trim().TrimEnd('.').Trim();
    foreach (var entry in _effectRules)
    {
      if (entry.Rule is not Activated.IMultiActivatedEffectRule multiRule)
      {
        continue;
      }
      if (multiRule.TryMatchMulti(trimmed, out var effects) && effects is not null)
      {
        return effects.ToList();
      }
    }
    return null;
  }

  /// <summary>
  /// Splits an effect half on sentence boundaries (". ") and parses each
  /// sentence via <see cref="ParseEffects(string)"/> recursively. Returns
  /// the concatenated effects when every sentence parses, otherwise null
  /// so the caller falls back to the single-effect path.
  /// </summary>
  private List<Effect>? TryParseMultiEffectSentences(string effectPart)
  {
    // Quick reject for single-sentence inputs to avoid infinite recursion on
    // the head-sentence path that the recursive ParseEffects call retreads.
    var trimmed = effectPart.Trim().TrimEnd('.').Trim();
    var pieces = Regex.Split(trimmed, @"\.\s+");
    if (pieces.Length < 2)
    {
      return null;
    }

    var combined = new List<Effect>();
    foreach (var piece in pieces)
    {
      var sentence = piece.Trim();
      if (sentence.Length == 0)
      {
        continue;
      }
      var parsed = ParseEffects(sentence + ".");
      if (parsed is null || parsed.Count == 0)
      {
        return null;
      }
      combined.AddRange(parsed);
    }
    return combined.Count > 0 ? combined : null;
  }

  /// <summary>
  /// Determines if this is a mana ability (Rule 605).
  /// A mana ability:
  /// - Isn't a loyalty ability
  /// - Doesn't target
  /// - Could produce mana (has AddManaEffect)
  /// </summary>
  private static bool IsManaAbility(IReadOnlyList<Cost> costs, IReadOnlyList<Effect> effects)
  {
    // Check if any effect could add mana. A CompositeEffect that bundles several
    // AddManaEffects ("Add {B}, then add an additional {B} …" — ADR 0009 S4)
    // still satisfies CR 605.1a: it could add mana when it resolves, so look one
    // level into composites.
    var hasAddManaEffect = effects.Any(EffectAddsMana);

    // For now, simple heuristic: if it adds mana and doesn't have complex targeting,
    // it's probably a mana ability
    return hasAddManaEffect;
  }

  /// <summary>
  /// Whether an effect could add mana — directly an <see cref="AddManaEffect"/>,
  /// or a <see cref="MagicAST.AST.Effects.Core.CompositeEffect"/> any of whose
  /// members could (CR 605.1a).
  /// </summary>
  private static bool EffectAddsMana(Effect effect) =>
    effect switch
    {
      AddManaEffect => true,
      MagicAST.AST.Effects.Core.CompositeEffect composite => composite.Effects.Any(EffectAddsMana),
      _ => false,
    };

}
