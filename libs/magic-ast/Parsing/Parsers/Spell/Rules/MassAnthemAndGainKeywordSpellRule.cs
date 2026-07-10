namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// Recognises the mass P/T-modification-plus-keyword-grant shape for creatures the
/// caster controls:
///   "Creatures you control get +N/+M and gain &lt;keyword(s)&gt; until end of turn."
///
/// Sibling of <see cref="MassAnthemSpellRule"/> (which covers the P/T-only shape) —
/// this rule requires the "and gain &lt;keyword&gt;" clause and is fully anchored so
/// it cannot substring-capture the P/T-only sentence that rule owns.
///
/// Emits a single <see cref="CompositeEffect"/> wrapping the <see cref="ModifyPTEffect"/>
/// and one <see cref="GainAbilityEffect"/> per keyword (all sharing the same duration and
/// the same "creatures you control" target), mirroring the conjunctive-modifier gold
/// convention used on the triggered side (Rule 611 — continuous effects applied
/// simultaneously; see <c>EtbTeamPumpTriggeredRule</c> for the trigger-context sibling).
///
/// Example: "Creatures you control get +1/+1 and gain haste until end of turn." (Goblin
/// War Party's second modal option.)
/// </summary>
[SpellRule]
public sealed class MassAnthemAndGainKeywordSpellRule : ISpellRule
{
  // "creatures you control get +N/+M and gain <keyword(s)> until end of turn"
  // The keywords group is everything between "gain" and " until end of turn".
  private static readonly Regex _pattern = new(
    @"^Creatures\s+you\s+control\s+get\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+and\s+gain\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    var keywordsText = m.Groups["kws"].Value;
    var duration = UntilTimeDuration.EndOfTurn;

    var creaturesYouControl = new ObjectReference
    {
      Kind = ObjectReferenceKind.Each,
      Filter = new ObjectFilter
      {
        CardTypes = ["creature"],
        Controller = ControllerFilter.You,
      },
    };

    var modifyPT = new ModifyPTEffect
    {
      Target = creaturesYouControl,
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = duration,
    };

    // Split keywords on " and " (case-insensitive); each segment names one keyword.
    var keywordNames = Regex.Split(keywordsText.Trim(), @"\s+and\s+", RegexOptions.IgnoreCase);
    var compositeEffects = new List<Effect>(1 + keywordNames.Length) { modifyPT };
    foreach (var name in keywordNames)
    {
      var ability = BuildKeywordAbility(name.Trim());
      if (ability is null)
      {
        // Unrecognised keyword — bail so the fallback parser handles the card.
        return false;
      }
      compositeEffects.Add(new GainAbilityEffect
      {
        Target = creaturesYouControl,
        GainedAbility = ability,
        Duration = duration,
      });
    }

    effect = new CompositeEffect { Effects = compositeEffects };
    return true;
  }

  // -------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors the mapping in ModifyPTAndGainKeywordSpellRule.BuildKeywordAbility
  // and EtbTeamPumpTriggeredRule.BuildKeywordAbility; kept local so each rule
  // is self-contained and independently evolvable.
  // -------------------------------------------------------------------------
  private static StaticAbility? BuildKeywordAbility(string keyword)
  {
    return keyword.ToLowerInvariant() switch
    {
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Haste }],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Trample }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Reach,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Reach }],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Flying,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.HasKeyword(KeywordAbility.Flying), Characteristic.HasKeyword(KeywordAbility.Reach)],
            },
          },
        ],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.FirstStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = KeywordAbility.DoubleStrike,
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new LifelinkEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Vigilance }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Deathtouch }],
      },
      "menace" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Menace,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter { CardTypes = ["creature"] },
            MinimumBlockers = 2,
          },
        ],
      },
      "indestructible" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Indestructible,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Indestructible }],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Hexproof,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Hexproof }],
      },
      "shroud" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Shroud,
        Effects = [new MagicAST.AST.Effects.Keyword.KeywordAbilityEffect { Keyword = MagicAST.AST.References.KeywordAbility.Shroud }],
      },
      _ => null,
    };
  }
}
