namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Exile target [state] creature" and "Exile target creature with power N or less/greater."
/// Covers:
/// <list type="bullet">
///   <item>State qualifiers: "Exile target tapped creature", "Exile target attacking creature"</item>
///   <item>Power filter (less-or-equal): "Exile target creature with power 3 or less"</item>
///   <item>Power filter (greater-or-equal): "Exile target creature with power 4 or greater"</item>
/// </list>
/// Priority 70 — runs before <see cref="ExileTargetSimpleRule"/> (50), which would otherwise
/// claim the raw text via its FilterPattern fallback and then fail to produce a filter for the
/// "with power N" or state-adjective shapes.
/// Rule 701.13 (exile action). Power comparisons: Rule 208.1 (power is a characteristic of
/// creature cards).
/// </summary>
[SpellRule(Priority = 70)]
public sealed class ExileTargetQualifiedRule : ISpellRule
{
  // "Exile target [tapped|attacking|...] creature"
  private static readonly Regex StatePattern = new(
    @"^Exile\s+target\s+(?<state>tapped|untapped|attacking|blocking|face-up|face-down)\s+creature$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  // "Exile target creature with power N or less/greater"
  private static readonly Regex PowerPattern = new(
    @"^Exile\s+target\s+creature\s+with\s+power\s+(?<n>\d+)\s+or\s+(?<dir>less|greater)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    // State-qualifier branch: "Exile target tapped creature"
    var state = StatePattern.Match(text);
    if (state.Success)
    {
      effect = new ExileEffect
      {
        Target = new ObjectReference
        {
          Kind = ObjectReferenceKind.Target,
          Filter = new ObjectFilter
          {
            CardTypes = ["creature"],
            Characteristics = [state.Groups["state"].Value.ToLowerInvariant()],
          },
        },
      };
      return true;
    }

    // Power-filter branch: "Exile target creature with power N or less/greater"
    var power = PowerPattern.Match(text);
    if (!power.Success)
    {
      return false;
    }

    var n = int.Parse(power.Groups["n"].Value);
    var dir = power.Groups["dir"].Value.ToLowerInvariant();
    var op = dir == "less" ? ComparisonOperator.LessThanOrEqual : ComparisonOperator.GreaterThanOrEqual;

    effect = new ExileEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          PowerComparison = new Comparison { Operator = op, Value = n },
        },
      },
    };
    return true;
  }
}
