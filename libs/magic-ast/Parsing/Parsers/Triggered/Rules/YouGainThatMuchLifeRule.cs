namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "you gain that much life" — lifelink-analog triggered effect where the amount
/// is derived from the damage dealt by the source in the triggering event.
/// Rule 120 (Damage): a source deals damage; the amount is "that much" referencing
/// the damage dealt in the triggering event.
/// Rule 701.20: "To gain life, a player adds the indicated amount to their life total."
/// Distinct from <see cref="YouGainLifeRule"/> (literal N life) — this rule models
/// a derived amount ("that much") backed by <see cref="DerivedKind.DamageDealt"/>.
/// </summary>
[TriggeredRule]
public sealed class YouGainThatMuchLifeRule : ITriggeredRule
{
  private static readonly Regex Pattern = new(
    @"^you\s+gain\s+that\s+much\s+life$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    if (!Pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new GainLifeEffect
    {
      Amount = new DerivedQuantity { DerivedFrom = DerivedKind.DamageDealt },
      Player = ObjectReference.You(),
    };
    return true;
  }
}
