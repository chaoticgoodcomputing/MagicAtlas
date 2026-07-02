namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "Target creature with power N or less/greater gains [keyword] until end of
/// turn." — activated ability effect that grants a keyword ability (CR 702) to
/// a target creature constrained by a power comparison filter, for the stated
/// duration (Mosstodon: "Target creature with power 5 or greater gains trample
/// until end of turn").
///
/// CR 602.1: "Activated abilities have a cost and an effect. They are written
/// as \"[Cost]: [Effect.] [Activation instructions (if any).]\"…" — governs the
/// "{1}: [Effect]" shape this rule's effect text is drawn from.
///
/// CR 611.1: "A continuous effect modifies characteristics of objects … for a
/// fixed or indefinite period." The grant is such a continuous effect, expiring
/// "until end of turn".
///
/// CR 702.19a: "Trample is a static ability that modifies the rules for
/// assigning an attacking creature's combat damage. The ability has no effect
/// when a creature with trample is blocking or is dealing noncombat damage."
/// Trample (and any other keyword <see cref="ActivatedRuleHelpers.BuildGrantedKeywordAbility"/>
/// already resolves) is granted as a <c>static</c> ability whose source is the
/// keyword itself (CR 702: keyword abilities are shorthand for static,
/// triggered, or activated abilities spelled out in full elsewhere in the
/// rules).
///
/// Power-filter parsing mirrors
/// <see cref="TargetCantBeBlockedWithPowerFilterThisTurnEffectRule"/>:
/// "power N or greater" maps to <see cref="ComparisonOperator.GreaterThanOrEqual"/>,
/// "power N or less" maps to <see cref="ComparisonOperator.LessThanOrEqual"/>.
///
/// Sits above (higher priority than) the generic, unfiltered
/// <see cref="GainAbilityEffectRule"/> (Priority 995) — collision-free, since
/// that rule's "Target creature [you control] gains …" branch requires
/// "gains"/"gain" to immediately follow "creature" (optionally "you control")
/// and cannot claim this "with power N or greater" phrase.
/// </summary>
[ActivatedEffectRule(Priority = 996)]
public sealed class TargetCreatureWithPowerFilterGainsKeywordEffectRule : IActivatedEffectRule
{
  // Anchored end-to-end so this rule only claims the specific power-filtered
  // "gains [keyword] until end of turn" phrase and never shadows other
  // activated effects.
  private static readonly Regex PowerFilterPattern = new(
    @"^Target\s+creature\s+with\s+power\s+(?<n>\d+)\s+or\s+(?<dir>less|greater)\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');

    var match = PowerFilterPattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var keyword = match.Groups["kw"].Value.ToLowerInvariant().Trim();
    var grantedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keyword);
    if (grantedAbility is null)
    {
      return null;
    }

    var value = int.Parse(match.Groups["n"].Value);
    var op = match.Groups["dir"].Value.Equals("greater", System.StringComparison.OrdinalIgnoreCase)
      ? ComparisonOperator.GreaterThanOrEqual
      : ComparisonOperator.LessThanOrEqual;

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          PowerComparison = new Comparison { Operator = op, Value = value },
        },
      },
      GainedAbility = grantedAbility,
      Duration = UntilTimeDuration.EndOfTurn,
    };
  }
}
