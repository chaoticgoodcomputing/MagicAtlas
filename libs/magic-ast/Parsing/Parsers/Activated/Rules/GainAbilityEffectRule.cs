namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "gains [keyword]" grants on a target. Supported shapes:
/// - "This [type] gains [keyword] [until end of turn]" (Self)
/// - "Target creature gains [keyword] until end of turn"
/// - "Creatures you control gain [keyword]" (Each, creatures you control)
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
          selfDuration = new UntilEndOfTurnDuration();
        }
        else if (lower.Contains("until your next turn"))
        {
          selfDuration = new UntilYourNextTurnDuration();
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
          Duration = new UntilEndOfTurnDuration(),
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
      duration = new UntilEndOfTurnDuration();
    }
    else if (lower.Contains("until your next turn"))
    {
      duration = new UntilYourNextTurnDuration();
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
