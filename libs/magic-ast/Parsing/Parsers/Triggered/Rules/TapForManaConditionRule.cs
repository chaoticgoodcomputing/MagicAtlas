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
///   <item>
///     Named-subtype passive (any controller): "a Forest is tapped for mana" (Vernal Bloom) —
///     passive voice like the enchanted-subject variant above, but naming a land subtype
///     directly instead of "enchanted land", and with no controller restriction at all (any
///     player's Forest triggers it, not just "you" or an enchanted one). Modeled as
///     <c>CardTypes: ["land"], Subtypes: [word]</c>, no <c>IsEnchanted</c> and no
///     <c>Controller</c>. No specific mana symbol is recorded (<see cref="TriggerCondition.ProducedMana"/>
///     is null) — the trigger fires for any mana the named land produces.
///   </item>
///   <item>
///     Creature any-mana: "you tap a creature for mana" (Leyline of Abundance) — the trigger
///     fires on any mana production by tapping a creature (<c>CardTypes: ["creature"]</c>). No
///     specific symbol is recorded (<see cref="TriggerCondition.ProducedMana"/> is null) — the
///     mirror-image of the Dictate of Karametra land variant above, for creatures rather than
///     lands. Controller: You when "you tap"; Any when "a player taps".
///   </item>
///   <item>
///     Subtype-land any-mana: "you tap a Swamp for mana" (Nirkana Revenant) — narrows the land
///     any-mana variant above to a single named land subtype (<c>CardTypes: ["land"]</c> +
///     <c>Subtypes: [word]</c>, mirroring the land-subtype filter shape used by
///     <see cref="MagicAST.Parsing.Parsers.Activated.Rules.TapSingularSubtypeCostRule"/>). No
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

  // "a Forest is tapped for mana" — Vernal Bloom shape. Passive voice naming a land subtype
  // directly (not "enchanted land"), with no controller qualifier at all — any player's
  // matching land triggers it. Anchored on "a <Subtype> is tapped for mana" (start-of-clause
  // "a", not "enchanted" or "you"/"a player tap[s]"), so it cannot collide with the enchanted
  // or subject-tap variants above/below. Only matches known land subtypes (capitalised),
  // mirroring the "you tap a Swamp for mana" guard.
  private static readonly Regex _subtypeIsTappedForAnyManaPattern = new(
    @"\ba\s+(?<subtype>[A-Z][A-Za-z]+)\s+is\s+tapped\s+for\s+mana\s*$",
    RegexOptions.Compiled
  );

  // "you tap a creature for mana" — Leyline of Abundance shape. No "permanent" word
  // appears (the tapped object is explicitly "a creature"), so this is checked before
  // the "permanent" guard below, mirroring the land variant above. No specific mana
  // symbol; no nonland exclusion needed (the creature card type is already narrower
  // than "nonland permanent").
  private static readonly Regex _tapCreatureForAnyManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+a\s+creature\s+for\s+mana\s*$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // "you tap a Swamp for mana" — Nirkana Revenant shape. The tapped object is named by a
  // capitalised land subtype word rather than "land"/"permanent", so this pattern is NOT
  // case-insensitive: requiring an uppercase first letter is what distinguishes it from the
  // lowercase "a land"/"a creature"/"a permanent" shapes above.
  private static readonly Regex _tapLandSubtypeForAnyManaPattern = new(
    @"\b(?<subject>you|a\s+player)\s+tap[s]?\s+an?\s+(?<subtype>[A-Z][A-Za-z]+)\s+for\s+mana\s*$",
    RegexOptions.Compiled
  );

  // Land subtypes (CR 205.3i): the five basics plus single-word nonbasic land types. Mirrors
  // the set in TapSingularSubtypeCostRule / ReturnLandSubtypeToHandCostRule.
  private static readonly HashSet<string> LandSubtypes = new(System.StringComparer.OrdinalIgnoreCase)
  {
    "Plains", "Island", "Swamp", "Mountain", "Forest",
    "Desert", "Gate", "Lair", "Locus", "Mine", "Tower", "Cave", "Sphere",
  };

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

    // "a Forest is tapped for mana" — Vernal Bloom. Checked before the "permanent" guard
    // and before the subject-tap variants because this shape is passive voice naming a land
    // subtype directly, with no controller qualifier. Only matches known land subtypes, to
    // avoid over-generalizing to arbitrary capitalised nouns.
    var subtypePassive = _subtypeIsTappedForAnyManaPattern.Match(triggerText.Trim());
    if (subtypePassive.Success && LandSubtypes.Contains(subtypePassive.Groups["subtype"].Value))
    {
      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["land"],
          Subtypes = [subtypePassive.Groups["subtype"].Value],
        },
        // ProducedMana is null — the trigger fires for any mana the named land produces.
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

    // "you tap a creature for mana" — Leyline of Abundance. Checked before the
    // "permanent" guard because this shape names "a creature", not "a permanent".
    var c = _tapCreatureForAnyManaPattern.Match(triggerText.Trim());
    if (c.Success)
    {
      var subject = c.Groups["subject"].Value.Trim();
      var controller = subject.Equals("you", System.StringComparison.OrdinalIgnoreCase)
        ? ControllerFilter.You
        : ControllerFilter.Any;

      return new TriggerCondition
      {
        Timing = timing,
        Event = TriggerEvent.TapsForMana,
        Filter = new ObjectFilter
        {
          CardTypes = ["creature"],
          Controller = controller,
        },
        // ProducedMana is null — the trigger fires for any mana type.
      };
    }

    // "you tap a Swamp for mana" — Nirkana Revenant. Checked before the "permanent" guard
    // because this shape names a capitalised land subtype, not "a land"/"a permanent". Only
    // matches when the captured word is a known land subtype, to avoid over-generalizing to
    // arbitrary capitalised nouns.
    var sub = _tapLandSubtypeForAnyManaPattern.Match(triggerText.Trim());
    if (sub.Success && LandSubtypes.Contains(sub.Groups["subtype"].Value))
    {
      var subject = sub.Groups["subject"].Value.Trim();
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
          Subtypes = [sub.Groups["subtype"].Value],
          Controller = controller,
        },
        // ProducedMana is null — the trigger fires for any mana type the tapped land produces.
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
