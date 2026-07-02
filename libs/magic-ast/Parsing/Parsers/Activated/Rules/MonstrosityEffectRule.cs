namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "Monstrosity N" — maps the keyword-action to a <see cref="MonstrosityEffect"/>.
///
/// <para>
/// CR 701.37a: "Monstrosity N means 'If this permanent isn't monstrous, put N
/// +1/+1 counters on it and it becomes monstrous.'"
/// CR 701.37b: "Monstrous is a designation that has no rules meaning other than
/// to act as a marker that the monstrosity action and other spells and abilities
/// can identify."
/// </para>
///
/// <para>
/// The parenthetical reminder text "(If this creature isn't monstrous, put N
/// +1/+1 counters on it and it becomes monstrous.)" is stripped upstream by
/// <c>ActivatedAbilityParser.StripTrailingReminder</c> before this rule fires.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 980)]
public sealed class MonstrosityEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Monstrosity\s+(\d+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var match = Pattern.Match(trimmed);
    if (!match.Success)
    {
      return null;
    }

    var value = int.Parse(match.Groups[1].Value);
    return new MonstrosityEffect { Value = value };
  }
}
