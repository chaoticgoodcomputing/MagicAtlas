namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Take an extra turn after this one." — a spell-resolution effect (CR 113.3a)
/// that schedules an additional full turn for the spell's controller to be
/// taken immediately after the current turn.
///
/// <para>
/// CR 500.7 (verbatim): "Some effects can give a player extra turns. They do
/// this by adding the turns directly after the specified turn. If a player is
/// given multiple extra turns, the extra turns are added one at a time. If
/// multiple players are given extra turns, the extra turns are added one at a
/// time, in APNAP order (see rule 101.4). The most recently created turn will
/// be taken first." MAST records the verb and player reference; the
/// turn-ordering bookkeeping is engine territory (ADR 0001).
/// </para>
///
/// <para>
/// ANCHOR: pattern is anchored (^...$) to prevent partial matches inside
/// longer effect text. This is the canonical singular "an extra turn" form
/// (controller takes a turn); the sibling activated-ability form lives at
/// <see cref="Activated.Rules.TakeExtraTurnEffectRule"/> and the N-turn form
/// at <see cref="Activated.Rules.TakeNExtraTurnsEffectRule"/>.
/// </para>
/// </summary>
[SpellRule]
public sealed class TakeExtraTurnSpellRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Take\s+an\s+extra\s+turn\s+after\s+this\s+one$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text))
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
