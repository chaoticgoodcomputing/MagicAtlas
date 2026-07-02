namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;

/// <summary>
/// "After this phase, there is an additional combat phase." — triggered-ability mirror
/// of <see cref="MagicAST.Parsing.Parsers.Activated.Rules.AdditionalCombatPhaseEffectRule"/>.
///
/// <para>Emits an <see cref="AdditionalCombatPhaseEffect"/>, which inserts exactly one
/// additional combat phase into the current turn without an accompanying main phase
/// (distinct from the Aggravated Assault pattern). This variant is used inside a
/// triggered ability (e.g. Godo, Bandit Warlord's second ability), where the sentence
/// bundle dispatcher routes each sentence through <see cref="ITriggeredRule"/>
/// implementations rather than through activated rules.</para>
///
/// <para>Pattern anchored (^...$) so it cannot match as a substring of a longer
/// phrase. CR 506 (combat phase); CR 500.1 (turn structure).</para>
/// </summary>
[TriggeredRule(Priority = 60)]
public sealed class AdditionalCombatPhaseTriggeredRule : ITriggeredRule
{
  private static readonly Regex _pattern = new(
    @"^After\s+this\s+phase[,\s]+there\s+is\s+an\s+additional\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    if (!_pattern.IsMatch(text.Trim()))
    {
      return false;
    }

    effect = new AdditionalCombatPhaseEffect();
    return true;
  }
}
