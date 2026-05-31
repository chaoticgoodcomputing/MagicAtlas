namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice this creature unless you pay {COST}" — the upkeep-tax pattern
/// on creature cards (Rule 701.21a — Sacrifice; Rule 117.7 — unless clause).
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "At the beginning of your upkeep"
///   effect  = "sacrifice this creature unless you pay {B}"
/// </para>
///
/// <para>
/// The pronoun form "this creature" is a self-reference (Rule 109.2): the
/// creature bearing the ability is the object that would be sacrificed.
/// MAST models this as <see cref="ObjectReferenceKind.Self"/>, distinct from
/// the "sacrifice it" ETB-land pattern handled by
/// <see cref="SacrificeUnlessPayTriggeredRule"/> which uses
/// <see cref="ObjectReferenceKind.It"/>.
/// </para>
///
/// <para>
/// Produces a <see cref="SacrificeEffect"/> with Target = Self and an
/// <see cref="UnlessClause"/> whose Player is You and whose Cost is the
/// parsed mana expression.
/// </para>
///
/// <para>
/// Representative cards: Whipstitched Zombie (NEM), Wild Leotau (CON),
/// Spindrift Drake (WTH), Molting Harpy (UDS).
/// Rule citations: 701.21 (Sacrifice), 117.7 (unless clause).
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeSelfUnlessPayRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+this\s+(?:creature|permanent)\s+unless\s+you\s+pay\s+(?<cost>(?:\{[^}]+\})+)$",
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

    effect = MagicAST.AST.Effects.Core.EffectWrap.Preventable(new SacrificeEffect {
      Target = ObjectReference.Self()}, new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = manaCost,
      });
    return true;
  }
}
