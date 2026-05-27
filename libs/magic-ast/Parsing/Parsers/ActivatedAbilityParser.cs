namespace MagicAST.Parsing.Parsers;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Control;
using MagicAST.AST.Effects.Counter;
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
  private readonly ManaCostParser _manaCostParser = new();
  private readonly OracleTokenizer _tokenizer = new();
  private readonly FallbackParser _fallback = new();

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

    // Extract trailing "Activate only as ..." restriction sentences from
    // effectPart before effect parsing. These are not effects — they constrain
    // when the ability can be activated (Rule 602.5). Stripping them prevents
    // TryParseMultiEffectSentences from failing when it encounters them.
    var restrictions = ExtractActivationRestrictions(ref effectPart);

    // Strip trailing parenthetical reminder text (Rule 207.2) before effect
    // parsing. Reminder text follows the effect sentence as "(explanation...)" —
    // e.g. "Create a Treasure token. (It's an artifact with ...)".
    StripTrailingReminder(ref effectPart);

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
      // Try mana cost first (e.g., "{1}", "{2}{G}", "{T}")
      if (component.Contains('{'))
      {
        var manaCost = TryParseManaCostComponent(component);
        if (manaCost != null)
        {
          costs.Add(manaCost);
          hasParsedAnyCost = true;
          continue;
        }
      }

      // Try sacrifice cost (e.g., "Sacrifice another creature", "Sacrifice X Squirrels")
      var sacrificeCost = TryParseSacrificeCost(component);
      if (sacrificeCost != null)
      {
        costs.Add(sacrificeCost);
        hasParsedAnyCost = true;
        continue;
      }

      // Try discard cost (e.g., "Discard a card", "Discard a legendary card")
      var discardCost = TryParseDiscardCost(component);
      if (discardCost != null)
      {
        costs.Add(discardCost);
        hasParsedAnyCost = true;
        continue;
      }

      // Try pay-life cost (e.g., "Pay 1 life", "Pay 3 life")
      var payLifeCost = TryParsePayLifeCost(component);
      if (payLifeCost != null)
      {
        costs.Add(payLifeCost);
        hasParsedAnyCost = true;
        continue;
      }

      // Try remove-counters cost (e.g., "Remove three spore counters from this creature")
      var removeCountersCost = TryParseRemoveCountersCost(component);
      if (removeCountersCost != null)
      {
        costs.Add(removeCountersCost);
        hasParsedAnyCost = true;
        continue;
      }

      // If we can't parse this component, the whole cost parse fails
      // (We should be able to understand all cost components)
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

    // Try different effect types in sequence

    // Add mana
    var addManaEffect = TryParseAddManaEffect(effectPart);
    if (addManaEffect != null)
    {
      return new List<Effect> { addManaEffect };
    }

    // Scry
    var scryEffect = TryParseScryEffect(effectPart);
    if (scryEffect != null)
    {
      return new List<Effect> { scryEffect };
    }

    // Draw cards
    var drawEffect = TryParseDrawCardsEffect(effectPart);
    if (drawEffect != null)
    {
      return new List<Effect> { drawEffect };
    }

    // Discard cards
    var discardEffect = TryParseDiscardCardsEffect(effectPart);
    if (discardEffect != null)
    {
      return new List<Effect> { discardEffect };
    }

    // Put counters
    var putCounterEffect = TryParsePutCountersEffect(effectPart);
    if (putCounterEffect != null)
    {
      return new List<Effect> { putCounterEffect };
    }

    // Gain ability
    var gainAbilityEffect = TryParseGainAbilityEffect(effectPart);
    if (gainAbilityEffect != null)
    {
      return new List<Effect> { gainAbilityEffect };
    }

    // Tap [count] target [type]   (e.g. "Tap X target lands", "Tap two target creatures", "Tap target creature")
    var tapEffect = TryParseTapEffect(effectPart);
    if (tapEffect != null)
    {
      return new List<Effect> { tapEffect };
    }

    // Untap target [subtype]
    var untapEffect = TryParseUntapEffect(effectPart);
    if (untapEffect != null)
    {
      return new List<Effect> { untapEffect };
    }

    // Gain life
    var gainLifeEffect = TryParseGainLifeEffect(effectPart);
    if (gainLifeEffect != null)
    {
      return new List<Effect> { gainLifeEffect };
    }

    // Denethor's two-effect tail: "Target player becomes the monarch."
    // / "Denethor deals 3 damage to any target." — recognised when the
    // surrounding multi-sentence dispatch reaches a single sentence.
    var becomeMonarch = TryParseBecomeMonarchEffect(effectPart);
    if (becomeMonarch != null)
    {
      return new List<Effect> { becomeMonarch };
    }

    var selfDealsDamage = TryParseSelfDealsDamageToAnyTargetEffect(effectPart);
    if (selfDealsDamage != null)
    {
      return new List<Effect> { selfDealsDamage };
    }

    // "[This creature|CardName] deals N damage to target attacking or blocking creature."
    var selfDamageAttackOrBlock = TryParseSelfDealsDamageToAttackingOrBlockingCreatureEffect(effectPart);
    if (selfDamageAttackOrBlock != null)
    {
      return new List<Effect> { selfDamageAttackOrBlock };
    }

    // Return target [X] from [zone] to the battlefield
    var returnToBf = TryParseReturnToBattlefieldEffect(effectPart);
    if (returnToBf != null)
    {
      return new List<Effect> { returnToBf };
    }

    // "Target creature gets +N/+M for as long as [condition]." —
    // or "Target creature gets +N/+M until end of turn." —
    // P/T modifier on an activated ability effect.
    var modifyPT = TryParseModifyPTEffect(effectPart);
    if (modifyPT != null)
    {
      return new List<Effect> { modifyPT };
    }

    // "Create a [P]/[T] [color] [subtype] creature token." — token creation as an
    // activated-ability effect (Rule 111). Reuses the same regex shape as the
    // Spell and Triggered CreateTokenRule counterparts; keeps the activated form
    // inline rather than in a separate rule file since the effect is narrowly scoped.
    var createToken = TryParseCreateTokenEffect(effectPart);
    if (createToken != null)
    {
      return new List<Effect> { createToken };
    }

    // Regenerate this creature / target [type]
    var regenerateEffect = TryParseRegenerateEffect(effectPart);
    if (regenerateEffect != null)
    {
      return new List<Effect> { regenerateEffect };
    }

    // For now, we can't parse other effect types
    // Return null to signal that we need to fall back to unparsed
    return null;
  }

  private static readonly System.Text.RegularExpressions.Regex _createTokenPattern = new(
    @"^Create\s+(?<count>a|X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<power>\d+)/(?<toughness>\d+)\s+(?<color>white|blue|black|red|green)\s+(?<subtype>\w+)\s+creature\s+tokens?$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled
  );

  /// <summary>
  /// Matches predefined artifact token creation: "Create [count] [Subtype] token(s)."
  /// Handles Treasure, Food, Clue, Blood — the standard artifact token subtypes
  /// whose rules text is reminder-only (Rule 107.10b). Count is "a" (1), a number
  /// word, or a digit.
  /// </summary>
  private static readonly System.Text.RegularExpressions.Regex _createPredefinedTokenPattern = new(
    @"^Create\s+(?<count>a|an|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+(?<subtype>Treasure|Food|Clue|Blood)\s+tokens?$",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled
  );

  private static readonly System.Collections.Generic.Dictionary<string, string> _activatedColorMap = new(
    System.StringComparer.OrdinalIgnoreCase
  )
  {
    ["white"] = "W", ["blue"] = "U", ["black"] = "B", ["red"] = "R", ["green"] = "G",
  };

  /// <summary>
  /// "Create a [P]/[T] [color] [subtype] creature token." — single-token creation
  /// effect. Mirrors <see cref="Spell.Rules.CreateTokenRule"/> but lives in the
  /// activated-ability parser so artifact abilities like Combine Chrysalis parse
  /// without needing a sibling spell or triggered rule.
  /// </summary>
  private static MagicAST.AST.Effects.TokenCopy.CreateTokenEffect? TryParseCreateTokenEffect(string effectText)
  {
    var stripped = effectText.Trim().TrimEnd('.').Trim();

    // --- Predefined artifact token: Treasure, Food, Clue, Blood ---
    var predefined = _createPredefinedTokenPattern.Match(stripped);
    if (predefined.Success)
    {
      var rawPCount = predefined.Groups["count"].Value.ToLowerInvariant();
      int pCount = rawPCount switch
      {
        "a" or "an" or "one" => 1,
        "two" => 2,
        "three" => 3,
        "four" => 4,
        "five" => 5,
        "six" => 6,
        "seven" => 7,
        "eight" => 8,
        "nine" => 9,
        "ten" => 10,
        _ => int.TryParse(rawPCount, out var pn) ? pn : 1,
      };
      var subtype = predefined.Groups["subtype"].Value;
      // Title-case normalize
      subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();
      var tokenFactory = subtype switch
      {
        "Treasure" => MagicAST.AST.Effects.TokenDefinition.Treasure(),
        "Food" => MagicAST.AST.Effects.TokenDefinition.Food(),
        "Clue" => MagicAST.AST.Effects.TokenDefinition.Clue(),
        "Blood" => MagicAST.AST.Effects.TokenDefinition.Blood(),
        _ => null,
      };
      if (tokenFactory is not null)
      {
        return new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
        {
          Count = MagicAST.AST.Quantities.LiteralQuantity.Of(pCount),
          Token = tokenFactory,
          IsOptional = false,
        };
      }
    }

    // --- Standard creature token: "Create [count] P/T color subtype creature token(s)" ---
    var m = _createTokenPattern.Match(stripped);
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    MagicAST.AST.Quantities.Quantity count = rawCount switch
    {
      "x" or "y" or "z" => new MagicAST.AST.Quantities.VariableQuantity { Name = rawCount.ToUpperInvariant() },
      "a" or "one" => MagicAST.AST.Quantities.LiteralQuantity.Of(1),
      "two" => MagicAST.AST.Quantities.LiteralQuantity.Of(2),
      "three" => MagicAST.AST.Quantities.LiteralQuantity.Of(3),
      _ => MagicAST.AST.Quantities.LiteralQuantity.Of(int.TryParse(rawCount, out var ctn) ? ctn : 1),
    };

    if (!_activatedColorMap.TryGetValue(m.Groups["color"].Value, out var colorCode))
    {
      return null;
    }

    var ctSubtype = m.Groups["subtype"].Value;
    ctSubtype = char.ToUpperInvariant(ctSubtype[0]) + ctSubtype[1..];

    return new MagicAST.AST.Effects.TokenCopy.CreateTokenEffect
    {
      Count = count,
      Token = new MagicAST.AST.Effects.TokenDefinition
      {
        Power = m.Groups["power"].Value,
        Toughness = m.Groups["toughness"].Value,
        Colors = [colorCode],
        Types = ["creature"],
        Subtypes = [ctSubtype],
        IsCopy = false,
      },
      IsOptional = false,
    };
  }

  /// <summary>
  /// "Target creature gets [+-](N|X)/[+-](M|X) (until end of turn | for as long as [condition])." —
  /// P/T modification on an activated ability's effect. Handles both literal
  /// modifiers (+1/+1, -2/-2) and variable modifiers (+X/+X, +X/-X, -X/-X)
  /// covering the Chatterfang shape "{B}, Sacrifice X Squirrels: Target
  /// creature gets +X/-X until end of turn." Variable negation is encoded
  /// descriptively as a <see cref="CalculatedQuantity"/> with
  /// <c>Operation = "negate"</c> wrapping the <see cref="VariableQuantity"/>
  /// — see manifest's "Variable-negation convention" note.
  /// </summary>
  private static ModifyPTEffect? TryParseModifyPTEffect(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();

    // Token grammar for one modifier side: [+\-](\d+|X) e.g. "+1", "-2", "+X", "-X".
    // The literal and variable shapes share a single grammar so a single match
    // captures e.g. "+X/-X" (Chatterfang) or "+1/+1" (Tawnos's Weaponry).
    const string modGrammar = @"(?<{0}>[+\-](?:\d+|X))";
    var pGroup = string.Format(modGrammar, "p");
    var tGroup = string.Format(modGrammar, "t");

    // Shape A: "Target creature gets <mod>/<mod> until end of turn"
    var eotMatch = System.Text.RegularExpressions.Regex.Match(
      trimmed,
      $@"^Target\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (eotMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = ParseSignedModifier(eotMatch.Groups["p"].Value),
        ToughnessModifier = ParseSignedModifier(eotMatch.Groups["t"].Value),
        Duration = new UntilEndOfTurnDuration(),
      };
    }

    // Shape B: "Target creature gets <mod>/<mod> for as long as [condition]"
    var asLongAsMatch = System.Text.RegularExpressions.Regex.Match(
      trimmed,
      $@"^Target\s+creature\s+gets\s+{pGroup}/{tGroup}\s+for\s+as\s+long\s+as\s+(?<cond>.+)$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (asLongAsMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = ParseSignedModifier(asLongAsMatch.Groups["p"].Value),
        ToughnessModifier = ParseSignedModifier(asLongAsMatch.Groups["t"].Value),
        Duration = new AsLongAsDuration { Condition = asLongAsMatch.Groups["cond"].Value.Trim() },
      };
    }

    // Shape C: "This creature gets <mod>/<mod> until end of turn" —
    // self-referential P/T modifier as an activated-ability effect (Rule 613.4c).
    // Subject is the ability's source permanent itself, encoded as ObjectReference.Self().
    // Covers "Sacrifice a [filter]: This creature gets +N/+M until end of turn."
    // and the discard-cost variant "Discard a card: This creature gets +N/+M until end of turn."
    var selfEotMatch = System.Text.RegularExpressions.Regex.Match(
      trimmed,
      $@"^This\s+creature\s+gets\s+{pGroup}/{tGroup}\s+until\s+end\s+of\s+turn$",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (selfEotMatch.Success)
    {
      return new ModifyPTEffect
      {
        Target = ObjectReference.Self(),
        PowerModifier = ParseSignedModifier(selfEotMatch.Groups["p"].Value),
        ToughnessModifier = ParseSignedModifier(selfEotMatch.Groups["t"].Value),
        Duration = new UntilEndOfTurnDuration(),
      };
    }

    return null;
  }

  /// <summary>
  /// Maps a signed modifier token (e.g. "+1", "-2", "+X", "-X") onto a
  /// <see cref="Quantity"/>. Literals fold the sign into the
  /// <see cref="LiteralQuantity.Value"/>; the variable case keeps the variable
  /// name and (for "-X") wraps it in a <see cref="CalculatedQuantity"/> with
  /// <c>Operation = "negate"</c>. The descriptive form preserves the variable
  /// identity while making the sign explicit on the wrapper — no new AST type
  /// needed (per skill discipline: model what oracle text *says*, compose with
  /// existing primitives).
  /// </summary>
  private static Quantity ParseSignedModifier(string token)
  {
    var sign = token[0]; // '+' or '-'
    var rest = token[1..];
    if (string.Equals(rest, "X", StringComparison.OrdinalIgnoreCase)
      || string.Equals(rest, "Y", StringComparison.OrdinalIgnoreCase)
      || string.Equals(rest, "Z", StringComparison.OrdinalIgnoreCase))
    {
      var variable = new VariableQuantity { Name = rest.ToUpperInvariant() };
      if (sign == '+')
      {
        return variable;
      }
      return new CalculatedQuantity
      {
        Expression = $"-{rest.ToUpperInvariant()}",
        BaseQuantity = variable,
        Operation = "negate",
      };
    }

    var magnitude = int.Parse(rest);
    return LiteralQuantity.Of(sign == '-' ? -magnitude : magnitude);
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
  /// "Target player becomes the monarch." — Rule 716 monarch designation
  /// granted to a chosen player.
  /// </summary>
  private static MagicAST.AST.Effects.Timing.BecomeMonarchEffect? TryParseBecomeMonarchEffect(
    string effectText
  )
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (
      !Regex.IsMatch(
        trimmed,
        @"^Target\s+player\s+becomes\s+the\s+monarch$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return null;
    }
    return new MagicAST.AST.Effects.Timing.BecomeMonarchEffect
    {
      Player = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["player"] },
      },
    };
  }

  /// <summary>
  /// "[Self] deals N damage to any target." — self-as-source dealDamage with
  /// AnyTarget. Captures Denethor's burn tail; works for any card whose
  /// oracle line references itself by name in the source position.
  /// </summary>
  private static MagicAST.AST.Effects.Damage.DealDamageEffect? TryParseSelfDealsDamageToAnyTargetEffect(
    string effectText
  )
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+any\s+target$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    // The subject is the card's own name (capitalised); detect via leading
    // capital + ASCII-letter-only word chars to avoid matching pronouns.
    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return null;
    }
    var raw = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = raw switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(raw),
    };
    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
    };
  }

  /// <summary>
  /// "[This creature|CardName] deals N damage to target attacking or blocking creature." —
  /// activated-ability pattern for archers, ballistas, and similar tap-to-deal-damage
  /// permanents (Rule 700.4 — "attacking" and "blocking" are combat-status predicates).
  /// Accepts either the pronoun form ("This creature deals …") or the self-by-name form
  /// ("Lady Caleria deals …") in the subject slot; both encode the source as
  /// <see cref="ObjectReference.Self()"/>. The target is a creature with
  /// <see cref="ObjectFilter.Characteristics"/> = ["attacking or blocking"].
  /// </summary>
  private static MagicAST.AST.Effects.Damage.DealDamageEffect? TryParseSelfDealsDamageToAttackingOrBlockingCreatureEffect(
    string effectText
  )
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var m = Regex.Match(
      trimmed,
      @"^(?<subject>This\s+creature|\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five)\s+damage\s+to\s+target\s+attacking\s+or\s+blocking\s+creature$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    // Accept "This creature" (pronoun self-ref) or a capitalised proper-noun subject
    // (self-by-name). Reject lowercase pronouns or uncapitalised words to avoid
    // misrouting other effect phrasings.
    var subject = m.Groups["subject"].Value;
    var isThisCreature = subject.Equals("This creature", StringComparison.OrdinalIgnoreCase);
    var isNamedSelf = subject.Length > 0 && char.IsUpper(subject[0]) && !isThisCreature;
    if (!isThisCreature && !isNamedSelf)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value.ToLowerInvariant();
    int amount = rawAmount switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      _ => int.Parse(rawAmount),
    };

    return new MagicAST.AST.Effects.Damage.DealDamageEffect
    {
      Amount = LiteralQuantity.Of(amount),
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Characteristics = ["attacking or blocking"],
        },
      },
    };
  }

  /// <summary>
  /// Tries to parse "You gain N life" effects, where N is a literal number,
  /// number word, or a variable like X.
  /// Patterns: "You gain X life.", "You gain 2 life.", "You gain three life."
  /// </summary>
  private GainLifeEffect? TryParseGainLifeEffect(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.');
    var match = Regex.Match(
      text,
      @"^You\s+gain\s+(?<amount>X|Y|Z|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var amountText = match.Groups["amount"].Value;
    Quantity amount;
    if (amountText.Equals("X", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.X;
    }
    else if (amountText.Equals("Y", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Y;
    }
    else if (amountText.Equals("Z", StringComparison.OrdinalIgnoreCase))
    {
      amount = VariableQuantity.Z;
    }
    else
    {
      var count = ParseNumberWord(amountText) ?? 1;
      amount = LiteralQuantity.Of(count);
    }

    return new GainLifeEffect { Amount = amount, Player = ObjectReference.You() };
  }

  /// <summary>
  /// Tries to parse "Tap [count] target [type]" effects.
  /// Patterns:
  /// - "Tap target creature."           → Count = null (single target)
  /// - "Tap X target lands."            → Count = VariableQuantity X
  /// - "Tap two target creatures."      → Count = LiteralQuantity 2
  /// Rule 701.21 (Tap). For variable-X activated abilities, the X in the cost and
  /// the X in the effect refer to the same chosen value (Rule 107.3b/c).
  /// </summary>
  private TapEffect? TryParseTapEffect(string effectText)
  {
    var text = effectText.Trim().TrimEnd('.');
    var lower = text.ToLowerInvariant();

    if (!lower.StartsWith("tap "))
    {
      return null;
    }

    // Strip leading "tap " before parsing count + target noun.
    var rest = text[4..].Trim();
    var restLower = rest.ToLowerInvariant();

    Quantity? count = null;
    var quantityMatch = System.Text.RegularExpressions.Regex.Match(
      rest,
      @"^(X|\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+target\b",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (quantityMatch.Success)
    {
      var qStr = quantityMatch.Groups[1].Value;
      if (string.Equals(qStr, "X", StringComparison.OrdinalIgnoreCase))
      {
        count = VariableQuantity.X;
      }
      else
      {
        var n = ParseNumberWord(qStr) ?? int.Parse(qStr);
        count = LiteralQuantity.Of(n);
      }
    }
    else if (!restLower.StartsWith("target"))
    {
      // We can't recognize what's between "tap" and "target" — bail.
      return null;
    }

    // Match "target X or Y" (two card types) or "target X" (single card type).
    var orTargetMatch = System.Text.RegularExpressions.Regex.Match(
      rest,
      @"\btarget\s+(\w+)\s+or\s+(\w+)",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (orTargetMatch.Success)
    {
      var noun1 = orTargetMatch.Groups[1].Value.ToLowerInvariant();
      if (noun1.EndsWith("s") && noun1.Length > 1)
      {
        noun1 = noun1[..^1];
      }
      var noun2 = orTargetMatch.Groups[2].Value.ToLowerInvariant();
      if (noun2.EndsWith("s") && noun2.Length > 1)
      {
        noun2 = noun2[..^1];
      }
      var orFilter = new ObjectFilter { CardTypes = [noun1, noun2] };
      return new TapEffect { Target = ObjectReference.Target(orFilter), Count = count };
    }

    var targetMatch = System.Text.RegularExpressions.Regex.Match(
      rest,
      @"\btarget\s+(\w+)",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase
    );
    if (!targetMatch.Success)
    {
      return null;
    }
    var noun = targetMatch.Groups[1].Value.ToLowerInvariant();
    if (noun.EndsWith("s") && noun.Length > 1)
    {
      noun = noun[..^1];
    }

    var filter = new ObjectFilter { CardTypes = [noun] };
    var target = ObjectReference.Target(filter);

    return new TapEffect { Target = target, Count = count };
  }

  /// <summary>
  /// Tries to parse "Untap target [subtype]" effects.
  /// Pattern: "Untap target Forest.", "Untap target creature."
  /// </summary>
  /// <remarks>
  /// First cut — recognises a single-token target subtype/cardtype and produces an
  /// <see cref="UntapEffect"/> with an <see cref="ObjectFilter.Subtypes"/> entry.
  /// More elaborate targeting (multiple subtypes, conditions, "up to N", etc.) is
  /// out of scope for the smoke test and should land in a follow-up session.
  /// </remarks>
  private static UntapEffect? TryParseUntapEffect(string effectText)
  {
    var text = effectText.Trim();
    if (text.EndsWith('.'))
    {
      text = text[..^1].Trim();
    }

    if (!text.StartsWith("Untap ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var remainder = text["Untap ".Length..].Trim();

    // "another target creature or land [you control]" — excludes the source
    // permanent from targeting ("another" → Characteristics: ["another"]) and
    // accepts either a creature or a land as the target. Rule 701.20 / 115.1.
    if (remainder.StartsWith("another target ", StringComparison.OrdinalIgnoreCase))
    {
      var afterAnotherTarget = remainder["another target ".Length..].Trim();
      var creatureOrLandMatch = Regex.Match(
        afterAnotherTarget,
        @"^(?:creature\s+or\s+land|land\s+or\s+creature)(?:\s+you\s+control)?$",
        RegexOptions.IgnoreCase
      );
      if (creatureOrLandMatch.Success)
      {
        var hasController = afterAnotherTarget.Contains(" you control", StringComparison.OrdinalIgnoreCase);
        return new UntapEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter
            {
              CardTypes = ["creature", "land"],
              Characteristics = ["another"],
              Controller = hasController ? ControllerFilter.You : null,
            },
          },
        };
      }
    }

    if (!remainder.StartsWith("target ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    var subtype = remainder["target ".Length..].Trim();
    if (string.IsNullOrEmpty(subtype) || subtype.Contains(' '))
    {
      // Multi-word filter (e.g., "target tapped creature") — beyond this smoke-test cut.
      return null;
    }

    return new UntapEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { Subtypes = [subtype] },
      },
    };
  }

  /// <summary>
  /// Tries to parse "Add {mana}" effects.
  /// Pattern: "Add {G}", "Add {C}{C}{C}", "Add {W}{U}{B}{R}{G}", etc.
  /// Also handles "Add one mana of any color" (Crystal Grotto / Chromatic Lantern
  /// shape) where the produced mana is a single choice across all five colors.
  /// </summary>
  private AddManaEffect? TryParseAddManaEffect(string effectText)
  {
    // Normalize whitespace
    effectText = effectText.Trim();

    // Pattern: "Add" followed by mana symbols, optionally ending with "."
    if (!effectText.StartsWith("Add ", StringComparison.OrdinalIgnoreCase))
    {
      return null;
    }

    // Extract the mana portion (everything after "Add" and before optional ".")
    var manaText = effectText[4..].Trim();
    if (manaText.EndsWith('.'))
    {
      manaText = manaText[..^1].Trim();
    }

    // "one mana of any color" — single-pip wildcard production. The choice
    // axis lives on AnyColor; Mana is left empty because no concrete symbol
    // is committed at print time.
    if (
      Regex.IsMatch(
        manaText,
        @"^one\s+mana\s+of\s+any\s+color$",
        RegexOptions.IgnoreCase
      )
    )
    {
      return new AddManaEffect { Mana = string.Empty, AnyColor = true };
    }

    // The mana text should be a sequence of mana symbols like "{G}" or "{C}{C}{C}"
    // We'll just pass it through as-is since AddManaEffect.Mana is a string
    if (string.IsNullOrWhiteSpace(manaText) || !manaText.Contains('{'))
    {
      return null;
    }

    return new AddManaEffect { Mana = manaText, AnyColor = false };
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

  /// <summary>
  /// Checks if a token kind represents a mana symbol.
  /// </summary>
  private static bool IsManaToken(OracleToken kind)
  {
    return kind == OracleToken.GenericMana
      || kind == OracleToken.VariableMana
      || kind == OracleToken.WhiteMana
      || kind == OracleToken.BlueMana
      || kind == OracleToken.BlackMana
      || kind == OracleToken.RedMana
      || kind == OracleToken.GreenMana
      || kind == OracleToken.ColorlessMana
      || kind == OracleToken.HybridMana
      || kind == OracleToken.PhyrexianMana
      || kind == OracleToken.TwoHybridMana
      || kind == OracleToken.HybridPhyrexianMana
      || kind == OracleToken.SnowMana;
  }

  /// <summary>
  /// Converts an OracleToken to a ManaSymbol.
  /// </summary>
  private ManaSymbol? ConvertTokenToManaSymbol(Token<OracleToken> token)
  {
    var content = token.ToStringValue().Trim('{', '}').ToUpperInvariant();

    // Use ManaCostParser to parse the symbol
    var parsed = _manaCostParser.Parse($"{{{content}}}");
    return parsed.Symbols.FirstOrDefault();
  }

  #region Cost Component Parsers

  /// <summary>
  /// Tries to parse a mana cost component like "{1}", "{2}{G}", "{T}", "{Q}".
  /// Returns ManaCost, TapCost, or UntapCost depending on the symbols.
  /// </summary>
  private Cost? TryParseManaCostComponent(string costText)
  {
    costText = costText.Trim();

    // Check for tap symbol
    if (costText == "{T}")
    {
      return new TapCost();
    }

    // Check for untap symbol
    if (costText == "{Q}")
    {
      return new UntapCost();
    }

    // Try to parse as mana cost using ManaCostParser
    try
    {
      var parsed = _manaCostParser.Parse(costText);
      if (parsed.Symbols.Count > 0)
      {
        return new ManaCost { Symbols = parsed.Symbols };
      }
    }
    catch
    {
      // Parsing failed, return null
    }

    return null;
  }

  /// <summary>
  /// Tries to parse sacrifice costs like "Sacrifice another creature", "Sacrifice X Squirrels".
  /// Reuses shared pattern logic with sacrifice effects.
  /// </summary>
  private SacrificeCost? TryParseSacrificeCost(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("sacrifice"))
    {
      return null;
    }

    // Parse using shared pattern helpers
    var (quantity, filter) = ParseSacrificePattern(costText);
    if (filter == null)
    {
      return null;
    }

    return new SacrificeCost { Filter = filter, Quantity = quantity };
  }

  /// <summary>
  /// Tries to parse discard costs like "Discard a card", "Discard a legendary card".
  /// Reuses shared pattern logic with discard effects.
  /// </summary>
  private DiscardCost? TryParseDiscardCost(string costText)
  {
    costText = costText.Trim();
    var lower = costText.ToLowerInvariant();

    if (!lower.StartsWith("discard"))
    {
      return null;
    }

    // Parse using shared pattern helpers
    var (quantity, filter) = ParseDiscardPattern(costText);

    return new DiscardCost { Filter = filter, Quantity = quantity };
  }

  /// <summary>
  /// Tries to parse pay-life costs like "Pay 1 life", "Pay 3 life".
  /// Rule 118. The life payment is a cost that reduces the player's life total.
  /// </summary>
  private PayLifeCost? TryParsePayLifeCost(string costText)
  {
    var trimmed = costText.Trim();
    var m = Regex.Match(
      trimmed,
      @"^Pay\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var rawAmount = m.Groups["amount"].Value;
    var amount = ParseNumberWord(rawAmount) ?? (int.TryParse(rawAmount, out var n) ? n : (int?)null);
    if (amount is null)
    {
      return null;
    }

    return new PayLifeCost { Amount = LiteralQuantity.Of(amount.Value) };
  }

  /// <summary>
  /// Tries to parse remove-counters costs like
  /// "Remove three spore counters from this creature"
  /// or "Remove two charge counters from this permanent".
  /// Rule 122 (counters). The counter type is a named counter (Rule 122.1);
  /// the source is encoded as <see cref="ObjectReference.Self()"/> because
  /// oracle text always reads "from this creature / from this permanent"
  /// for activated-ability costs on a card referencing itself.
  /// </summary>
  private static RemoveCountersCost? TryParseRemoveCountersCost(string costText)
  {
    var trimmed = costText.Trim();
    // Pattern: "Remove <count> <type> counter(s) from this <noun>"
    var m = Regex.Match(
      trimmed,
      @"^Remove\s+(?<count>a|an|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[\w\-/+]+)\s+counters?\s+from\s+this\s+\w+$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var rawCount = m.Groups["count"].Value.ToLowerInvariant();
    var quantity = rawCount switch
    {
      "a" or "an" or "one" => LiteralQuantity.Of(1),
      "two" => LiteralQuantity.Of(2),
      "three" => LiteralQuantity.Of(3),
      "four" => LiteralQuantity.Of(4),
      "five" => LiteralQuantity.Of(5),
      "six" => LiteralQuantity.Of(6),
      "seven" => LiteralQuantity.Of(7),
      "eight" => LiteralQuantity.Of(8),
      "nine" => LiteralQuantity.Of(9),
      "ten" => LiteralQuantity.Of(10),
      _ => int.TryParse(rawCount, out var n) ? LiteralQuantity.Of(n) : null,
    };
    if (quantity is null)
    {
      return null;
    }

    var counterType = m.Groups["type"].Value.ToLowerInvariant();

    return new RemoveCountersCost
    {
      CounterType = counterType,
      Quantity = quantity,
      Target = ObjectReference.Self(),
    };
  }

  #endregion

  #region Shared Pattern Parsers (used by both costs and effects)

  /// <summary>
  /// Parses "sacrifice [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  private (Quantity quantity, ObjectFilter? filter) ParseSacrificePattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    Quantity quantity;
    if (lower.Contains(" x "))
    {
      quantity = VariableQuantity.X;
    }
    else
    {
      var count = ParseNumberWord(text) ?? 1;
      quantity = LiteralQuantity.Of(count);
    }

    // Parse filter
    ObjectFilter? filter = null;
    if (lower.Contains("another creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"], Characteristics = ["another"] };
    }
    else if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"], Characteristics = ["this permanent"] };
    }
    else if (lower.Contains("this artifact"))
    {
      filter = new ObjectFilter { CardTypes = ["artifact"], Characteristics = ["this permanent"] };
    }
    else if (lower.Contains("this enchantment"))
    {
      filter = new ObjectFilter { CardTypes = ["enchantment"], Characteristics = ["this permanent"] };
    }
    else if (lower.Contains("this land"))
    {
      filter = new ObjectFilter { CardTypes = ["land"], Characteristics = ["this permanent"] };
    }
    else if (lower.Contains(" land") || lower.EndsWith("land"))
    {
      // "Sacrifice a land" / "Sacrifice another land" — land is a card type (Rule 205.3a),
      // not a subtype. Must precede the creature/artifact branches to avoid misrouting via
      // the generic fallback regex which would emit Subtypes: ["land"] instead.
      filter = new ObjectFilter { CardTypes = ["land"] };
    }
    else if (Regex.IsMatch(lower, @"\btoken\b") && !lower.Contains("creature") && !lower.Contains("artifact"))
    {
      // "Sacrifice a token" — "token" is a characteristic predicate (Rule 111.7),
      // not a card type or subtype. Encodes as Characteristics: ["token"] to
      // match the gold convention and distinguish from typed-token costs like
      // "Sacrifice a creature token".
      filter = new ObjectFilter { Characteristics = ["token"] };
    }
    else if (lower.Contains("creature"))
    {
      filter = new ObjectFilter { CardTypes = ["creature"] };
    }
    else if (lower.Contains("artifact"))
    {
      filter = new ObjectFilter { CardTypes = ["artifact"] };
    }
    else
    {
      // Try to extract the type from the text.
      //
      // Shape 1: "Sacrifice [count-word] [Subtype]s" — e.g. "Sacrifice three Treasures".
      // The count word was already parsed into `quantity` above; here we just need
      // the subtype. We look for an explicit count-word + capitalized-subtype pair
      // BEFORE the generic article regex so "three" doesn't get captured as the type.
      var countSubtypeMatch = Regex.Match(
        text,
        @"(?:Sacrifice|sacrifice)\s+(?:one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+(?<type>[A-Z]\w+?)s?$",
        RegexOptions.None
      );
      if (countSubtypeMatch.Success)
      {
        var typeRaw = countSubtypeMatch.Groups["type"].Value;
        filter = new ObjectFilter { Subtypes = [typeRaw] };
      }
      else
      {
        // Shape 2: "Sacrifice [article] [type]"
        // Capture the optional article/count so we can distinguish self-reference
        // from creature-subtype: "Sacrifice Denethor" (no article → self) vs.
        // "Sacrifice a Saproling" (article "a" → creature subtype, Rule 205.3m).
        var match = Regex.Match(
          text,
          @"(?:Sacrifice|sacrifice) (?<article>a |an |X )?(?<type>\w+)",
          RegexOptions.IgnoreCase
        );
        if (match.Success)
        {
          var typeRaw = match.Groups["type"].Value;
          var type = typeRaw.ToLowerInvariant();
          var hasArticle = match.Groups["article"].Success && match.Groups["article"].Value.Trim() is "a" or "an";
          var wasPlural = type.EndsWith("s") && type != "this";
          // Handle plurals (e.g., "Squirrels" -> "Squirrel")
          if (wasPlural)
          {
            type = type[..^1];
          }
          // Capitalized SINGULAR without an article (e.g., "Sacrifice Denethor") —
          // the card refers to itself by name. Encode as a "this permanent"
          // self-reference on Characteristics rather than a literal Subtypes
          // entry, matching the gold convention for self-by-name cost references.
          //
          // Capitalized PLURAL (e.g., "Sacrifice X Squirrels") or capitalized with
          // an article (e.g., "Sacrifice a Saproling") is a creature subtype, not
          // a self-reference — oracle text capitalizes creature subtypes (Rule 205.3m).
          // Singularize and emit on Subtypes. Without this distinction the plural-subtype
          // case and the article-preceded case collapse onto the "this permanent"
          // self-ref shape.
          if (char.IsUpper(typeRaw[0]) && !wasPlural && !hasArticle)
          {
            filter = new ObjectFilter { Characteristics = ["this permanent"] };
          }
          else
          {
            // Title-case the subtype to match oracle-text capitalisation
            // convention. Subtype names are proper-noun-ish (Squirrel, Goblin,
            // Treasure, etc.) and the existing CreateTokenEffect emitters
            // (e.g. ActivatedAbilityParser.TryParseCreateTokenEffect at the
            // top of this file) already title-case their subtype output.
            if (char.IsUpper(typeRaw[0]) && type.Length > 0)
            {
              type = char.ToUpperInvariant(type[0]) + type[1..];
            }
            filter = new ObjectFilter { Subtypes = [type] };
          }
        }
      }
    }

    return (quantity, filter);
  }

  /// <summary>
  /// Parses "discard [quantity] [filter]" patterns.
  /// Returns (quantity, filter) tuple that can be used for both costs and effects.
  /// </summary>
  private (Quantity quantity, ObjectFilter filter) ParseDiscardPattern(string text)
  {
    var lower = text.ToLowerInvariant();

    // Parse quantity
    var count = ParseNumberWord(text) ?? 1;
    var quantity = LiteralQuantity.Of(count);

    // Parse filter
    ObjectFilter filter;
    if (lower.Contains("legendary card"))
    {
      filter = new ObjectFilter { Supertypes = ["Legendary"], CardTypes = ["card"] };
    }
    else
    {
      filter = new ObjectFilter { CardTypes = ["card"] };
    }

    return (quantity, filter);
  }

  #endregion

  #region Effect Parsers

  /// <summary>
  /// Tries to parse "Scry N" effects.
  /// Pattern: "Scry 2", "Scry 1", etc.
  /// </summary>
  private ScryEffect? TryParseScryEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');

    var match = Regex.Match(effectText, @"^Scry\s+(\d+)$", RegexOptions.IgnoreCase);
    if (!match.Success)
    {
      return null;
    }

    var count = int.Parse(match.Groups[1].Value);
    return new ScryEffect { Count = LiteralQuantity.Of(count) };
  }

  /// <summary>
  /// Tries to parse "Draw N cards" effects.
  /// Patterns: "Draw two cards", "Draw a card", "Each other player draws a card"
  /// </summary>
  private DrawCardsEffect? TryParseDrawCardsEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    // Pattern: "draw [count] card(s)"
    if (!lower.Contains("draw"))
    {
      return null;
    }

    // Determine player. "Each other player" is broader than "each opponent" —
    // it includes everyone except the controller, which matters in multiplayer
    // formats (Rule 109.1 / 102.1). Map onto EachOtherPlayer rather than
    // collapsing onto EachOpponent.
    ObjectReference player;
    if (lower.Contains("each other player"))
    {
      player = new ObjectReference { Kind = ObjectReferenceKind.EachOtherPlayer };
    }
    else if (lower.Contains("each opponent"))
    {
      player = new ObjectReference { Kind = ObjectReferenceKind.EachOpponent };
    }
    else if (lower.Contains("you"))
    {
      player = ObjectReference.You();
    }
    else
    {
      // Default to "you"
      player = ObjectReference.You();
    }

    // Parse count
    var count = ParseNumberWord(effectText) ?? 1;

    return new DrawCardsEffect { Count = LiteralQuantity.Of(count), Player = player };
  }

  /// <summary>
  /// Tries to parse "Discard N cards" effects.
  /// Patterns: "Discard up to two cards", "Discard a legendary card"
  /// </summary>
  private DiscardCardsEffect? TryParseDiscardCardsEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("discard"))
    {
      return null;
    }

    // Parse "up to N"
    var upToMatch = Regex.Match(effectText, @"up to (\w+)", RegexOptions.IgnoreCase);
    int count;
    if (upToMatch.Success)
    {
      count = ParseNumberWord(upToMatch.Groups[1].Value) ?? 1;
    }
    else
    {
      count = ParseNumberWord(effectText) ?? 1;
    }

    // Check for filter (e.g., "a legendary card")
    ObjectFilter? filter = null;
    if (lower.Contains("legendary"))
    {
      filter = new ObjectFilter { Supertypes = ["legendary"] };
    }

    return new DiscardCardsEffect
    {
      Count = LiteralQuantity.Of(count),
      Player = ObjectReference.You(),
      Filter = filter,
      Random = false,
    };
  }

  /// <summary>
  /// Tries to parse "Put N +1/+1 counters on [target]" effects.
  /// Patterns: "Put a +1/+1 counter on this creature", "Put a +1/+1 counter on target creature you control"
  /// </summary>
  private PutCountersEffect? TryParsePutCountersEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("put") || !lower.Contains("counter"))
    {
      return null;
    }

    // Parse counter type
    string counterType;
    if (lower.Contains("+1/+1"))
    {
      counterType = "+1/+1";
    }
    else if (lower.Contains("-1/-1"))
    {
      counterType = "-1/-1";
    }
    else
    {
      return null; // Unknown counter type
    }

    // Parse count
    var count = ParseNumberWord(effectText) ?? 1;

    // Parse target
    ObjectReference target;
    if (lower.Contains("this creature") || lower.Contains("this permanent"))
    {
      target = ObjectReference.Self();
    }
    else if (lower.Contains("target creature you control"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      };
    }
    else if (lower.Contains("target creature"))
    {
      target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      };
    }
    else
    {
      // Default to self
      target = ObjectReference.Self();
    }

    return new PutCountersEffect
    {
      Target = target,
      CounterType = counterType,
      Count = LiteralQuantity.Of(count),
    };
  }

  /// <summary>
  /// Tries to parse "gains [keyword]" effects on a specific target.
  /// Supported patterns:
  /// - "Creatures you control gain [keyword]" (Each, creatureFilter)
  /// - "This [type] gains [keyword] until end of turn" (Self)
  /// </summary>
  private GainAbilityEffect? TryParseGainAbilityEffect(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("gain"))
    {
      return null;
    }

    // Pattern: "This [type] gains [keyword]" — self-targeting activated ability effect.
    var selfMatch = Regex.Match(
      effectText,
      @"^This\s+\w+\s+gains?\s+(\w+)",
      RegexOptions.IgnoreCase
    );
    if (selfMatch.Success)
    {
      var selfKeyword = selfMatch.Groups[1].Value;
      var selfAbility = BuildGrantedKeywordAbility(selfKeyword);
      if (selfAbility is not null)
      {
        Duration? selfDuration = null;
        if (lower.Contains("until end of turn"))
        {
          selfDuration = new UntilEndOfTurnDuration();
        }
        else if (lower.Contains("until your next turn"))
        {
          selfDuration = new UntilYourNextTurnDuration();
        }
        return new GainAbilityEffect
        {
          Target = ObjectReference.Self(),
          GainedAbility = selfAbility,
          Duration = selfDuration,
        };
      }
    }

    // Pattern: "Creatures you control gain [ability]"
    var match = Regex.Match(
      effectText,
      @"Creatures you control gain (\w+)",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var keyword = match.Groups[1].Value;
    var gainedAbility = BuildGrantedKeywordAbility(keyword);
    if (gainedAbility is null)
    {
      return null;
    }

    Duration? duration = null;
    if (lower.Contains("until end of turn"))
    {
      duration = new UntilEndOfTurnDuration();
    }
    else if (lower.Contains("until your next turn"))
    {
      duration = new UntilYourNextTurnDuration();
    }

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      },
      GainedAbility = gainedAbility,
      Duration = duration,
    };
  }

  /// <summary>
  /// Wraps a granted-keyword name into a structured <see cref="StaticAbility"/>
  /// carrying the keyword's effect node. Returns null when the keyword has no
  /// modeled effect yet — caller treats that as a parser miss.
  /// </summary>
  /// <remarks>
  /// Granted keywords are descriptively "the gainer now has [keyword]", which
  /// is a static keyword ability — same shape MAST already uses when the
  /// keyword appears directly on a card (see Vito's lifelink, Rory's
  /// first-strike, etc.). The discriminator stays consistent across direct vs.
  /// granted appearances.
  /// </remarks>
  private static Ability? BuildGrantedKeywordAbility(string keywordRaw)
  {
    var keyword = keywordRaw.Trim().ToLowerInvariant();
    Effect? effect = keyword switch
    {
      "lifelink" => new LifelinkEffect(),
      "haste" => new HasteEffect(),
      "trample" => new TrampleEffect(),
      "vigilance" => new VigilanceEffect(),
      "reach" => new ReachEffect(),
      "indestructible" => new IndestructibleEffect(),
      _ => null,
    };

    if (effect is null)
    {
      return null;
    }

    // Title-case the keyword for KeywordSource (matches direct-keyword ability convention).
    var keywordSource = char.ToUpperInvariant(keyword[0]) + keyword[1..];
    return new StaticAbility { Effects = [effect], KeywordSource = keywordSource };
  }

  /// <summary>
  /// Tries to parse "Return target [X] from [zone] to the battlefield." effects.
  /// Pattern: "Return target creature or Vehicle card from your graveyard to the battlefield."
  /// Produces a <see cref="ReturnToBattlefieldEffect"/> with a Target ObjectReference whose
  /// Filter captures the Zone and the full type-phrase as a Characteristics entry.
  /// </summary>
  private static ReturnToBattlefieldEffect? TryParseReturnToBattlefieldEffect(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    // Pattern: "Return target <what> from [your|the|a] <zone> to the battlefield"
    var m = Regex.Match(
      trimmed,
      @"^return\s+target\s+(?<what>.+?)\s+from\s+(?:your|the|a|an)\s+(?<zone>graveyard|hand|library|exile)\s+to\s+the\s+battlefield$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }

    var what = m.Groups["what"].Value.Trim();
    var zoneRaw = m.Groups["zone"].Value.ToLowerInvariant();

    var zone = zoneRaw switch
    {
      "graveyard" => Zone.Graveyard,
      "hand" => Zone.Hand,
      "library" => Zone.Library,
      "exile" => Zone.Exile,
      _ => Zone.Graveyard,
    };

    return new ReturnToBattlefieldEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          Zone = zone,
          Characteristics = [what],
        },
      },
    };
  }

  /// <summary>
  /// Parses number words like "one", "two", "three" into integers.
  /// Returns null if no number word is found.
  /// </summary>
  private int? ParseNumberWord(string text)
  {
    var lower = text.ToLowerInvariant();

    if (lower.Contains("two"))
      return 2;
    if (lower.Contains("three"))
      return 3;
    if (lower.Contains("four"))
      return 4;
    if (lower.Contains("five"))
      return 5;
    if (lower.Contains("six"))
      return 6;
    if (lower.Contains("seven"))
      return 7;
    if (lower.Contains("eight"))
      return 8;
    if (lower.Contains("nine"))
      return 9;
    if (lower.Contains("ten"))
      return 10;
    if (lower.Contains("one") || lower.Contains(" a ") || lower.Contains("an "))
      return 1;

    // Try to find a digit
    var digitMatch = Regex.Match(text, @"\b(\d+)\b");
    if (digitMatch.Success)
    {
      return int.Parse(digitMatch.Groups[1].Value);
    }

    return null;
  }

  /// <summary>
  /// Tries to parse "Regenerate this creature." / "Regenerate target [type]." effects.
  /// Rule 701.19. Creates a regeneration shield on the target permanent: the next
  /// time it would be destroyed this turn, instead remove all damage, tap it, and
  /// remove it from combat if applicable. MAST records the effect and target only;
  /// the shield / destruction-replacement semantics are engine territory.
  /// </summary>
  private static RegenerateEffect? TryParseRegenerateEffect(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    var lower = trimmed.ToLowerInvariant();

    if (!lower.StartsWith("regenerate "))
    {
      return null;
    }

    // "Regenerate this creature" — self-reference
    if (lower == "regenerate this creature" || lower == "regenerate this permanent")
    {
      return new RegenerateEffect { Target = ObjectReference.Self() };
    }

    // "Regenerate target [type]"
    var m = Regex.Match(
      trimmed,
      @"^regenerate\s+target\s+(?<type>\w+)$",
      RegexOptions.IgnoreCase
    );
    if (m.Success)
    {
      var cardType = m.Groups["type"].Value.ToLowerInvariant();
      return new RegenerateEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = [cardType] },
        },
      };
    }

    return null;
  }

  #endregion
}
