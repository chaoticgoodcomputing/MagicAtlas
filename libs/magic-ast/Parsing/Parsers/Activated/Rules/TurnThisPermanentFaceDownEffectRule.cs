namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Turn this enchantment face down" / "Turn this creature face down" / "Turn this
/// permanent face down" — an activated ability that flips a face-up permanent to
/// face down (Obscuring Aether: "{1}{G}: Turn this enchantment face down. (It
/// becomes a 2/2 creature.)"; also the Morph-adjacent "un-morph" grant on cards
/// like Wall of Deceit: "{3}: Turn this creature face down.").
///
/// <para>
/// CR 708.1: "Some cards allow spells and permanents to be face down." CR 708.2a:
/// "If a face-up permanent is turned face down by a spell or ability that doesn't
/// list any characteristics for that object, it becomes a 2/2 face-down creature
/// with no text, no name, no subtypes, and no mana cost." The reminder-text
/// parenthetical describing the 2/2 default is stripped upstream by
/// <c>ActivatedAbilityParser.StripTrailingReminder</c> before this rule fires.
/// </para>
///
/// <para>
/// Maps to a <see cref="TurnFaceDownEffect"/> with a <see cref="ObjectReference.Self"/>
/// subject. Sibling of <see cref="TransformSelfEffectRule"/> (same self-referential
/// state-flip idiom), but a distinct game action per CR 701.27b (transforming and
/// turning face up/down "uses the same physical action" but "are different game
/// actions").
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class TurnThisPermanentFaceDownEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Turn\s+this\s+(creature|artifact|enchantment|permanent|land)\s+face\s+down$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new TurnFaceDownEffect { Target = ObjectReference.Self() };
  }
}
