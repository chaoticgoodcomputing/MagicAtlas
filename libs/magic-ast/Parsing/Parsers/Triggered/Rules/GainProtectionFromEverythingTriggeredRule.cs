namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Keyword;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;

/// <summary>
/// "you gain protection from everything until your next turn" — The One Ring's ETB consequent.
///
/// <para>
/// A player gains protection from everything (CR 702.16 — protection; CR 702.16e — a player can have
/// protection). Modeled as a <see cref="GainAbilityEffect"/> granting the controller (<see
/// cref="ObjectReferenceKind.You"/>) a static <see cref="KeywordAbility.Protection"/> ability whose
/// quality is <see cref="ProtectionQualityKind.Everything"/>, for <see cref="UntilTimeDuration.YourNextTurn"/>
/// (CR 611 — continuous effect with a duration). The "if you cast it" gate is a separate intervening-if
/// (<see cref="CastThisObjectCondition"/>) handled by the triggered-ability parser, not this effect.
/// </para>
///
/// <para>ANCHORED (<c>^…$</c>) on the full clause so no sibling protection-grant is mislabeled.</para>
/// </summary>
[TriggeredRule(Priority = 50)]
public sealed class GainProtectionFromEverythingTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^you\s+gain\s+protection\s+from\s+everything\s+until\s+your\s+next\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference { Kind = ObjectReferenceKind.You },
      GainedAbility = new StaticAbility
      {
        KeywordSource = KeywordAbility.Protection,
        Effects =
        [
          new ProtectionEffect
          {
            From = [new ProtectionQuality { Kind = ProtectionQualityKind.Everything }],
          },
        ],
      },
      Duration = UntilTimeDuration.YourNextTurn,
    };
    return true;
  }
}
