namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This [type] gains fear until end of turn." — a self-targeting activated grant
/// of the fear keyword for the rest of the turn (Hooded Kavu
/// "{B}: This creature gains fear until end of turn.").
///
/// Fear is missing from <see cref="ActivatedRuleHelpers.BuildGrantedKeywordAbility"/>,
/// so the generic self-grant branch of <see cref="GainAbilityEffectRule"/>
/// (Priority 995) returns null for it and the effect lands unstructured. This
/// dedicated, fully-anchored rule structures the interior into the same
/// <see cref="GainAbilityEffect"/> shape those siblings emit, granting fear's
/// canonical structured expansion (mirrors <c>FearKeyword</c> and the printed
/// gold for Razortooth Rats).
///
/// Fear's structured form is an <see cref="EvasionEffect"/> whose
/// <see cref="EvasionEffect.CanBeBlockedBy"/> is restricted to artifact creatures
/// and/or black creatures — the evasion semantics of the keyword (the artifact type
/// on CardTypes, the black color on Colors, as one disjunctive filter).
///
/// CR 702.36a: "Fear is an evasion ability." CR 702.36b: "A creature with fear
/// can't be blocked except by artifact creatures and/or black creatures." CR 611.1:
/// "A continuous effect modifies characteristics of objects … for a fixed or
/// indefinite period." (the grant expires "until end of turn"). CR 602.1: activated
/// abilities are written "[Cost]: [Effect.]" — this rule handles the post-colon
/// effect fragment.
///
/// Anchored end-to-end (^…$) so it claims only this exact self-grant phrase and
/// never shadows another activated effect. Sits above the generic
/// <see cref="GainAbilityEffectRule"/> (Priority 995); that rule cannot claim fear
/// anyway (helper miss), so this is collision-free.
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class ThisCreatureGainsFearEffectRule : IActivatedEffectRule
{
  // "This <type> gains fear until end of turn" — mirrors the self-grant shape
  // of GainAbilityEffectRule ("This\s+\w+\s+gains?"), pinned to the fear keyword.
  private static readonly Regex SelfGrantsFearPattern = new(
    @"^This\s+\w+\s+gains?\s+fear\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!SelfGrantsFearPattern.IsMatch(trimmed))
    {
      return null;
    }

    return new GainAbilityEffect
    {
      Target = ObjectReference.Self(),
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Fear,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature", "artifact"],
              Colors = ["B"],
            },
          },
        ],
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
