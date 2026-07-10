namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever a land of the chosen type [you control|an opponent controls] becomes
/// tapped" — the structured consumer side of a CR 607 linked ability whose producer
/// is an as-enters "choose [basic land type]" static replacement (CR 614.12; Roots of
/// Life). The "of the chosen type" qualifier back-references the earlier choice via
/// <see cref="ObjectFilter.ChosenCharacteristic"/> = <see cref="ChosenCharacteristicKind.BasicLandType"/>
/// rather than free text.
///
/// <para>CR 603.2: "Some trigger events use the word 'becomes' … These trigger only at
/// the time the named event happens." Reuses <see cref="TriggerEvent.BecomesTapped"/>,
/// the same event as the plain "becomes tapped" sibling <see cref="BecomesTappedConditionRule"/>.
/// This rule is tried FIRST (higher priority) so the chosen-characteristic reference is
/// captured before <see cref="BecomesTappedConditionRule"/>'s generic
/// <c>TriggeredRuleHelpers.ParseObjectFilter</c> path (which recognizes "a land ...
/// controls" but has no notion of "of the chosen type") claims the clause and silently
/// drops it.</para>
/// </summary>
[TriggerConditionRule(Priority = 990)]
public sealed class LandOfChosenTypeBecomesTappedConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^(?:When|Whenever)\s+a\s+land\s+of\s+the\s+chosen\s+type\s+"
      + @"(?<control>an\s+opponent\s+controls|you\s+control)\s+becomes\s+tapped\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    var match = _pattern.Match(triggerText);
    if (!match.Success)
    {
      return null;
    }

    var controller = match.Groups["control"].Value.TrimStart().StartsWith("an opponent", StringComparison.OrdinalIgnoreCase)
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.BecomesTapped,
      Filter = new ObjectFilter
      {
        CardTypes = ["land"],
        Controller = controller,
        ChosenCharacteristic = ChosenCharacteristicKind.BasicLandType,
      },
    };
  }
}
