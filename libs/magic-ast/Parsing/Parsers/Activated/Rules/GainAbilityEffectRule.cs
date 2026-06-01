namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "gains [keyword]" grants on a target. Supported shapes:
/// - "This [type] gains [keyword] [until end of turn]" (Self)
/// - "Target creature gains [keyword] until end of turn"
/// - "Target [Subtype] gains [keyword] until end of turn" (subtype-filtered target,
///   e.g. Olivia's Bloodsworn "Target Vampire gains haste until end of turn")
/// - "Creatures you control gain [keyword]" (Each, creatures you control)
///
/// The grant is a continuous effect that modifies the target's characteristics for
/// the stated duration (CR 611.1: "A continuous effect modifies characteristics of
/// objects … for a fixed or indefinite period."). The granted keyword is itself a
/// static ability (e.g. CR 702.10a: "Haste is a static ability.").
/// </summary>
[ActivatedEffectRule(Priority = 995)]
public sealed class GainAbilityEffectRule : IActivatedEffectRule
{
  public Effect? TryMatch(string effectText)
  {
    effectText = effectText.Trim().TrimEnd('.');
    var lower = effectText.ToLowerInvariant();

    if (!lower.Contains("gain"))
    {
      return null;
    }

    // Pattern: "This [type] gains [keyword]" — self-targeting. Captures 1–2 word
    // keywords using a negative-lookahead to avoid swallowing "until"/"for"/"as".
    var selfMatch = Regex.Match(
      effectText,
      @"^This\s+\w+\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)",
      RegexOptions.IgnoreCase
    );
    if (selfMatch.Success)
    {
      var selfKeyword = selfMatch.Groups["kw"].Value.ToLowerInvariant().Trim();
      var selfAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(selfKeyword);
      if (selfAbility is not null)
      {
        Duration? selfDuration = null;
        if (lower.Contains("until end of turn"))
        {
          selfDuration = UntilTimeDuration.EndOfTurn;
        }
        else if (lower.Contains("until your next turn"))
        {
          selfDuration = UntilTimeDuration.YourNextTurn;
        }
        return new GainAbilityEffect
        {
          Target = ObjectReference.Self(),
          GainedAbility = selfAbility,
          Duration = selfDuration,
        };
      }
    }

    // Pattern: "Target creature [you control] gains [keyword] until end of turn".
    // The optional "you control" group narrows the target's controller axis
    // (ObjectFilter.Controller = You). Without it the regex would reject
    // "Target creature you control gains …" and fall through to unparsed.
    var targetCreatureGainsMatch = Regex.Match(
      effectText,
      @"^Target\s+creature(?<youcontrol>\s+you\s+control)?\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
      RegexOptions.IgnoreCase
    );
    if (targetCreatureGainsMatch.Success)
    {
      var targetKeyword = targetCreatureGainsMatch.Groups["kw"].Value.ToLowerInvariant().Trim();
      var targetAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(targetKeyword);
      if (targetAbility is not null)
      {
        var targetFilter = targetCreatureGainsMatch.Groups["youcontrol"].Success
          ? new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You }
          : new ObjectFilter { CardTypes = ["creature"] };
        return new GainAbilityEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = targetFilter,
          },
          GainedAbility = targetAbility,
          Duration = UntilTimeDuration.EndOfTurn,
        };
      }
    }

    // Pattern: "Target [Subtype] gains [keyword] until end of turn" — the target is
    // narrowed by a creature subtype rather than the bare "creature" card type
    // (Olivia's Bloodsworn: "Target Vampire gains haste until end of turn"). Subtypes
    // are capitalized in oracle text (CR 205.3), so the subtype token is captured by
    // its leading capital and emitted on ObjectFilter.Subtypes — mirroring the
    // "Destroy target Spirit" → Filter {Subtypes:["Spirit"]} convention. Placed after
    // the "Target creature" branch so the literal card type still routes to CardTypes.
    var targetSubtypeGainsMatch = Regex.Match(
      effectText,
      @"^Target\s+(?<subtype>[A-Z][a-z]+)\s+gains?\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
      RegexOptions.None
    );
    if (targetSubtypeGainsMatch.Success)
    {
      var subtypeKeyword = targetSubtypeGainsMatch.Groups["kw"].Value.ToLowerInvariant().Trim();
      var subtypeAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(subtypeKeyword);
      if (subtypeAbility is not null)
      {
        return new GainAbilityEffect
        {
          Target = new ObjectReference
          {
            Kind = ObjectReferenceKind.Target,
            Filter = new ObjectFilter { Subtypes = [targetSubtypeGainsMatch.Groups["subtype"].Value] },
          },
          GainedAbility = subtypeAbility,
          Duration = UntilTimeDuration.EndOfTurn,
        };
      }
    }

    // Pattern: "Creatures you control gain [ability]"
    var match = Regex.Match(
      effectText,
      @"Creatures you control gain (\w+)",
      RegexOptions.IgnoreCase
    );
    if (!match.Success)
    {
      return null;
    }

    var keyword = match.Groups[1].Value;
    var gainedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keyword);
    if (gainedAbility is null)
    {
      return null;
    }

    Duration? duration = null;
    if (lower.Contains("until end of turn"))
    {
      duration = UntilTimeDuration.EndOfTurn;
    }
    else if (lower.Contains("until your next turn"))
    {
      duration = UntilTimeDuration.YourNextTurn;
    }

    return new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
      },
      GainedAbility = gainedAbility,
      Duration = duration,
    };
  }
}
