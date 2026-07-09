namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "This [type] gains shadow until end of turn." — a self-targeting activated
/// grant of the shadow keyword for the rest of the turn (Soltari Emissary
/// "{W}: This creature gains shadow until end of turn."; also Thalakos Drifters
/// and Trespasser il-Vec, whose cost is "Discard a card").
///
/// Shadow is missing from <see cref="ActivatedRuleHelpers.BuildGrantedKeywordAbility"/>,
/// so the generic self-grant branch of <see cref="GainAbilityEffectRule"/>
/// (Priority 995) returns null for it and the effect lands unstructured. This
/// dedicated, fully-anchored rule structures the interior into the same
/// <see cref="GainAbilityEffect"/> shape those siblings emit, granting shadow's
/// canonical structured expansion (mirrors <c>ShadowKeyword</c> and the printed
/// gold for Soltari Foot Soldier).
///
/// Shadow's structured form is an <see cref="EvasionEffect"/> whose
/// <see cref="EvasionEffect.CanBeBlockedBy"/> is restricted to creatures that
/// also have shadow — the mutual-evasion semantics of the keyword.
///
/// CR 702.28a: "Shadow is an evasion ability." CR 702.28b: "A creature with
/// shadow can't be blocked by creatures without shadow, and a creature without
/// shadow can't be blocked by creatures with shadow." CR 611.1: "A continuous
/// effect modifies characteristics of objects … for a fixed or indefinite
/// period." (the grant expires "until end of turn"). CR 602.1: activated
/// abilities are written "[Cost]: [Effect.]" — this rule handles the post-colon
/// effect fragment.
///
/// Anchored end-to-end (^…$) so it claims only this exact self-grant phrase and
/// never shadows another activated effect. Sits above the generic
/// <see cref="GainAbilityEffectRule"/> (Priority 995); that rule cannot claim
/// shadow anyway (helper miss), so this is collision-free.
/// </summary>
[ActivatedEffectRule(Priority = 997)]
public sealed class ThisCreatureGainsShadowEffectRule : IActivatedEffectRule
{
  // "This <type> gains shadow until end of turn" — mirrors the self-grant shape
  // of GainAbilityEffectRule ("This\s+\w+\s+gains?"), pinned to the shadow keyword.
  private static readonly Regex SelfGrantsShadowPattern = new(
    @"^This\s+\w+\s+gains?\s+shadow\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!SelfGrantsShadowPattern.IsMatch(trimmed))
    {
      return null;
    }

    return new GainAbilityEffect
    {
      Target = ObjectReference.Self(),
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Shadow,
        Effects =
        [
          new EvasionEffect
          {
            CanBeBlockedBy = new ObjectFilter
            {
              CardTypes = ["creature"],
              Characteristics = [Characteristic.HasKeyword(KeywordAbility.Shadow)],
            },
          },
        ],
      },
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
