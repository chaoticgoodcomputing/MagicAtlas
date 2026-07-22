namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Turn target face-down creature face up." — an activated ability that flips a
/// targeted face-down creature to face up (Ixidor, Reality Sculptor:
/// "{2}{U}: Turn target face-down creature face up.").
///
/// <para>
/// CR 708.4: turning a permanent face up reveals its face-up characteristics. The
/// "face-down creature" qualifier is the target restriction (CR 708.2 — a face-down
/// permanent is a creature), recorded as an <c>IsFaceDown</c> filter on the target
/// reference — the same axis the passive "Face-down creatures get +1/+1" anthem on
/// the same card already uses.
/// </para>
///
/// <para>
/// Maps to a <see cref="TurnFaceUpEffect"/>; the mirror direction of
/// <see cref="TurnThisPermanentFaceDownEffectRule"/>. Distinct from the Morph /
/// Disguise keyword grants, which supply their own cost-paying "turn face up"
/// action rather than a standalone targeted instruction.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 970)]
public sealed class TurnTargetFaceDownCreatureFaceUpEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^Turn\s+target\s+face-down\s+creature\s+face\s+up$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new TurnFaceUpEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          IsFaceDown = true,
        },
      },
    };
  }
}
