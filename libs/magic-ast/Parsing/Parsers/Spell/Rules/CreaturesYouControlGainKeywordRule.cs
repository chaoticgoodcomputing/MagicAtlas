namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// "Creatures you control gain [keyword] until end of turn." — spell-resolution mass
/// keyword grant to all creatures the caster controls (e.g. Deafening Clarion's mode
/// "Creatures you control gain lifelink until end of turn."). This is a modal spell
/// (CR 700.2) option: choosing it triggers a one-shot imperative grant (Rule 113.3a),
/// distinct from the declarative static form "Creatures you control have [keyword]."
/// The controller filter is <see cref="ControllerFilter.You"/> to record that the
/// creatures' controller is declared directly (the caster), not resolved via a
/// targeting requirement.
///
/// For lifelink specifically, CR 702.15a ("Lifelink is a static ability.") and CR 702.15b
/// ("Damage dealt by a source with lifelink causes that source's controller, or its owner
/// if it has no controller, to gain that much life (in addition to any other results that
/// damage causes). See rule 120.3.") define the granted ability's semantics; the grant
/// itself is temporary ("until end of turn"), which is what marks this clause as a spell
/// effect rather than a permanent static ability.
/// </summary>
[SpellRule(Priority = 75)]
public sealed class CreaturesYouControlGainKeywordRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Creatures\s+you\s+control\s+gain\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
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
          Controller = ControllerFilter.You,
        },
      },
      GainedAbility = gainedAbility,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
