namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Compound trigger: "When [SelfName] enters and whenever a creature you control deals
/// combat damage to a player" — Derevi, Empyrial Tactician (C13). A single triggered
/// ability that fires on EITHER of two events:
/// (1) when this permanent (self) enters the battlefield, or
/// (2) whenever a creature the controller controls deals combat damage to a player.
///
/// <para>
/// CR 603.2: a triggered ability triggers whenever any of its listed events occurs.
/// The "and" in oracle text ("When A and whenever B") combines two disjoint trigger
/// conditions into one ability; the ability fires whenever EITHER condition is met.
/// The primary condition (ETB self-by-name) is returned from <see cref="Match"/>;
/// the secondary (creature you control deals combat damage) is signalled via
/// <see cref="PendingAdditionalTrigger"/> so <c>TriggeredAbilityParser</c> can
/// promote it to <c>TriggeredAbility.AdditionalTrigger</c>.
/// </para>
///
/// <para>
/// Rule 603.6a (ETB triggers): "Enters-the-battlefield abilities trigger when a
/// permanent enters the battlefield." The self-by-name ETB primary (CR 201.5) is
/// encoded as <c>IsSelf=true</c> on the filter (not <c>Controller=You</c>), because
/// it refers to this permanent itself, not any creature the controller controls.
/// </para>
///
/// <para>
/// Rule 510 (Combat Damage Step): combat damage is dealt; CR 603.2 triggers fire.
/// The secondary condition is <see cref="TriggerEvent.DealsCombatDamageToPlayer"/>
/// with <c>Controller=You</c> on the filter — "a creature you control" means any
/// creature the ability's controller controls (CR 109.5).
/// </para>
///
/// <para>
/// Anchored (<c>^...$</c>) to prevent substring-matching a broader trigger phrase.
/// Must be tried at higher priority than the generic <see cref="EntersConditionRule"/>
/// and <see cref="DealsCombatDamageConditionRule"/> so the compound phrase is matched
/// as a unit before either sub-phrase claims it.
/// </para>
/// </summary>
[TriggerConditionRule(Priority = 997)]
public sealed class EntersAndCreatureControllerDealsCombatDamageToPlayerConditionRule : ITriggerConditionRule
{
  // Anchored pattern: "When [SelfName] enters and whenever a creature you control
  // deals combat damage to a player". The self-name is one or more name-words
  // (capitalised content words, optional lowercase function words, optional
  // trailing comma for legendary epithets) followed by "enters". Leading timing
  // word stripped by the caller before this method is invoked — but the trigger
  // text still includes the timing word at the front.
  //
  // The name segment uses the same liberal pattern as IsSelfByNameTrigger in
  // TriggeredRuleHelpers — first word capitalised, subsequent words either
  // capitalised or a small set of function words.
  private static readonly Regex _pattern = new(
    @"^When\s+[A-Z][A-Za-z',\-]*(?:\s+(?:[A-Z][A-Za-z',\-]*|of|the|a|an|from|for|to|in|at|with|by|and|or|as),?)*\s+enters\s+and\s+whenever\s+a\s+creature\s+you\s+control\s+deals\s+combat\s+damage\s+to\s+a\s+player$",
    RegexOptions.Compiled | RegexOptions.CultureInvariant
  );

  /// <summary>
  /// The secondary trigger condition synthesized from the "whenever a creature you
  /// control deals combat damage to a player" branch. Stored statically since it is
  /// invariant for this rule family.
  /// Rule 510 — Combat Damage Step; Rule 603.2 — triggered abilities.
  /// </summary>
  private static readonly TriggerCondition _secondaryTrigger = new()
  {
    Timing = TriggerTiming.Whenever,
    Event = TriggerEvent.DealsCombatDamageToPlayer,
    Filter = new ObjectFilter
    {
      CardTypes = ["creature"],
      Controller = ControllerFilter.You,
    },
  };

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    // Quick pre-filter before running the full regex.
    if (!lower.Contains("enters") || !lower.Contains("deals combat damage") || !lower.Contains("you control"))
    {
      return null;
    }

    if (!_pattern.IsMatch(triggerText.Trim()))
    {
      return null;
    }

    // The primary trigger is the self ETB condition ("When [SelfName] enters").
    // Encode as IsSelf=true with CardTypes=["creature"] — the default type for a
    // self-by-name ETB (SelfReferenceTypeCorrector in CardParser will correct the
    // type to "creature" based on the type line; CR 201.5 self-reference).
    //
    // Implementation note: because ITriggerConditionRule.Match can only return one
    // TriggerCondition, we piggyback the secondary condition on a thread-local so
    // TriggeredAbilityParser can retrieve it immediately after. Mirrors the pattern
    // established by EntersAndOpponentDrawsNotFirstConditionRule.
    PendingAdditionalTrigger = _secondaryTrigger;

    return new TriggerCondition
    {
      Timing = TriggerTiming.When,
      Event = TriggerEvent.Enters,
      Filter = new ObjectFilter { CardTypes = ["creature"], IsSelf = true },
    };
  }

  /// <summary>
  /// The additional trigger condition synthesized by the most recent successful
  /// <see cref="Match"/> call on this thread. <c>TriggeredAbilityParser</c> reads
  /// this immediately after calling <see cref="Match"/> (same synchronous call chain,
  /// same thread) and resets it to null to prevent stale reads on the next call.
  ///
  /// <para>
  /// Thread-local because the parser may be called concurrently from multiple test
  /// threads. The get/reset pattern (read then null) is safe within a single
  /// synchronous parse call.
  /// </para>
  /// </summary>
  [ThreadStatic]
  public static TriggerCondition? PendingAdditionalTrigger;
}
