namespace MagicAST.Parsing.Parsers;

using System.Reflection;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Spell;
using MagicAST.Parsing.Tokens;

/// <summary>
/// Dispatches spell-ability oracle-text clauses to the priority-ordered set of
/// <see cref="ISpellRule"/> implementations discovered by reflection at construction
/// time. Each rule lives in its own file under <c>Parsers/Spell/Rules/</c> and is
/// decorated with <see cref="SpellRuleAttribute"/> (see attribute docs for priority
/// convention). Adding a new shape means dropping a new file in <c>Rules/</c> with
/// no edits to any shared file.
/// </summary>
/// <remarks>
/// Falls through to <see cref="FallbackParser"/> when no rule matches.
/// </remarks>
[OracleAbilityParser(AbilityKind.Spell)]
public sealed class SpellAbilityParser : IAbilityParser
{
  private readonly FallbackParser _fallback = new();
  private readonly IReadOnlyList<RuleEntry> _rules;

  /// <summary>
  /// Records the (rule, priority) pair for dispatch and diagnostic attribution.
  /// The optional <see cref="MultiRule"/> is set when the rule also implements
  /// <see cref="IMultiSpellRule"/> and should be tried before the single-effect path.
  /// </summary>
  private readonly record struct RuleEntry(ISpellRule Rule, IMultiSpellRule? MultiRule, string Name, int Priority);

  public SpellAbilityParser()
  {
    _rules = DiscoverRules();
  }

  private static IReadOnlyList<RuleEntry> DiscoverRules()
  {
    var assembly = typeof(SpellAbilityParser).Assembly;
    var found = new List<RuleEntry>();
    foreach (var type in assembly.GetTypes())
    {
      var attr = type.GetCustomAttribute<SpellRuleAttribute>(inherit: false);
      if (attr is null)
      {
        continue;
      }
      if (!typeof(ISpellRule).IsAssignableFrom(type))
      {
        throw new InvalidOperationException(
          $"{type.FullName} has [SpellRule] but does not implement ISpellRule."
        );
      }
      var instance = (ISpellRule?)Activator.CreateInstance(type)
        ?? throw new InvalidOperationException(
          $"Failed to instantiate {type.FullName} (parameterless constructor required)."
        );
      var multiRule = instance as IMultiSpellRule;
      found.Add(new RuleEntry(instance, multiRule, $"SpellAbilityParser.{type.Name}", attr.Priority));
    }
    // Highest priority first; stable secondary by name for determinism within a band.
    return found
      .OrderByDescending(r => r.Priority)
      .ThenBy(r => r.Name, StringComparer.Ordinal)
      .ToList();
  }

  /// <inheritdoc/>
  public IReadOnlyList<Ability> Parse(OracleClause clause, ClauseClassification classification)
  {
    var (effectsText, instructions) = StripAbilityWordConditionalPreamble(
      clause.RawText,
      classification.AbilityWord
    );

    var dispatch = TryParseEffects(effectsText);
    if (dispatch.Effects is null || dispatch.Effects.Count == 0)
    {
      return
      [
        _fallback.Parse(
          clause,
          classification,
          "Spell ability parser couldn't recognise effect",
          lastAttemptedRule: dispatch.LastAttemptedRule ?? "SpellAbilityParser.Parse",
          failurePosition: clause.SourceSpan.Start
        ),
      ];
    }

    return
    [
      new SpellAbility
      {
        Effects = dispatch.Effects,
        AbilityWord = classification.AbilityWord,
        Instructions = instructions,
      },
    ];
  }

  /// <summary>
  /// Peels the "[AbilityWord] — If &lt;condition&gt;," preamble off a spell line.
  /// </summary>
  private static (string EffectsText, IReadOnlyList<string>? Instructions) StripAbilityWordConditionalPreamble(
    string rawText,
    string? abilityWord
  )
  {
    if (abilityWord is null)
    {
      return (rawText, null);
    }
    var emDashIndex = rawText.IndexOf('—');
    if (emDashIndex <= 0)
    {
      return (rawText, null);
    }
    var body = rawText[(emDashIndex + 1)..].TrimStart();
    var ifMatch = Regex.Match(
      body,
      @"^(?<cond>If\s+[^,]+),\s*(?<rest>.+)$",
      RegexOptions.IgnoreCase
    );
    if (!ifMatch.Success)
    {
      return (rawText, null);
    }
    var condition = ifMatch.Groups["cond"].Value.Trim();
    var rest = ifMatch.Groups["rest"].Value;
    return (rest, new[] { condition });
  }

