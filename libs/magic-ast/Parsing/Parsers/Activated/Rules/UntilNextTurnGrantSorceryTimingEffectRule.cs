namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Until your next turn, you may cast sorcery spells as though they had flash." —
/// Teferi, Time Raveler's +1 loyalty ability. This is a duration-bounded flash grant
/// that allows the controller to cast sorcery spells at instant speed (as though they
/// had flash, per CR 702.8a) until the beginning of their next turn.
///
/// <para>
/// CR 702.8a: "'Flash' means 'You may play this card any time you could cast an
/// instant.'" The grant here is scoped to sorcery-type spells only (the controller
/// may cast them as though they had flash) and bounded to "until your next turn" —
/// the beginning of the controller's next turn (CR 501.1: your upkeep step begins
/// your turn, so "your next turn" ends at the start of your upkeep).
/// </para>
///
/// <para>
/// Modelled as a <see cref="TimingModificationEffect"/> with
/// <c>Modification = Grant</c>, <c>Timing = Instant</c>, an <c>AppliesTo</c>
/// <see cref="SpellReference"/> restricted to sorceries you control, and a
/// <c>Duration = UntilTimeDuration.YourNextTurn</c>.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 986)]
public sealed class UntilNextTurnGrantSorceryTimingEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^\s*Until\s+your\s+next\s+turn,\s+you\s+may\s+cast\s+sorcery\s+spells?\s+as\s+though\s+they\s+had\s+flash\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText))
    {
      return null;
    }

    return new TimingModificationEffect
    {
      Modification = TimingModificationType.Grant,
      Timing = TimingWindow.Instant,
      AppliesTo = new SpellReference
      {
        Filter = new ObjectFilter
        {
          CardTypes = ["sorcery"],
          Controller = ControllerFilter.You,
        },
      },
      Duration = UntilTimeDuration.YourNextTurn,
    };
  }
}
