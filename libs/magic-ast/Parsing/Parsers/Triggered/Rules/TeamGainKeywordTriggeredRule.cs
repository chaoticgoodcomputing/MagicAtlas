namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Creatures you control gain &lt;keyword&gt; until end of turn." — the bare
/// team keyword-grant (no power/toughness modifier), as on the Rally/Ally
/// rallying creatures (Chasm Guide, Ondu Champion).
///
/// <para>
/// Distinct from <see cref="EtbTeamPumpTriggeredRule"/>, which requires a
/// leading "get +N/+M" P/T modifier. Here the whole resolution clause is a
/// keyword grant: the entering creature grants the named keyword to the whole
/// team for the turn. CR 611.1 — the grant is a continuous effect with a fixed
/// duration (until end of turn).
/// </para>
///
/// <para>
/// The granted keyword is structured, not free text: each keyword resolves to a
/// <see cref="GainAbilityEffect"/> whose <c>GainedAbility</c> is a
/// <see cref="StaticAbility"/> carrying the keyword (mirroring the existing
/// team-pump and modal grant golds — e.g. Flame-Kin Zealot, Savage Alliance).
/// Multiple keywords joined by "and" each become their own grant effect, wrapped
/// in a <see cref="CompositeEffect"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TeamGainKeywordTriggeredRule : ITriggeredRule
{
  // "creatures you control gain <keyword(s)> until end of turn"
  // The keywords group is everything between "gain" and " until end of turn".
  private static readonly Regex _pattern = new(
    @"^creatures\s+you\s+control\s+gain\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim();

    var match = _pattern.Match(trimmed);
    if (!match.Success)
    {
      return false;
    }

    var keywordsText = match.Groups["kws"].Value;
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

    // Split keywords on " and " (case-insensitive) so "gain trample and haste"
    // produces one grant per keyword.
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

    // Single keyword: emit the bare grant. Multiple keywords: wrap in a composite
    // (single-sentence conjunctive grant — CR 611.1 continuous effects applied
    // simultaneously).
    effect = gainEffects.Count == 1
      ? gainEffects[0]
      : new CompositeEffect { Effects = gainEffects };
    return true;
  }

  // ---------------------------------------------------------------------------
  // Keyword → StaticAbility factory.
  // Mirrors EtbTeamPumpTriggeredRule.BuildKeywordAbility so the granted-keyword
  // gold shape is identical across the team-grant family.
  // ---------------------------------------------------------------------------
  private static StaticAbility? BuildKeywordAbility(string keyword)
  {
    return keyword.ToLowerInvariant() switch
    {
      "haste" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Haste,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Haste }],
      },
      "trample" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Trample,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Trample }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Reach,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Reach }],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Vigilance,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Vigilance }],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Deathtouch,
        Effects = [new KeywordAbilityEffect { Keyword = KeywordAbility.Deathtouch }],
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
      "lifelink" => new StaticAbility
      {
        KeywordSource = KeywordAbility.Lifelink,
        Effects = [new AST.Effects.Damage.LifelinkEffect()],
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
        KeywordSource = KeywordAbility.DoubleStrike,
        Effects =
        [
          new AST.Effects.Combat.CombatDamageTimingEffect
          {
            Timing = AST.Effects.Combat.CombatDamageTiming.Both,
          },
        ],
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
      _ => null,
    };
  }
}
