namespace MagicAST.Parsing.Parsers.Triggered.Rules;

using System.Text.RegularExpressions;
using MagicAST.AST.References;
using MagicAST.AST.Triggers;

/// <summary>
/// "Whenever you sacrifice [object]" — player-sacrifice trigger (Rule 701.21 Sacrifice; Rule 603).
///
/// <para>
/// Covers two oracle shapes, both keyed on <see cref="TriggerEvent.Sacrifices"/>:
/// <list type="bullet">
///   <item><b>"you sacrifice another [creature|permanent|…]"</b> — the aristocrat sac-payoff family
///     (Bloodbriar, Gixian Infiltrator, Pirate Peddlers, Body Dropper). "you sacrifice another
///     permanent" → Filter{ CardTypes:["permanent"], Controller:You, ExcludeSelf:true }; the
///     creature variant → CardTypes:["creature"]. The card-type + "another" exclusion are recovered
///     by <see cref="TriggeredRuleHelpers.ParseObjectFilter"/> (the same machinery the dies-family
///     uses for "another creature dies"); the controller (You) is overlaid here because the player
///     doing the sacrificing is "you" (CR 701.21a — a player can only sacrifice a permanent they
///     control), regardless of whether the oracle text spells out "you control".</item>
///   <item><b>"you sacrifice a [Subtype]"</b> — a named permanent subtype such as Food, Treasure,
///     Clue, or Blood. "you sacrifice a Food" → Filter{ Subtypes:["Food"], Controller:You }.
///     ParseObjectFilter does not recognise a bare subtype phrase ("a Food"), so this path is the
///     fallback when the general object form yields no filter.</item>
/// </list>
/// </para>
///
/// CR 701.21a: "To sacrifice a permanent, its controller moves it from the battlefield directly
/// to its owner's graveyard. A player can't sacrifice something that isn't a permanent, or
/// something that's a permanent they don't control."
/// "another" excludes the source object of the ability (plain-language, per the dies-family convention).
/// CR 603.2: "Whenever a game event or game state matches a triggered ability's trigger event,
/// that ability automatically triggers."
/// </summary>
[TriggerConditionRule(Priority = 979)]
public sealed class SacrificeConditionRule : ITriggerConditionRule
{
  // Matches "you sacrifice a [Subtype]" where Subtype is a single capitalized word.
  // The literal " a " separator means this never matches "you sacrifice another …"
  // ("another" is one token, with no standalone "a"), so the two shapes don't collide.
  private static readonly Regex _youSacrificeSubtype = new(
    @"\byou\s+sacrifice\s+a\s+(?<subtype>[A-Za-z]+)\b",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  // Captures the sacrificed-object phrase after "you sacrifice " so it can be handed to the
  // shared ParseObjectFilter helper. Drives the "another creature/permanent/…" generalization
  // (and any other filter shape ParseObjectFilter already recognises).
  private static readonly Regex _youSacrificeObject = new(
    @"\byou\s+sacrifice\s+(?<object>.+)$",
    RegexOptions.IgnoreCase | RegexOptions.Compiled
  );

  public TriggerCondition? Match(string triggerText, string lower, TriggerTiming timing)
  {
    if (!lower.Contains("sacrifice"))
    {
      return null;
    }

    if (!lower.Contains("you sacrifice"))
    {
      return null;
    }

    // General object form first: "you sacrifice another creature/permanent/…". Hand the
    // sacrificed-object phrase to the shared filter parser so the card-type and the
    // "another" self-exclusion (plain-language "another", per the dies-family convention)
    // are recovered the same way as for the
    // dies/enters families. Overlay Controller=You — the sacrificing player is "you"
    // (CR 701.21a), which the bare "another permanent" phrasing leaves implicit.
    // ParseObjectFilter returns null for a bare subtype phrase ("a Food"), so the
    // subtype path below still owns the named-subtype shape.
    var objMatch = _youSacrificeObject.Match(triggerText);
    if (objMatch.Success)
    {
      var objectFilter = TriggeredRuleHelpers.ParseObjectFilter(objMatch.Groups["object"].Value);
      if (objectFilter is not null)
      {
        return new TriggerCondition
        {
          Timing = timing,
          Event = TriggerEvent.Sacrifices,
          Filter = objectFilter with { Controller = ControllerFilter.You },
        };
      }
    }

    var m = _youSacrificeSubtype.Match(triggerText);
    if (!m.Success)
    {
      return null;
    }

    var subtype = m.Groups["subtype"].Value;
    // Title-case normalize
    subtype = char.ToUpperInvariant(subtype[0]) + subtype[1..].ToLowerInvariant();

    return new TriggerCondition
    {
      Timing = timing,
      Event = TriggerEvent.Sacrifices,
      Filter = new ObjectFilter
      {
        Subtypes = [subtype],
        Controller = ControllerFilter.You,
      },
    };
  }
}
