namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;

/// <summary>
/// "After this phase, there is an additional combat phase." — inserts a single
/// additional combat phase into the current turn without an accompanying main phase.
///
/// <para>Distinct from <see cref="AdditionalCombatAndMainPhaseEffectRule"/>, which
/// handles "After this main phase, there is an additional combat phase followed by an
/// additional main phase." (Aggravated Assault). Godo, Bandit Warlord's trigger says
/// only "After this phase, there is an additional combat phase." — no additional main
/// phase, and "this phase" rather than "this main phase".</para>
///
/// <para>CR 506: The combat phase. CR 500.1: turn structure. The insertion point
/// ("after this phase") and the single inserted phase (combat) are the full printed
/// content; engine phase-order bookkeeping (CR 505.1a) is not encoded here.</para>
///
/// <para>Priority 990 — above <see cref="AdditionalCombatAndMainPhaseEffectRule"/>
/// (Priority 991) so the more-specific single-phase form is tried first. Both are
/// anchored (^...$), so they cannot cross-match.</para>
/// </summary>
[ActivatedEffectRule(Priority = 990)]
public sealed class AdditionalCombatPhaseEffectRule : IActivatedEffectRule
{
  private static readonly Regex Pattern = new(
    @"^After\s+this\s+phase[,\s]+there\s+is\s+an\s+additional\s+combat\s+phase\.?$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim();
    if (!Pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new AdditionalCombatPhaseEffect();
  }
}
