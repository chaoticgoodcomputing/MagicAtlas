namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 962)]
public sealed class EntersTappedRule : IStaticRule
{
  private static readonly Regex _entersTappedPattern = new(
    @"^\s*This\s+(?:permanent|land|creature|artifact|enchantment|spell)\s+enters\s+tapped"
    + @"(?:\s+unless\s+(?<condition>[^.]+?))?\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _entersTappedIfConditionPattern = new(
    @"^\s*If\s+(?<condition>[^,]+),\s+this\s+(?:permanent|land|creature|artifact|enchantment|spell)\s+enters\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Regex _entersTappedOpponentsCreaturesPattern = new(
    @"^\s*Creatures\s+your\s+opponents\s+control\s+enter\s+tapped\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    // "Enters tapped" is the composite "as this enters, tap it": the timing
    // (StaticTimingKind.AsThisEnters) lives on the StaticAbility, the action is a
    // plain TapEffect targeting the permanent itself (CR 603.6d / 614.1c).

    // Arm 1: "This [permanent] enters tapped [unless condition]." (fastland/checkland)
    var match = _entersTappedPattern.Match(clause.RawText);
    if (match.Success)
    {
      var conditionGroup = match.Groups["condition"];
      // The "unless [condition]" clause names when the permanent enters UNtapped;
      // it gates the as-enters tap and so lives on StaticAbility.Condition.
      Condition? entryCondition = conditionGroup.Success
        ? MagicAST.Parsing.ConditionParser.Parse(conditionGroup.Value.Trim())
        : null;

      return
      [
        new StaticAbility
        {
          When = StaticTimingKind.AsThisEnters,
          Effects = [new MagicAST.AST.Effects.Control.TapEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          }],
          Condition = entryCondition,
        },
      ];
    }

    // Arm 2: "If [condition], this [permanent] enters tapped." (slow land)
    // The condition is the positive "if" gate (the land enters tapped WHEN it
    // holds); it is carried on StaticAbility.Condition. The predicate wording
    // ("two or more …") distinguishes it from the fastland "unless" form
    // ("two or fewer …") in the corpus.
    var ifMatch = _entersTappedIfConditionPattern.Match(clause.RawText);
    if (ifMatch.Success)
    {
      var conditionText = ifMatch.Groups["condition"].Value.Trim();
      return
      [
        new StaticAbility
        {
          When = StaticTimingKind.AsThisEnters,
          Effects = [new MagicAST.AST.Effects.Control.TapEffect
          {
            Target = new ObjectReference { Kind = ObjectReferenceKind.Self },
          }],
          Condition = MagicAST.Parsing.ConditionParser.Parse(conditionText),
        },
      ];
    }

    // Arm 3: "Creatures your opponents control enter tapped." — Blind Obedience
    // creature variant. The replacement applies to a class of OTHER permanents
    // (creatures opponents control), not the source itself (CR 614.1d), so the
    // composite taps each such creature (Target.Kind = Each + filter) as it
    // enters (When = AsObjectEnters, distinct from the self-entry AsThisEnters).
    if (_entersTappedOpponentsCreaturesPattern.IsMatch(clause.RawText))
    {
      return
      [
        new StaticAbility
        {
          When = StaticTimingKind.AsObjectEnters,
          Effects = [new MagicAST.AST.Effects.Control.TapEffect
          {
            Target = new ObjectReference
            {
              Kind = ObjectReferenceKind.Each,
              Filter = new ObjectFilter
              {
                CardTypes = ["creature"],
                Controller = ControllerFilter.Opponent,
              },
            },
          }],
        },
      ];
    }

    return null;
  }
}
