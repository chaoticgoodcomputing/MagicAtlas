namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Keyword;
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
      _fallback.Parse(
        clause,
        classification,
        "Static ability parser not yet implemented",
        lastAttemptedRule: "StaticAbilityParser.Parse",
        failurePosition: clause.SourceSpan.Start
      ),
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

    // "You control enchanted [type]." — Aura control-steal static (Rules 612,
    // 702.5). Emits a GainControlEffect targeting EnchantedOrEquipped. The
    // "you" (controller) is implicit — Rule 612 treats the current controller
    // of the Aura as the new controller of the enchanted permanent for as long
    // as the Aura remains attached; no Duration field is needed (Aura-attached
    // behavior persists while the Aura is on the battlefield).
    var gainControlEnchanted = TryParseGainControlOfEnchanted(clause);
    if (gainControlEnchanted != null)
    {
      return gainControlEnchanted;
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

    // "[Filter] spells you cast cost {N} less to cast." — type/color/subtype/
    // supertype-filtered cost reduction (Rule 117.6). The filter goes on
    // StaticAbility.AffectedObjects; the effect is a flat-amount
    // CostReductionEffect. Generic-mana amounts only — colored / hybrid
    // reductions (Ragemonger shape) are out of scope.
    var typeSpellCostReduction = TryParseTypeSpellCostReduction(clause);
    if (typeSpellCostReduction != null)
    {
      return typeSpellCostReduction;
    }

    // "TypeA and TypeB spells you cast cost {N} less to cast." and
    // "TypeA spells and TypeB spells you cast cost {N} less to cast." —
    // conjunctive two-type filtered cost reduction (Rule 117.6). Covers the
    // Banneret family and the Starnheim Courser / Goblin Electromancer shapes.
    var conjunctiveSpellCostReduction = TryParseConjunctiveTypeSpellCostReduction(clause);
    if (conjunctiveSpellCostReduction != null)
    {
      return conjunctiveSpellCostReduction;
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

    // "If you would create a Clue, Food, or Treasure token, instead create
    // one of each." — Rule 614 pure substitution: the original token creation
    // is replaced by creating one of each listed token type (Academy Manufactor
    // shape). More specific than the augmentation pattern below — must be tried
    // first so the "instead" keyword routes here, not to the "plus" branch.
    var tokenSubstitution = TryParseTokenSubstitutionReplacement(clause);
    if (tokenSubstitution != null)
    {
      return tokenSubstitution;
    }

    // "If one or more tokens would be created under your control, those tokens
    // plus that many [token-def] are created instead." — Rule 614 augmentation
    // (Doubling Season / Chatterfang shape). OriginalEventOccurs=true: the
    // original tokens still appear; the replacement ADDS more.
    var tokenAugmentation = TryParseTokenAugmentationReplacement(clause);
    if (tokenAugmentation != null)
    {
      return tokenAugmentation;
    }

    // "If an effect would create one or more tokens under your control, it
    // creates twice that many of those tokens instead." — Rule 614 scaling
    // replacement: the token count is doubled (Doubling Season token line).
    // OriginalEventOccurs=false with a Modifier of Type "double".
    var tokenDoubling = TryParseTokenDoublingReplacement(clause);
    if (tokenDoubling != null)
    {
      return tokenDoubling;
    }

    // "If an effect would put one or more counters on a permanent you control,
    // it puts twice that many of those counters on that permanent instead." —
    // Rule 614 scaling replacement: the counter count is doubled (Doubling
    // Season counter line). OriginalEventOccurs=false with Modifier "double".
    var counterDoubling = TryParseCounterDoublingReplacement(clause);
    if (counterDoubling != null)
    {
      return counterDoubling;
    }

    // "[CardName]'s power is equal to the number of [type] you control." —
    // Characteristic-defining ability (Rule 604.3) that defines the creature's
    // power (and/or toughness) as equal to a count of permanents. The * in the
    // P/T box is explained by this ability. Emits a DefinePTEffect with a
    // CountQuantity, wrapped in a StaticAbility.
    var definePT = TryParseDefinePT(clause);
    if (definePT != null)
    {
      return definePT;
    }

    // "This creature gets +N/+M for each <filter> you control." — self
    // P/T modifier scaled by a count of permanents the controller controls
    // (Rule 613.4c, layer 7c — effects and counters that modify power and/or
    // toughness). PowerModifier or ToughnessModifier is a CountQuantity whose
    // CountOf captures the filter + "you control" as a free-text phrase; the
    // zero side uses LiteralQuantity.Of(0).
    var selfPTForEach = TryParseSelfPTForEach(clause);
    if (selfPTForEach != null)
    {
      return selfPTForEach;
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

    // "[FilterDescription] (get|gets) [+-]N/[+-]N." — lord-effect P/T buff on a
    // filtered set of creatures (Rule 613.1c, layer 7C). Handles global
    // (no controller), controller-scoped, color-scoped, subtype+type, and
    // bare-subtype filter shapes in a single consolidated surface.
    var lordPT = TryParseLordPTBuff(clause);
    if (lordPT != null)
    {
      return lordPT;
    }

    // "... as long as <condition>." — Rule 611 trailing-duration suffix on a
    // static-grant effect. Peels the suffix, parses the remaining text as
    // either a P/T buff or a keyword grant, then wraps the result's effect
    // with AsLongAsDuration. The condition text is stored verbatim — not
    // parsed further. Placed last so more-specific rules above get first look
    // at the full text; only reaches here when every other rule returns null.
    var asLongAs = TryParseAsLongAsStaticGrant(clause, classification);
    if (asLongAs != null)
    {
      return asLongAs;
    }

    // "(Enchanted|Equipped) creature has <keyword>." or
    // "<filter> tokens you control have <keyword>." — bare keyword grant with no
    // P/T modifier. Distinct from TryParseEnchantedPTAndKeyword (batch 5) which
    // requires the "+N/+M and has" conjunction. Placed before the composite rule
    // because both start with "Enchanted/Equipped creature" and the bare shape
    // is more common — we want it to win without the composite rule trying to
    // match and failing on the absent P/T modifier.
    var bareKeywordGrant = TryParseBareKeywordGrant(clause);
    if (bareKeywordGrant != null)
    {
      return bareKeywordGrant;
    }

    // "(Enchanted|Equipped) creature gets +N/+M and has <keyword>." — Aura/Equipment
    // composite static: a P/T buff bundled with a keyword grant on the attached
    // object (Rule 702.5 / 613.1c). Emits a CompositeEffect wrapping a
    // ModifyPTEffect and a GainAbilityEffect, both targeting EnchantedOrEquipped.
    // No Duration: the modifier persists while the Aura/Equipment is attached.
    var enchantedPTAndKeyword = TryParseEnchantedPTAndKeyword(clause);
    if (enchantedPTAndKeyword != null)
    {
      return enchantedPTAndKeyword;
    }

    // "This [permanent/land/...] enters tapped." — Rule 614 replacement-effect
    // property recorded as a static-ability-attached <see cref="EntersTappedEffect"/>.
    // No KeywordSource: the oracle text is a full sentence, not a keyword token.
    // Placed last in the chain because the sentence shape is unambiguous and
    // won't compete with any keyword-dispatch path above.
    var entersTapped = TryParseEntersTapped(clause);
    if (entersTapped != null)
    {
      return entersTapped;
    }

    // "As this [permanent] enters, you may pay N life. If you don't, it
    // enters tapped." — Shockland pattern (Rule 614.1c as-enters static
    // replacement). Emits a <see cref="PayLifeOnEntryEffect"/> with
    // IsOptional=true and IfYouDoNot=EntersTappedEffect. Placed after
    // TryParseEntersTapped so the bare "This land enters tapped" shape gets
    // first look — the painland shape is distinguished by its leading
    // "As this ..." preamble and is unambiguous.
    var painland = TryParsePainland(clause);
    if (painland != null)
    {
      return painland;
    }

    // "This (creature|land|permanent) can't block." — Rule 509.1c blocker-side
    // restriction. Full declarative sentence, not a keyword token, so no
    // KeywordSource is set. Mirrors TryParseEntersTapped in structure.
    var cantBlock = TryParseCantBlock(clause);
    if (cantBlock != null)
    {
      return cantBlock;
    }

    // "Enchanted creature can't attack or block." — dual combat restriction on the
    // attached object (Pacifism / Luminous Bonds Aura shape). Emits two effects —
    // CantAttackEffect and CantBlockEffect — both targeting EnchantedOrEquipped.
    // Must be placed AFTER TryParseCantBlock to avoid shadowing the self-subject
    // single-restriction shape, and BEFORE TryParseCantBeBlocked for symmetry.
    var enchantedCantAttackOrBlock = TryParseEnchantedCantAttackOrBlock(clause);
    if (enchantedCantAttackOrBlock != null)
    {
      return enchantedCantAttackOrBlock;
    }

    // "This (creature|land|permanent) can't be blocked." — Rule 509.1b evasion
    // (full unblockability). Full declarative sentence, not a keyword token, so
    // no KeywordSource is set. Mirrors TryParseCantBlock in structure.
    var cantBeBlocked = TryParseCantBeBlocked(clause);
    if (cantBeBlocked != null)
    {
      return cantBeBlocked;
    }

    // "This creature enters with N +1/+1 counters on it." — Rule 614.1c
    // self-replacement effect property recorded as a static-ability-attached
    // EntersWithCountersEffect. No KeywordSource: the oracle text is a full
    // declarative sentence, not a keyword token. Count supports both
    // literal-N and variable-X printings. Mirrors TryParseEntersTapped.
    var entersWithCounters = TryParseEntersWithCounters(clause);
    if (entersWithCounters != null)
    {
      return entersWithCounters;
    }

    // "You may choose not to untap this [permanent] during your untap step." —
    // Rule 302.6 / 116 opt-out. Full sentence, not a keyword. Emits a
    // parameterless SkipUntapEffect marker with IsOptional=false (the "may
    // choose" language belongs to the untap-step turn-based action, not to the
    // IOptionalEffect "You may" prefix convention). Mirrors TryParseCantBlock.
    var skipUntap = TryParseSkipUntap(clause);
    if (skipUntap != null)
    {
      return skipUntap;
    }

    // "This creature can block only creatures with <characteristic>." —
    // Rule 509.1c blocker-side narrowing restriction. The creature CAN block,
    // but only attackers matching the filter (e.g. "creatures with flying").
    // No KeywordSource: the restriction is a full declarative sentence.
    var canBlockOnly = TryParseCanBlockOnly(clause);
    if (canBlockOnly != null)
    {
      return canBlockOnly;
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
          Effects = [new MagicAST.AST.Effects.Timing.CantBeCastEffect()],
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
          Effects = [new MagicAST.AST.Effects.Timing.CantBeCastEffect()],
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

    var power = int.Parse(match.Groups["psign"].Value + match.Groups["p"].Value);
    var toughness = int.Parse(match.Groups["tsign"].Value + match.Groups["t"].Value);

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }

  private static readonly Regex _anthemModifyPTPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This creature gets +N/+M for each &lt;filter&gt; you control." —
  /// self-referential P/T modifier scaled by a count of permanents the
  /// controller controls (Rule 613.4c, layer 7c — PT-modifier sublayer,
  /// with Rule 613.1g as the parent Layer 7 entry). The per-count increment
  /// must be 1 for both power and toughness sides — cards with multipliers
  /// other than 1 are not covered by this surface.
  ///
  /// <para>
  /// The modifier uses a <see cref="MagicAST.AST.Quantities.CountQuantity"/>
  /// whose <c>CountOf</c> is the filter noun-phrase followed by "you control"
  /// (verbatim from the oracle line). The zero side uses a
  /// <see cref="MagicAST.AST.Quantities.LiteralQuantity"/> of 0.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseSelfPTForEach(OracleClause clause)
  {
    var match = _selfPTForEachPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;

    // Only handle multiplier-1 increments. Multiplier > 1 is a different
    // family shape and should fall through to the fallback parser.
    if (Math.Abs(power) > 1 || Math.Abs(toughness) > 1)
    {
      return null;
    }

    // The oracle fragment after "for each" and before the period is the
    // filter description; the "you control" suffix is already part of the
    // oracle text and is included verbatim in CountOf.
    var filterPhrase = match.Groups["filter"].Value.Trim();
    // filterPhrase is the noun phrase between "for each" and the period;
    // it already ends with "you control" (captured from the regex).
    var countOf = filterPhrase;

    MagicAST.AST.Quantities.Quantity powerModifier = power == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = countOf };

    MagicAST.AST.Quantities.Quantity toughnessModifier = toughness == 0
      ? MagicAST.AST.Quantities.LiteralQuantity.Of(0)
      : new MagicAST.AST.Quantities.CountQuantity { CountOf = countOf };

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = ObjectReference.Self(),
          PowerModifier = powerModifier,
          ToughnessModifier = toughnessModifier,
        }],
      },
    ];
  }

  // "This creature gets +N/+M for each <filter> you control."
  // Captures the sign and digit for each side, and the complete filter
  // phrase (including the trailing "you control") between "for each" and
  // the terminal period.
  private static readonly Regex _selfPTForEachPattern = new(
    @"^\s*This\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+for\s+each\s+(?<filter>.+?\s+you\s+control)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "[CardName]'s power is equal to the number of [type] you control." —
  /// characteristic-defining ability (Rule 604.3) that defines the creature's
  /// power, toughness, or both as equal to a count of objects. The * in the
  /// P/T box is explained by this oracle line.
  ///
  /// <para>
  /// Three patterns are recognised:
  /// <list type="bullet">
  ///   <item><c>[Name]'s power is equal to the number of [filter].</c></item>
  ///   <item><c>[Name]'s toughness is equal to the number of [filter].</c></item>
  ///   <item><c>[Name]'s power and toughness are each equal to the number of [filter].</c></item>
  /// </list>
  /// All three emit a <see cref="DefinePTEffect"/> with the appropriate
  /// <see cref="PTCharacteristic"/> and a <see cref="CountQuantity"/> whose
  /// <c>CountOf</c> is the filter noun-phrase verbatim from oracle text.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseDefinePT(OracleClause clause)
  {
    // Try "power and toughness are each equal to" first (most specific).
    var bothMatch = _definePTBothPattern.Match(clause.RawText);
    if (bothMatch.Success)
    {
      var filter = bothMatch.Groups["filter"].Value.Trim();
      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = PTCharacteristic.Both,
            Value = new MagicAST.AST.Quantities.CountQuantity { CountOf = filter },
          }],
        },
      ];
    }

    // Try "power is equal to" or "toughness is equal to".
    var singleMatch = _definePTSinglePattern.Match(clause.RawText);
    if (singleMatch.Success)
    {
      var which = singleMatch.Groups["which"].Value.ToLowerInvariant();
      var filter = singleMatch.Groups["filter"].Value.Trim();
      var characteristic = which == "power"
        ? PTCharacteristic.Power
        : PTCharacteristic.Toughness;

      return
      [
        new StaticAbility
        {
          Effects = [new DefinePTEffect
          {
            Characteristic = characteristic,
            Value = new MagicAST.AST.Quantities.CountQuantity { CountOf = filter },
          }],
        },
      ];
    }

    return null;
  }

  // "[Name]'s power and toughness are each equal to the number of [filter]."
  // The <name> capture is non-greedy to stop at the possessive. The <filter>
  // capture grabs everything between "the number of " and the terminal period.
  private static readonly Regex _definePTBothPattern = new(
    @"^\s*.+?'s\s+power\s+and\s+toughness\s+are\s+each\s+equal\s+to\s+the\s+number\s+of\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "[Name]'s (power|toughness) is equal to the number of [filter]."
  private static readonly Regex _definePTSinglePattern = new(
    @"^\s*.+?'s\s+(?<which>power|toughness)\s+is\s+equal\s+to\s+the\s+number\s+of\s+(?<filter>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "(Enchanted|Equipped) creature gets +N/+M and has &lt;keyword&gt;." — Aura/Equipment
  /// composite static: a P/T buff bundled with a keyword grant on the attached
  /// object (Rule 702.5 / 613.1c). Emits a <see cref="StaticAbility"/> wrapping
  /// a <see cref="CompositeEffect"/> whose <c>Effects</c> list contains:
  /// <list type="bullet">
  ///   <item><see cref="ModifyPTEffect"/> targeting <c>EnchantedOrEquipped</c>.</item>
  ///   <item><see cref="GainAbilityEffect"/> targeting <c>EnchantedOrEquipped</c>,
  ///         whose <c>GainedAbility</c> is the canonical <see cref="StaticAbility"/>
  ///         for the keyword (via <see cref="MapKeywordToStaticAbility"/>).</item>
  /// </list>
  /// No <c>Duration</c>: the modifier persists while the Aura/Equipment is attached.
  /// Generalises to both "Enchanted" and "Equipped" subjects so Equipment cards
  /// with the same composite shape share the parser surface.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEnchantedPTAndKeyword(OracleClause clause)
  {
    var match = _enchantedPTAndKeywordPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var psign = match.Groups["psign"].Value;
    var power = int.Parse(match.Groups["p"].Value);
    if (psign == "-") power = -power;

    var tsign = match.Groups["tsign"].Value;
    var toughness = int.Parse(match.Groups["t"].Value);
    if (tsign == "-") toughness = -toughness;

    var kw = match.Groups["kw"].Value.Trim();
    var grantedAbility = MapKeywordToStaticAbility(kw);
    if (grantedAbility is null)
    {
      // Unrecognised keyword — fall through so the fallback surfaces the gap.
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Core.CompositeEffect
        {
          Effects =
          [
            new ModifyPTEffect
            {
              Target = target,
              PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
              ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            },
            new GainAbilityEffect
            {
              Target = target,
              GainedAbility = grantedAbility,
            },
          ],
        }],
      },
    ];
  }

  private static readonly Regex _enchantedPTAndKeywordPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+gets\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\s+and\s+has\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Bare keyword grant on an anchor or filter target — two arms in one surface:
  /// <list type="bullet">
  ///   <item><c>(Enchanted|Equipped) creature has &lt;keyword&gt;.</c> — the Aura/Equipment
  ///         static grant (Rule 702.5). Emits a <see cref="StaticAbility"/> wrapping a
  ///         <see cref="GainAbilityEffect"/> with <c>Target: EnchantedOrEquipped</c>. No P/T
  ///         modifier (that composite shape is handled by
  ///         <see cref="TryParseEnchantedPTAndKeyword"/>).</item>
  ///   <item><c>&lt;filter&gt; [tokens] you control have &lt;keyword&gt;.</c> — controller-scoped
  ///         continuous grant on a token (or creature) filter (Rule 613.1c). The filter arm
  ///         parses the leading noun phrase to determine card type, subtype, and the
  ///         <c>token</c> predicate (placed on <see cref="ObjectFilter.Characteristics"/>).</item>
  /// </list>
  /// Both arms share <see cref="MapKeywordToStaticAbility"/> so any keyword added there
  /// is available to both target shapes without further edits.
  /// </summary>
  /// <summary>
  /// Strips trailing reminder text — a parenthetical clause at the end of the
  /// oracle line — before matching patterns that use end-of-string anchors.
  /// Reminder text is purely explanatory (Rule 207.2); stripping it before pattern
  /// matching is safe because the gold AST does not carry the parenthetical on these
  /// bare-grant shapes.
  /// </summary>
  private static string StripReminderText(string text)
  {
    return Regex.Replace(text, @"\s*\([^)]*\)\s*$", string.Empty).Trim();
  }

  private static IReadOnlyList<Ability>? TryParseBareKeywordGrant(OracleClause clause)
  {
    // Strip trailing reminder text before pattern matching so lines like
    // "Creature tokens you control have deathtouch. (Any amount ...)" still match.
    var rawText = StripReminderText(clause.RawText);

    // Arm 1: anchor target — "(Enchanted|Equipped) creature has <keyword>."
    var anchorMatch = _bareAnchorKeywordPattern.Match(rawText);
    if (anchorMatch.Success)
    {
      var kw = anchorMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var grantedAbility = MapKeywordToStaticAbility(kw);
      if (grantedAbility is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            GainedAbility = grantedAbility,
          }],
        },
      ];
    }

    // Arm 3: "Other <filter> [you control] have <keyword>." — lord-grants-keyword
    // with the "Other" exclusion-of-self prefix (Rule 613.1c). The "Other" qualifier
    // maps to Characteristics: ["other"] on the filter, matching the convention used
    // by TryParseTribalAnthemModifyPT and TryParseLordPTBuff. Filter parsing is
    // delegated to ParseLordPTFilter (isOther: true) which handles the full range
    // of filter shapes: bare "creatures", "[Subtype] creatures", bare plural subtype,
    // "[Color] creatures", and the optional "you control" controller suffix.
    var otherMatch = _bareOtherKeywordPattern.Match(rawText);
    if (otherMatch.Success)
    {
      var otherFilterText = otherMatch.Groups["filter"].Value.Trim();
      // Include trailing "you control" in the filter text so ParseLordPTFilter
      // can peel it and set Controller = ControllerFilter.You.
      if (otherMatch.Groups["ctrl"].Success)
      {
        otherFilterText = otherFilterText + " you control";
      }
      var otherKw = otherMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
      var otherGranted = MapKeywordToStaticAbility(otherKw);
      if (otherGranted is null)
      {
        return null;
      }
      var otherFilter = ParseLordPTFilter(otherFilterText, isOther: true);
      if (otherFilter is null)
      {
        return null;
      }
      return
      [
        new StaticAbility
        {
          Effects = [new GainAbilityEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = otherFilter,
            },
            GainedAbility = otherGranted,
          }],
        },
      ];
    }

    // Arm 2: filter target — "<filter> [tokens] you control have <keyword>."
    // Handles: "Creature tokens you control have <kw>.",
    //          "<Subtype> tokens you control have <kw>.",
    //          "Creatures you control have <kw>."
    var filterMatch = _bareFilterKeywordPattern.Match(rawText);
    if (!filterMatch.Success)
    {
      return null;
    }

    var filterText = filterMatch.Groups["filter"].Value.Trim();
    var filterKw = filterMatch.Groups["kw"].Value.Trim().ToLowerInvariant();
    var isTokenFilter = filterMatch.Groups["token"].Success;

    var grantedAbility2 = MapKeywordToStaticAbility(filterKw);
    if (grantedAbility2 is null)
    {
      return null;
    }

    var target = BuildBareGrantFilterTarget(filterText, isTokenFilter);
    if (target is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainAbilityEffect
        {
          Target = target,
          GainedAbility = grantedAbility2,
        }],
      },
    ];
  }

  // Arm 1: "(Enchanted|Equipped) creature has <keyword>."
  // No P/T modifier allowed — this is the bare-grant shape only.
  private static readonly Regex _bareAnchorKeywordPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+has\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Arm 3: "Other <filter> [you control] have <keyword>." — "Other" prefix signals
  // the exclusion-of-self qualifier (Characteristics: ["other"]). The optional
  // "you control" group <ctrl> is captured separately so it can be stitched back
  // onto <filter> before passing to ParseLordPTFilter (which strips it and sets
  // Controller = ControllerFilter.You). Without "you control" the grant applies to
  // all qualifying permanents regardless of controller (e.g. Zombie Master).
  private static readonly Regex _bareOtherKeywordPattern = new(
    @"^\s*Other\s+(?<filter>[A-Za-z][A-Za-z ]+?)\s+(?<ctrl>you\s+control\s+)?have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.Compiled
  );

  // Arm 2: "<filter> [tokens] you control have <keyword>."
  // Captures the noun-phrase before the optional "tokens" word and the trailing keyword.
  // The "token" group is present only when the literal word "tokens" appears in the phrase,
  // indicating the grant is restricted to token permanents. BuildBareGrantFilterTarget
  // disambiguates the filter noun into three shapes: bare card-type ("Creatures"),
  // "[Subtype] creatures" (subtype + card-type), and bare plural subtype ("Goblins",
  // "Warlocks", etc.) — the last added in batch 33 for the lord-grants-keyword cluster.
  private static readonly Regex _bareFilterKeywordPattern = new(
    @"^\s*(?<filter>[A-Za-z][A-Za-z ]+?)\s+(?<token>tokens?\s+)?you\s+control\s+have\s+(?<kw>[a-z][a-z ]+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Builds an <see cref="ObjectReference"/> for the filter arm of
  /// <see cref="TryParseBareKeywordGrant"/>. Two filter shapes are recognised:
  /// <list type="bullet">
  ///   <item>"Creature(s)" — plain card-type filter; no subtype.</item>
  ///   <item>"[Subtype] creatures" or bare "[Subtype]" — subtype filter.</item>
  /// </list>
  /// When <paramref name="isToken"/> is <see langword="true"/> the filter
  /// includes <c>Characteristics: ["token"]</c> (matching how
  /// <c>ObjectFilter.Characteristics</c> encodes the token predicate).
  /// Returns <see langword="null"/> for unrecognised filter shapes.
  /// </summary>
  private static ObjectReference? BuildBareGrantFilterTarget(string filterText, bool isToken)
  {
    var lower = filterText.Trim().ToLowerInvariant();

    // "Creature" / "Creatures" — bare card-type filter.
    if (lower is "creature" or "creatures")
    {
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = isToken ? (IReadOnlyList<string>?)["token"] : null,
          Controller = ControllerFilter.You,
        },
      };
    }

    // "[Subtype] creature(s)" — subtype + card-type filter (e.g. "Wolf creatures").
    var subtypeCreatureMatch = Regex.Match(filterText, @"^(?<sub>[A-Z][a-z]+)\s+creatures?$");
    if (subtypeCreatureMatch.Success)
    {
      var subtype = subtypeCreatureMatch.Groups["sub"].Value;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Subtypes = [subtype],
          Characteristics = isToken ? (IReadOnlyList<string>?)["token"] : null,
          Controller = ControllerFilter.You,
        },
      };
    }

    // Bare plural subtype (e.g. "Goblins", "Warlocks", "Zombies") — no explicit
    // "creatures" word. The capitalised plural noun is the distinguishing feature;
    // the singular form is produced by <see cref="DepluralizeSubtype"/> so known
    // irregular plurals ("Elves" → "Elf") round-trip correctly. No CardTypes is
    // emitted: the subtype constraint on ObjectFilter.Subtypes already scopes the
    // filter to permanents carrying that subtype, regardless of card type (matching
    // the convention established by Sachi's Shamans you control gold).
    var bareSubtypeMatch = Regex.Match(filterText.Trim(), @"^(?<sub>[A-Z][a-z]+)s?$");
    if (bareSubtypeMatch.Success && !isToken)
    {
      var pluralWord = bareSubtypeMatch.Groups[0].Value;
      var subtype = DepluralizeSubtype(pluralWord);
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          Subtypes = [subtype],
          Controller = ControllerFilter.You,
        },
      };
    }

    return null;
  }

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
        Effects = [new ModifyPTEffect
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
        }],
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
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
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
        }],
      },
    ];
  }

  private static readonly Regex _drawReplacementPattern = new(
    @"^\s*If\s+you\s+would\s+draw\s+a\s+card,\s+draw\s+(?<count>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+cards?\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "If one or more tokens would be created under your control, those tokens
  /// plus that many &lt;P&gt;/&lt;T&gt; &lt;color&gt; &lt;subtype&gt; creature
  /// tokens are created instead." — Rule 614.1a (replacement effects using "instead"): the original token
  /// creation still occurs, AND an equal number of additional tokens (per the
  /// supplied token definition) are also created. Chatterfang shape; Doubling
  /// Season shares the structural pattern with a tweak ("twice that many").
  /// </summary>
  /// <remarks>
  /// <see cref="MagicAST.AST.Effects.Replacement.ReplacementEffect.OriginalEventOccurs"/>
  /// is <c>true</c>: the original tokens enter as printed; this rule layers on
  /// extra Squirrel tokens. The count on the replacement
  /// <see cref="MagicAST.AST.Effects.TokenCopy.CreateTokenEffect"/> is encoded
  /// as a <see cref="MagicAST.AST.Quantities.CalculatedQuantity"/> whose
  /// expression "that many" / operation "match" descriptively refers back to
  /// the count of the original token-creation event — see the manifest entry
  /// "Reminder-modifier convention" for the choice rationale.
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseTokenAugmentationReplacement(OracleClause clause)
  {
    var match = _tokenAugmentationPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    if (!_tokenAugmentationColorMap.TryGetValue(
      match.Groups["color"].Value.ToLowerInvariant(),
      out var colorCode))
    {
      return null;
    }

    var subtypeRaw = match.Groups["subtype"].Value;
    var subtype = char.ToUpperInvariant(subtypeRaw[0]) + subtypeRaw[1..];

    var replacement = new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
    {
      Count = new MagicAST.AST.Quantities.CalculatedQuantity
      {
        Expression = "that many",
        Operation = "match",
      },
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = match.Groups["p"].Value,
        Toughness = match.Groups["t"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [subtype],
        IsCopy = false,
      },
    };

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            MinimumQuantity = 1,
          },
          OriginalEventOccurs = true,
          Replacement = replacement,
        }],
      },
    ];
  }

  // Token-augmentation pattern (Chatterfang / Doubling-Season family). The
  // capture groups describe the ADDITIONAL token's printed face: power,
  // toughness, color, and creature subtype. The leading clause ("one or more
  // tokens would be created under your control") is fixed; per-event variation
  // (e.g. "twice that many" for Doubling Season) is intentionally NOT covered
  // here — that's a separate replacement-modifier shape.
  private static readonly Regex _tokenAugmentationPattern = new(
    @"^\s*If\s+one\s+or\s+more\s+tokens\s+would\s+be\s+created\s+under\s+your\s+control,\s+those\s+tokens\s+plus\s+that\s+many\s+(?<p>\d+)/(?<t>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens\s+are\s+created\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> _tokenAugmentationColorMap = new(
    StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W",
    ["blue"] = "U",
    ["black"] = "B",
    ["red"] = "R",
    ["green"] = "G",
  };

  /// <summary>
  /// "If you would create a Clue, Food, or Treasure token, instead create one
  /// of each." — Rule 614.1a pure substitution: the original single-token
  /// creation is replaced by creating one of each listed predefined token type
  /// (Academy Manufactor shape). The event filter captures the disjunction of
  /// subtypes; the replacement is a <see cref="CompositeEffect"/> with one
  /// <see cref="CreateTokenEffect"/> per listed token type.
  /// <para>
  /// <see cref="ReplacementEffect.OriginalEventOccurs"/> is <c>false</c>:
  /// the original token does NOT appear — the replacement produces all three
  /// predefined tokens instead.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseTokenSubstitutionReplacement(OracleClause clause)
  {
    var match = _tokenSubstitutionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Parse the comma-separated list of token subtypes from the "a X, Y, or Z token" clause.
    var subtypeList = match.Groups["subtypes"].Value;
    var subtypes = ParseDisjunctionList(subtypeList);
    if (subtypes.Count == 0)
    {
      return null;
    }

    // Build one CreateTokenEffect per subtype using predefined token factories.
    var createEffects = new List<Effect>();
    foreach (var subtype in subtypes)
    {
      var token = subtype switch
      {
        "Clue" => MagicAST.AST.Effects.TokenDefinition.Clue(),
        "Food" => MagicAST.AST.Effects.TokenDefinition.Food(),
        "Treasure" => MagicAST.AST.Effects.TokenDefinition.Treasure(),
        "Blood" => MagicAST.AST.Effects.TokenDefinition.Blood(),
        _ => null,
      };
      if (token is null)
      {
        return null; // Unknown predefined token type — bail.
      }
      createEffects.Add(new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
      {
        Count = MagicAST.AST.Quantities.LiteralQuantity.Of(1),
        Token = token,
      });
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            TokenFilter = new ObjectFilter
            {
              Subtypes = subtypes,
            },
          },
          OriginalEventOccurs = false,
          Replacement = new MagicAST.AST.Effects.Core.CompositeEffect
          {
            Effects = createEffects,
          },
        }],
      },
    ];
  }

  // Pattern: "If you would create a [Subtype1], [Subtype2], or [Subtype3] token,
  // instead create one of each."
  // The subtypes group captures the comma-and-"or"-delimited list between "a " and
  // " token". The list must contain at least two elements (single-element would be
  // a different pattern — "If you would create a Treasure token, instead ...").
  private static readonly Regex _tokenSubstitutionPattern = new(
    @"^\s*If\s+you\s+would\s+create\s+a\s+(?<subtypes>[A-Z][a-z]+(?:,\s+[A-Z][a-z]+)*,?\s+or\s+[A-Z][a-z]+)\s+token,\s+instead\s+create\s+one\s+of\s+each\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Parses a comma-and-"or"-delimited English list into individual tokens.
  /// Handles "A, B, or C", "A or B", and "A, B, C, or D" shapes.
  /// </summary>
  private static IReadOnlyList<string> ParseDisjunctionList(string text)
  {
    // Split on ", " and " or " to get individual tokens.
    // Handle "A, B, or C" → ["A", "B", "C"]
    var result = new List<string>();
    var parts = text.Split(new[] { ", or ", ",or ", " or " }, StringSplitOptions.RemoveEmptyEntries);
    foreach (var part in parts)
    {
      var trimmed = part.Trim();
      // The first part may contain further comma-separated items: "A, B" from "A, B, or C"
      var subParts = trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries);
      foreach (var sub in subParts)
      {
        var s = sub.Trim();
        if (s.Length > 0)
        {
          result.Add(s);
        }
      }
    }
    return result;
  }

  /// <summary>
  /// "If an effect would create one or more tokens under your control, it creates
  /// twice that many of those tokens instead." — Rule 614.1a scaling replacement:
  /// the original token-creation count is doubled (Doubling Season token line).
  /// <see cref="ReplacementEffect.OriginalEventOccurs"/> is <c>false</c>: the
  /// original event is fully replaced by the doubled version. The scaling is
  /// described by <see cref="ReplacementModifier"/> with <c>Type = "double"</c>;
  /// no separate <see cref="ReplacementEffect.Replacement"/> is needed since the
  /// replacement IS the scaled original event.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseTokenDoublingReplacement(OracleClause clause)
  {
    if (!_tokenDoublingPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.TokenCreationEvent
          {
            MinimumQuantity = 1,
            Controller = ObjectReference.You(),
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = "double",
          },
        }],
      },
    ];
  }

  // Pattern: "If an effect would create one or more tokens under your control,
  // it creates twice that many of those tokens instead."
  private static readonly Regex _tokenDoublingPattern = new(
    @"^\s*If\s+an\s+effect\s+would\s+create\s+one\s+or\s+more\s+tokens\s+under\s+your\s+control,\s+it\s+creates\s+twice\s+that\s+many\s+of\s+those\s+tokens\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "If an effect would put one or more counters on a permanent you control, it
  /// puts twice that many of those counters on that permanent instead." — Rule 614
  /// scaling replacement: the counter-placement count is doubled (Doubling Season
  /// counter line). Same pattern as
  /// <see cref="TryParseTokenDoublingReplacement"/> but on the
  /// <see cref="CounterPlacementEvent"/> axis.
  /// <see cref="ReplacementEffect.OriginalEventOccurs"/> is <c>false</c>; the
  /// scaling is carried by <see cref="ReplacementModifier"/> <c>Type = "double"</c>.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseCounterDoublingReplacement(OracleClause clause)
  {
    if (!_counterDoublingPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.CounterPlacementEvent
          {
            MinimumQuantity = 1,
            AffectedObjects = new ObjectFilter
            {
              CardTypes = ["permanent"],
              Controller = ControllerFilter.You,
            },
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = "double",
          },
        }],
      },
    ];
  }

  // Pattern: "If an effect would put one or more counters on a permanent you
  // control, it puts twice that many of those counters on that permanent instead."
  private static readonly Regex _counterDoublingPattern = new(
    @"^\s*If\s+an\s+effect\s+would\s+put\s+one\s+or\s+more\s+counters\s+on\s+a\s+permanent\s+you\s+control,\s+it\s+puts\s+twice\s+that\s+many\s+of\s+those\s+counters\s+on\s+that\s+permanent\s+instead\.?\s*$",
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
      "a" => 1,
      "an" => 1,
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
        Effects = [new MagicAST.AST.Effects.Replacement.LegendRuleSuppressionEffect()],
      },
    ];
  }

  private static readonly Regex _legendRuleSuppressionPattern = new(
    @"^\s*The\s+[""""“”]legend\s+rule[""""“”]\s+doesn'?t\s+apply\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This spell costs {N} less to cast [condition]." — flat-amount self-cost
  /// reduction conditioned on game state or timing.  Two condition shapes are
  /// recognised (Rule 117.6):
  /// <list type="bullet">
  ///   <item><c>during [your turn / each opponent's turn / combat]</c> —
  ///         Mental Modulation shape.</item>
  ///   <item><c>if you control a [Subtype]</c> — Wizard's Lightning /
  ///         Squash / Lookout's Dispersal shape; the subtype is any
  ///         capitalised noun (Wizard, Giant, Pirate, …).</item>
  /// </list>
  /// Emits a <see cref="StaticAbility"/> whose <c>Condition.Text</c> preserves
  /// the oracle-text condition verbatim, wrapping a
  /// <see cref="MagicAST.AST.Effects.Resource.CostReductionEffect"/> with a
  /// literal generic-mana <c>Amount</c>.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseConditionalSpellCostReduction(OracleClause clause)
  {
    var match = Regex.Match(
      clause.RawText,
      @"^\s*This\s+spell\s+costs\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\s+(?<cond>"
        + @"during\s+(?:your\s+turn|each\s+(?:opponent|player)'?s\s+turn|combat)"
        + @"|if\s+you\s+control\s+an?\s+[A-Z][A-Za-z]+"
        + @")\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }
    var amount = int.Parse(match.Groups["amount"].Value);
    // Preserve oracle-text casing for the condition verbatim so the fixture
    // comparison sees exactly what the oracle line says (e.g. "if you control
    // a Wizard", "during your turn").
    var conditionText = match.Groups["cond"].Value;
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        Condition = new MagicAST.AST.Abilities.Condition { Text = conditionText },
      },
    ];
  }

  /// <summary>
  /// "&lt;filter&gt; spells you cast cost {N} less to cast." — type/color/
  /// subtype/supertype-filtered cost reduction (Rule 117.6 — cost
  /// modification). The filter noun-phrase before "spells" maps onto a
  /// <see cref="ObjectFilter"/> with <c>CardTypes: ["spell"]</c> as the root,
  /// augmented by one of:
  /// <list type="bullet">
  ///   <item><c>Colors</c> — when the filter is a colour name
  ///         (Red/White/Blue/Black/Green).</item>
  ///   <item><c>CardTypes</c> — when the filter names another card type
  ///         (Artifact/Creature/Enchantment/Instant/Sorcery/Planeswalker/Land/
  ///         Battle); the type is appended so the filter becomes
  ///         <c>["spell", "&lt;type&gt;"]</c>.</item>
  ///   <item><c>Supertypes</c> — when the filter names a supertype
  ///         (Legendary/Snow/Basic).</item>
  ///   <item><c>Subtypes</c> — otherwise (Angel, Aura, Giant, Goblin, …).</item>
  /// </list>
  /// The trailing "you cast" qualifier maps to
  /// <c>Controller = ControllerFilter.You</c>. The reduction amount is a
  /// generic literal (e.g. <c>{1}</c>, <c>{2}</c>) emitted as a
  /// <see cref="MagicAST.AST.Quantities.LiteralQuantity"/>; coloured /
  /// hybrid-cost reductions (Ragemonger shape) are not covered here.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseTypeSpellCostReduction(OracleClause clause)
  {
    var match = _typeSpellCostReductionPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var amount = int.Parse(match.Groups["amount"].Value);

    var affected = BuildTypeSpellFilter(filterText);
    if (affected is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = affected,
      },
    ];
  }

  // Pattern: a single capitalised noun (no internal spaces — keeps compound
  // filters like "Instant and sorcery" or "White creature" out of scope so
  // they fall through to the fallback for a future family), then
  // " spells you cast cost {N} less to cast." with optional trailing period.
  // Amount is restricted to a single generic-mana digit (the cluster covers
  // {1} and {2} cleanly); coloured-cost reductions are a separate family.
  private static readonly Regex _typeSpellCostReductionPattern = new(
    @"^\s*(?<filter>[A-Z][A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  /// <summary>
  /// "TypeA and TypeB spells you cast cost {N} less to cast." (phrasing A)
  /// "TypeA spells and TypeB spells you cast cost {N} less to cast." (phrasing B)
  ///
  /// Two-type conjunctive cost reduction (Rule 117.6). The two types classify
  /// the disjunctive set of spells that benefit — a spell qualifies if it
  /// belongs to either type.
  ///
  /// Encoding conventions (descriptive, mirrors oracle structure):
  /// - If both nouns are card types (Rule 205.2): emit
  ///   <c>CardTypes: [typeA, typeB]</c> (no "spell" root — the type pair is
  ///   sufficient to describe the filter without creating a nonsensical
  ///   intersection).
  /// - If both nouns are subtypes (Rule 205.3) — the banneret pattern
  ///   ("Kithkin spells and Soldier spells"): emit
  ///   <c>CardTypes: ["spell"], Subtypes: [typeA, typeB]</c>.
  /// - Mixed (one card type, one subtype): not yet in the corpus; falls
  ///   through to null.
  ///
  /// Returns null for any noun that fails classification so the fallback
  /// parser can record an honest failure.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseConjunctiveTypeSpellCostReduction(OracleClause clause)
  {
    // Phrasing A: "TypeA and TypeB spells you cast cost {N} less to cast."
    var matchA = _conjunctiveTypeSpellCostReductionPatternA.Match(clause.RawText);
    // Phrasing B: "TypeA spells and TypeB spells you cast cost {N} less to cast."
    var matchB = _conjunctiveTypeSpellCostReductionPatternB.Match(clause.RawText);

    string typeA, typeB;
    int amount;

    if (matchA.Success)
    {
      typeA  = matchA.Groups["typeA"].Value.Trim();
      typeB  = matchA.Groups["typeB"].Value.Trim();
      amount = int.Parse(matchA.Groups["amount"].Value);
    }
    else if (matchB.Success)
    {
      typeA  = matchB.Groups["typeA"].Value.Trim();
      typeB  = matchB.Groups["typeB"].Value.Trim();
      amount = int.Parse(matchB.Groups["amount"].Value);
    }
    else
    {
      return null;
    }

    var affected = BuildConjunctiveTypeSpellFilter(typeA, typeB);
    if (affected is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
        }],
        AffectedObjects = affected,
      },
    ];
  }

  // Phrasing A: "Artifact and enchantment spells you cast cost {1} less to cast."
  // typeA is capitalised; typeB is lowercase (oracle style: "Instant and sorcery").
  private static readonly Regex _conjunctiveTypeSpellCostReductionPatternA = new(
    @"^\s*(?<typeA>[A-Z][A-Za-z]+)\s+and\s+(?<typeB>[A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  // Phrasing B: "Kithkin spells and Soldier spells you cast cost {1} less to cast."
  private static readonly Regex _conjunctiveTypeSpellCostReductionPatternB = new(
    @"^\s*(?<typeA>[A-Z][A-Za-z]+)\s+spells\s+and\s+(?<typeB>[A-Za-z]+)\s+spells\s+you\s+cast\s+cost\s+\{(?<amount>\d+)\}\s+less\s+to\s+cast\.?\s*$",
    RegexOptions.Compiled
  );

  /// <summary>
  /// Classifies two filter nouns captured from a conjunctive oracle phrase
  /// and builds an <see cref="ObjectFilter"/> that covers spells of either
  /// type. Returns <see langword="null"/> for unrecognised or mixed-tier
  /// noun pairs (triggers an honest fallback failure rather than silently
  /// emitting a wrong filter).
  /// </summary>
  private static ObjectFilter? BuildConjunctiveTypeSpellFilter(string nounA, string nounB)
  {
    var aIsCardType   = _spellFilterCardTypes.Contains(nounA);
    var bIsCardType   = _spellFilterCardTypes.Contains(nounB);
    var aIsColor      = _colorNameToCode.ContainsKey(nounA);
    var bIsColor      = _colorNameToCode.ContainsKey(nounB);
    var aIsSupertype  = _spellFilterSupertypes.Contains(nounA);
    var bIsSupertype  = _spellFilterSupertypes.Contains(nounB);

    // Both card types (Rule 205.2) — e.g. "Artifact and enchantment spells",
    // "Instant and sorcery spells". Emit as CardTypes disjunction; omit the
    // "spell" root because instant/sorcery cards that are on the stack are
    // always spells, and adding it would create an unreachable intersection
    // with types like Instant that cannot also be a "spell" card type.
    if (aIsCardType && bIsCardType)
    {
      return new ObjectFilter
      {
        CardTypes = [nounA.ToLowerInvariant(), nounB.ToLowerInvariant()],
        Controller = ControllerFilter.You,
      };
    }

    // Both colour names (Rule 105) — e.g. "Red spells and white spells"
    // (Familiar cycle from Planeshift). Emit as Colors disjunction.
    if (aIsColor && bIsColor)
    {
      _ = _colorNameToCode.TryGetValue(nounA, out var codeA);
      _ = _colorNameToCode.TryGetValue(nounB, out var codeB);
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Colors = [codeA!, codeB!],
        Controller = ControllerFilter.You,
      };
    }

    // One colour, one card type — not yet in corpus; fall through to null.
    // Both supertypes — not yet in corpus; fall through to null.

    // Otherwise treat both as creature/permanent subtypes (Rule 205.3) —
    // e.g. "Kithkin spells and Soldier spells" (Banneret cycle from
    // Morningtide). Root the filter at "spell" and carry both subtypes.
    if (!aIsCardType && !bIsCardType && !aIsColor && !bIsColor
        && !aIsSupertype && !bIsSupertype)
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Subtypes = [Capitalize(nounA), Capitalize(nounB)],
        Controller = ControllerFilter.You,
      };
    }

    return null;
  }

  // Card types the filter may name (lowercased on emit, matching the
  // existing CardTypes convention — see GaddockTeeg).
  private static readonly HashSet<string> _spellFilterCardTypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "Artifact", "Creature", "Enchantment", "Instant", "Sorcery",
      "Planeswalker", "Land", "Battle", "Tribal",
    };

  // Supertypes the filter may name (PascalCase on emit — the supertype
  // axis preserves casing, matching how the TypeLine record encodes them).
  private static readonly HashSet<string> _spellFilterSupertypes =
    new(StringComparer.OrdinalIgnoreCase)
    {
      "Legendary", "Snow", "Basic", "World", "Ongoing",
    };

  /// <summary>
  /// Maps the filter noun captured before "spells" onto an
  /// <see cref="ObjectFilter"/> rooted at <c>CardTypes: ["spell"]</c>. The
  /// noun is classified in priority order: colour → card type → supertype →
  /// subtype (catch-all). Returns <see langword="null"/> when the noun is
  /// empty (defensive — the regex requires at least one letter).
  /// </summary>
  private static ObjectFilter? BuildTypeSpellFilter(string filterNoun)
  {
    if (string.IsNullOrWhiteSpace(filterNoun))
    {
      return null;
    }

    // Colour adjective (Rule 105) — emit as Colors single-letter code.
    if (_colorNameToCode.TryGetValue(filterNoun, out var colorCode))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Colors = [colorCode],
        Controller = ControllerFilter.You,
      };
    }

    // Colorless filter (Rule 105.1 — "Colorless is not a color"); encoded
    // as IsColorless rather than on the Colors axis.
    if (filterNoun.Equals("Colorless", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        IsColorless = true,
        Controller = ControllerFilter.You,
      };
    }

    // Card type (Rule 205.2) — appended to the CardTypes axis so the filter
    // reads as "a spell that is also of type X" (multi-element CardTypes
    // precedent: e.g. "artifact land" → ["artifact", "land"]).
    if (_spellFilterCardTypes.Contains(filterNoun))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell", filterNoun.ToLowerInvariant()],
        Controller = ControllerFilter.You,
      };
    }

    // Supertype (Rule 205.4) — emit on the Supertypes axis, PascalCase.
    if (_spellFilterSupertypes.Contains(filterNoun))
    {
      return new ObjectFilter
      {
        CardTypes = ["spell"],
        Supertypes = [Capitalize(filterNoun)],
        Controller = ControllerFilter.You,
      };
    }

    // Otherwise treat as a creature/permanent subtype (Rule 205.3) — the
    // catch-all branch handles tribal lords ("Angel spells", "Giant spells",
    // "Goblin spells", …) and equipment/aura subtypes ("Aura spells",
    // "Equipment spells", …).
    return new ObjectFilter
    {
      CardTypes = ["spell"],
      Subtypes = [Capitalize(filterNoun)],
      Controller = ControllerFilter.You,
    };
  }

  // Lowercase the rest, uppercase the first letter — matches oracle-text
  // casing for both subtypes and supertypes.
  private static string Capitalize(string s) =>
    s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s[1..].ToLowerInvariant();

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
        Effects = [effect],
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
        Effects = [new MagicAST.AST.Effects.Resource.CostReductionEffect
        {
          Amount = new MagicAST.AST.Quantities.DerivedQuantity
          {
            DerivedFrom = derivedKind,
            Source = source,
          },
          BasedOn = source,
        }],
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
  /// "Enchanted [type] doesn't untap during its controller's untap step." and
  /// produces a <see cref="StaticAbility"/> wrapping a <see cref="DoesntUntapEffect"/>.
  /// The enchanted-creature form targets <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>;
  /// the self-reference form leaves Target null (caller infers Self from context).
  /// Rule 701.20 (Untap); Rule 303.4 (Aura attachment / enchanted permanent).
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseDoesntUntap(OracleClause clause)
  {
    // Self-reference form: "This [type] doesn't untap during [your|its controller's] untap step."
    var selfMatch = Regex.Match(
      clause.RawText,
      @"^\s*This\s+(?:permanent|creature|artifact|enchantment|land)\s+doesn'?t\s+untap\s+during\s+(?<possessive>your|its\s+controller'?s)\s+untap\s+step\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (selfMatch.Success)
    {
      var possessive = selfMatch.Groups["possessive"].Value.Trim();
      if (possessive.Equals("your", StringComparison.OrdinalIgnoreCase))
      {
        possessive = "your";
      }
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Control.DoesntUntapEffect
          {
            WhoseUntapStep = possessive,
          }],
        },
      ];
    }

    // Aura form: "Enchanted [type] doesn't untap during its controller's untap step."
    // Targets the attached permanent via EnchantedOrEquipped.
    var enchantedMatch = Regex.Match(
      clause.RawText,
      @"^\s*(?:Enchanted|Equipped)\s+\w+\s+doesn'?t\s+untap\s+during\s+its\s+controller'?s\s+untap\s+step\.?\s*$",
      RegexOptions.IgnoreCase
    );
    if (enchantedMatch.Success)
    {
      return
      [
        new StaticAbility
        {
          Effects = [new MagicAST.AST.Effects.Control.DoesntUntapEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
            WhoseUntapStep = "its controller's",
          }],
        },
      ];
    }

    return null;
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
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect { CanBeBlockedBy = canBeBlockedBy }],
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
        Effects = [new MagicAST.AST.Effects.Combat.EnchantRestrictionEffect
        {
          LegalTargets = filter,
        }],
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
  /// "You control enchanted [type]." — Aura control-steal continuous effect
  /// (Rule 612 control-changing effects; Rule 702.5 Aura). Emits a
  /// <see cref="StaticAbility"/> wrapping a <see cref="GainControlEffect"/>
  /// whose <c>Target</c> is <see cref="ObjectReferenceKind.EnchantedOrEquipped"/>.
  ///
  /// <para>
  /// No <see cref="GainControlEffect.Duration"/> is set: the controller
  /// change persists while the Aura remains attached to the permanent —
  /// Aura-attached continuous effects have no explicit duration clause in
  /// oracle text; their end condition is implied by the Aura's detachment
  /// (Rule 702.5d), which the rules engine tracks, not the AST.
  /// </para>
  ///
  /// <para>
  /// Recognized [type] values: creature, permanent, land, artifact,
  /// enchantment, planeswalker, equipment (Equipment). The match is
  /// case-insensitive and the trailing period is optional.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseGainControlOfEnchanted(OracleClause clause)
  {
    if (!_gainControlEnchantedPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new GainControlEffect
        {
          Target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped },
        }],
      },
    ];
  }

  // Matches "You control enchanted <type>." where <type> is any recognised
  // permanent type. The type noun is not captured — the target is always
  // EnchantedOrEquipped regardless of the specific type word used, because
  // that information is already carried by the sibling "Enchant [type]" line.
  private static readonly Regex _gainControlEnchantedPattern = new(
    @"^\s*You\s+control\s+enchanted\s+(?:creature|permanent|land|artifact|enchantment|planeswalker|equipment)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

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
        Effects = [new MustAttackEffect { Target = target }],
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
        Effects = [new MustBeBlockedEffect { Target = ObjectReference.Self() }],
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
        Effects = [new MustBlockEffect { Target = target }],
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
        Effects = [new GainAbilityEffect
        {
          Target = target,
          GainedAbility = innerAbility,
        }],
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

    // "White creatures you control" / "Red artifacts an opponent controls" —
    // colour-scoped card-type grant (Resplendent Mentor). Same shape as the
    // controller-scoped card-type branch below, plus a colour adjective that
    // lands on ObjectFilter.Colors. The colour word is capitalised at the
    // start of a clause (oracle convention); the regex matches the colour
    // case-insensitively but the resulting code is normalised to the
    // single-letter colour code on ObjectFilter.Colors. Listed before the
    // bare card-type branch because that pattern is anchored at the
    // card-type noun — a leading colour word would simply fail it, not be
    // misclassified, but ordering keeps the colour-specific branch self-evident.
    var colorTypeMatch = Regex.Match(
      trimmed,
      @"^(?<color>White|Blue|Black|Red|Green)\s+(?<type>creatures|artifacts|enchantments|lands|planeswalkers|permanents)\s+(?<ctrl>you\s+control|an\s+opponent\s+controls)\.?$",
      RegexOptions.IgnoreCase
    );
    if (colorTypeMatch.Success)
    {
      var colorName = colorTypeMatch.Groups["color"].Value.ToLowerInvariant();
      var colorCode = colorName switch
      {
        "white" => "W",
        "blue" => "U",
        "black" => "B",
        "red" => "R",
        "green" => "G",
        _ => null,
      };
      if (colorCode is null)
      {
        return null;
      }
      var pluralType = colorTypeMatch.Groups["type"].Value.ToLowerInvariant();
      var singularType = pluralType.EndsWith('s') ? pluralType[..^1] : pluralType;
      var ctrlText = colorTypeMatch.Groups["ctrl"].Value.ToLowerInvariant();
      var colorController = ctrlText.StartsWith("you")
        ? ControllerFilter.You
        : ControllerFilter.Opponent;
      return new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = [singularType],
          Colors = [colorCode],
          Controller = colorController,
        },
      };
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

  /// <summary>
  /// "... as long as [condition]." — Rule 611 continuous-effect trailing
  /// duration. Detects the suffix, strips it, then tries to parse the
  /// remainder as either:
  /// <list type="bullet">
  ///   <item><c>This creature/This permanent gets +N/+M</c> —
  ///         <see cref="ModifyPTEffect"/> targeting <c>Self</c>.</item>
  ///   <item><c>[subject] has [keyword]</c> (unquoted) —
  ///         <see cref="GainAbilityEffect"/> targeting <c>Self</c>, wrapping
  ///         the keyword as a nested <see cref="StaticAbility"/>. The subject
  ///         before "has" collapses to <c>Self</c> per the card-name-as-subject
  ///         oracle convention.</item>
  /// </list>
  /// The peeled condition text (e.g. <c>"it's untapped"</c>) is stored verbatim
  /// on <see cref="AsLongAsDuration.Condition"/> — not parsed further.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseAsLongAsStaticGrant(OracleClause clause, ClauseClassification classification)
  {
    // Peel " as long as <condition>." from the end of the clause.
    var suffixMatch = _asLongAsSuffixPattern.Match(clause.RawText);
    if (!suffixMatch.Success)
    {
      return null;
    }

    var remainingText = suffixMatch.Groups["main"].Value.Trim();
    var conditionText = suffixMatch.Groups["cond"].Value.Trim();
    var duration = new AsLongAsDuration { Condition = conditionText };

    // Strip any ability-word prefix ("Threshold — ", "Metalcraft — ", etc.)
    // from remainingText. The classifier has already captured the word into
    // classification.AbilityWord (Rule 207.2c). Without stripping, the em-dash
    // prefix breaks every downstream sub-parser regex that anchors on "This".
    string? abilityWord = classification.AbilityWord;
    if (abilityWord is not null)
    {
      var emDashIdx = remainingText.IndexOf('—');
      if (emDashIdx >= 0)
      {
        remainingText = remainingText[(emDashIdx + 1)..].TrimStart();
      }
    }

    // Sub-parser A: "This creature/This permanent gets +N/+M"
    var ptMatch = _selfGetsPTPattern.Match(remainingText);
    if (ptMatch.Success)
    {
      var power = int.Parse(ptMatch.Groups["p"].Value);
      var toughness = int.Parse(ptMatch.Groups["t"].Value);
      return
      [
        new StaticAbility
        {
          AbilityWord = abilityWord,
          Effects = [new MagicAST.AST.Effects.Modification.ModifyPTEffect
          {
            Target = ObjectReference.Self(),
            PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
            ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
            Duration = duration,
          }],
        },
      ];
    }

    // Sub-parser B: "[subject] has [keyword]" — unquoted keyword grant.
    // Subject collapses to Self (card-name-as-subject oracle convention).
    var kwMatch = _subjectHasKeywordPattern.Match(remainingText);
    if (kwMatch.Success)
    {
      var kw = kwMatch.Groups["kw"].Value.Trim();
      var grantedAbility = MapKeywordToStaticAbility(kw);
      if (grantedAbility != null)
      {
        return
        [
          new StaticAbility
          {
            AbilityWord = abilityWord,
            Effects = [new MagicAST.AST.Effects.Modification.GainAbilityEffect
            {
              Target = ObjectReference.Self(),
              GainedAbility = grantedAbility,
              Duration = duration,
            }],
          },
        ];
      }
    }

    return null;
  }

  // Strips " as long as <cond>." from the end. The "main" group is everything
  // before the suffix; "cond" is the condition body without the trailing period.
  private static readonly Regex _asLongAsSuffixPattern = new(
    @"^\s*(?<main>.+?)\s+as\s+long\s+as\s+(?<cond>.+?)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the sub-clause after suffix stripping for P/T modifiers.
  // Handles "+0/+3", "+1/+0", etc. (non-negative only — oracle uses explicit
  // +/- signs; negative modifiers use the dash form which we don't see here).
  private static readonly Regex _selfGetsPTPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+gets\s+\+(?<p>\d+)/\+(?<t>\d+)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches the sub-clause for an unquoted keyword grant on any subject.
  // Subject (anything before "has") collapses to Self.
  private static readonly Regex _subjectHasKeywordPattern = new(
    @"^\s*\S.*?\s+has\s+(?<kw>[A-Za-z][A-Za-z\s]*?)\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// Maps a keyword phrase to its canonical <see cref="StaticAbility"/> node.
  /// Returns null for keywords not yet supported, causing the caller to fall
  /// through to the fallback path.
  /// </summary>
  private static StaticAbility? MapKeywordToStaticAbility(string keyword)
  {
    return keyword.ToLowerInvariant() switch
    {
      "first strike" => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.First,
        }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new MagicAST.AST.Effects.Combat.CombatDamageTimingEffect
        {
          Timing = MagicAST.AST.Effects.Combat.CombatDamageTiming.Both,
        }],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["flying", "reach"],
          },
        }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = "Indestructible",
        Effects = [new MagicAST.AST.Effects.Keyword.IndestructibleEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new MagicAST.AST.Effects.Keyword.VigilanceEffect()],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = "Haste",
        Effects = [new MagicAST.AST.Effects.Keyword.HasteEffect()],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new MagicAST.AST.Effects.Damage.LifelinkEffect()],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = "Reach",
        Effects = [new MagicAST.AST.Effects.Keyword.ReachEffect()],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = "Trample",
        Effects = [new MagicAST.AST.Effects.Keyword.TrampleEffect()],
      },
      "defender" => new StaticAbility
      {
        KeywordSource = "Defender",
        Effects = [new MagicAST.AST.Effects.Keyword.DefenderEffect { IsOptional = false }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = "Deathtouch",
        Effects = [new MagicAST.AST.Effects.Keyword.DeathtouchEffect { IsOptional = false }],
      },
      // Menace: this creature can't be blocked except by two or more creatures.
      // Rule 702.111. EvasionEffect with MinimumBlockers=2; CanBeBlockedBy carries
      // the creature-typed filter (any two-or-more creatures qualify as blockers).
      "menace" => new StaticAbility
      {
        KeywordSource = "Menace",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
          MinimumBlockers = 2,
        }],
      },
      // Shadow: only creatures with shadow can block this creature, and this creature
      // can only block other shadow creatures. Rule 702.28. Mutual evasion —
      // EvasionEffect with CanBeBlockedBy restricted to the "shadow" characteristic.
      "shadow" => new StaticAbility
      {
        KeywordSource = "Shadow",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["shadow"],
          },
        }],
      },
      // Intimidate: can't be blocked except by artifact creatures and/or creatures
      // sharing a color. Rule 702.13. Mirrors Fear (702.36) but with the color-share
      // predicate instead of the fixed black-color predicate.
      "intimidate" => new StaticAbility
      {
        KeywordSource = "Intimidate",
        Effects = [new MagicAST.AST.Effects.Keyword.EvasionEffect
        {
          CanBeBlockedBy = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["artifact", "shares a color"],
          },
        }],
      },
      // Myriad: triggered keyword. MAST records keyword presence; the per-opponent
      // copy-creation and delayed-exile semantics are engine territory. Rule 702.116.
      "myriad" => new StaticAbility
      {
        KeywordSource = "Myriad",
        Effects = [new MagicAST.AST.Effects.Keyword.MyriadEffect()],
      },
      // Melee: triggered keyword. MAST records keyword presence; the per-opponent
      // attack counting and P/T buff are engine territory. Rule 702.121.
      "melee" => new StaticAbility
      {
        KeywordSource = "Melee",
        Effects = [new MagicAST.AST.Effects.Keyword.MeleeEffect()],
      },
      _ => null,
    };
  }

  /// <summary>
  /// "[FilterDescription] (get|gets) [+-]N/[+-]N." — lord-effect P/T buff
  /// (Rule 613.1c, layer 7C). ONE consolidated parser surface covering the
  /// family of filter dimensions:
  /// <list type="bullet">
  ///   <item><c>All creatures get …</c> — global creature buff; no controller.</item>
  ///   <item><c>Creatures get …</c> / <c>Creatures you control get …</c> — card-type
  ///         filter; optional controller scope.</item>
  ///   <item><c>White creatures get …</c> — color + card-type filter.</item>
  ///   <item><c>Dragon creatures you control get …</c> — subtype + card-type + controller.</item>
  ///   <item><c>Elves you control get …</c> — bare subtype (depluralised) + controller.</item>
  ///   <item><c>Other Cats you control get …</c> — "Other" prefix adds
  ///         <c>Characteristics: ["other"]</c> to the filter.</item>
  /// </list>
  /// Negative modifiers (e.g. <c>-1/-1</c>) are emitted as
  /// <see cref="MagicAST.AST.Quantities.LiteralQuantity.Of(int)"/> with a
  /// negative value. Does NOT match the
  /// "Enchanted creature gets …" shape (handled by
  /// <see cref="TryParseAnthemModifyPT"/>). Also does NOT match lines ending
  /// in "as long as …" — those are handled by
  /// <see cref="TryParseAsLongAsStaticGrant"/>, which is tried after this
  /// method in the dispatch chain, ensuring the more-specific rules take
  /// priority.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseLordPTBuff(OracleClause clause)
  {
    var match = _lordPTBuffPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    // Don't steal from AsLongAs — that parser peels the suffix itself.
    // If the raw text has " as long as" anywhere, skip here.
    if (clause.RawText.Contains(" as long as", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var filterText = match.Groups["filter"].Value.Trim();
    var isOther = match.Groups["other"].Success;
    var psign = match.Groups["psign"].Value;
    var p = int.Parse(match.Groups["p"].Value);
    var tsign = match.Groups["tsign"].Value;
    var t = int.Parse(match.Groups["t"].Value);

    var power = psign == "-" ? -p : p;
    var toughness = tsign == "-" ? -t : t;

    var filter = ParseLordPTFilter(filterText, isOther);
    if (filter is null)
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new ModifyPTEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Each,
            Filter = filter,
          },
          PowerModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(power),
          ToughnessModifier = MagicAST.AST.Quantities.LiteralQuantity.Of(toughness),
        }],
      },
    ];
  }

  // Pattern: optional "Other " or "All " prefix, then a filter noun-phrase,
  // then "get"/"gets", then [+-]N/[+-]N. Anchored; does not match mid-sentence.
  // The named group "other" fires when "Other " is present; used to populate
  // ObjectFilter.Characteristics: ["other"] on the resulting filter.
  private static readonly Regex _lordPTBuffPattern = new(
    @"^\s*(?:(?<other>Other)\s+|All\s+)?(?<filter>\S.+?)\s+gets?\s+(?<psign>[+\-])(?<p>\d+)/(?<tsign>[+\-])(?<t>\d+)\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Color-name → single-letter code map (WUBRG order, all five colours).
  private static readonly IReadOnlyDictionary<string, string> _colorNameToCode =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["White"] = "W",
      ["Blue"] = "U",
      ["Black"] = "B",
      ["Red"] = "R",
      ["Green"] = "G",
    };

  /// <summary>
  /// Parses the filter noun-phrase in a lord-effect P/T line into an
  /// <see cref="ObjectFilter"/>. Returns null for unrecognised shapes.
  /// When <paramref name="isOther"/> is <see langword="true"/>, the filter
  /// includes <c>Characteristics: ["other"]</c> to represent the "Other"
  /// qualifier on the oracle line (e.g. "Other Cats you control get +2/+1.").
  /// </summary>
  private static ObjectFilter? ParseLordPTFilter(string filterText, bool isOther = false)
  {
    var text = filterText.Trim();

    // "Other " qualifier on the oracle line → record as a Characteristics
    // entry so the AST preserves the exclusion-of-self semantics.
    IReadOnlyList<string>? characteristics = isOther ? ["other"] : null;

    // Peel optional "you control" controller suffix.
    ControllerFilter? controller = null;
    if (text.EndsWith(" you control", StringComparison.OrdinalIgnoreCase))
    {
      controller = ControllerFilter.You;
      text = text[..^" you control".Length].Trim();
    }

    // --- Shape: "[Color] creatures" (e.g. "White creatures", "Black creatures") ---
    // Must be checked BEFORE the generic "[Subtype] creatures" branch because
    // colour adjectives like "White" also match the capitalised-subtype pattern.
    // Oracle colour adjectives are capitalised at the start of a clause.
    var colorCreatureMatch = Regex.Match(
      text,
      @"^(?<color>White|Blue|Black|Red|Green)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (colorCreatureMatch.Success)
    {
      var colorName = colorCreatureMatch.Groups["color"].Value;
      if (!_colorNameToCode.TryGetValue(colorName, out var colorCode))
      {
        return null;
      }
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Colors = [colorCode],
        Controller = controller,
        Characteristics = characteristics,
      };
    }

    // --- Shape: "[Subtype] creatures" (e.g. "Dragon creatures", "Bird creatures") ---
    // Capitalised subtype immediately before the lower-case "creatures" noun.
    // Checked after the colour branch so that colour adjectives ("White") are
    // not misclassified as subtypes.
    var subtypeCreatureMatch = Regex.Match(
      text,
      @"^(?<sub>[A-Z][a-z]+)\s+creatures?$",
      RegexOptions.IgnoreCase
    );
    if (subtypeCreatureMatch.Success)
    {
      var subtype = subtypeCreatureMatch.Groups["sub"].Value;
      // Normalise to singular oracle-canonical capitalised form.
      // Oracle capitalises creature subtypes; the matched group already has
      // its original capitalisation (e.g. "Dragon", "Bird").
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Subtypes = [subtype],
        Controller = controller,
        Characteristics = characteristics,
      };
    }

    // --- Shape: "creatures" / "Creatures" (bare card-type) ---
    if (text.Equals("creatures", StringComparison.OrdinalIgnoreCase))
    {
      return new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = controller,
        Characteristics = characteristics,
      };
    }

    // --- Shape: bare plural subtype (e.g. "Elves", "Goblins", "Saprolings") ---
    // Capitalised plural noun, no "creatures" word. Depluralise to the
    // singular canonical oracle-capitalised form. Irregular plurals (e.g.
    // "Elves" → "Elf") are handled via a lookup before the fallback simple
    // strip-s path.
    // When the "Other " prefix is present the filter describes a tribal-lord
    // shape ("Other Cats you control get …") where the subtype is a
    // creature-only subtype. Include CardTypes: ["creature"] so the filter
    // aligns with the "[Subtype] creatures" branch — both describe creatures
    // of that subtype. Without the "Other" qualifier the subtype noun may span
    // non-creature permanents (e.g. "Elves" in some enchantment anthems) so
    // CardTypes is omitted for backward compatibility.
    var bareSubtypeMatch = Regex.Match(text, @"^(?<sub>[A-Z][a-z]+)s$");
    if (bareSubtypeMatch.Success)
    {
      var pluralWord = bareSubtypeMatch.Groups["sub"].Value + "s";
      var subtype = DepluralizeSubtype(pluralWord);
      return new ObjectFilter
      {
        CardTypes = isOther ? (IReadOnlyList<string>?)["creature"] : null,
        Subtypes = [subtype],
        Controller = controller,
        Characteristics = characteristics,
      };
    }

    // Unrecognised filter shape — fall through to the fallback parser.
    return null;
  }

  // Known irregular plural → singular mappings for MTG creature subtypes.
  // Checked before the fallback simple strip-s in DepluralizeSubtype.
  private static readonly IReadOnlyDictionary<string, string> _subtypeIrregularPlurals =
    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
      ["Elves"] = "Elf",
      ["Mice"] = "Mouse",
      ["Loci"] = "Locus",
      ["Dwarves"] = "Dwarf",
      ["Wolves"] = "Wolf",
      ["Djinn"] = "Djinn",
    };

  /// <summary>
  /// Returns the oracle-canonical singular form of a plural subtype word.
  /// Handles known irregular plurals first; falls back to stripping a
  /// trailing "s" for regular "-s" plurals.
  /// </summary>
  private static string DepluralizeSubtype(string plural)
  {
    if (_subtypeIrregularPlurals.TryGetValue(plural, out var singular))
    {
      return singular;
    }
    // Simple regular plural: strip trailing "s".
    return plural.EndsWith('s') ? plural[..^1] : plural;
  }

  /// <summary>
  /// "This [permanent/land/creature/artifact/enchantment] enters tapped[unless condition]." —
  /// Rule 614 property recorded as a <see cref="EntersTappedEffect"/> on a
  /// <see cref="StaticAbility"/>. No <c>KeywordSource</c> is set: oracle text
  /// uses a full declarative sentence, not a keyword token. MAST records what
  /// the oracle text <em>says</em>; the replacement-effect machinery Rule 614
  /// derives at run-time is out of scope.
  ///
  /// <para>When oracle text carries "unless [condition]" (fastland / checkland
  /// shape, e.g. "This land enters tapped unless you control two or fewer other
  /// lands."), the condition text is captured in
  /// <see cref="EntersTappedEffect.EntryCondition"/>. This is distinct from
  /// <see cref="UnlessClause"/> which represents "unless [player] pays [cost]".</para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEntersTapped(OracleClause clause)
  {
    var match = _entersTappedPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var conditionGroup = match.Groups["condition"];
    Condition? entryCondition = conditionGroup.Success
      ? new Condition { Text = conditionGroup.Value.Trim() }
      : null;

    return
    [
      new StaticAbility
      {
        Effects = [new EntersTappedEffect { EntryCondition = entryCondition }],
      },
    ];
  }

  // Matches "This [permanent|land|creature|artifact|enchantment] enters tapped[.]"
  // optionally followed by "unless [condition text]."
  // The permanent-type noun is flexible to cover the full range of oracle
  // printings. The trailing period is optional to accommodate minor formatting
  // variants. The named capture group <condition> captures the board-state
  // predicate text after "unless" for fastland / checkland entries.
  private static readonly Regex _entersTappedPattern = new(
    @"^\s*This\s+(?:permanent|land|creature|artifact|enchantment|spell)\s+enters\s+tapped"
    + @"(?:\s+unless\s+(?<condition>[^.]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "As this [permanent] enters, you may pay N life. If you don't, it
  /// enters tapped." — Shockland-pattern as-enters static (Rule 614.1c).
  /// Emits a <see cref="PayLifeOnEntryEffect"/> with
  /// <see cref="IOptionalEffect.IsOptional"/> = true and
  /// <see cref="IOptionalEffect.IfYouDoNot"/> = <see cref="EntersTappedEffect"/>.
  ///
  /// <para>Why a dedicated effect rather than extending <c>EntersTappedEffect</c>:
  /// the surface verb in the oracle text is "pay N life on entry" — that's
  /// the optional action. The enters-tapped consequence is the negative
  /// branch via the reusable <see cref="IOptionalEffect.IfYouDoNot"/>
  /// machinery, keeping the AST descriptively faithful to what the oracle
  /// line says.</para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParsePainland(OracleClause clause)
  {
    var match = _painlandPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var amountText = match.Groups["amount"].Value.ToLowerInvariant();
    if (!TryParseSmallCount(amountText, out var amount))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new PayLifeOnEntryEffect
        {
          Amount = MagicAST.AST.Quantities.LiteralQuantity.Of(amount),
          IsOptional = true,
          IfYouDoNot = new EntersTappedEffect(),
        }],
      },
    ];
  }

  // Matches "As this [permanent-type] enters, you may pay N life. If you don't,
  // it enters tapped." The permanent-type noun is open-ended (creature/artifact/
  // enchantment/land/permanent) so the same parser surface covers any future
  // non-land printings using the shape. The life amount is a small-count token
  // (digit or number-word "one".."ten") to share the existing
  // <see cref="TryParseSmallCount"/> vocabulary with sibling parsers.
  // The closing period is optional for minor formatting variants.
  private static readonly Regex _painlandPattern = new(
    @"^\s*As\s+this\s+(?:permanent|land|creature|artifact|enchantment)\s+enters,\s+you\s+may\s+pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.\s+If\s+you\s+don'?t,\s+it\s+enters\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This (creature|land|permanent) can't block." — Rule 509.1c blocker-side
  /// restriction (Hulking Cyclops shape). Full declarative sentence, so no
  /// <c>KeywordSource</c> is set. Parameterless: the subject is the card the
  /// ability is printed on. Mirrors <see cref="TryParseEntersTapped"/> in
  /// structure.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseCantBlock(OracleClause clause)
  {
    if (!_cantBlockPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effects = [new CantBlockEffect()],
      },
    ];
  }

  // Matches "This (creature|land|permanent) can't block."
  // The permanent-type noun covers the oracle printings seen in the corpus.
  // The trailing period is optional for minor formatting variants.
  private static readonly Regex _cantBlockPattern = new(
    @"^\s*This\s+(?:creature|land|permanent)\s+can'?t\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "Enchanted creature can't attack or block." — dual combat restriction
  /// (attacker-side + blocker-side) imposed by an Aura on its enchanted object
  /// (Rule 508.1c / 509.1c). Emits a <see cref="StaticAbility"/> with two
  /// effects:
  /// <list type="bullet">
  ///   <item><see cref="CantAttackEffect"/> targeting <c>EnchantedOrEquipped</c>.</item>
  ///   <item><see cref="CantBlockEffect"/> targeting <c>EnchantedOrEquipped</c>.</item>
  /// </list>
  /// Both effects carry <c>IsOptional = false</c> — the restriction is mandatory
  /// and does not have a "You may" prefix.
  /// <para>
  /// Per the multi-effect-per-clause doctrine, the conjunction "can't attack or
  /// block" in a single oracle line bundles both restrictions into one
  /// <c>StaticAbility.Effects</c> list rather than splitting into two ability
  /// nodes.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEnchantedCantAttackOrBlock(OracleClause clause)
  {
    if (!_enchantedCantAttackOrBlockPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    var target = new ObjectReference { Kind = ObjectReferenceKind.EnchantedOrEquipped };

    return
    [
      new StaticAbility
      {
        Effects =
        [
          new CantAttackEffect { Target = target, IsOptional = false },
          new CantBlockEffect  { Target = target, IsOptional = false },
        ],
      },
    ];
  }

  // Matches "Enchanted creature can't attack or block."
  // The subject is always "Enchanted creature" for this Aura-body shape.
  // "Equipped creature" is included for symmetry (same multi-effect semantics
  // would apply to an Equipment printing of this restriction).
  // The trailing period is optional for minor formatting variants.
  private static readonly Regex _enchantedCantAttackOrBlockPattern = new(
    @"^\s*(?:Enchanted|Equipped)\s+creature\s+can'?t\s+attack\s+or\s+block\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This (creature|land|permanent) can't be blocked." — Rule 509.1b full
  /// unblockability, or "This creature can't be blocked by [color] creatures."
  /// — color-restricted variant. Full declarative sentence, so no
  /// <c>KeywordSource</c> is set. Mirrors <see cref="TryParseCantBlock"/>
  /// in structure.
  /// <para>
  /// When a color qualifier is present (<c>BlockedByFilter</c> is set), the
  /// creature can still be blocked — just not by creatures of the named
  /// color(s). This is descriptively distinct from full unblockability
  /// (null filter) and from <see cref="EvasionEffect.CanBeBlockedBy"/>
  /// ("can't be blocked EXCEPT by"), which names what CAN block.
  /// </para>
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseCantBeBlocked(OracleClause clause)
  {
    // Full unblockability: "This (creature|land|permanent) can't be blocked."
    if (_cantBeBlockedPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          Effects = [new CantBeBlockedEffect()],
        },
      ];
    }

    // Color-restricted variant: "This creature can't be blocked by [color] creatures."
    var colorMatch = _cantBeBlockedByColorPattern.Match(clause.RawText);
    if (colorMatch.Success)
    {
      var colorName = colorMatch.Groups["color"].Value.ToLowerInvariant();
      var colorCode = colorName switch
      {
        "white" => "W",
        "blue"  => "U",
        "black" => "B",
        "red"   => "R",
        "green" => "G",
        _       => null,
      };
      if (colorCode != null)
      {
        return
        [
          new StaticAbility
          {
            Effects =
            [
              new CantBeBlockedEffect
              {
                BlockedByFilter = new ObjectFilter
                {
                  CardTypes = ["creature"],
                  Colors = [colorCode],
                },
              },
            ],
          },
        ];
      }
    }

    return null;
  }

  // Matches "This (creature|land|permanent) can't be blocked."
  // The permanent-type noun covers the oracle printings seen in the corpus.
  // The trailing period is optional for minor formatting variants.
  private static readonly Regex _cantBeBlockedPattern = new(
    @"^\s*This\s+(?:creature|land|permanent)\s+can'?t\s+be\s+blocked\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "This creature can't be blocked by [color] creatures."
  // Captures the color name for mapping to WUBRG code.
  private static readonly Regex _cantBeBlockedByColorPattern = new(
    @"^\s*This\s+(?:creature|permanent)\s+can'?t\s+be\s+blocked\s+by\s+(?<color>white|blue|black|red|green)\s+creatures\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This creature can block only creatures with &lt;characteristic&gt;." —
  /// Rule 509.1c blocker-side narrowing restriction (Cloud Elemental shape).
  /// The creature can block, but only attackers matching the filter. Distinct
  /// from <see cref="TryParseCantBlock"/> (blanket can't-block) and from
  /// <see cref="TryParseMustBlock"/> (blocker-side requirement). No
  /// <c>KeywordSource</c>: the restriction is a full declarative sentence.
  /// </summary>
  /// <remarks>
  /// The filter phrase is always of the form <c>creatures with &lt;X&gt;</c>
  /// for this family (e.g. "creatures with flying", "creatures with reach").
  /// The captured characteristic rides on
  /// <see cref="ObjectFilter.Characteristics"/> as a verbatim string so the
  /// gold shape matches the hand-parsed fixture exactly.
  /// </remarks>
  private static IReadOnlyList<Ability>? TryParseCanBlockOnly(OracleClause clause)
  {
    var match = _canBlockOnlyPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var filterPhrase = match.Groups["filter"].Value.Trim();

    // "creatures with <X>" — the standard filter shape for this family.
    var withMatch = _creaturesWithPattern.Match(filterPhrase);
    if (!withMatch.Success)
    {
      return null;
    }

    var characteristic = withMatch.Groups["char"].Value.Trim().ToLowerInvariant();

    return
    [
      new StaticAbility
      {
        Effects = [new CanBlockOnlyEffect
        {
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [$"with {characteristic}"],
          },
        }],
      },
    ];
  }

  // Matches "This creature can block only <filter>."
  // The filter group captures everything between "only " and the terminal period.
  private static readonly Regex _canBlockOnlyPattern = new(
    @"^\s*This\s+creature\s+can\s+block\s+only\s+(?<filter>.+?)\.\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Matches "creatures with <characteristic>" — the standard filter shape.
  private static readonly Regex _creaturesWithPattern = new(
    @"^creatures\s+with\s+(?<char>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "You may choose not to untap this [permanent] during your untap step." —
  /// Rule 302.6 opt-out. Full declarative sentence, not a keyword token, so no
  /// <c>KeywordSource</c> is set. Emits a parameterless
  /// <see cref="MagicAST.AST.Effects.Timing.SkipUntapEffect"/> marker with
  /// <c>IsOptional = false</c>: the "may choose" language is attached to the
  /// untap-step turn-based action (Rule 116), not to the
  /// <see cref="IOptionalEffect.IsOptional"/> "You may" prefix convention. Mirrors
  /// <see cref="TryParseCantBlock"/> in structure.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseSkipUntap(OracleClause clause)
  {
    if (!_skipUntapPattern.IsMatch(clause.RawText))
    {
      return null;
    }
    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Timing.SkipUntapEffect { IsOptional = false }],
      },
    ];
  }

  // Matches "You may choose not to untap this [permanent-type] during your untap step."
  // The permanent-type noun is flexible — oracle uses the card's own type ("this
  // artifact", "this creature", "this permanent", etc.). The trailing period is
  // optional for minor formatting variants.
  private static readonly Regex _skipUntapPattern = new(
    @"^\s*You\s+may\s+choose\s+not\s+to\s+untap\s+this\s+(?:creature|permanent|artifact|enchantment|land)\s+during\s+your\s+untap\s+step\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// "This &lt;type&gt; enters with N +1/+1 counters on it." — Rules 122 (counters),
  /// 614.1d (enters-with replacement effects). Handles subject types creature,
  /// artifact, enchantment, land, and permanent. Count may be a decimal digit,
  /// the variable "X", or an English word-count ("one" through "ten"). Emits a
  /// <see cref="StaticAbility"/> wrapping an
  /// <see cref="MagicAST.AST.Effects.Replacement.EntersWithCountersEffect"/>
  /// whose <c>Count</c> is either a <see cref="MagicAST.AST.Quantities.LiteralQuantity"/>
  /// or a <see cref="MagicAST.AST.Quantities.VariableQuantity"/>. No
  /// <c>KeywordSource</c>: the oracle text is a full declarative sentence, not
  /// a keyword token. Mirrors <see cref="TryParseEntersTapped"/> in structure.
  /// </summary>
  private static IReadOnlyList<Ability>? TryParseEntersWithCounters(OracleClause clause)
  {
    var match = _entersWithCountersPattern.Match(clause.RawText);
    if (!match.Success)
    {
      return null;
    }

    var countText = match.Groups["count"].Value;
    MagicAST.AST.Quantities.Quantity count;
    if (countText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      count = MagicAST.AST.Quantities.VariableQuantity.X;
    }
    else if (TryParseSmallCount(countText.ToLowerInvariant(), out var intCount))
    {
      count = MagicAST.AST.Quantities.LiteralQuantity.Of(intCount);
    }
    else
    {
      return null;
    }

    var counterType = match.Groups["counterType"].Value;

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.EntersWithCountersEffect
        {
          Count = count,
          CounterType = counterType,
          IsOptional = false,
        }],
      },
    ];
  }

  // Matches "This <type> enters with N <counterType> counters on it." where <type> is
  // creature, artifact, enchantment, land, or permanent; N is a decimal digit,
  // the variable "X", an English word-count ("one" through "ten"), or the
  // article "a"/"an" (treated as 1); and <counterType> is "+1/+1" or "-1/-1".
  // Handles "counter" and "counters" (singular for N=1) and an optional
  // trailing period. Rules: 122 (counters), 614.1d (enters-with replacement).
  private static readonly Regex _entersWithCountersPattern = new(
    @"^\s*This\s+(?<type>creature|artifact|enchantment|land|permanent)\s+enters\s+with\s+(?<count>\d+|X|an?|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<counterType>[+-]1/[+-]1)\s+counters?\s+on\s+it\.?\s*$",
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
