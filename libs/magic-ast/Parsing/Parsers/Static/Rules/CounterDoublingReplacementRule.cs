namespace MagicAST.Parsing.Parsers.Static;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;

[StaticRule(Priority = 976)]
public sealed class CounterDoublingReplacementRule : IStaticRule
{
  // Pattern: "If an effect would put one or more counters on a permanent you
  // control, it puts twice that many of those counters on that permanent instead."
  private static readonly Regex _counterDoublingPattern = new(
    @"^\s*If\s+an\s+effect\s+would\s+put\s+one\s+or\s+more\s+counters\s+on\s+a\s+permanent\s+you\s+control,\s+it\s+puts\s+twice\s+that\s+many\s+of\s+those\s+counters\s+on\s+that\s+permanent\s+instead\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public IReadOnlyList<Ability>? TryParse(OracleClause clause, ClauseClassification classification)
  {
    if (!_counterDoublingPattern.IsMatch(clause.RawText))
    {
      return null;
    }

    return
    [
      new StaticAbility
      {
        Effects = [new MagicAST.AST.Effects.Replacement.ReplacementEffect
        {
          Event = new MagicAST.AST.Effects.Replacement.CounterPlacementEvent
          {
            MinimumQuantity = 1,
            AffectedObjects = new ObjectFilter
            {
              CardTypes = ["permanent"],
              Controller = ControllerFilter.You,
            },
          },
          OriginalEventOccurs = false,
          Modifier = new MagicAST.AST.Effects.Replacement.ReplacementModifier
          {
            Type = "double",
          },
        }],
      },
    ];
  }
}
