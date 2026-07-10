namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Modification;
using MagicAST.AST.References;
using MagicAST.Parsing.Parsers.Activated;

/// <summary>
/// "Permanents you control gain [keyword] until end of turn." — spell-resolution mass
/// keyword grant to every permanent the caster controls (e.g. Simic Charm's modal option
/// "Permanents you control gain hexproof until end of turn."). This is a modal spell
/// (CR 700.2) option: choosing it triggers a one-shot imperative grant (Rule 113.3a),
/// distinct from the declarative static form "Permanents you control have [keyword]."
/// The target uses the CR 110.4a pseudo card-type "permanent" (broader than "creature")
/// with <see cref="ControllerFilter.You"/>, mirroring the narrower creature-scoped sibling
/// <see cref="CreaturesYouControlGainKeywordRule"/>.
/// </summary>
[SpellRule(Priority = 75)]
public sealed class PermanentsYouControlGainKeywordRule : ISpellRule
{
  private static readonly Regex _pattern = new(
    @"^Permanents\s+you\s+control\s+gain\s+(?<kw>[a-z]+(?:\s+(?!until|for|as\b)[a-z]+)?)\s+until\s+end\s+of\s+turn$",
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
          CardTypes = ["permanent"],
          Controller = ControllerFilter.You,
        },
      },
      GainedAbility = gainedAbility,
      Duration = UntilTimeDuration.EndOfTurn,
    };
    return true;
  }
}
