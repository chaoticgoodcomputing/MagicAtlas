namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever an opponent casts a spell with the chosen name" (Silverquill
/// Silencer) — the trigger-condition consumer half of a CR 614.12 named-card
/// binder, paired with a "choose a [nonland] card name" declaration elsewhere
/// on the card (<see cref="MagicAST.AST.Effects.Keyword.ChooseCardNameEffect"/>).
/// The chosen-name restriction is modeled as
/// <see cref="ObjectFilter.ChosenCharacteristic"/> =
/// <see cref="ChosenCharacteristicKind.CardName"/> on the trigger's spell filter
/// (the structured consumer side of the CR 607 linked ability), mirroring
/// <see cref="MagicAST.Parsing.Parsers.Activated.Rules.CounterTargetSpellWithChosenNameActivatedEffectRule"/>'s
/// activation-cost analogue for the same binder shape.
///
/// <para>
/// Anchored end-to-end so it is disjoint from the generic
/// <see cref="SpellCastConditionRule"/> (which has no "with the chosen name"
/// handling and would otherwise silently drop the chosen-name filter). Priority
/// 999 — one above <see cref="SpellCastConditionRule"/> (998) so this more
/// specific shape is tried first.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 999)]
public sealed class OpponentCastsSpellWithChosenNameConditionRule : ITriggerConditionRule
{
  private static readonly Regex _pattern = new(
    @"^(?:when(?:ever)?\s+)?an\s+opponent\s+casts\s+a\s+spell\s+with\s+the\s+chosen\s+name$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.SpellCast,
      Filter = new ObjectFilter
      {
        CardTypes = ["spell"],
        Controller = ControllerFilter.Opponent,
        ChosenCharacteristic = ChosenCharacteristicKind.CardName,
      },
    };
  }
}
