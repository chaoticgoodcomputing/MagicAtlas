namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;

/// <summary>
/// "Target creature you don't control phases out." — Teferi, Master of Time −3 loyalty
/// ability. Causes the targeted creature to phase out (CR 702.26) until its
/// controller's next untap step.
///
/// <para>
/// CR 702.26a (verbatim): "Phasing is a static ability that modifies the rules of
/// the untap step. During each player's untap step, before the active player untaps
/// permanents, all phased-in permanents with phasing that player controls 'phase out.'
/// Simultaneously, all phased-out permanents that had phased out under that player's
/// control 'phase in.'"
/// </para>
///
/// <para>
/// CR 702.26b: "If a permanent phases out, its status changes to 'phased out.' Except
/// for rules and effects that specifically mention phased-out permanents, a phased-out
/// permanent is treated as though it does not exist."
/// </para>
///
/// <para>
/// "You don't control" maps to <see cref="ControllerFilter.Opponent"/> (CR 109.5:
/// an object "you don't control" is controlled by another player — effectively an
/// opponent in the context of targeted effects).
/// </para>
///
/// <para>
/// The parenthetical reminder "(Treat it and anything attached to it as though they
/// don't exist until its controller's next turn.)" is stripped by
/// <see cref="MagicAST.Parsing.Parsers.ActivatedAbilityParser"/>'s
/// <c>StripTrailingReminder</c> before this rule sees the text.
/// </para>
///
/// <para>
/// ANCHORED (^...$): prevents partial matches inside broader phase-out effect text.
/// Priority 982 — specific shape, below generic exile (983) but above fallback.
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 982)]
public sealed class PhaseOutTargetEffectRule : IActivatedEffectRule
{
  // "Target creature you don't control phases out"
  // Apostrophe: straight (u+0027) or curly right (u+2019).
  private static readonly Regex _pattern = new(
    @"^Target\s+creature\s+you\s+don['']t\s+control\s+phases\s+out$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var trimmed = effectText.Trim().TrimEnd('.');
    if (!_pattern.IsMatch(trimmed))
    {
      return null;
    }

    return new PhaseOutEffect
    {
      Target = new ObjectReference
      {
        Kind = ObjectReferenceKind.Target,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = ControllerFilter.Opponent,
        },
      },
    };
  }
}
