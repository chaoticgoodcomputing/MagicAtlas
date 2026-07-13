namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Self] deals N damage to any target and M damage to you." — the Fireslinger
/// self-ping-with-recoil pattern (a {T} ability that pings any target while also
/// dealing damage back to its own controller).
///
/// <para>
/// A single "… and …"-joined sentence that expands to two sibling
/// <see cref="DealDamageEffect"/>s, both sourced from the ability's own object
/// (<see cref="ObjectReference.Self"/>): the first to "any target"
/// (<see cref="ObjectReferenceKind.AnyTarget"/> — a creature, player, or
/// planeswalker), the second to "you" (<see cref="ObjectReferenceKind.You"/>, the
/// controller). Emitted as a flat list via
/// <see cref="IMultiActivatedEffectRule.TryMatchMulti"/> so the two pings sit as
/// sibling effects rather than nested in a composite — mirroring the spell-side
/// <c>DealDamageToAnyTargetAndGainLifeRule</c>.
/// </para>
///
/// <para>
/// CR 120.1: "Objects can deal damage to battles, creatures, planeswalkers, and
/// players. This is generally detrimental to the object or player that receives
/// that damage. An object that deals damage is the source of that damage." Both
/// pings are non-combat damage (CR 120), so <c>IsCombat</c> is left null.
/// </para>
///
/// <para>
/// ANCHORED (<c>^…$</c>): the trailing "and M damage to you" clause is part of the
/// match, so this cannot be confused with the bare
/// <c>SelfDealsDamageToAnyTargetEffectRule</c> (which anchors on
/// "… to any target$" and thus never sees the recoil clause). The anchor also
/// prevents a future broader pattern from claiming this sentence and silently
/// dropping the recoil conjunct.
/// </para>
///
/// <para>
/// Implemented as <see cref="IMultiActivatedEffectRule"/> so the two effects sit
/// as a flat sibling pair on <c>Effects</c>. <see cref="TryMatch"/> always returns
/// null so the single-effect path never claims the sentence.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class SelfDealsDamageToAnyTargetAndDamageToYouEffectRule
  : IActivatedEffectRule,
    IMultiActivatedEffectRule
{
  // Anchored: "[Subject] deals N damage to any target and M damage to you"
  private static readonly Regex Pattern = new(
    @"^(?<subject>\S.*?)\s+deals?\s+(?<amount1>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+any\s+target\s+and\s+(?<amount2>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+you$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>Always returns null — this shape always produces two sibling effects.</remarks>
  public Effect? TryMatch(string effectText) => null;

  /// <inheritdoc/>
  public bool TryMatchMulti(string effectText, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = Pattern.Match(effectText.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var subject = m.Groups["subject"].Value;
    if (subject.Length == 0 || !char.IsUpper(subject[0]))
    {
      return false;
    }

    var amount1 = ActivatedRuleHelpers.ParseNumberWord(m.Groups["amount1"].Value) ?? 1;
    var amount2 = ActivatedRuleHelpers.ParseNumberWord(m.Groups["amount2"].Value) ?? 1;

    effects = new List<Effect>
    {
      new DealDamageEffect
      {
        Amount = LiteralQuantity.Of(amount1),
        Source = ObjectReference.Self(),
        Target = new ObjectReference { Kind = ObjectReferenceKind.AnyTarget },
      },
      new DealDamageEffect
      {
        Amount = LiteralQuantity.Of(amount2),
        Source = ObjectReference.Self(),
        Target = ObjectReference.You(),
      },
    };
    return true;
  }
}
