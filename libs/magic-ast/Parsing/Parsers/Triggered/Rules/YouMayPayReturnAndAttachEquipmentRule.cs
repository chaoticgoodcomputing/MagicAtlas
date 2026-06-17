namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "you may pay {COST}. If you do, return that card to the battlefield and attach this
/// Equipment to it." — the conditional-pay reanimation-and-attach pattern.
///
/// <para>
/// This is the canonical Nim Deathmantle (SOM) shape: a player may pay a mana cost;
/// if they do, the triggering card (a nontoken creature that just died) is returned to
/// the battlefield AND this Equipment is attached to it. The two consequent actions are
/// a composite one-shot: return + attach are sequential but inseparable per the oracle
/// sentence structure.
/// </para>
///
/// <para>
/// AST structure:
/// <c>OptionalEffect { Inner: ConditionalPayEffect { Cost }, IfYouDo: CompositeEffect [
///   ReturnToBattlefieldEffect { Target: It },
///   AttachEffect { Target: It }
/// ] }</c>
///
/// The "that card" pronoun in "return that card to the battlefield" is the creature that
/// triggered the ability — encoded as <see cref="ObjectReferenceKind.It"/> (CR 113.8b:
/// "it" in an ability refers to the object the trigger is about). "attach this Equipment
/// to it" refers to the same returned creature, also encoded as
/// <see cref="ObjectReferenceKind.It"/>.
/// </para>
///
/// <para>
/// Priority 81 — higher than <see cref="ConditionalPayTriggeredRule"/> (priority 80) so
/// this more-specific rule fires first and the generic handler does not attempt to
/// dispatch the "return and attach" consequence through its limited if-you-do vocabulary.
/// </para>
///
/// <para>
/// CR 117.7 ("you may"); CR 701.3 (attach); CR 400.6 (enters under owner's control).
/// CR 113.8b ("it" — the pronoun back-reference).
/// </para>
/// </summary>
[TriggeredRule(Priority = 81)]
public sealed class YouMayPayReturnAndAttachEquipmentRule : ITriggeredRule
{
  // "you may pay {COST}. If you do, return that card to the battlefield and attach this Equipment to it"
  // The COST is one or more {X} mana symbols.
  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\.\s*If\s+you\s+do,\s*return\s+that\s+card\s+to\s+the\s+battlefield\s+and\s+attach\s+this\s+[A-Z][a-zA-Z]*\s+to\s+it$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var costStr = match.Groups["cost"].Value;
    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(costStr);
    if (manaCost is null)
    {
      return false;
    }

    // The composite "if you do" consequence: return the triggering card to the battlefield,
    // then attach this Equipment to it. Both reference "it" — the creature that died (CR 113.8b).
    var it = ObjectReference.It();
    var ifYouDo = new CompositeEffect
    {
      Effects =
      [
        new ReturnToBattlefieldEffect
        {
          Target = it,
        },
        new AttachEffect
        {
          Target = it,
        },
      ],
    };

    effect = EffectWrap.Optional(
      new ConditionalPayEffect { Cost = manaCost },
      isOptional: true,
      ifYouDo: ifYouDo
    );
    return true;
  }
}
