namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Sacrifice it at the beginning of the next end step." — the delayed-sacrifice
/// sentence that trails a token-creation effect in an activated ability
/// (Kiki-Jiki, Mirror Breaker). The pronoun "it" back-references the token
/// created by the preceding sentence (CR 109.2 game objects).
///
/// <para>
/// CR 603.7: a triggered ability created by a resolving effect is a delayed
/// triggered ability. MAST records this as a
/// <see cref="CreateDelayedTriggerEffect"/> whose
/// <see cref="DelayedTriggeredAbility"/> fires "at the beginning of the next end
/// step" (a <see cref="GameTime"/> clock point) and whose resolution sacrifices
/// the referenced object. Mirrors the triggered-ability sibling
/// <c>SacrificeAtEndStepTriggeredRule</c> so an activated ability with the same
/// trailing sentence (reached via the multi-sentence pre-pass) lands the same
/// shape. CR 701.21 (Sacrifice); CR 513 (End Step).
/// </para>
/// </summary>
[ActivatedEffectRule(Priority = 70)]
public sealed class SacrificeAtEndStepEffectRule : IActivatedEffectRule
{
  private static readonly Regex _pattern = new(
    @"^sacrifice\s+(it|this\s+creature|this\s+permanent)\s+at\s+the\s+beginning\s+of\s+the\s+next\s+end\s+step\.?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    if (!_pattern.IsMatch(effectText.Trim()))
    {
      return null;
    }

    return new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.At,
          Event = new GameTime
          {
            Part = TurnPart.End,
            Edge = TimeBoundary.Beginning,
            When = TimeRelation.Next,
          },
        },
        Effects = [new SacrificeEffect { Target = ObjectReference.It() }],
      },
    };
  }
}
