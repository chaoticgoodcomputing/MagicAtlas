namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the controller-qualified X-scaled composite buff shape:
///   "Target creature you control gets +X/+M and gains &lt;keyword(s)&gt; until end of turn."
///
/// This is the variable-power sibling of <see cref="ModifyPTAndGainKeywordControlledSpellRule"/>
/// (which only matches literal-digit power/toughness). It covers X-cost combat tricks such as
/// Frantic Confrontation ({X}{R}: "Target creature you control gets +X/+0 and gains first strike
/// and trample until end of turn.") where the power modifier is the announced spell variable X
/// (CR 107.3a — the controller chooses X on cast) and the toughness modifier is a literal.
///
/// Emits a flat list via <see cref="IMultiSpellRule.TryMatchMulti"/>:
/// <c>[ModifyPTEffect (Target=you-control creature), GainAbilityEffect (Target=It), …]</c>,
/// one <see cref="GainAbilityEffect"/> per keyword, all sharing a single
/// <see cref="UntilTimeDuration"/> instance. The single-effect <see cref="ISpellRule.TryMatch"/>
/// always returns false so the flat-list path is the only active route.
///
/// <para>
/// CR citations:
/// CR 107.3a — "If a spell or activated ability has a mana cost … with an {X} … in it, and the
/// value of X isn't defined by the text of that spell or ability, the controller of the spell or
/// ability chooses the value of X."
/// CR 613.4c — "Layer 7c: Effects and counters that modify power and/or toughness (but don't set
/// power and/or toughness to a specific number or value) are applied." (the +X/+0 buff).
/// CR 702.7a — "First strike is a static ability that modifies the rules for the combat damage step."
/// CR 702.19a — "Trample is a static ability that modifies the rules for assigning an attacking
/// creature's combat damage."
/// </para>
/// </summary>
[SpellRule]
public sealed class TargetControlledVariablePTAndGainKeywordsSpellRule : ISpellRule, IMultiSpellRule
{
  // Matches: "Target creature you control gets +X/+0 and gains first strike and trample until end of turn"
  // Power slot is a variable (X/Y/Z); toughness slot is a signed literal. The "you control"
  // qualifier restricts the target; the anchored "+<var>/" gate makes this disjoint from
  // ModifyPTAndGainKeywordControlledSpellRule (literal power) and from ModifyPTBothVariable
  // (variable toughness, no keyword clause).
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+you\s+control\s+gets\s+\+(?<pvar>[XYZ])/(?<tsign>[+\-])(?<t>\d+)\s+and\s+gains\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // -------------------------------------------------------------------------
  // ISpellRule — single-effect path intentionally disabled.
  // -------------------------------------------------------------------------
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  // -------------------------------------------------------------------------
  // IMultiSpellRule — flat effect list.
  // -------------------------------------------------------------------------
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var powerVarName = m.Groups["pvar"].Value.ToUpperInvariant();
    var toughness = int.Parse(m.Groups["t"].Value);
    if (m.Groups["tsign"].Value == "-")
    {
      toughness = -toughness;
    }
    var keywordsText = m.Groups["kws"].Value;

    var duration = UntilTimeDuration.EndOfTurn;
    var targetCreatureYouControl = new ObjectReference
    {
      Kind = ObjectReferenceKind.Target,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
      },
    };
    var it = new ObjectReference { Kind = ObjectReferenceKind.It };

    var list = new List<Effect>
    {
      new ModifyPTEffect
      {
        Target = targetCreatureYouControl,
        PowerModifier = new VariableQuantity { Name = powerVarName },
        ToughnessModifier = LiteralQuantity.Of(toughness),
        Duration = duration,
      },
    };

    // Split keywords on " and " (case-insensitive); each segment names one keyword.
    var keywordNames = Regex.Split(keywordsText.Trim(), @"\s+and\s+", RegexOptions.IgnoreCase);
    foreach (var name in keywordNames)
    {
      var trimmed = name.Trim();
      var ability = BuildKeywordAbility(trimmed);
      if (ability is null)
      {
        // Unrecognised keyword — bail so the fallback parser handles the card.
        effects = null;
        return false;
      }
      list.Add(new GainAbilityEffect
      {
        Target = it,
        GainedAbility = ability,
        Duration = duration,
      });
    }

    effects = list;
    return true;
  }

  // -------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors ModifyPTAndGainKeywordControlledSpellRule.BuildKeywordAbility; kept local so
  // this rule is self-contained and independently evolvable.
  // -------------------------------------------------------------------------
  private static StaticAbility? BuildKeywordAbility(string keyword) =>
    keyword.ToLowerInvariant() switch
    {
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Trample }],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.FirstStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Reach,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Reach }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.DoubleStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Haste }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Deathtouch }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new LifelinkEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Vigilance }],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Indestructible,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Indestructible }],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Hexproof,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Hexproof }],
      },
      "shroud" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shroud,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Shroud }],
      },
      _ => null,
    };
}
