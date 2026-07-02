namespace MagicAST.Parsing.Parsers.Spell.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.CardFlow;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you cast a[n] [type] spell this turn, draw a card." — a spell whose
/// resolution creates a delayed triggered ability (CR 603.7) bounded to this turn:
/// the trigger is an event (a spell of the given type is cast by you), the window
/// is "this turn" (<c>untilTime</c> end of turn), the effect is a draw. ADR 0002/0004.
///
/// Examples: Glimpse of Nature (creature), Beck.
/// </summary>
[SpellRule(Priority = 60)]
public sealed class CastSpellThisTurnDelayedDrawRule : ISpellRule
{
  private static readonly Regex Pattern = new(
    @"^Whenever\s+you\s+cast\s+an?\s+(?<type>creature|instant|sorcery|artifact|enchantment|planeswalker|land)\s+spell\s+this\s+turn,\s+draw\s+a\s+card$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;
    var m = Pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    var type = m.Groups["type"].Value.ToLowerInvariant();
    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.Whenever,
          Event = TriggerEvent.SpellCast,
          Filter = new ObjectFilter { CardTypes = [type], Controller = ControllerFilter.You },
        },
        Window = UntilTimeDuration.EndOfTurn,
        Effects = [new DrawCardsEffect { Count = LiteralQuantity.Of(1), Player = ObjectReference.You() }],
      },
    };
    return true;
  }
}
