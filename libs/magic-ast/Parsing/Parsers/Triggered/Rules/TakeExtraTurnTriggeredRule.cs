namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Take an extra turn after this one." — schedules an additional full turn for
/// the ability's controller when used as a triggered effect.
///
/// <para>
/// CR 500.7: "Some effects can give a player extra turns. They do this by adding
/// the turns directly after the specified turn." MAST records the verb and player
/// reference; the turn-ordering bookkeeping is engine territory.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class TakeExtraTurnTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^[Tt]ake\s+an\s+extra\s+turn\s+after\s+this\s+one$",
    RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new TakeExtraTurnEffect
    {
      Player = ObjectReference.You(),
    };
    return true;
  }
}
