namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// "Creatures target player/opponent controls gain [keyword] until end of turn." —
/// spell-resolution mass keyword grant to all creatures controlled by a targeted player
/// or opponent (e.g. Savage Alliance: "Creatures target player controls gain trample
/// until end of turn."). The "target player/opponent" marks this as a one-shot imperative
/// spell effect (Rule 113.3a) with a runtime-chosen controller axis, distinct from the
/// declarative static form "Creatures you control have [keyword]." The controller filter
/// is <see cref="ControllerFilter.Target"/> to record that the creatures' controller is
/// determined at resolution from the targeting requirement.
/// </summary>
[SpellRule(Priority = 75)]
public sealed class CreaturesTargetPlayerGainKeywordRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Creatures\s+target\s+(?<scope>player|opponent)\s+controls\s+gain\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var keyword = m.Groups["kw"].Value.ToLowerInvariant().Trim();
    var gainedAbility = ActivatedRuleHelpers.BuildGrantedKeywordAbility(keyword);
    if (gainedAbility is null)
    {
      return false;
    }

    effect = new GainAbilityEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Target,
        },
      },
      GainedAbility = gainedAbility,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
