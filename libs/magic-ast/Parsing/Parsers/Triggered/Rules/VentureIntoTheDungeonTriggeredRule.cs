namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;

/// <summary>
/// "venture into the dungeon" — the Venture into the Dungeon keyword action
/// (CR 701.49) on the triggered side (e.g. Clattering Skeletons:
/// "When this creature dies, venture into the dungeon.").
///
/// <para>
/// The dispatcher (<see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/>)
/// has already split off the trigger condition, stripped the trailing reminder
/// parenthetical (Rule 207.2), and trimmed the trailing period, so the effect
/// fragment reaching this rule is the bare imperative "venture into the dungeon".
/// Anchored to that exact phrase so it never matches the "venture into [quality]"
/// variant (CR 701.49d) as a substring.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class VentureIntoTheDungeonTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^venture into the dungeon$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var trimmed = text.Trim().TrimEnd('.').Trim();
    if (!_pattern.IsMatch(trimmed))
    {
      return false;
    }

    effect = new VentureIntoTheDungeonEffect();
    return true;
  }
}
