namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control get +N/+M [and gain &lt;keyword&gt;] until end of turn."
///
/// ETB team-pump pattern: the triggering creature buffs the whole team.
/// The keyword grant is optional — both shapes are covered by one rule:
/// <list type="bullet">
///   <item>"Creatures you control get +1/+0 until end of turn."</item>
///   <item>"Creatures you control get +1/+1 and gain haste until end of turn."</item>
/// </list>
///
/// When only P/T is present the rule emits a bare <see cref="ModifyPTEffect"/>.
/// When a keyword follows "and gain" the rule wraps both effects in a
/// <see cref="CompositeEffect"/>, matching the conjunctive-modifier gold
/// convention used on the static side (Rule 611 — continuous effects applied
/// simultaneously).
///
/// Recognised keywords (the set that appears in existing oracle corpus):
/// haste, trample, flying, first strike, double strike, reach, lifelink,
/// vigilance, deathtouch, menace, indestructible, hexproof, shroud.
/// </summary>
[TriggeredRule]
public sealed class EtbTeamPumpTriggeredRule : ITriggeredRule
{
  // "creatures you control get +N/+M until end of turn"
  // Accepts positive or negative modifiers on both axes.
  private static readonly Regex _ptOnlyPattern = new(
    @"^creatures\s+you\s+control\s+get\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "creatures you control get +N/+M and gain <keyword(s)> until end of turn"
  // The keywords group is everything between "gain" and " until end of turn".
  private static readonly Regex _ptAndGainPattern = new(
    @"^creatures\s+you\s+control\s+get\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+and\s+gain\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    // Try the simpler P/T-only shape first.
    var ptOnly = _ptOnlyPattern.Match(trimmed);
    if (ptOnly.Success)
    {
      effect = BuildModifyPT(ptOnly, duration: UntilTimeDuration.EndOfTurn);
      return true;
    }

    // Try the P/T + keyword-grant shape.
    var ptGain = _ptAndGainPattern.Match(trimmed);
    if (!ptGain.Success)
    {
      return false;
    }

    var power = int.Parse(ptGain.Groups["p"].Value);
    var toughness = int.Parse(ptGain.Groups["t"].Value);
    var keywordsText = ptGain.Groups["kws"].Value;
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

    // Split keywords on " and " (case-insensitive).
    var keywordNames = Regex.Split(keywordsText.Trim(), @"\s+and\s+", RegexOptions.IgnoreCase);
    var gainEffects = new List<Effect>(keywordNames.Length);
    foreach (var name in keywordNames)
    {
      var ability = BuildKeywordAbility(name.Trim());
      if (ability is null)
      {
        // Unrecognised keyword — bail so the fallback handles the card.
        return false;
      }
      gainEffects.Add(new GainAbilityEffect
      {
        Target = creaturesYouControl,
        GainedAbility = ability,
        Duration = duration,
      });
    }

    // Wrap P/T + keyword grants in a composite (single-sentence conjunctive modifier).
    var compositeEffects = new List<Effect>(1 + gainEffects.Count) { modifyPT };
    compositeEffects.AddRange(gainEffects);

    effect = new CompositeEffect
    {
      Effects = compositeEffects,
      IsOptional = false,
    };
    return true;
  }

  private static ModifyPTEffect BuildModifyPT(Match m, Duration duration)
  {
    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    return new ModifyPTEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.You,
        },
      },
      PowerModifier = LiteralQuantity.Of(power),
      ToughnessModifier = LiteralQuantity.Of(toughness),
      Duration = duration,
    };
  }

  // ---------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors the set in ModifyPTAndGainKeywordSpellRule.BuildKeywordAbility.
  // ---------------------------------------------------------------------------
  private static StaticAbility? BuildKeywordAbility(string keyword)
  {
    return keyword.ToLowerInvariant() switch
    {
      "haste" => new StaticAbility
      {
        KeywordSource = "Haste",
        Effects = [new HasteEffect()],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = "Trample",
        Effects = [new TrampleEffect()],
      },
      "flying" => new StaticAbility
      {
        KeywordSource = "Flying",
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
        KeywordSource = "First strike",
        Effects =
        [
          new AST.Effects.Combat.CombatDamageTimingEffect
          {
            Timing = AST.Effects.Combat.CombatDamageTiming.First,
          },
        ],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects =
        [
          new AST.Effects.Combat.CombatDamageTimingEffect
          {
            Timing = AST.Effects.Combat.CombatDamageTiming.Both,
          },
        ],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = "Reach",
        Effects = [new ReachEffect()],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new AST.Effects.Damage.LifelinkEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new VigilanceEffect()],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = "Deathtouch",
        Effects = [new AST.Effects.Keyword.DeathtouchEffect()],
      },
      "menace" => new StaticAbility
      {
        KeywordSource = "Menace",
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
        KeywordSource = "Indestructible",
        Effects = [new IndestructibleEffect()],
      },
      "hexproof" => new StaticAbility
      {
        KeywordSource = "Hexproof",
        Effects = [new HexproofEffect()],
      },
      "shroud" => new StaticAbility
      {
        KeywordSource = "Shroud",
        Effects = [new ShroudEffect()],
      },
      _ => null,
    };
  }
}
