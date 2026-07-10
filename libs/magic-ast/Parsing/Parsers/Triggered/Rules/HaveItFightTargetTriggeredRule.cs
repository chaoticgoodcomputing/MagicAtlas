namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.References;

/// <summary>
/// Recognises the ETB-trigger "have it fight" shape:
///   "you may have it fight target creature you don't control."
///   "you may have it fight target creature an opponent controls."
///   "have it fight target creature you don't control." (non-optional variant)
///
/// The controller's participant is a pronoun back-reference ("it", CR 109.5 /
/// antecedent) to the object that triggered the ability — the creature that just
/// entered (e.g. Somberwald Stag: "When this creature enters, you may have it
/// fight target creature you don't control."). Modelled as
/// <see cref="ObjectReferenceKind.It"/>, mirroring
/// <see cref="MagicAST.Parsing.Parsers.Spell.Rules.ItFightsColorControlledSpellRule"/>.
///
/// Distinct from that spell rule in two ways: (1) surface is the causative
/// "have it fight" (an ETB trigger directs the entering creature) rather than the
/// bare "It fights"; (2) the opponent's creature carries no colour restriction.
/// As in <see cref="MagicAST.Parsing.Parsers.Spell.Rules.FightRule"/>, both
/// "you don't control" and "an opponent controls" produce identical AST — the
/// Comprehensive Rules treat them as equivalent for targeting, so
/// <see cref="FightEffect.Opposed"/> is always <c>Controller: Opponent</c>.
/// Reminder text "(Each deals damage equal to its power to the other.)" is stripped
/// before rules run. CR 701.14 (Fight keyword action).
///
/// The "you may" prefix is optional; its presence wraps the <see cref="FightEffect"/>
/// in an <see cref="OptionalEffect"/> via <see cref="EffectWrap.Optional"/> (ADR 0005
/// clause-modifier composition), the same convention the sibling ETB tutor rules use.
///
/// Anchored with <c>^…$</c>; the mandatory "have it fight" core keeps it from matching
/// the "Target creature you control fights …" or "It fights …" surfaces handled by the
/// spell-side fight rules.
/// </summary>
[TriggeredRule(Priority = 70)]
public sealed class HaveItFightTargetTriggeredRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^(?:you\s+may\s+)?have\s+it\s+fight\s+target\s+creature\s+"
    + @"(?:you\s+don't\s+control|an\s+opponent\s+controls)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var trimmed = text.Trim().TrimEnd('.').Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return false;
    }

    var isOptional = trimmed.StartsWith("you may", StringComparison.OrdinalIgnoreCase);

    var fight = new FightEffect
    {
      Controlled = new ObjectReference { Kind = ObjectReferenceKind.It },
      Opposed = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Opponent,
        },
      },
    };

    effect = EffectWrap.Optional(fight, isOptional);
    return true;
  }
}
