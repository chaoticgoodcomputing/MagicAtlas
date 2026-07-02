namespace MagicAST.Parsing.Parsers.Activated.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Activated-ability effect that creates a turn-scoped delayed triggered ability
/// (CR 603.7) watching for cards entering an opponent's graveyard:
///
///   "Whenever a card is put into an opponent's graveyard from anywhere this turn,
///    that player loses N life."
///
/// The "this turn" clause establishes the delayed trigger's window
/// (<see cref="UntilTimeDuration.EndOfTurn"/>). The trigger event is
/// <see cref="TriggerEvent.PutIntoGraveyard"/> filtered to
/// <see cref="ControllerFilter.Opponent"/>. The effect is
/// <see cref="LoseLifeEffect"/> targeting <see cref="ObjectReferenceKind.ThatPlayer"/>
/// — the pronoun for the player whose graveyard received the card (CR 109.5).
///
/// <para>
/// CR rule citations:
/// <list type="bullet">
/// <item>CR 602.1: Activated abilities have a cost and an effect (cost:effect form).</item>
/// <item>CR 603.7: A resolving effect may create a delayed triggered ability.</item>
/// <item>CR 404 (graveyard) / 701.17: "put into a graveyard" / mill.</item>
/// <item>CR 119.3: Life loss.</item>
/// </list>
/// </para>
///
/// Representative card: Duskmantle Guildmage (GTC).
/// </summary>
[ActivatedEffectRule(Priority = 71)]
public sealed class WheneverCardPutIntoOpponentGraveyardThisTurnLoseLifeEffectRule : IActivatedEffectRule
{
  // Anchored so it cannot match as a substring of a longer effect sentence.
  // Captures the life-loss amount (digit or word).
  private static readonly Regex _pattern = new(
    @"^Whenever\s+a\s+(?:(?<type>land|creature|artifact|enchantment|planeswalker|spell|permanent)\s+)?card\s+is\s+put\s+into\s+an\s+opponent's\s+graveyard\s+from\s+anywhere\s+this\s+turn,\s+that\s+player\s+loses\s+(?<amount>\d+|one|two|three|four|five|six|seven|eight|nine|ten)\s+life\.?\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public Effect? TryMatch(string effectText)
  {
    var m = _pattern.Match(effectText.Trim());
    if (!m.Success)
    {
      return null;
    }

    var amount = ParseAmount(m.Groups["amount"].Value);

    var typeName = m.Groups["type"].Success
      ? m.Groups["type"].Value.ToLowerInvariant()
      : "card";

    return new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.Whenever,
          Event = TriggerEvent.PutIntoGraveyard,
          Filter = new ObjectFilter
          {
            CardTypes = [typeName],
            Controller = ControllerFilter.Opponent,
          },
        },
        Window = UntilTimeDuration.EndOfTurn,
        Effects =
        [
          new LoseLifeEffect
          {
            Amount = LiteralQuantity.Of(amount),
            Player = new ObjectReference { Kind = ObjectReferenceKind.ThatPlayer },
          },
        ],
      },
    };
  }

  private static int ParseAmount(string raw) =>
    raw.ToLowerInvariant() switch
    {
      "one" => 1,
      "two" => 2,
      "three" => 3,
      "four" => 4,
      "five" => 5,
      "six" => 6,
      "seven" => 7,
      "eight" => 8,
      "nine" => 9,
      "ten" => 10,
      var t => int.TryParse(t, out var n) ? n : 1,
    };
}
