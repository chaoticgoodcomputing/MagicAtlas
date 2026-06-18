namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.References;

/// <summary>
/// "that player's life total becomes [N]" — sets the triggering player's
/// life total to a fixed value.
///
/// <para>
/// Rule 119.5: "If an effect sets a player's life total to a specific number,
/// the player gains or loses the necessary amount of life to end up with the new total."
/// "That player" refers to the player identified by the trigger condition (CR 603.2);
/// it maps to <see cref="ObjectReferenceKind.ThatPlayer"/>.
/// </para>
///
/// <para>
/// Canonical use: Master of Cruelties — "Whenever this creature attacks a player
/// and isn't blocked, that player's life total becomes 1." The effect reduces (or
/// in unusual situations raises) the attacked player's life total to exactly 1.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class ThatPlayerLifeTotalBecomesRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^that\s+player'?s\s+life\s+total\s+becomes\s+(?<total>\d+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    effect = new SetLifeTotalEffect
    {
      Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
      Total = int.Parse(m.Groups["total"].Value),
    };
    return true;
  }
}
