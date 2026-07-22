namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "sacrifice this [Aura|permanent|creature] unless [game condition]" — a delayed sacrifice
/// with a game-STATE reprieve (as distinct from the "unless you pay [cost]" reprieve of
/// <see cref="SacrificeItUnlessDiscardRandomRule"/> / <c>SacrificeUnlessPayTriggeredRule</c>).
/// Chorale of the Void's Void end-step: "sacrifice this Aura unless a nonland permanent left
/// the battlefield this turn or a spell was warped this turn." (CR 207.2c Void ability word).
///
/// <para>
/// Produces a <see cref="SacrificeEffect"/> (Target = Self, "this Aura" = the source) whose
/// <see cref="SacrificeEffect.UnlessCondition"/> is the reprieve parsed through
/// <see cref="ConditionParser"/> — the Void disjunction resolves to a
/// <see cref="MagicAST.AST.Abilities.VoidCondition"/>. Reference-not-resolution (ADR 0004):
/// MAST records the printed unless-gate; the engine evaluates it and skips the sacrifice when
/// it holds. Anchored (^…$) so it cannot match a substring of a longer clause.
/// </para>
///
/// CR 701.21a (Sacrifice); CR 608.2 (resolving the unless-gate); CR 207.2c (Void).
/// </summary>
[TriggeredRule]
public sealed class SacrificeThisUnlessConditionTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+this\s+(?:Aura|permanent|creature|enchantment|artifact)\s+unless\s+(?<cond>.+?)\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var match = _pattern.Match(text.Trim());
    if (!match.Success)
    {
      return false;
    }

    var condition = ConditionParser.Parse(match.Groups["cond"].Value.Trim());

    // Only structure when the reprieve is a recognised condition — leave a bare free-text
    // residual to the L1 shell fallback rather than asserting an OtherCondition here.
    if (condition is MagicAST.AST.Abilities.OtherCondition)
    {
      return false;
    }

    effect = new SacrificeEffect
    {
      Target = ObjectReference.Self(),
      UnlessCondition = condition,
    };
    return true;
  }
}
