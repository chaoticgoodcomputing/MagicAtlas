namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// Matches "transform this creature" / "transform this permanent" effect text.
///
/// Classic Innistrad werewolves carry explicit transform triggered abilities,
/// e.g.:
/// <list type="bullet">
///   <item><description>
///     "At the beginning of each upkeep, if a player cast two or more spells
///     last turn, transform this creature." (human-to-wolf)
///   </description></item>
///   <item><description>
///     "At the beginning of each upkeep, if no spells were cast last turn,
///     transform this creature." (wolf-to-human)
///   </description></item>
/// </list>
///
/// The "if [condition]" intervening-if clause is stripped by
/// <see cref="MagicAST.Parsing.Parsers.TriggeredAbilityParser"/> before this
/// rule receives the effect fragment. This rule handles the remaining
/// "transform this creature" / "transform this permanent" text.
/// </summary>
[TriggeredRule]
public sealed class TransformTriggeredRule : ITriggeredRule
{
  // Matches: "transform this creature" / "transform this permanent" (Rule 701.28)
  private static readonly Regex _pattern = new(
    @"^transform\s+this\s+(creature|permanent)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
    {
      return false;
    }

    effect = new TransformEffect
    {
      Target = ObjectReference.Self(),
    };
    return true;
  }
}
