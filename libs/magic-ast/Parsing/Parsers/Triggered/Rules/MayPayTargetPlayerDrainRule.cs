namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;

/// <summary>
/// "you may pay {COST}. If you do, target player loses N life and you gain N life."
/// — the optional-drain conditional-pay shape (Kalastria Highborn, WWK:
/// "Whenever this creature or another Vampire you control dies, you may pay {B}.
/// If you do, target player loses 2 life and you gain 2 life.").
///
/// <para>CR 118.12: a spell or ability that reads "[A player] may [do something].
/// If [that player] does, [effect]" treats the "do something" (here, paying
/// {COST}) as a cost paid on resolution; the consequent runs only if the payment
/// was made. This is the <see cref="ConditionalPayEffect"/> shape wrapped in an
/// <see cref="OptionalEffect"/>, mirroring <see cref="ConditionalPayTriggeredRule"/>
/// but with the Blood-Artist drain composite ("target player loses N life and you
/// gain N life") as the "if you do" consequent. CR 603.1: the whole ability is a
/// triggered ability whose effect is this composite.</para>
///
/// <para>The consequent is delegated to the existing anchored
/// <see cref="TargetPlayerLoseAndYouGainLifeRule"/>, which emits the
/// <see cref="CompositeEffect"/> of <see cref="LoseLifeEffect"/> (target player) +
/// <see cref="GainLifeEffect"/> (you). Priority 90 (above
/// <see cref="ConditionalPayTriggeredRule"/> at 80) so this specific drain shape
/// wins cleanly; the regex is fully anchored so no other conditional-pay text
/// matches.</para>
/// </summary>
[TriggeredRule(Priority = 90)]
public sealed class MayPayTargetPlayerDrainRule : ITriggeredRule
{
  private static readonly TargetPlayerLoseAndYouGainLifeRule _drainRule = new();

  private static readonly Regex _pattern = new(
    @"^you\s+may\s+pay\s+(?<cost>(?:\{[^}]+\})+)\s*\.\s*If\s+you\s+do,\s*(?<rest>.+?)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text);
    if (!m.Success)
    {
      return false;
    }

    var manaCost = TriggeredRuleHelpers.TryBuildManaCost(m.Groups["cost"].Value);
    if (manaCost is null)
    {
      return false;
    }

    var rest = m.Groups["rest"].Value.TrimEnd('.').Trim();
    if (!_drainRule.TryMatch(rest, out var drain) || drain is null)
    {
      return false;
    }

    effect = EffectWrap.Optional(
      new ConditionalPayEffect { Cost = manaCost },
      isOptional: true,
      ifYouDo: drain
    );
    return true;
  }
}
