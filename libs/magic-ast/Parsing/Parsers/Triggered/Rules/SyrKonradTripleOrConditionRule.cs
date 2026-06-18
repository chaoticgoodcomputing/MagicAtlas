namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Compound triple-OR trigger for Syr Konrad, the Grim (ELD):
/// "Whenever another creature dies, or a creature card is put into a graveyard
/// from anywhere other than the battlefield, or a creature card leaves your graveyard, …"
///
/// <para>
/// This is a single triggered ability with THREE disjoint firing events (CR 603.2:
/// "Whenever a game event … matches a triggered ability's trigger event, that ability
/// automatically triggers"):
/// <list type="number">
///   <item>Primary: another creature dies (Dies event, ExcludeSelf filter).</item>
///   <item>Secondary: a creature card is put into a graveyard from anywhere other than
///   the battlefield (PutIntoGraveyard event — covers hand/library/exile/stack origins;
///   the "other than battlefield" restriction distinguishes it from the Dies clause which
///   already covers battlefield→graveyard transitions via CR 700.4).</item>
///   <item>Tertiary: a creature card leaves your graveyard (LeavesGraveyard event,
///   Controller=You — covers cards removed from graveyard by any means).</item>
/// </list>
/// </para>
///
/// <para>
/// The primary condition is returned normally; the secondary is signalled via
/// <see cref="TriggeredAbility.AdditionalTrigger"/>'s normal single-pending mechanism
/// (piggy-backed on <see cref="EntersAndOpponentDrawsNotFirstConditionRule.PendingAdditionalTrigger"/>
/// — we use our own static field here to avoid coupling). The tertiary is stored in
/// <see cref="PendingAdditionalTriggers"/> so <c>TriggeredAbilityParser</c> can populate
/// <see cref="TriggeredAbility.AdditionalTriggers"/> (the list form introduced for 3+ events).
/// </para>
///
/// <para>
/// CR 700.4 (verbatim): "The term dies means 'is put into a graveyard from the battlefield.'"
/// CR 603.2 (verbatim): "Whenever a game event or game state matches a triggered ability's
/// trigger event, that ability automatically triggers."
/// CR 701.17a (verbatim, mill): "For a player to mill a number of cards, that player puts
/// that many cards from the top of their library into their graveyard."
/// </para>
///
/// <para>
/// Priority 998 — must fire before the generic <see cref="DiesConditionRule"/> (991) or
/// <see cref="PutIntoGraveyardConditionRule"/> (985) see the first clause in isolation.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 998)]
public sealed class SyrKonradTripleOrConditionRule : ITriggerConditionRule
{
  // Full Syr-Konrad trigger phrase (case-insensitive, anchored).
  // Must match exactly: "another creature dies, or a creature card is put into a graveyard
  // from anywhere other than the battlefield, or a creature card leaves your graveyard"
  private static readonly Regex _pattern = new(
    @"^another\s+creature\s+dies,\s+or\s+a\s+creature\s+card\s+is\s+put\s+into\s+a\s+graveyard\s+from\s+anywhere\s+other\s+than\s+the\s+battlefield,\s+or\s+a\s+creature\s+card\s+leaves\s+your\s+graveyard$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  /// <summary>
  /// The secondary trigger condition (clause 2: PutIntoGraveyard from non-battlefield).
  /// This is invariant for this rule and is forwarded to
  /// <see cref="TriggeredAbility.AdditionalTrigger"/> by the parser.
  /// </summary>
  private static readonly TriggerCondition _secondaryTrigger = new()
  {
    Timing = TriggerTiming.Whenever,
    Event = TriggerEvent.PutIntoGraveyard,
    Filter = new ObjectFilter { CardTypes = ["creature"] },
  };

  /// <summary>
  /// The tertiary trigger condition (clause 3: LeavesGraveyard from controller's graveyard).
  /// Forwarded to <see cref="TriggeredAbility.AdditionalTriggers"/> by the parser.
  /// </summary>
  private static readonly TriggerCondition _tertiaryTrigger = new()
  {
    Timing = TriggerTiming.Whenever,
    Event = TriggerEvent.LeavesGraveyard,
    Filter = new ObjectFilter { CardTypes = ["creature"], Controller = ControllerFilter.You },
  };

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick pre-filter before running the full regex.
    if (!lower.Contains("another creature dies") || !lower.Contains("leaves your graveyard"))
    {
      return null;
    }

    // Strip the leading timing word ("whenever") before matching the body.
    var body = Regex.Replace(triggerText.Trim(), @"^whenever\s+", string.Empty, RegexOptions.IgnoreCase).Trim();

    if (!_pattern.IsMatch(body))
    {
      return null;
    }

    // Signal the secondary and tertiary conditions to TriggeredAbilityParser.
    // The secondary goes through the EntersAndOpponentDrawsNotFirstConditionRule's
    // pending slot (which TriggeredAbilityParser already reads for AdditionalTrigger).
    EntersAndOpponentDrawsNotFirstConditionRule.PendingAdditionalTrigger = _secondaryTrigger;

    // The tertiary goes through our own slot for AdditionalTriggers.
    PendingAdditionalTriggers = [_tertiaryTrigger];

    // Return the primary condition (another creature dies).
    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Dies,
      Filter = new ObjectFilter { CardTypes = ["creature"], ExcludeSelf = true },
    };
  }

  /// <summary>
  /// The list of additional trigger conditions (3rd, 4th, … clauses) set by the
  /// most recent successful <see cref="Match"/> call on this thread.
  /// <c>TriggeredAbilityParser</c> reads this immediately after calling
  /// <see cref="Match"/> (same synchronous call chain, same thread) and resets it
  /// to null to prevent stale reads on the next call.
  ///
  /// <para>
  /// Thread-local: the parser may be called concurrently from multiple test threads.
  /// The get/reset pattern (read then null) is safe within a single synchronous parse.
  /// </para>
  /// </summary>
  [ThreadStatic]
  public static IReadOnlyList<TriggerCondition>? PendingAdditionalTriggers;
}
