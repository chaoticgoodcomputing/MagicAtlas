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

    // Find the colon that separates cost from effect
    var colonIndex = text.IndexOf(':');
    if (colonIndex < 0)
    {
      return null;
    }

    // Split into cost and effect parts
    var costPart = text[..colonIndex].Trim();
    var effectPart = text[(colonIndex + 1)..].Trim();

    // Parse costs
    var costs = ParseCosts(costPart, classification);
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

    // Extract trailing "Activate only as ..." restriction sentences from
    // effectPart before effect parsing. These are not effects — they constrain
    // when the ability can be activated (Rule 602.5). Stripping them prevents
    // TryParseMultiEffectSentences from failing when it encounters them.
    var restrictions = ExtractActivationRestrictions(ref effectPart);

    // Parse effects
    var effects = ParseEffects(effectPart);
    if (effects == null || effects.Count == 0)
    {
      // Cost parsed but the effect didn't. Surface as an Activated ability
      // carrying a structured UnparsedEffect so the cost-half still lands in
      // the AST (matches the malformed-fixture contract: the Tap cost is
      // still real even when the right-hand side is garbage).
      var effectSpan = new MagicAST.AST.TextSpan(
        clause.SourceSpan.Start + colonIndex + 1,
        Math.Max(0, clause.RawText.Length - (colonIndex + 1))
      );
      var unparsedEffect = new MagicAST.AST.Effects.Core.UnparsedEffect
      {
        SourceSpan = effectSpan,
        RawText = effectPart,
      };
      return new ActivatedAbility
      {
        Costs = costs,
        Effects = [unparsedEffect],
        Restrictions = restrictions,
        IsManaAbility = false,
        LoyaltyCost = classification.LoyaltyCost,
        AbilityWord = classification.AbilityWord,
      };
    }

    // Determine if this is a mana ability
    var isManaAbility = IsManaAbility(costs, effects);

    // Build the activated ability
    return new ActivatedAbility
    {
      Costs = costs,
      Effects = effects,
      Restrictions = restrictions,
      IsManaAbility = isManaAbility,
      LoyaltyCost = classification.LoyaltyCost,
      AbilityWord = classification.AbilityWord,
    };
  }

  /// <summary>
  /// Strips trailing "Activate only as a sorcery." / "Activate only once each turn." /
  /// etc. sentences from <paramref name="effectPart"/> and returns the parsed
  /// <see cref="ActivationRestriction"/> list (null if none found). Modifies
  /// <paramref name="effectPart"/> in-place to remove the extracted sentences.
  /// </summary>
  private static IReadOnlyList<ActivationRestriction>? ExtractActivationRestrictions(
    ref string effectPart
  )
  {
    var restrictions = new List<ActivationRestriction>();

    // Greedily strip "Activate only as ..." sentences from the end of effectPart.
    // These always appear after the real effect sentence(s).
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

      var restriction = TryParseActivationRestriction(candidate);
      if (restriction is null)
      {
        break;
      }
      restrictions.Add(restriction.Value);
      remaining = prefix;
    }

    if (restrictions.Count == 0)
    {
      return null;
    }

    restrictions.Reverse(); // restore original order (we iterated from the end)
    effectPart = remaining ?? string.Empty;
    return restrictions;
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
    if (lower.Contains("activate only during your turn") || lower.Contains("activate this ability only during your turn"))
    {
      return ActivationRestriction.OnlyDuringYourTurn;
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
  private List<Cost>? ParseCosts(string costPart, ClauseClassification classification)
  {
    var costs = new List<Cost>();

    // Handle loyalty abilities (costs are empty, loyalty is tracked separately)
    if (classification.LoyaltyCost.HasValue)
    {
      return costs;
    }

    // Split by comma to get individual cost components
    var costComponents = costPart.Split(',').Select(c => c.Trim()).ToList();
    var hasParsedAnyCost = false;

    foreach (var component in costComponents)
    {
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
        costs.Add(registryCost);
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
    // Check if any effect is an AddManaEffect
    var hasAddManaEffect = effects.Any(e => e is AddManaEffect);

    // For now, simple heuristic: if it adds mana and doesn't have complex targeting,
    // it's probably a mana ability
    return hasAddManaEffect;
  }

}
