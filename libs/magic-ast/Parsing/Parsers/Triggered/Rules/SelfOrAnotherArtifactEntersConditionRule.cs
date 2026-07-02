namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "[CardName] or another artifact you control enters" — self-by-name disjunction
/// enters trigger on artifacts. The oracle pattern "X or another artifact you control
/// enters" means the trigger fires when ANY artifact the controller controls enters —
/// including the source card itself (named in the "X" half) and any other artifact.
///
/// <para>
/// This is the artifact-type analogue of the creature disjunction handled in
/// <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> (the Blood Artist shape:
/// "this creature or another creature"). The correct filter is artifact + Controller=You,
/// with NO ExcludeSelf (the disjunction explicitly includes the source). Setting
/// ExcludeSelf = true would be rules-wrong: the card itself does trigger the ability
/// when it enters.
/// </para>
///
/// <para>
/// Example: Gonti's Aether Heart — "Whenever Gonti's Aether Heart or another artifact
/// you control enters, you get {E}{E}."
/// </para>
///
/// <para>
/// Rule 603.1: triggered abilities use "when", "whenever", or "at" to watch for
/// events. Rule 603.2: the trigger fires each time the condition is met.
/// Rule 107.14: energy counters ({E}) are a player resource.
/// </para>
///
/// <para>
/// ANCHOR: the pattern is anchored (^...$) and requires "or another artifact you
/// control" to prevent collision with the generic EntersConditionRule on simpler
/// artifact triggers. Runs at Priority 992 — above EntersConditionRule (990)
/// and AnotherSubtypeEntersConditionRule (995), below the most-specific rules.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 992)]
public sealed class SelfOrAnotherArtifactEntersConditionRule : ITriggerConditionRule
{
  // Matches "[Name words] or another artifact [you control ]enters[[ the battlefield]]"
  // Name words: any capitalized or function words (like the IsSelfByNameTrigger heuristic).
  // "or another artifact" is the disjunction marker distinguishing this shape from
  // the plain "another artifact" ExcludeSelf shape.
  private static readonly Regex _pattern = new(
    @"^.+\s+or\s+another\s+artifact\s+you\s+control\s+enters(?:\s+the\s+battlefield)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("or another artifact"))
    {
      return null;
    }

    if (!lower.Contains("enters"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter
      {
        CardTypes = ["artifact"],
        Controller = ControllerFilter.You,
        // No ExcludeSelf: the disjunction explicitly includes the source card itself.
        // The "X or another artifact" pattern means "any artifact you control, including X."
      },
    };
  }
}
