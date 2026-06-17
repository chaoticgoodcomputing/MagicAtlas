namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Recognises "a [type] card is put into an opponent's graveyard from anywhere" —
/// the trigger event that fires whenever any card moves to an opponent's graveyard
/// from any zone (Bloodchief Ascension, ZEN).
///
/// <para>
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers." The matched event is a card
/// entering a graveyard (CR 701.7 — "put into a graveyard"). The destination is
/// specifically an opponent's graveyard rather than the controller's own graveyard
/// (CR 102.2 — "opponent" is any player not on the same team). The "from anywhere"
/// clause omits a source-zone restriction.
/// </para>
///
/// <para>
/// The controller axis (<see cref="ControllerFilter.Opponent"/>) on the filter
/// identifies whose graveyard receives the card, mirroring the <see cref="ControllerFilter.You"/>
/// convention in the sibling <see cref="PutIntoGraveyardConditionRule"/>.
/// </para>
///
/// <para>
/// Representative card: Bloodchief Ascension (ZEN) — "Whenever a card is put into
/// an opponent's graveyard from anywhere, if this enchantment has three or more
/// quest counters on it, …"
/// Rule citations: CR 603.2 (Triggered Abilities), CR 701.7 (Put into a graveyard),
/// CR 102.2 (Opponent).
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 984)]
public sealed class CardPutIntoOpponentGraveyardConditionRule : ITriggerConditionRule
{
  // Matches "a [type] card is put into an opponent's graveyard [from anywhere/from zone]"
  // Named group "type" optionally captures the card type qualifier (land, creature, etc.)
  // or defaults to generic "card" when no qualifier is present.
  private static readonly Regex _pattern = new(
    @"\ba\s+(?:(?<type>land|creature|artifact|enchantment|planeswalker|spell|permanent)\s+)?card\s+(?:are|is)\s+put\s+into\s+an?\s+opponent's\s+graveyard\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("opponent") || !lower.Contains("graveyard"))
    {
      return null;
    }

    if (!lower.Contains("put into") && !lower.Contains("put in to"))
    {
      return null;
    }

    var m = _pattern.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var typeName = m.Groups["type"].Success
      ? m.Groups["type"].Value.ToLowerInvariant()
      : "card";

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.PutIntoGraveyard,
      Filter = new ObjectFilter
      {
        CardTypes = [typeName],
        Controller = ControllerFilter.Opponent,
      },
    };
  }
}
