namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;

/// <summary>
/// "After this main phase, there is an additional combat phase followed by an additional
/// main phase." — inserts a combat phase and main phase into the current turn's timeline.
///
/// <para>CR 500.7 (verbatim): "Some spells and abilities can add phases to a player's
/// turn. They do this by adding the phases directly after the specified phase. If two
/// effects add phases to a player's turn, the most recently created one is added first."
/// </para>
///
/// <para>
/// Emits an <see cref="AdditionalCombatAndMainPhaseEffect"/> (a parameterless marker —
/// the insertion point "after this main phase" and the specific phases inserted are
/// fixed by the oracle text and need no parameters, just as SkipUntapEffect is
/// parameterless).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 991)]
public sealed class AdditionalCombatAndMainPhaseEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^After\s+this\s+main\s+phase[,\s]+there\s+is\s+an\s+additional\s+combat\s+phase\s+followed\s+by\s+an\s+additional\s+main\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new AdditionalCombatAndMainPhaseEffect();
  }
}
