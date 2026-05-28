namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Damage;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "[Subject] deals N damage to each creature target player/opponent controls." —
/// spell-resolution sweeper damage to all creatures owned by a targeted player or opponent
/// (e.g. Savage Alliance: "Savage Alliance deals 1 damage to each creature target opponent
/// controls."). The "target player/opponent" marks this as a one-shot imperative spell
/// effect (Rule 113.3a) whose damage scope is determined at resolution by the targeting
/// requirement. The controller filter is <see cref="ControllerFilter.Target"/> to record
/// that the creatures' controller is the targeted player/opponent rather than a static
/// "opponent" or "you" axis.
/// </summary>
[SpellRule(Priority = 75)]
public sealed class DealDamageToEachCreatureTargetPlayerRule : ISpellRule
{
  // Matches: "[Subject] deals N damage to each creature target player/opponent controls."
  // Subject must start with an uppercase letter (it is the self-reference substituted card name).
  private static readonly Regex _pattern = new(
    @"^(?<subject>[A-Z]\S.*?)\s+deals?\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+damage\s+to\s+each\s+creature\s+target\s+(?<scope>player|opponent)\s+controls$",
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

    var amount = LiteralQuantity.Of(SpellRuleHelpers.ParseSmallWord(m.Groups["amount"].Value));

    effect = new DealDamageEffect
    {
      Amount = amount,
      Source = ObjectReference.Self(),
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Each,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Target,
        },
      },
    };
    return true;
  }
}
