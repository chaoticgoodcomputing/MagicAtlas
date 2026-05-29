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
/// Recognises the controller-qualified composite buff shape:
///   "Target creature you control gets +N/+M and gains &lt;keyword(s)&gt; until end of turn."
///
/// This covers protective instants like Dive Down (XLN) where the caster targets only
/// one of their own creatures to apply both a P/T modification and a keyword grant.
/// The "you control" qualifier restricts the target to permanents the active player
/// controls (CR 109.5 — controller filter on target).
///
/// Emits a flat list via <see cref="IMultiSpellRule.TryMatchMulti"/>:
/// <c>[ModifyPTEffect (Target=you-control creature), GainAbilityEffect (Target=It), …]</c>,
/// one <see cref="GainAbilityEffect"/> per keyword, all sharing a single
/// <see cref="UntilEndOfTurnDuration"/> instance.
///
/// <para>
/// The single-effect <see cref="ISpellRule.TryMatch"/> always returns false so the
/// flat-list path is the only active route.
/// </para>
///
/// <para>
/// CR citations:
/// 702.11b — "Hexproof" on a permanent means "This permanent can't be the target of
/// spells or abilities your opponents control."
/// CR 613.1 — "The values of an object's characteristics are determined by starting with
/// the actual object… Then all applicable continuous effects are applied in a series of
/// layers in the following order:" (P/T and ability-granting are continuous effects).
/// </para>
/// </summary>
[SpellRule]
public sealed class ModifyPTAndGainKeywordControlledSpellRule : ISpellRule, IMultiSpellRule
{
  // Matches: "Target creature you control gets +0/+3 and gains hexproof until end of turn"
  //          "Target creature you control gets +2/+2 and gains trample and haste until end of turn"
  // The "you control" qualifier restricts the target; "until end of turn" terminates the clause.
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+you\s+control\s+gets\s+(?<p>[+-]\d+)/(?<t>[+-]\d+)\s+and\s+gains\s+(?<kws>.+?)\s+until\s+end\s+of\s+turn$",
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

    var power = int.Parse(m.Groups["p"].Value);
    var toughness = int.Parse(m.Groups["t"].Value);
    var keywordsText = m.Groups["kws"].Value;

    var duration = new UntilEndOfTurnDuration();
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
        PowerModifier = LiteralQuantity.Of(power),
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
  // Mirrors the mapping in ModifyPTAndGainKeywordSpellRule.BuildKeywordAbility;
  // kept local so each rule is self-contained and independently evolvable.
  // -------------------------------------------------------------------------
  private static StaticAbility? BuildKeywordAbility(string keyword) =>
    keyword.ToLowerInvariant() switch
    {
      "trample" => new StaticAbility
      {
        KeywordSource = "Trample",
        Effects = [new TrampleEffect()],
      },
      "first strike" => new StaticAbility
      {
        KeywordSource = "First strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.First }],
      },
      "reach" => new StaticAbility
      {
        KeywordSource = "Reach",
        Effects = [new ReachEffect()],
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
              Characteristics = [Characteristic.Keyword(KeywordAbility.Flying), Characteristic.Keyword(KeywordAbility.Reach)],
            },
          },
        ],
      },
      "double strike" => new StaticAbility
      {
        KeywordSource = "Double strike",
        Effects = [new CombatDamageTimingEffect { Timing = CombatDamageTiming.Both }],
      },
      "haste" => new StaticAbility
      {
        KeywordSource = "Haste",
        Effects = [new HasteEffect()],
      },
      "deathtouch" => new StaticAbility
      {
        KeywordSource = "Deathtouch",
        Effects = [new DeathtouchEffect()],
      },
      "lifelink" => new StaticAbility
      {
        KeywordSource = "Lifelink",
        Effects = [new LifelinkEffect()],
      },
      "vigilance" => new StaticAbility
      {
        KeywordSource = "Vigilance",
        Effects = [new VigilanceEffect()],
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
