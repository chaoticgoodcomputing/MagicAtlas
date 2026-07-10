namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature gains [basic landwalk] until end of turn." — a targeted
/// activated grant of a basic-land landwalk keyword for the rest of the turn
/// (Ivy Dancer "{T}: Target creature gains forestwalk until end of turn.").
///
/// The basic landwalk keywords (forestwalk / islandwalk / swampwalk / mountainwalk
/// / plainswalk) are absent from
/// <see cref="ActivatedRuleHelpers.BuildGrantedKeywordAbility"/>, so the generic
/// "Target creature gains …" branch of <see cref="GainAbilityEffectRule"/>
/// (Priority 995) returns null for them and the effect lands unstructured (L1).
/// This dedicated, fully-anchored rule structures the interior into the same
/// <see cref="GainAbilityEffect"/> shape those siblings emit — the target being an
/// <see cref="ObjectReferenceKind.Target"/> creature — granting the landwalk
/// keyword's canonical structured expansion: an <see cref="EvasionEffect"/> whose
/// <see cref="EvasionEffect.UnblockableCondition"/> is
/// <see cref="EvasionConditionType.DefendingPlayerControls"/> over the matching
/// basic land subtype. This is the target-scoped dual of
/// <see cref="ThisCreatureGainsLandwalkEffectRule"/> (the self-grant shape).
///
/// CR 702.14 (Landwalk): "Landwalk is an evasion ability." and "A creature with
/// landwalk can't be blocked as long as the defending player controls at least one
/// land with the specified land type (as in 'islandwalk')…". CR 611.1: "A continuous
/// effect modifies characteristics of objects … for a fixed or indefinite period."
/// (the grant expires "until end of turn"). CR 602.1: activated abilities are written
/// "[Cost]: [Effect.]" — this rule handles the post-colon effect fragment.
///
/// Anchored end-to-end (^…$) and restricted to the five basic-land landwalk words,
/// so it claims only this exact target-grant phrase and never shadows another
/// activated effect. Sits above the generic <see cref="GainAbilityEffectRule"/>
/// (Priority 995); that rule cannot claim these keywords anyway (helper miss), so
/// this is collision-free.
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class TargetCreatureGainsLandwalkEffectRule : IActivatedEffectRule
{
  // Maps each basic-land landwalk word to its keyword and the basic-land subtype
  // whose control by the defending player suppresses blocking (CR 702.14c).
  private static readonly Dictionary<string, (KeywordAbility Keyword, string LandSubtype)> Landwalks = new()
  {
    ["forestwalk"] = (KeywordAbility.Forestwalk, "Forest"),
    ["islandwalk"] = (KeywordAbility.Islandwalk, "Island"),
    ["swampwalk"] = (KeywordAbility.Swampwalk, "Swamp"),
    ["mountainwalk"] = (KeywordAbility.Mountainwalk, "Mountain"),
    ["plainswalk"] = (KeywordAbility.Plainswalk, "Plains"),
  };

  // "Target creature gains <basic landwalk> until end of turn" — mirrors the target
  // shape of GainAbilityEffectRule ("Target creature gains?"), pinned to the landwalk words.
  private static readonly Regex TargetCreatureGainsLandwalkPattern = new(
    @"^Target\s+creature\s+gains?\s+(?<walk>forestwalk|islandwalk|swampwalk|mountainwalk|plainswalk)\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = TargetCreatureGainsLandwalkPattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var (keyword, landSubtype) = Landwalks[match.Groups["walk"].Value.ToLowerInvariant()];

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter { CardTypes = ["creature"] },
      },
      GainedAbility = new StaticAbility
      {
        KeywordSource = keyword,
        Effects =
        [
          new EvasionEffect
          {
            UnblockableCondition = new EvasionCondition
            {
              ConditionType = EvasionConditionType.DefendingPlayerControls,
              PermanentFilter = new ObjectFilter { Subtypes = [landSubtype] },
            },
          },
        ],
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
