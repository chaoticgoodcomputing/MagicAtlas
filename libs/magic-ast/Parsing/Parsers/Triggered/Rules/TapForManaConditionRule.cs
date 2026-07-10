namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// Trigger condition for "tap a [nonland] permanent for [mana/{C}]" shapes.
///
/// Handles two variants:
/// <list type="bullet">
///   <item>
///     Specific-symbol: "you tap a permanent for {C}" (Forsaken Monument) — the produced mana
///     symbol is recorded in <see cref="TriggerCondition.ProducedMana"/>. Controller: You when
///     "you tap"; Any when "a player taps".
///   </item>
///   <item>
///     Any-mana: "you tap a nonland permanent for mana" (Kinnan, Bonder Prodigy) — the trigger
///     fires on any mana production by tapping a nonland permanent. No specific symbol is
///     recorded (<see cref="TriggerCondition.ProducedMana"/> is null). The nonland qualifier is
///     expressed as <c>ExcludedCardTypes: ["land"]</c> on the filter.
///   </item>
///   <item>
///     Enchanted-subject (passive): "enchanted land is tapped for mana" (Fertile Ground, Wild
///     Growth, Glittering Frost, Market Festival — the mana-doubler Aura family). The triggering
///     object is the enchanted land itself, modeled as <c>IsEnchanted: true</c> with the named
///     card type. No specific symbol is recorded (<see cref="TriggerCondition.ProducedMana"/> is
///     null) — the trigger fires for any mana the enchanted land produces.
///   </item>
///   <item>
///     Land any-mana: "a player taps a land for mana" (Dictate of Karametra) — the trigger fires
///     on any mana production by ANY player tapping a land (<c>CardTypes: ["land"]</c>, no
///     nonland exclusion — the mirror-image of the Kinnan nonland-permanent variant above). No
///     specific symbol is recorded (<see cref="TriggerCondition.ProducedMana"/> is null).
///     Controller: You when "you tap"; Any when "a player taps".
///   </item>
/// </list>
///
/// CR 106.12: "To 'tap [a permanent] for mana' is to activate a mana ability of that permanent
/// that includes the {T} symbol in its activation cost." CR 603.2: the event-match (the
/// enchanted land being tapped for mana) is the trigger. This rule encodes the trigger that
/// fires when that mana-tap occurs.
/// </summary>
[TriggerConditionRule(Priority = 994)]
public sealed class TapForManaConditionRule : ITriggerConditionRule
{
  // "you tap a permanent for {C}" — the specific mana symbol in {braces}.
  // Anchors at the end (after the mana symbol) to ensure we match exactly this
  // shape and not a broader clause. triggerText includes the timing word
  // ("Whenever you tap a permanent for {C}"), so we use a non-start-anchored
  // pattern that captures the subject and mana symbol.
  private static readonly Regex _tapForManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+permanent\s+for\s+(?<mana>\{[A-Z0-9/]+\})\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "you tap a nonland permanent for mana" — Kinnan, Bonder Prodigy shape.
  // No specific mana symbol; the nonland qualifier excludes lands. ProducedMana is null.
  private static readonly Regex _tapNonlandForAnyManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+nonland\s+permanent\s+for\s+mana\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "enchanted land is tapped for mana" — the mana-doubler Aura family (Fertile Ground,
  // Wild Growth, Glittering Frost, Market Festival). Passive voice: the triggering object is
  // the enchanted land itself, not a player tapping a permanent. Modeled as the enchanted
  // permanent (IsEnchanted = true) of the named card type. No specific mana symbol — the
  // trigger fires for any mana the enchanted land is tapped for (ProducedMana is null).
  private static readonly Regex _enchantedTappedForManaPattern = new(
    @"\benchanted\s+(?<type>land)\s+is\s+tapped\s+for\s+mana\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "a player taps a land for mana" — Dictate of Karametra shape. No "permanent" word appears
  // (the tapped object is explicitly "a land"), so this is checked before the "permanent" guard
  // below. No specific mana symbol; no nonland exclusion (mirrors the Kinnan nonland-permanent
  // variant, but for lands rather than nonland permanents).
  private static readonly Regex _tapLandForAnyManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+land\s+for\s+mana\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("tap"))
    {
      return null;
    }

    // "enchanted land is tapped for mana" — Aura mana-doublers. Checked before the
    // "permanent" guard because this shape names the enchanted land, not a "permanent".
    var e = _enchantedTappedForManaPattern.Match(triggerText.Trim());
    if (e.Success)
    {
      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = [e.Groups["type"].Value.ToLowerInvariant()],
          IsEnchanted = true,
        },
        // ProducedMana is null — the trigger fires for any mana the enchanted land produces.
      };
    }

    // "a player taps a land for mana" — Dictate of Karametra. Checked before the
    // "permanent" guard because this shape names "a land", not "a permanent".
    var l = _tapLandForAnyManaPattern.Match(triggerText.Trim());
    if (l.Success)
    {
      var subject = l.Groups["subject"].Value.Trim();
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["land"],
          Controller = controller,
        },
        // ProducedMana is null — the trigger fires for any mana type.
      };
    }

    if (!lower.Contains("permanent"))
    {
      return null;
    }

    // Try specific-symbol variant first (Forsaken Monument shape).
    var m = _tapForManaPattern.Match(triggerText.Trim());
    if (m.Success)
    {
      var subject = m.Groups["subject"].Value.Trim();
      var mana = m.Groups["mana"].Value.ToUpperInvariant();

      // "you tap" → Controller: You; "a player taps" → Controller: Any
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          Controller = controller,
        },
        ProducedMana = mana,
      };
    }

    // Try any-mana nonland variant (Kinnan, Bonder Prodigy shape).
    var n = _tapNonlandForAnyManaPattern.Match(triggerText.Trim());
    if (n.Success)
    {
      var subject = n.Groups["subject"].Value.Trim();
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["permanent"],
          ExcludedCardTypes = ["land"],
          Controller = controller,
        },
        // ProducedMana is null — the trigger fires for any mana type.
      };
    }

    return null;
  }
}
