namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Take an extra turn after this one." — schedules an additional full turn for
/// the ability's controller to be taken immediately after the current turn.
///
/// <para>
/// CR 500.7 (verbatim): "Some effects can give a player extra turns. They do this
/// by adding the turns directly after the specified turn." MAST records the verb
/// and player reference; the turn-ordering bookkeeping is engine territory.
/// </para>
///
/// <para>
/// ANCHOR: pattern is anchored (^...$) to prevent partial matches inside longer
/// effect text. This is the most-specific extra-turn form (controller takes a turn);
/// target-player forms are handled by extending this rule or adding a sibling rule.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 985)]
public sealed class TakeExtraTurnEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^[Tt]ake\s+an\s+extra\s+turn\s+after\s+this\s+one$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new TakeExtraTurnEffect
    {
      Player = ObjectReference.You(),
    };
  }
}
