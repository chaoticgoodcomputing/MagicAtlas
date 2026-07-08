namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Combat;
using MagicAST.AST.References;

/// <summary>
/// Recognises the pronoun-back-referenced fight shape with a colour-qualified
/// opponent's creature:
///   "It fights target green creature an opponent controls."
///   "It fights target green creature you don't control."
///
/// CR 701.14 (Fight keyword action). Distinct from <see cref="FightRule"/>'s
/// "Target creature you control fights …" form: here the caster's participant is a
/// pronoun back-reference ("It", CR 109.5 / rules of antecedent) to an object named
/// by an earlier sentence in the same spell (e.g. Hunt the Hunter's pumped creature),
/// modelled as <see cref="ObjectReferenceKind.It"/>. The opponent's creature carries a
/// colour restriction on <see cref="ObjectFilter.Colors"/> (CR 105.1).
///
/// As in <see cref="FightRule"/>, both "an opponent controls" and "you don't control"
/// produce identical AST — the Comprehensive Rules treat them as equivalent for
/// targeting, so <see cref="FightEffect.Opposed"/> is always
/// <c>Controller: Opponent</c>. Reminder text "(Each deals damage equal to its power
/// to the other.)" is stripped before spell rules run.
///
/// Examples:
/// <list type="bullet">
///   <item>"It fights target green creature an opponent controls."  (Hunt the Hunter — second sentence)</item>
/// </list>
///
/// Anchored with <c>^…$</c>; the mandatory "It fights" prefix keeps it from matching
/// the "Target creature you control fights …" surface handled by <see cref="FightRule"/>.
/// </summary>
[SpellRule]
public sealed class ItFightsColorControlledSpellRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^It\s+fights\s+target\s+(?<color>white|blue|black|red|green)\s+creature\s+(?:you\s+don't\s+control|an\s+opponent\s+controls)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly Dictionary<string, string> ColorToCode =
    new(StringComparer.OrdinalIgnoreCase)
    {
      { "white", "W" },
      { "blue", "U" },
      { "black", "B" },
      { "red", "R" },
      { "green", "G" },
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var color = ColorToCode[m.Groups["color"].Value];

    effect = new FightEffect
    {
      Controlled = new ObjectReference { Kind = ObjectReferenceKind.It },
      Opposed = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Colors = [color],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
    return true;
  }
}
