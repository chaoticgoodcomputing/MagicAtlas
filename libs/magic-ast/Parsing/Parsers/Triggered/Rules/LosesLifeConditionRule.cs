namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever an opponent loses life" / "Whenever you lose life" — life-loss
/// trigger (Exquisite Blood, Bloodthirsty Conqueror). The controller axis is
/// carried on the trigger's filter: <see cref="ControllerFilter.Opponent"/> when
/// the oracle text names an opponent, else <see cref="ControllerFilter.You"/>.
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." The matched event is a
/// player losing life (CR 119.3: "If an effect causes a player to gain life or
/// lose life, that player's life total is adjusted accordingly."). The amount of
/// life lost is referenced by a downstream "that much" effect (a derived
/// quantity); this rule records only the event + the player class.
/// </para>
///
/// <para>
/// Mirrors <see cref="GainsLifeConditionRule"/> (the <see cref="TriggerEvent.GainsLife"/>
/// sibling): same controller-default-flip shape, the opposite life event.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class LosesLifeConditionRule : ITriggerConditionRule
{
  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("lose") || !lower.Contains("life"))
    {
      return null;
    }

    if (!Regex.IsMatch(lower, @"\b(you|an?\s+opponent|a player)\s+lose(s)?\s+life\b"))
    {
      return null;
    }

    ControllerFilter controller = lower.Contains("opponent")
      ? ControllerFilter.Opponent
      : ControllerFilter.You;

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.LosesLife,
      Filter = new ObjectFilter { Controller = controller },
    };
  }
}
