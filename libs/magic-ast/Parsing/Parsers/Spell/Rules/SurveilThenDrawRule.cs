namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;

/// <summary>
/// "Surveil N, then draw M cards." — Diresight, Cruel Truths, Risky Research pattern.
/// Expands to a flat [<see cref="SurveilEffect"/>, <see cref="DrawCardsEffect"/>] pair
/// so both effects sit as siblings on <see cref="MagicAST.AST.Abilities.SpellAbility.Effects"/>.
///
/// <para>
/// The optional "You lose N life" suffix (sentence 2 in the spell's oracle text) is
/// handled by the sentence-bundle dispatcher, which routes it through
/// <see cref="YouLoseLifeSpellRule"/>. This rule only matches sentence 1.
/// </para>
///
/// <para>
/// Rule 701.42 (surveil), Rule 120.1 (draw). Surveil count and draw count are
/// independent small integers and are allowed to differ (e.g. "Surveil 3, then draw
/// three cards").
/// </para>
/// </summary>
[SpellRule]
public sealed class SurveilThenDrawRule : ISpellRule, IMultiSpellRule
{
  private static readonly Regex Pattern = new(
    @"^Surveil\s+(?<sn>\d+|one|two|three|four|five|six|seven|eight|nine|ten),\s*then\s+draw\s+(?<dn>a|one|two|three|four|five|six|seven|eight|nine|ten|\d+)\s+cards?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <inheritdoc/>
  /// <remarks>
  /// Returns <c>false</c> unconditionally — the compound surveil+draw shape always
  /// produces two sibling effects, so callers must use
  /// <see cref="TryMatchMulti"/> instead.
  /// </remarks>
  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    return false;
  }

  /// <inheritdoc/>
  public bool TryMatchMulti(string text, out IReadOnlyList<Effect>? effects)
  {
    effects = null;
    var m = Pattern.Match(text.Trim());
    if (!m.Success)
    {
      return false;
    }

    var surveilCount = SpellRuleHelpers.ParseSmallWord(m.Groups["sn"].Value);
    var drawCount = SpellRuleHelpers.ParseSmallWord(m.Groups["dn"].Value);

    effects = new List<Effect>
    {
      new SurveilEffect
      {
        Count = LiteralQuantity.Of(surveilCount),
      },
      new DrawCardsEffect
      {
        Count = LiteralQuantity.Of(drawCount),
        Player = ObjectReference.You(),
      },
    };
    return true;
  }
}
