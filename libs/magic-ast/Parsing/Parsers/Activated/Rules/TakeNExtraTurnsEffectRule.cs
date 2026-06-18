namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Timing;
using MagicAST.AST.References;

/// <summary>
/// "Take two extra turns after this one." — Teferi, Master of Time −10 loyalty ability.
/// Grants the controller two additional full turns immediately following the current turn.
///
/// <para>
/// CR 500.7 (verbatim): "Some effects can give a player extra turns. They do this by
/// adding the turns directly after the specified turn. If a player is given multiple
/// extra turns, the extra turns are added one at a time. … The most recently created
/// turn will be taken first."
/// </para>
///
/// <para>
/// The <see cref="TakeExtraTurnEffect.Count"/> field records the two-turn quantity; the
/// engine applies CR 500.7's sequencing rule. This rule handles the "N extra turns"
/// (N ≥ 2) form; the canonical "an extra turn" (N = 1) form is handled by the
/// existing <see cref="TakeExtraTurnEffectRule"/> (no Count field emitted for the
/// singular case).
/// </para>
///
/// <para>
/// ANCHORED (^...$): prevents partial matches inside broader text. Priority 984 —
/// more-specific than the generic single-turn rule (985), fires first so the two-turn
/// form is not silently truncated to one turn.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 984)]
public sealed class TakeNExtraTurnsEffectRule : IActivatedEffectRule
{
  // "Take [number-word] extra turns after this one"
  // Supports two, three, … (word-form numbers).
  private static readonly Regex _pattern = new(
    @"^[Tt]ake\s+(?<count>\w+)\s+extra\s+turns\s+after\s+this\s+one$",
    RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    var m = _pattern.Match(trimmed);
    if (!m.Success)
    {
      return null;
    }

    var countWord = m.Groups["count"].Value;
    var count = ActivatedRuleHelpers.ParseNumberWord(countWord);
    if (count is null or < 2)
    {
      // Guard: count < 2 should use the singular-turn rule; null falls back.
      return null;
    }

    return new TakeExtraTurnEffect
    {
      Player = ObjectReference.You(),
      Count = count.Value,
    };
  }
}
