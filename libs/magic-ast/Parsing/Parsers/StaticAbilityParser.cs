namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Combinators;
using MagicAST.Parsing.Tokens;
using Superpower;
using Superpower.Model;

/// <summary>
/// Parser for static abilities using token-based combinators.
/// Handles keyword abilities (Flying, Vigilance, etc.) and other static effects.
/// </summary>
/// <remarks>
/// This parser uses monadic combinators from OracleParsers to parse keywords
/// directly from token sequences, avoiding string manipulation.
/// </remarks>
[OracleAbilityParser(AbilityKind.Static)]
public sealed class StaticAbilityParser : IAbilityParser
{
  private readonly OracleTokenizer _tokenizer = new();
  private readonly FallbackParser _fallback = new();

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var parsed = TryParse(clause, classification);
    if (parsed is { Count: > 0 })
    {
      return parsed;
    }
    return
    [
      _fallback.Parse(clause, classification, "Static ability parser not yet implemented"),
    ];
  }

  /// <summary>
  /// Attempts to parse static abilities from a clause.
  /// Returns a list of StaticAbility nodes (one per keyword or effect).
  /// </summary>
  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    var tokens = clause.Tokens;

    // Try parsing as keyword list using token combinators
    var keywordAbilities = TryParseKeywordList(tokens);
    if (keywordAbilities != null && keywordAbilities.Count > 0)
    {
      return keywordAbilities;
    }

    // "[Self] attacks each combat if able." — Rule 508-style attack requirement.
    // Descriptive: records that the oracle line imposes a must-attack restriction
    // on the named object. Does not model runtime enforcement.
    var mustAttack = TryParseMustAttack(clause);
    if (mustAttack != null)
    {
      return mustAttack;
    }

    // "[Self] must be blocked if able." — Rule 509.1c block requirement.
    // Dual of the must-attack pattern above; same parser shape applies.
    var mustBeBlocked = TryParseMustBeBlocked(clause);
    if (mustBeBlocked != null)
    {
      return mustBeBlocked;
    }

    // "[subject] blocks each combat if able." — blocker-side Rule 509.1c
    // requirement (e.g., Grand Melee's "All creatures block each combat if
    // able"). Mirrors TryParseMustAttack but lands on a MustBlockEffect.
    var mustBlock = TryParseMustBlock(clause);
    if (mustBlock != null)
    {
      return mustBlock;
    }

    // "[filter] has \"[activated ability]\"." — Aura-style ability grant on the
    // enchanted/equipped object (Rule 113.6/113.10). The inner activated
    // ability is parsed by reusing ActivatedAbilityParser, so the recursive
    // Ability shape stays consistent with how the same ability would be
    // modeled if it appeared directly on a card.
    var grantedAbility = TryParseGrantedAbility(clause, classification);
    if (grantedAbility != null)
    {
      return grantedAbility;
    }

    // "This artifact doesn't untap during your untap step." (Rule 701.20)
    // Encodes the possessive ("your" / "its controller's") on
    // <see cref="DoesntUntapEffect.WhoseUntapStep"/> for downstream consumers.
    var doesntUntap = TryParseDoesntUntap(clause);
    if (doesntUntap != null)
    {
      return doesntUntap;
    }

    // "This creature can't be blocked except by [filter]." — Rule 509-style evasion.
    var evasion = TryParseEvasion(clause);
    if (evasion != null)
    {
      return evasion;
    }

    // "Enchant [filter]." — Aura legal-target restriction (Rule 702.5).
    var enchant = TryParseEnchant(clause);
    if (enchant != null)
    {
      return enchant;
    }

    // "Ward {N}" / "Ward — [effect]" — emits a TriggeredAbility with
    // KeywordSource="Ward", structured as the trigger Rule 702.21 expands to.
    var ward = TryParseWardKeyword(clause);
    if (ward != null)
    {
      return ward;
    }

    // "This spell costs {X} less to cast, where X is ..." — cost reduction
    // scaled by a derived quantity (Chandra's Incinerator shape).
    var costReduction = TryParseCostReductionWhereX(clause);
    if (costReduction != null)
    {
      return costReduction;
    }

    // "During [period], [self] has [keyword]." — conditional static keyword
    // (Zurgo's during-your-turn indestructibility).
    var conditionalKeyword = TryParseConditionalSelfKeyword(clause);
    if (conditionalKeyword != null)
    {
      return conditionalKeyword;
    }

    // "This spell costs {N} less to cast during [period]." — flat-amount
    // cost reduction guarded by a duration condition (Mental Modulation).
    var conditionalCostReduction = TryParseConditionalSpellCostReduction(clause);
    if (conditionalCostReduction != null)
    {
      return conditionalCostReduction;
    }

    // "The \"legend rule\" doesn't apply." — Rule 704.5j state-based-action
    // suppression (Mirror Gallery). Parameterless; the mere presence of the
    // effect records the suppression.
    var legendRuleSuppression = TryParseLegendRuleSuppression(clause);
    if (legendRuleSuppression != null)
    {
      return legendRuleSuppression;
    }

    // "If you would draw a card, draw [N] cards instead." — Rule 121 (drawing)
    // + Rule 614 (replacement effects). Pure substitution: the original draw
    // does not occur; the replacement draw of N cards happens in its place
    // (Thought Reflection shape).
    var drawReplacement = TryParseDrawReplacement(clause);
    if (drawReplacement != null)
    {
      return drawReplacement;
    }

    // "Enchanted creature gets +N/+N." — anthem-style Aura P/T grant.
    // No Duration: the modifier persists while the Aura is attached (Rule
    // 303/702.5). Descriptive shape on EnchantedOrEquipped with literal
    // power/toughness modifiers; mirrors the gold for Gift of Strands.
    var anthemPT = TryParseAnthemModifyPT(clause);
    if (anthemPT != null)
    {
      return anthemPT;
    }

    // "Noncreature spells with mana value N or greater can't be cast." /
    // "Noncreature spells with {X} in their mana costs can't be cast." —
    // Rule 601.5 cast-restriction (Gaddock Teeg). The spell-filter lives on
    // StaticAbility.AffectedObjects; the effect itself is the parameterless
    // CantBeCastEffect, since the restriction's payload is "no cast at all".
    var cantBeCast = TryParseCantBeCastRestriction(clause);
    if (cantBeCast != null)
    {
      return cantBeCast;
    }

    // "Other [Subtype] creatures you control get +N/+N." — tribal-lord
    // anthem (Sachi, Daughter of Seshiro). Same shape as the Aura anthem
    // above (no Duration; persists while the source is on the battlefield,
    // Rule 604.3), but targets an Each-reference filtered by subtype +
    // controller + an "other" characteristic instead of EnchantedOrEquipped.
    var tribalAnthemPT = TryParseTribalAnthemModifyPT(clause);
    if (tribalAnthemPT != null)
    {
      return tribalAnthemPT;
    }

    return null;
  }

  /// <summary>
  /// "[filter] can't be cast." — Rule 601.5 cast-restriction. Two filter
  /// shapes are recognized today, both rooted at <c>Noncreature spells</c>:
  /// <list type="bullet">
  ///   <item><c>Noncreature spells with mana value N or greater</c> — emits a
  ///         <see cref="ObjectFilter"/> with <c>CardTypes:["spell"]</c>,
  ///         <c>Characteristics:["noncreature"]</c>, and a
  ///         <see cref="Comparison"/> on <see cref="ObjectFilter.ManaValueComparison"/>.</item>
  ///   <item><c>Noncreature spells with {X} in their mana costs</c> — emits the
  ///         same root filter plus a second <c>Characteristics</c> entry
  ///         <c>"with {X} in their mana costs"</c>. The {X}-in-cost predicate
  ///         is descriptive, not a numeric comparison, so it lives on the
  ///         free-form characteristics axis rather than on
  ///         <see cref="ObjectFilter.ManaValueComparison"/>.</item>
  /// </list>
  /// The filter sits on <see cref="MagicAST.AST.Abilities.StaticAbility.AffectedObjects"/>;
  /// the wrapped effect is the parameterless
  /// <see cref="MagicAST.AST.Effects.Timing.CantBeCastEffect"/> (per its
  /// xml-doc, the restriction's targets are described by the containing
  /// ability's filter, not by a payload on the effect itself).
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseCantBeCastRestriction(OracleClause clause)
  {
    var mvMatch = _cantBeCastManaValuePattern.Match(clause.RawText);
    if (mvMatch.Success)
    {
      var value = int.Parse(mvMatch.Groups["value"].Value);
      return
      [
        new StaticAbility
        {
          Effect = new MagicAST.AST.Effects.Timing.CantBeCastEffect(),
          AffectedObjects = new ObjectFilter
          {
            CardTypes = ["spell"],
            Characteristics = ["noncreature"],
            ManaValueComparison = new Comparison
            {
              Operator = ComparisonOperator.GreaterThanOrEqual,
              Value = value,
            },
          },
        },
      ];
    }

    if (_cantBeCastXInCostPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effect = new MagicAST.AST.Effects.Timing.CantBeCastEffect(),
          AffectedObjects = new ObjectFilter
          {
            CardTypes = ["spell"],
            Characteristics = ["noncreature", "with {X} in their mana costs"],
          },
        },
      ];
    }

    return null;
  }

  private static readonly Regex _cantBeCastManaValuePattern = new(
    @"^\s*Noncreature\s+spells\s+with\s+mana\s+value\s+(?<value>\d+)\s+or\s+greater\s+can'?t\s+be\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _cantBeCastXInCostPattern = new(
    @"^\s*Noncreature\s+spells\s+with\s+\{X\}\s+in\s+their\s+mana\s+costs\s+can'?t\s+be\s+cast\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "Enchanted creature gets +N/+N." — Aura P/T grant on the attached
  /// object. Emits a <see cref="StaticAbility"/> wrapping a
  /// <see cref="ModifyPTEffect"/> with
  /// <see cref="ObjectReferenceKind.EnchantedOrEquipped"/> as the target.
  /// No <see cref="ModifyPTEffect.Duration"/> is set: anthem-style
  /// modifiers from Auras last while the Aura remains attached, not for a
  /// bounded duration clause (cf. "until end of turn" pumps).
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseAnthemModifyPT(OracleClause clause)
  {
    var match = _anthemModifyPTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effect = new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        },
      },
    ];
  }

  private static readonly Regex _anthemModifyPTPattern = new(
    @"^\s*Enchanted\s+creature\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "Other [Subtype] creatures you control get +N/+N." — tribal-lord
  /// anthem (Sachi). Emits the same <see cref="ModifyPTEffect"/> shape as
  /// the Aura anthem, but with an <see cref="ObjectReferenceKind.Each"/>
  /// target filtered by subtype + controller + an explicit "other"
  /// characteristic. The "Other" qualifier rides on
  /// <see cref="ObjectFilter.Characteristics"/> (matching how
  /// <c>ActivatedAbilityParser</c> encodes "another", and how
  /// <c>TriggeredAbilityParser</c>'s Barrin shape encodes "other"): an
  /// extrinsic exclusion of the source rather than a synthetic subtype.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseTribalAnthemModifyPT(OracleClause clause)
  {
    var match = _tribalAnthemModifyPTPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var subtype = match.Groups["sub"].Value;
    var ctrl = match.Groups["ctrl"].Value.ToLowerInvariant();
    var controller = ctrl.StartsWith("you")
      ? ControllerFilter.You
      : ControllerFilter.Opponent;
    var power = int.Parse(match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effect = new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature"],
              Subtypes = [subtype],
              Controller = controller,
              Characteristics = ["other"],
            },
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        },
      },
    ];
  }

  // Capitalised subtype (oracle text capitalises creature subtypes), followed by
  // "creatures" (lowercase plural card-type noun) and a controller clause. The
  // leading "Other " is what distinguishes this from the would-be inclusive
  // tribal anthem; without it the source itself would be in the filter and the
  // shape would need a different gold (no card hits that yet).
  private static readonly Regex _tribalAnthemModifyPTPattern = new(
    @"^\s*Other\s+(?<sub>[A-Z][a-z]+)\s+creatures\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\s+get\s+\+(?<p>\d+)/\+(?<t>\d+)\.?\s*$",
    RegexOptions.Compiled
  );

  /// <summary>
  /// "If you would draw a card, draw [N] cards instead." — Rule 614 pure
  /// replacement of a card-draw event. The leading "a card" is the elided
  /// singular: <see cref="MagicAST.AST.Effects.Replacement.DrawCardEvent.Count"/>
  /// is left unset (its xml-doc declares one as the default), so we do not
  /// emit a literal 1 on the event. The replacement side does carry an
  /// explicit count, matched either as a digit or a small number-word
  /// ("two", "three", ...).
  /// </summary>
  /// <remarks>
  /// <see cref="MagicAST.AST.Effects.Replacement.ReplacementEffect.OriginalEventOccurs"/>
  /// stays <c>false</c>: the gold for Thought Reflection treats this as a
  /// substitution, not an augmentation (cf. Chatterfang-style "in addition").
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseDrawReplacement(OracleClause clause)
  {
    var match = _drawReplacementPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value.ToLowerInvariant();
    if (!TryParseSmallCount(countText, out var count))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.DrawCardEvent
          {
            Player = ObjectReference.You(),
          },
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.CardFlow.DrawCardsEffect
          {
            Count = MagicAST.AST.Quantities.LiteralQuantity.Of(count),
            Player = ObjectReference.You(),
          },
        },
      },
    ];
  }

  private static readonly Regex _drawReplacementPattern = new(
    @"^\s*If\s+you\s+would\s+draw\s+a\s+card,\s+draw\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Maps a small-count token (digit or number-word "one".."ten") onto an
  /// integer. Returns false for anything outside that vocabulary so callers
  /// can fall through to the fallback path.
  /// </summary>
  private static bool TryParseSmallCount(string token, out int value)
  {
    if (int.TryParse(token, out value))
    {
      return true;
    }
    value = token switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      _ => 0,
    };
    return value > 0;
  }

  /// <summary>
  /// "The \"legend rule\" doesn't apply." — Rule 704.5j. Emits a
  /// <see cref="StaticAbility"/> wrapping a parameterless
  /// <see cref="MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect"/>.
  /// The oracle uses curly or straight double-quotes around "legend rule";
  /// the pattern accepts both shapes.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseLegendRuleSuppression(OracleClause clause)
  {
    if (!_legendRuleSuppressionPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect(),
      },
    ];
  }

  private static readonly Regex _legendRuleSuppressionPattern = new(
    @"^\s*The\s+[""""“”]legend\s+rule[""""“”]\s+doesn'?t\s+apply\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This spell costs {N} less to cast during [your turn / each opponent's turn / ...]." —
  /// emits a <see cref="StaticAbility"/> with a <see cref="MagicAST.AST.Abilities.Condition"/>
  /// preserving the duration clause, wrapping a <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect"/>
  /// whose <c>Amount</c> is the literal generic-mana reduction. Rule 117.6 (cost reductions).
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseConditionalSpellCostReduction(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+(?<cond>during\s+(?:your\s+turn|each\s+(?:opponent|player)'?s\s+turn|combat))\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var amount = int.Parse(match.Groups["amount"].Value);
    // Preserve oracle-text casing for the condition (the lower-case "during"
    // that follows "to cast " is correct as-is; don't recapitalize).
    var conditionText = match.Groups["cond"].Value;
    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        },
        Condition = new MagicAST.AST.Abilities.Condition { Text = conditionText },
      },
    ];
  }

  /// <summary>
  /// "During your turn, [self] has [keyword]." — produces a static ability
  /// guarded by a <see cref="MagicAST.AST.Abilities.Condition"/> whose text
  /// preserves the duration clause. The keyword tail is wrapped in the
  /// canonical effect node so the resulting ability mirrors the same shape
  /// a non-conditional version of that keyword would carry.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseConditionalSelfKeyword(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*(?<cond>During\s+(?:your\s+turn|each\s+(?:opponent|player)'?s\s+turn|combat)),\s+(?<subject>\S.*?)\s+has\s+(?<kw>\w+(?:\s+\w+)?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var kw = match.Groups["kw"].Value.ToLowerInvariant().Trim();
    Effect? effect = kw switch
    {
      "indestructible" => new MagicAST.AST.Effects.Keyword.IndestructibleEffect(),
      "haste" => new MagicAST.AST.Effects.Keyword.HasteEffect(),
      "trample" => new MagicAST.AST.Effects.Keyword.TrampleEffect(),
      "lifelink" => new MagicAST.AST.Effects.Damage.LifelinkEffect(),
      "vigilance" => new MagicAST.AST.Effects.Keyword.VigilanceEffect(),
      "reach" => new MagicAST.AST.Effects.Keyword.ReachEffect(),
      _ => null,
    };
    if (effect is null)
    {
      return null;
    }
    var conditionText = match.Groups["cond"].Value.Trim();
    return
    [
      new StaticAbility
      {
        Effect = effect,
        Condition = new MagicAST.AST.Abilities.Condition { Text = conditionText },
      },
    ];
  }

  /// <summary>
  /// "This spell costs {X} less to cast, where X is the total amount of
  /// noncombat damage dealt to your opponents this turn." — Chandra's
  /// Incinerator's cost-reduction static. The right-hand definition of X is
  /// captured as a <see cref="MagicAST.AST.Quantities.DerivedQuantity"/>
  /// whose <see cref="MagicAST.AST.Quantities.DerivedKind"/> is
  /// <c>DamageDealt</c>; the long source phrase lives on the same node's
  /// <c>Source</c> string and on <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect.BasedOn"/>.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseCostReductionWhereX(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+spell\s+costs\s+\{X\}\s+less\s+to\s+cast,\s+where\s+X\s+is\s+(?:the\s+total\s+amount\s+of\s+)?(?<source>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var source = match.Groups["source"].Value.Trim();
    var derivedKind = source.Contains("damage", StringComparison.OrdinalIgnoreCase)
      ? MagicAST.AST.Quantities.DerivedKind.DamageDealt
      : MagicAST.AST.Quantities.DerivedKind.Other;
    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = new MagicAST.AST.Quantities.DerivedQuantity
          {
            DerivedFrom = derivedKind,
            Source = source,
          },
          BasedOn = source,
        },
      },
    ];
  }

  /// <summary>
  /// "Ward {N}" / "Ward {X}{Y}" — Rule 702.21 keyword. The reminder-text
  /// expansion is canonically "Whenever this permanent becomes the target of
  /// a spell or ability an opponent controls, counter it unless that player
  /// pays [cost]." The parser emits that triggered shape directly with
  /// KeywordSource="Ward" so the resulting node mirrors how the same trigger
  /// would land if printed verbatim on a card.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseWardKeyword(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*Ward\s+(?<cost>(?:\{[^}]+\})+)\s*(?<rest>.*)$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var costStr = match.Groups["cost"].Value;
    MagicAST.AST.Costs.ManaCost? wardCost;
    try
    {
      var parsed = new ManaCostParser().Parse(costStr);
      if (parsed.Symbols.Count == 0)
      {
        return null;
      }
      wardCost = new MagicAST.AST.Costs.ManaCost { Symbols = parsed.Symbols };
    }
    catch
    {
      return null;
    }

    Parenthetical? reminder = null;
    var rest = match.Groups["rest"].Value.Trim();
    if (rest.StartsWith('(') && rest.EndsWith(')'))
    {
      reminder = new Parenthetical { Text = rest };
    }

    var trigger = new MagicAST.AST.Triggers.TriggerCondition
    {
      Timing = MagicAST.AST.Triggers.TriggerTiming.Whenever,
      Event = MagicAST.AST.Triggers.TriggerEvent.BecomesTarget,
      Filter = new ObjectFilter { Controller = ControllerFilter.Opponent },
    };

    var counterSpell = new MagicAST.AST.Effects.Control.CounterSpellEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.It },
      UnlessClause = new MagicAST.AST.Effects.UnlessClause
      {
        Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
        Cost = wardCost,
      },
    };

    return
    [
      new MagicAST.AST.Abilities.TriggeredAbility
      {
        KeywordSource = "Ward",
        Trigger = trigger,
        Effects = [counterSpell],
        Reminder = reminder,
      },
    ];
  }

  /// <summary>
  /// Matches "This [permanent] doesn't untap during [possessive] untap step." and
  /// produces a <see cref="StaticAbility"/> wrapping a <see cref="DoesntUntapEffect"/>.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseDoesntUntap(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+(?:permanent|creature|artifact|enchantment|land)\s+doesn'?t\s+untap\s+during\s+(?<possessive>your|its\s+controller'?s)\s+untap\s+step\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var possessive = match.Groups["possessive"].Value.Trim();
    // Normalise "its controller's" → "its controller's" (preserve apostrophe);
    // gold uses the literal possessive token so case-fold to lower-case "your".
    if (possessive.Equals("your", StringComparison.OrdinalIgnoreCase))
    {
      possessive = "your";
    }
    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Control.DoesntUntapEffect
        {
          WhoseUntapStep = possessive,
        },
      },
    ];
  }

  /// <summary>
  /// Matches "This creature can't be blocked except by [colour] creatures." —
  /// a structured evasion restriction (Rule 509). Builds a
  /// <see cref="EvasionEffect.CanBeBlockedBy"/> filter that captures the
  /// colour and card-type qualifiers in the "except by" tail.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEvasion(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+except\s+by\s+(?<tail>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var tail = match.Groups["tail"].Value.ToLowerInvariant();
    var colors = new List<string>();
    var colorMap = new Dictionary<string, string>
    {
      ["white"] = "W",
      ["blue"] = "U",
      ["black"] = "B",
      ["red"] = "R",
      ["green"] = "G",
    };
    foreach (var (name, code) in colorMap)
    {
      if (Regex.IsMatch(tail, $@"\b{name}\b"))
      {
        colors.Add(code);
      }
    }
    var cardTypes = new List<string>();
    foreach (var t in new[] { "creature", "permanent", "artifact" })
    {
      if (Regex.IsMatch(tail, $@"\b{t}s?\b"))
      {
        cardTypes.Add(t);
      }
    }

    var canBeBlockedBy = new ObjectFilter
    {
      CardTypes = cardTypes.Count > 0 ? cardTypes : null,
      Colors = colors.Count > 0 ? colors : null,
    };

    return
    [
      new StaticAbility
      {
        Effect = new MagicAST.AST.Effects.Keyword.EvasionEffect { CanBeBlockedBy = canBeBlockedBy },
      },
    ];
  }

  /// <summary>
  /// Matches a bare "Enchant [target descriptor]" line — the Aura legal-target
  /// declaration (Rule 702.5). The descriptor is mapped onto an
  /// <see cref="ObjectFilter"/> covering the common shapes ("creature", "land",
  /// "permanent"); more elaborate shapes (e.g. "Enchant creature you control")
  /// land their qualifiers on the filter axes.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEnchant(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*Enchant\s+(?<descriptor>.+?)\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var descriptor = match.Groups["descriptor"].Value.Trim().ToLowerInvariant();
    if (descriptor.Length == 0)
    {
      return null;
    }

    var filter = BuildEnchantFilter(descriptor);
    if (filter is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        KeywordSource = "Enchant",
        Effect = new MagicAST.AST.Effects.Combat.EnchantRestrictionEffect
        {
          LegalTargets = filter,
        },
      },
    ];
  }

  /// <summary>
  /// Builds an <see cref="ObjectFilter"/> from the descriptor noun-phrase that
  /// follows the <c>Enchant</c> keyword. Returns null for descriptors the
  /// parser doesn't yet recognise so the fallback path can report the gap.
  /// </summary>
  private static ObjectFilter? BuildEnchantFilter(string descriptor)
  {
    // Strip leading "a "/"an " articles that appear in some printings.
    var d = Regex.Replace(descriptor, @"^(?:a|an)\s+", "", RegexOptions.IgnoreCase).Trim();

    ControllerFilter? controller = null;
    if (d.EndsWith(" you control"))
    {
      controller = ControllerFilter.You;
      d = d[..^" you control".Length].Trim();
    }
    else if (d.EndsWith(" an opponent controls"))
    {
      controller = ControllerFilter.Opponent;
      d = d[..^" an opponent controls".Length].Trim();
    }

    // Simple-noun shape: "creature", "land", "permanent", "artifact", "enchantment".
    var simpleTypes = new[] { "creature", "land", "permanent", "artifact", "enchantment", "planeswalker", "player" };
    if (simpleTypes.Contains(d))
    {
      return new ObjectFilter { CardTypes = [d], Controller = controller };
    }

    return null;
  }

  /// <summary>
  /// Recognizes "[subject] attacks each combat if able." Subject may be the
  /// literal phrase "This creature"/"This permanent" or the card's own name
  /// (any leading word(s) before "attacks") — both mapped to <c>Self</c>;
  /// or "All creatures", which targets every creature (<c>Each</c> with a
  /// creature-typed filter, e.g. Grand Melee). Produces a
  /// <see cref="StaticAbility"/> wrapping a <see cref="MustAttackEffect"/>.
  /// </summary>
  /// <remarks>
  /// Card-name-as-subject is the standard oracle-text convention for self-reference
  /// in continuous abilities on a named permanent — the parser treats any leading
  /// word(s) before <c>attacks</c> as a synonym for <c>Self</c> when the rest of
  /// the line matches the restriction phrase. The "All creatures" variant is the
  /// global Rule 508.1d shape used by symmetric attack-requirement enchantments.
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseMustAttack(OracleClause clause)
  {
    var match = _mustAttackPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var target = ClassifyCombatRequirementSubject(match.Groups["subject"].Value);

    return
    [
      new StaticAbility
      {
        Effect = new MustAttackEffect { Target = target },
      },
    ];
  }

  private static readonly Regex _mustAttackPattern = new(
    @"^\s*(?<subject>\S.*?)\s+attacks?\s+each\s+combat\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// Recognizes "[Self] must be blocked if able." where [Self] is either
  /// the literal phrase "This creature"/"This permanent" or the card's own name
  /// (any leading word(s) before "must be blocked"). Produces a <see cref="StaticAbility"/>
  /// wrapping a <see cref="MustBeBlockedEffect"/> targeting <c>Self</c>.
  /// </summary>
  /// <remarks>
  /// Mirrors the must-attack pattern above. The leading subject is captured liberally
  /// (any non-empty prefix) on the same rationale: card-name-as-subject is the standard
  /// oracle-text convention for self-reference on a named permanent.
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseMustBeBlocked(OracleClause clause)
  {
    if (!_mustBeBlockedPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effect = new MustBeBlockedEffect { Target = ObjectReference.Self() },
      },
    ];
  }

  private static readonly Regex _mustBeBlockedPattern = new(
    @"^\s*\S.*?\s+must\s+be\s+blocked\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Recognizes "[subject] blocks each combat if able." — the blocker-side
  /// Rule 509.1c requirement. Mirrors <see cref="TryParseMustAttack"/>; subject
  /// classification (Self vs. "All creatures") flows through the shared
  /// <see cref="ClassifyCombatRequirementSubject"/> helper so attacker- and
  /// blocker-side lines stay shape-aligned.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseMustBlock(OracleClause clause)
  {
    var match = _mustBlockPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var target = ClassifyCombatRequirementSubject(match.Groups["subject"].Value);

    return
    [
      new StaticAbility
      {
        Effect = new MustBlockEffect { Target = target },
      },
    ];
  }

  private static readonly Regex _mustBlockPattern = new(
    @"^\s*(?<subject>\S.*?)\s+blocks?\s+each\s+combat\s+if\s+able\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Maps the captured subject phrase of a combat-requirement line onto the
  /// matching <see cref="ObjectReference"/>. Lines like "All creatures" become
  /// an <c>Each</c>-kinded reference with a creature filter (Grand Melee
  /// shape); everything else — including the card's own name or "This
  /// creature" / "This permanent" — collapses to <c>Self</c> (the
  /// long-standing convention for self-referential continuous abilities).
  /// </summary>
  private static ObjectReference ClassifyCombatRequirementSubject(string subjectText)
  {
    var subject = subjectText.Trim();
    if (subject.Equals("All creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    return ObjectReference.Self();
  }

  /// <summary>
  /// Recognizes lines of the form
  /// <c>[filter] has "[activated-ability text]"</c> — the Aura-style oracle
  /// shape for granting a permanent an activated ability (Rule 113.6/113.10).
  /// </summary>
  /// <remarks>
  /// The granted body is re-tokenized and handed to
  /// <see cref="ActivatedAbilityParser"/>, so the recursive
  /// <see cref="GainAbilityEffect.GainedAbility"/> matches the same shape
  /// the inner ability would have if it appeared directly on a card.
  /// We only return a hit when the inner parse succeeds — otherwise we
  /// fall through and let the fallback path surface a structured failure.
  /// </remarks>
  private IReadOnlyList<Ability>? TryParseGrantedAbility(
    OracleClause clause,
    ClauseClassification classification
  )
  {
    var match = _grantedAbilityPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var body = match.Groups["body"].Value.Trim();
    if (body.Length == 0)
    {
      return null;
    }

    var target = ClassifyGrantTarget(filterText);
    if (target is null)
    {
      return null;
    }

    var innerAbility = TryParseGrantedBody(body);
    if (innerAbility is null)
    {
      // The body's shape isn't yet supported by ActivatedAbilityParser.
      // Surface as a parser miss — the fallback path will record the gap.
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effect = new GainAbilityEffect
        {
          Target = target,
          GainedAbility = innerAbility,
        },
      },
    ];
  }

  /// <summary>
  /// Hands the quoted body off to <see cref="ActivatedAbilityParser"/>.
  /// </summary>
  private Ability? TryParseGrantedBody(string body)
  {
    var tokenResult = _tokenizer.TryTokenize(body);
    var tokens = tokenResult.HasValue
      ? tokenResult.Value
      : new TokenList<OracleToken>([]);

    var innerClause = new OracleClause
    {
      Tokens = tokens,
      RawText = body,
      SourceSpan = new MagicAST.AST.TextSpan(0, body.Length),
    };
    var innerClassification = new ClauseClassification
    {
      Kind = AbilityKind.Activated,
      Confidence = 1.0,
    };

    var inner = new ActivatedAbilityParser().TryParse(innerClause, innerClassification);
    return inner;
  }

  /// <summary>
  /// Maps the noun-phrase left of "has" onto an ObjectReference target.
  /// Three shapes are recognized today:
  /// <list type="bullet">
  ///   <item>Aura-vocabulary ("enchanted [type]" / "equipped [type]") collapses to
  ///         <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>; the kind itself
  ///         conveys the relationship, so no filter is emitted.</item>
  ///   <item>"All [Subtype]s" (e.g. <c>All Slivers</c>, <c>All Zombies</c>) — the
  ///         global tribal grant shape (Sliver-lords, anthem-style enchantments).
  ///         Maps to an <see cref="ObjectReferenceKind.Each"/> reference with a
  ///         <see cref="ObjectFilter.Subtypes"/> singleton holding the depluralised
  ///         subtype. The leading capital is the disambiguator: lower-case "all
  ///         creatures" would be a card-type grant (next bullet).</item>
  ///   <item>"[CardType]s you control" / "[CardType]s an opponent controls" — the
  ///         controller-scoped card-type grant (Citanul Hierophants, anthem-style
  ///         lords). Lowercase plural card-type noun followed by a controller
  ///         clause. Maps to an <see cref="ObjectReferenceKind.Each"/> reference
  ///         with the singularised card-type on <see cref="ObjectFilter.CardTypes"/>
  ///         and the matching <see cref="ControllerFilter"/>.</item>
  /// </list>
  /// </summary>
  private static ObjectReference? ClassifyGrantTarget(string filterText)
  {
    var trimmed = filterText.Trim();
    var lower = trimmed.ToLowerInvariant();
    if (lower.StartsWith("enchanted ") || lower.StartsWith("equipped "))
    {
      return new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };
    }

    // "Creatures you control" / "Artifacts an opponent controls" — controller-scoped
    // card-type grant. The lower-case plural card-type noun is what distinguishes
    // this from the capitalised-subtype "All [Subtype]s" branch below; the trailing
    // controller clause carries the scope onto ObjectFilter.Controller.
    var controlMatch = Regex.Match(
      trimmed,
      @"^(?<type>Creatures|Artifacts|Enchantments|Lands|Planeswalkers|Permanents)\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$",
      RegexOptions.IgnoreCase
    );
    if (controlMatch.Success)
    {
      var plural = controlMatch.Groups["type"].Value.ToLowerInvariant();
      // Depluralise — oracle plurals here are always simple "-s".
      var singular = plural.EndsWith('s') ? plural[..^1] : plural;
      var ctrl = controlMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var controller = ctrl.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [singular],
          Controller = controller,
        },
      };
    }

    // "Shamans you control" / "Goblins an opponent controls" — controller-scoped
    // tribal grant (Sachi, Daughter of Seshiro). Distinguished from the
    // card-type branch above by a capitalised subtype-plural noun, and from the
    // global "All [Subtype]s" branch below by the trailing controller clause —
    // the controller scope is what's load-bearing for this shape, and we
    // surface it onto ObjectFilter.Controller. No CardTypes is emitted: the
    // subtype carries the type-line constraint implicitly (Rule 205.3), and
    // the gold for Sachi confirms it.
    var tribalControlMatch = Regex.Match(
      trimmed,
      @"^(?<sub>[A-Z][a-z]+)s\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$"
    );
    if (tribalControlMatch.Success)
    {
      var subtype = tribalControlMatch.Groups["sub"].Value;
      var ctrl = tribalControlMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var controller = ctrl.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = controller,
        },
      };
    }

    // "All Slivers" / "All Zombies" — capitalised plural noun after a literal "All ".
    // We match the singular by stripping a trailing "s"; oracle text capitalises
    // creature subtypes, which is what lets us distinguish a subtype grant from a
    // generic "all creatures" grant (different shape, not handled here yet).
    var allMatch = Regex.Match(
      trimmed,
      @"^All\s+(?<sub>[A-Z][a-z]+)s\b\.?$"
    );
    if (allMatch.Success)
    {
      var subtype = allMatch.Groups["sub"].Value;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { Subtypes = [subtype] },
      };
    }
    return null;
  }

  // Anchors a single clause (no in-line newlines reach this layer — clauses
  // are split before us). Captures the noun-phrase subject and the quoted body
  // verbatim; nested quotes inside the body are unlikely in oracle text and
  // are out of scope for this first cut.
  //
  // Verb is "has" or "have" — oracle text agrees the verb with the subject:
  // singular ("Enchanted creature has", Find the Path's Aura grant) vs. plural
  // ("All Slivers have", Telekinetic Sliver's global tribal grant). Both shapes
  // land on the same GainAbilityEffect node; ClassifyGrantTarget distinguishes
  // the subject.
  private static readonly Regex _grantedAbilityPattern = new(
    @"^\s*(?<filter>[^""""]+?)\s+(?:has|have)\s+[""""](?<body>[^""""]+)[""""]\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  #region Keyword Parsing

  /// <summary>
  /// Parses comma-separated keyword abilities using token combinators.
  /// Example: "Flying, first strike, lifelink" → 3 separate StaticAbility nodes
  /// </summary>
  private IReadOnlyList<Ability>? TryParseKeywordList(TokenList<OracleToken> tokens)
  {
    // Try to parse using the OracleParsers.KeywordList combinator
    var parseResult = OracleParsers.KeywordList(tokens);

    if (!parseResult.HasValue)
    {
      return null;
    }

    // Convert StaticAbility[] to IReadOnlyList<Ability>
    return parseResult.Value.Cast<Ability>().ToList();
  }

  #endregion
}
