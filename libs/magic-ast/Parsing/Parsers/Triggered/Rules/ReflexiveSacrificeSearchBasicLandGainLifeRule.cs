namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.Abilities;
using MagicAST.AST.Effects;
using MagicAST.AST.Effects.Core;
using MagicAST.AST.Effects.Resource;
using MagicAST.AST.Effects.ZoneChange;
using MagicAST.AST.Quantities;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "When you do, search your library for a basic &lt;subtypes&gt; card, put it onto
/// the battlefield tapped, then shuffle[ and you gain &lt;N&gt; life]."
///
/// <para>
/// The reflexive tail of a fetchland-style ETB (the Obscura Storefront / SNC
/// "surveil dual" fetch family): "When this land enters, sacrifice it. When you
/// do, search your library for a basic Plains, Island, or Swamp card, put it onto
/// the battlefield tapped, then shuffle and you gain 1 life." The "sacrifice it"
/// sentence parses via <see cref="SacrificeTriggeredRule"/>; this rule handles the
/// following "When you do, …" sentence, which is a <b>reflexive triggered ability</b>
/// (CR 603.12) created when the ETB ability resolves — a separate object, not an
/// inline effect and not a second printed ability. Modelled as a
/// <see cref="CreateDelayedTriggerEffect"/> wrapping a
/// <see cref="DelayedTriggeredAbility"/> whose trigger is "when you sacrifice this
/// land" (<see cref="TriggerEvent.Sacrifices"/>, <c>IsSelf</c>).
/// </para>
///
/// <para>
/// CR 603.12 (verbatim): "A resolving spell or ability may allow or instruct a
/// player to take an action and create a triggered ability that triggers 'when [a
/// player] [does or doesn't]' take that action or 'when [something happens] this
/// way.' These reflexive triggered abilities follow the rules for delayed triggered
/// abilities (see rule 603.7), except that they're checked immediately after being
/// created and trigger based on whether the trigger event or events occurred earlier
/// during the resolution of the spell or ability that created them." Obscura
/// Storefront is structurally identical to the rule's Heart-Piercer Manticore
/// example, save that the sacrifice here is mandatory ("sacrifice it") rather than
/// "you may sacrifice" — so the reflexive trigger always fires, but it is still a
/// separate delayed/reflexive ability object, hence <see cref="CreateDelayedTriggerEffect"/>.
/// </para>
///
/// <para>
/// CR 603.7 (verbatim): "An effect may create a delayed triggered ability that can
/// do something at a later time. A delayed triggered ability will contain 'when,'
/// 'whenever,' or 'at,' although that word won't usually begin the ability."
/// </para>
///
/// <para>
/// CR 701.23a (Search): "To search for a card in a zone, look at all cards in that
/// zone (even if it's a hidden zone) and find a card that matches the given
/// description." CR 119.3 (gain life): "If an effect causes a player to gain life or
/// lose life, that player's life total is adjusted accordingly."
/// </para>
///
/// <para>
/// ANCHORED (^…$): fully anchored on the whole "When you do, search …" sentence, so
/// it cannot substring-match into a more specific sibling. The search is NOT wrapped
/// in optional — it is a mandatory instruction (contrast Solemn Simulacrum's "you
/// may search"). The optional "and you gain N life" tail adds a
/// <see cref="GainLifeEffect"/> only when present, so subtype/life variants across
/// the fetch cycle reuse this one rule.
/// </para>
/// </summary>
[TriggeredRule(Priority = 100)]
public sealed class ReflexiveSacrificeSearchBasicLandGainLifeRule : ITriggeredRule
{
  // Anchored pattern for the reflexive sentence. <subs> captures the comma-separated
  // basic land subtype list (e.g. "Plains, Island, or Swamp"); <life> is the optional
  // life-gain amount (digit or word).
  private static readonly Regex _pattern = new(
    @"^when you do, search your library for a basic "
    + @"(?<subs>[A-Z][a-zA-Z]+(?:,\s*(?:or\s+)?[A-Z][a-zA-Z]+)*) card, "
    + @"put it onto the battlefield tapped, then shuffle"
    + @"(?: and you gain (?<life>\d+|one|two|three|four|five|six|seven|eight|nine|ten) life)?$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  private static readonly IReadOnlyDictionary<string, int> _wordNumbers =
    new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
      ["one"] = 1, ["two"] = 2, ["three"] = 3, ["four"] = 4, ["five"] = 5,
      ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9, ["ten"] = 10,
    };

  public bool TryMatch(string text, out Effect? effect)
  {
    effect = null;

    var m = _pattern.Match(text.Trim().TrimEnd('.'));
    if (!m.Success)
    {
      return false;
    }

    // Parse the subtype list: split on commas, strip a leading "or ", keep the
    // oracle capitalization ("Plains", "Island", "Swamp").
    var subtypes = m.Groups["subs"].Value
      .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
      .Select(part => part.StartsWith("or ", StringComparison.OrdinalIgnoreCase)
        ? part["or ".Length..].Trim()
        : part.Trim())
      .Where(part => part.Length > 0)
      .ToList();

    if (subtypes.Count == 0)
    {
      return false;
    }

    var effects = new List<Effect>
    {
      new SearchLibraryEffect
      {
        Filter = new ObjectFilter
        {
          Supertypes = ["Basic"],
          CardTypes = ["land"],
          Subtypes = [.. subtypes],
        },
        Count = LiteralQuantity.Of(1),
        Destination = SearchDestination.BattlefieldTapped,
        Revealed = false,
      },
      new ShuffleEffect { Player = ObjectReference.You() },
    };

    var lifeGroup = m.Groups["life"];
    if (lifeGroup.Success)
    {
      var lifeStr = lifeGroup.Value.Trim();
      if (!int.TryParse(lifeStr, out var lifeAmount)
        && !_wordNumbers.TryGetValue(lifeStr, out lifeAmount))
      {
        return false;
      }

      effects.Add(new GainLifeEffect
      {
        Amount = LiteralQuantity.Of(lifeAmount),
        Player = ObjectReference.You(),
      });
    }

    // The reflexive triggered ability (CR 603.12): "When you do" == when you
    // sacrifice this land (the mandatory sacrifice from the preceding sentence).
    effect = new CreateDelayedTriggerEffect
    {
      DelayedTrigger = new DelayedTriggeredAbility
      {
        Trigger = new TriggerCondition
        {
          Timing = TriggerTiming.When,
          Event = TriggerEvent.Sacrifices,
          Filter = new ObjectFilter { CardTypes = ["land"], IsSelf = true },
        },
        Effects = effects,
      },
    };
    return true;
  }
}