  /// <summary>
  /// Multi-effect dispatch. Returns the effect list plus the name of the rule that
  /// was most recently attempted during dispatch — used for telemetry on fallback.
  /// </summary>
  private (IReadOnlyList<Effect>? Effects, string? LastAttemptedRule) TryParseEffects(string text)
  {
    text = StripReminderText(text);

    var trimmed = text.Trim().TrimEnd('.').Trim();
    var pair = TryParseModifyPTConjunctionEffectsList(trimmed);
    if (pair is not null)
    {
      return (pair, "SpellAbilityParser.ModifyPTConjunctionEffectsList");
    }

    var bundled = TryParseSentenceBundleEffects(text, out var bundledLastRule);
    if (bundled is not null)
    {
      return (bundled, bundledLastRule);
    }

    var (multi, multiLastRule) = TryParseMultiSpellRuleEffects(text);
    if (multi is not null)
    {
      return (multi, multiLastRule);
    }

    var (single, singleLastRule) = TryParseEffect(text);
    if (single is null)
    {
      return (null, singleLastRule);
    }
    return (new List<Effect> { single }, singleLastRule);
  }

  private IReadOnlyList<Effect>? TryParseSentenceBundleEffects(string text, out string? lastAttempted)
  {
    lastAttempted = null;
    var working = text.Trim();
    if (working.EndsWith('.'))
    {
      working = working[..^1];
    }

    var boundaries = Regex.Matches(working, @"\.\s+(?=[A-Z])");
    if (boundaries.Count == 0)
    {
      return null;
    }

    var sentences = Regex.Split(working, @"\.\s+(?=[A-Z])");
    if (sentences.Length < 2)
    {
      return null;
    }

    var collected = new List<Effect>(sentences.Length);
    foreach (var sentence in sentences)
    {
      var fragment = sentence.Trim();
      if (fragment.Length == 0)
      {
        return null;
      }
      var (effect, ruleName) = TryParseEffect(fragment);
      lastAttempted = ruleName;
      if (effect is null)
      {
        return null;
      }
      collected.Add(effect);
    }
    return collected;
  }

  /// <summary>
  /// Rookie-Mistake shape returning the gold's flat two-element list directly.
  /// Kept on the dispatcher because it returns a list (the per-effect dispatch
  /// returns a single Effect) — not a normal rule.
  /// </summary>
  private static IReadOnlyList<Effect>? TryParseModifyPTConjunctionEffectsList(string text)
  {
    var m = Regex.Match(
      text,
      @"^Until\s+end\s+of\s+turn,\s*target\s+creature\s+gets\s+(?<p1>[+-]\d+)/(?<t1>[+-]\d+)\s+and\s+another\s+target\s+creature\s+gets\s+(?<p2>[+-]\d+)/(?<t2>[+-]\d+)$",
      RegexOptions.IgnoreCase
    );
    if (!m.Success)
    {
      return null;
    }
    var p1 = int.Parse(m.Groups["p1"].Value);
    var t1 = int.Parse(m.Groups["t1"].Value);
    var p2 = int.Parse(m.Groups["p2"].Value);
    var t2 = int.Parse(m.Groups["t2"].Value);

    var duration = new UntilEndOfTurnDuration();
    return new List<Effect>
    {
      new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter { CardTypes = ["creature"] },
        },
        PowerModifier = LiteralQuantity.Of(p1),
        ToughnessModifier = LiteralQuantity.Of(t1),
        Duration = duration,
      },
      new ModifyPTEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = ["another"],
          },
        },
        PowerModifier = LiteralQuantity.Of(p2),
        ToughnessModifier = LiteralQuantity.Of(t2),
        Duration = duration,
      },
    };
  }

  /// <summary>
  /// Tries each <see cref="IMultiSpellRule"/> in the priority-ordered rule chain.
  /// Returns the flat effect list and the name of the matching rule on success;
  /// returns (null, null) when no multi-rule fires.
  /// </summary>
  private (IReadOnlyList<Effect>? Effects, string? LastAttemptedRule) TryParseMultiSpellRuleEffects(string text)
  {
    var trimmed = text.Trim().TrimEnd('.').Trim();
    foreach (var entry in _rules)
    {
      if (entry.MultiRule is null)
      {
        continue;
      }
      if (entry.MultiRule.TryMatchMulti(trimmed, out var effects) && effects is not null)
      {
        return (effects, entry.Name);
      }
    }
    return (null, null);
  }

  /// <summary>
  /// Dispatches a single fragment through the priority-ordered rule chain. Returns
  /// the matched effect (or null) plus the name of the last rule attempted, for
  /// telemetry attribution on fallback.
  /// </summary>
  private (Effect? Effect, string? LastAttemptedRule) TryParseEffect(string text)
  {
    var trimmed = text.Trim().TrimEnd('.').Trim();
    string? lastName = null;
    foreach (var entry in _rules)
    {
      lastName = entry.Name;
      if (entry.Rule.TryMatch(trimmed, out var effect) && effect is not null)
      {
        return (effect, entry.Name);
      }
    }
    return (null, lastName);
  }

  /// <summary>
  /// Removes parenthesized reminder text (Rule 207.2).
  /// </summary>
  private static string StripReminderText(string text)
  {
    var stripped = Regex.Replace(text, @"\s*\([^)]*\)", string.Empty);
    return stripped.Trim();
  }
}
