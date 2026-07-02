namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;
using MagicAST.Parsing;

[StaticRule(Priority = 946)]
public sealed class CastAsFlashWithConsequenceRule : IStaticRule
{
  // Matches the two-sentence Armor-of-Thorns oracle text:
  //   "You may cast this spell as though it had flash. If you cast it any time
  //    a sorcery couldn't have been cast, the controller of the permanent it
  //    becomes sacrifices it at the beginning of the next cleanup step."
  // The match is anchored at both ends. Minor formatting variants (optional
  // trailing period, apostrophe vs. right-quote for "couldn't") are handled by
  // the character class on the contraction.
  private static readonly Regex _castAsFlashWithConsequencePattern = new(
    @"^\s*You\s+may\s+cast\s+this\s+spell\s+as\s+though\s+it\s+had\s+flash\.\s+"
    + @"If\s+you\s+cast\s+it\s+any\s+time\s+a\s+sorcery\s+couldn'?t\s+have\s+been\s+cast,\s+"
    + @"the\s+controller\s+of\s+the\s+permanent\s+it\s+becomes\s+sacrifices\s+it\s+"
    + @"at\s+the\s+beginning\s+of\s+the\s+next\s+cleanup\s+step\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_castAsFlashWithConsequencePattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [MagicAST.AST.Effects.Core.EffectWrap.Optional(new TimingModificationEffect {
          Modification = TimingModificationType.Grant,
          Timing = TimingWindow.Instant,
          Consequence = new MagicAST.AST.Effects.Core.CreateDelayedTriggerEffect
          {
            DelayedTrigger = new MagicAST.AST.Abilities.DelayedTriggeredAbility
            {
              Trigger = new MagicAST.AST.Triggers.TriggerCondition
              {
                Timing = MagicAST.AST.Triggers.TriggerTiming.At,
                Event = new MagicAST.AST.References.GameTime
                {
                  Part = MagicAST.AST.References.TurnPart.Cleanup,
                  Edge = MagicAST.AST.References.TimeBoundary.Beginning,
                  When = MagicAST.AST.References.TimeRelation.Next,
                },
              },
              Effects = [new MagicAST.AST.Effects.ZoneChange.SacrificeEffect { Target = ObjectReference.Self() }],
            },
          }}, true)],
      },
    ];
  }
}
