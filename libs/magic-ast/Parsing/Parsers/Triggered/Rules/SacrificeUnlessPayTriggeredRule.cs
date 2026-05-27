namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Costs;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice it unless you pay {COST}" — the Karoo/bounce-land ETB sacrifice
/// pattern (Rule 701.21a — Sacrifice; Rule 117.7 — unless clause).
///
/// <para>
/// Oracle text split by <see cref="TriggeredAbilityParser"/>:
///   trigger = "When this land enters"
///   effect  = "sacrifice it unless you pay {1}"
/// </para>
///
/// <para>
/// Produces a <see cref="SacrificeEffect"/> with Target = It and an
/// <see cref="UnlessClause"/> whose Player is You and whose Cost is the
/// parsed mana expression. "It" refers back to the land named as the
/// trigger subject — matching the pronoun-reference convention used in
/// <see cref="SacrificeTriggeredRule"/>.
/// </para>
/// </summary>
[TriggeredRule]
public sealed class SacrificeUnlessPayTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+it\s+unless\s+you\s+pay\s+(?<cost>(?:\{[^}]+\})+)$",
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

    effect = new SacrificeEffect
    {
      Target = ObjectReference.It(),
      UnlessClause = new UnlessClause
      {
        Player = ObjectReference.You(),
        Cost = manaCost,
      },
    };
    return true;
  }
}
